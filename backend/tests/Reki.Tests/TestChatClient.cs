using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Reki.Tests;

public sealed class TestChatClient : IChatClient
{
    private int _calls;
    private int _returnUnknownArgument;
    private Exception? _nextException;

    public ChatOptions? LastOptions { get; private set; }

    public int Calls => Volatile.Read(ref _calls);

    public void Dispose()
    {
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _calls);
        LastOptions = options?.Clone();
        Exception? exception = Interlocked.Exchange(ref _nextException, null);
        if (exception is not null)
        {
            return Task.FromException<ChatResponse>(exception);
        }

        bool directMessage = messages.Any(message => message.Text.Contains(
            "tool dm",
            StringComparison.Ordinal));
        FunctionCallContent call = directMessage
            ? CreateDirectMessageCall()
            : CreateCommentCall(Interlocked.Exchange(ref _returnUnknownArgument, 0) == 1);
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, [call]));
        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        yield break;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public void Reset()
    {
        LastOptions = null;
        _nextException = null;
        Volatile.Write(ref _returnUnknownArgument, 0);
        Volatile.Write(ref _calls, 0);
    }

    public void ReturnUnknownArgumentOnce()
    {
        Volatile.Write(ref _returnUnknownArgument, 1);
    }

    public void ThrowOnce(Exception exception)
    {
        Interlocked.Exchange(ref _nextException, exception);
    }

    private static FunctionCallContent CreateCommentCall(bool includeUnknownArgument)
    {
        using JsonDocument reasons = JsonDocument.Parse(
            """[{"icon":"🔥","label":"в голосе канала"}]""");
        var arguments = new Dictionary<string, object?>
        {
            ["author"] = "Соня",
            ["star_id"] = "sonya",
            ["text"] = "Есть голос. Продолжай в том же духе.",
            ["hook"] = "Соня вернётся к этой теме через пару постов",
            ["brave"] = 0.7,
            ["humor"] = 0.6,
            ["topic"] = 0.8,
            ["why"] = reasons.RootElement.Clone(),
            ["on_topic"] = true,
        };
        if (includeUnknownArgument)
        {
            arguments["followers"] = 1_000_000;
        }

        return new FunctionCallContent("test-call", "comment", arguments);
    }

    private static FunctionCallContent CreateDirectMessageCall()
    {
        var arguments = new Dictionary<string, object?>
        {
            ["star_id"] = "sonya",
            ["text"] = "Вижу, о чём ты. Вернёмся к этому после следующего поста.",
            ["memory_fact"] = "любит короткие посты",
        };
        return new FunctionCallContent("test-dm-call", "dm", arguments);
    }
}