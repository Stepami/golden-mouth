using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Reki.Tests;

public sealed class OpenAiStub : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly WireMockServer _embeddingServer = WireMockServer.Start();
    private readonly Lock _requestGate = new();
    private int _generation;
    private int _requestCount;
    private string? _lastRequest;

    public OpenAiStub()
    {
        var zeros = string.Join(',', Enumerable.Repeat("0", 311));
        _embeddingServer.Given(Request.Create().WithPath("/v1/embeddings").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(request =>
                {
                    using var document = JsonDocument.Parse(request.Body ?? throw new InvalidOperationException("Missing embedding request body."));
                    var input = document.RootElement.GetProperty("input");
                    var count = input.ValueKind == JsonValueKind.Array ? input.GetArrayLength() : 1;
                    var embeddings = Enumerable.Range(0, count).Select(index =>
                        $$$"""{"object":"embedding","index":{{{index}}},"embedding":[1,{{{zeros}}}]}""");
                    return $$$"""{"object":"list","model":"embedding","data":[{{{string.Join(',', embeddings)}}}],"usage":{"prompt_tokens":1,"total_tokens":1}}""";
                }));
    }

    public string BaseUrl => $"{_server.Url}/v1";

    public string EmbeddingBaseUrl => $"{_embeddingServer.Url}/v1";

    public int RequestCount
    {
        get
        {
            lock (_requestGate)
            {
                return _requestCount;
            }
        }
    }

    public int MappingCount => _server.MappingModels.Count();

    public void Reset()
    {
        lock (_requestGate)
        {
            _generation++;
            _requestCount = 0;
            _lastRequest = null;
        }

        _server.Reset();
    }

    public void Respond(ChatResponse response) => Respond(_ => response);

    public void Respond(Func<IReadOnlyList<ChatMessage>, ChatResponse> createResponse) =>
        Respond(messages => Task.FromResult(createResponse(messages)));

    public void Respond(Func<IReadOnlyList<ChatMessage>, Task<ChatResponse>> createResponse)
    {
        var generation = GetGeneration();
        _server.ResetMappings();
        foreach (var streaming in new[] { false, true })
        {
            _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost()
                    .WithBody((string? body) => body is not null && IsStreaming(body) == streaming))
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader("Content-Type", streaming ? "text/event-stream" : "application/json")
                    .WithBody(async request =>
                    {
                        RecordRequest(generation, request.Body);
                        using var document = JsonDocument.Parse(request.Body ?? throw new InvalidOperationException("Missing AI request body."));
                        var response = await createResponse(ReadMessages(document.RootElement));
                        return CreateResponseBody(response, document.RootElement.GetProperty("model").GetString(), streaming);
                    }));
        }
    }

    public void RespondWithTimeout()
    {
        var generation = GetGeneration();
        _server.ResetMappings();
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(3)).WithBody(request =>
            {
                RecordRequest(generation, request.Body);
                return string.Empty;
            }));
    }

    public JsonElement GetLastRequest()
    {
        string body;
        lock (_requestGate)
        {
            body = _lastRequest ?? throw new InvalidOperationException("Missing AI request body.");
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    public IReadOnlyList<ChatMessage> GetLastMessages() => ReadMessages(GetLastRequest());

    public void Dispose()
    {
        _server.Dispose();
        _embeddingServer.Dispose();
    }

    public void RespondWithHttpError()
    {
        var generation = GetGeneration();
        _server.ResetMappings();
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody(request =>
            {
                RecordRequest(generation, request.Body);
                return string.Empty;
            }));
    }

    private static bool IsStreaming(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement.TryGetProperty("stream", out var stream) && stream.GetBoolean();
    }

    private static ChatMessage[] ReadMessages(JsonElement request) => request.GetProperty("messages")
        .EnumerateArray()
        .Select(message => new ChatMessage(new ChatRole(message.GetProperty("role").GetString() ?? throw new InvalidOperationException("Missing message role.")), ReadContent(message)))
        .ToArray();

    private static string ReadContent(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content) || content.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }

        return content.ValueKind == JsonValueKind.String
            ? content.GetString() ?? string.Empty
            : string.Concat(content.EnumerateArray().Select(part => part.GetProperty("text").GetString()));
    }

    private static string CreateResponseBody(ChatResponse response, string? model, bool streaming)
    {
        var calls = new JsonArray();
        var index = 0;
        foreach (var call in response.Messages.SelectMany(message => message.Contents).OfType<FunctionCallContent>())
        {
            var tool = new JsonObject
            {
                ["id"] = call.CallId,
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = call.Name,
                    ["arguments"] = JsonSerializer.Serialize(call.Arguments, OpenAiStubJsonContext.Default.DictionaryStringObject),
                },
            };
            if (streaming)
            {
                tool["index"] = index;
                index++;
            }

            calls.Add(tool);
        }

        var message = new JsonObject { ["role"] = "assistant" };
        if (calls.Count > 0)
        {
            message["tool_calls"] = calls;
        }

        if (!string.IsNullOrEmpty(response.Text))
        {
            message["content"] = response.Text;
        }

        var completion = new JsonObject
        {
            ["id"] = "test-completion",
            ["object"] = streaming ? "chat.completion.chunk" : "chat.completion",
            ["created"] = 1_759_665_600,
            ["model"] = model,
            ["choices"] = new JsonArray(new JsonObject
            {
                ["index"] = 0,
                [streaming ? "delta" : "message"] = message,
                ["finish_reason"] = calls.Count > 0 ? "tool_calls" : "stop",
            }),
        };
        return streaming ? $"data: {completion.ToJsonString()}\n\ndata: [DONE]\n\n" : completion.ToJsonString();
    }

    private int GetGeneration()
    {
        lock (_requestGate)
        {
            return _generation;
        }
    }

    private void RecordRequest(int generation, string? body)
    {
        lock (_requestGate)
        {
            // Late responses from a timed-out previous test cannot affect this test's observations.
            if (generation == _generation)
            {
                _requestCount++;
                _lastRequest = body;
            }
        }
    }
}
