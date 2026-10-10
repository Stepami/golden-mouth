using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using WireMock.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Reki.Tests;

public sealed class OpenAiStub : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly HashSet<Guid> _mappingIds = [];

    public string BaseUrl => $"{_server.Url}/v1";

    public int RequestCount => _server.LogEntries.Count(IsCurrentRequest);

    public void Reset(Func<IReadOnlyList<ChatMessage>, ChatResponse> createResponse)
    {
        _mappingIds.Clear();
        _server.ResetLogEntries();
        Respond(createResponse);
    }

    public void Respond(ChatResponse response) => Respond(_ => response);

    public void Respond(Func<IReadOnlyList<ChatMessage>, ChatResponse> createResponse)
    {
        _server.ResetMappings();
        foreach (var streaming in new[] { false, true })
        {
            var mappingId = Guid.NewGuid();
            _mappingIds.Add(mappingId);
            _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost()
                    .WithBody((string? body) => body is not null && IsStreaming(body) == streaming))
                .WithGuid(mappingId)
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader("Content-Type", streaming ? "text/event-stream" : "application/json")
                    .WithBody(request =>
                    {
                        using var document = JsonDocument.Parse(request.Body ?? throw new InvalidOperationException("Missing AI request body."));
                        var response = createResponse(ReadMessages(document.RootElement));
                        return CreateResponseBody(response, document.RootElement.GetProperty("model").GetString(), streaming);
                    }));
        }
    }

    public void RespondWithTimeout()
    {
        _server.ResetMappings();
        var mappingId = Guid.NewGuid();
        _mappingIds.Add(mappingId);
        _server.Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .WithGuid(mappingId)
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(3)).WithBody(string.Empty));
    }

    public JsonElement GetLastRequest()
    {
        var body = _server.LogEntries.Last(IsCurrentRequest)
            .RequestMessage?.Body ?? throw new InvalidOperationException("Missing AI request body.");
        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    public IReadOnlyList<ChatMessage> GetLastMessages() => ReadMessages(GetLastRequest());

    public void Dispose() => _server.Dispose();

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

        var message = new JsonObject { ["role"] = "assistant", ["tool_calls"] = calls };
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
                ["finish_reason"] = "tool_calls",
            }),
        };
        return streaming ? $"data: {completion.ToJsonString()}\n\ndata: [DONE]\n\n" : completion.ToJsonString();
    }

    // Delayed responses from a previous test can be logged after ResetLogEntries.
    private bool IsCurrentRequest(ILogEntry entry) =>
        entry.MappingGuid is { } mappingId && _mappingIds.Contains(mappingId);
}
