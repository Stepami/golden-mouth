using Mediator;

namespace Reki.Application.Example.GetExample;

public sealed class GetExampleHandler(TimeProvider timeProvider)
    : IRequestHandler<GetExampleQuery, GetExampleResult>
{
    public ValueTask<GetExampleResult> Handle(
        GetExampleQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset generatedAtUtc = timeProvider.GetUtcNow();
        var result = new GetExampleResult(
            "Reki backend is running.",
            generatedAtUtc);

        return ValueTask.FromResult(result);
    }
}