using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using Reki.Application.Posts.AI;

namespace Reki.Infrastructure.ML;

internal sealed class LocalNliScorer : INliScorer, IHealthCheck, IDisposable
{
    private readonly bool _enabled;
    private readonly BertTokenizer? _tokenizer;
    private readonly InferenceSession? _session;

    public LocalNliScorer(IOptions<NliOptions> options)
    {
        _enabled = options.Value.Enabled;
        if (!_enabled)
        {
            return;
        }

        _tokenizer = BertTokenizer.Create(
            options.Value.VocabPath,
            new BertOptions { LowerCaseBeforeTokenization = false, ApplyBasicTokenization = true });
        using var sessionOptions = new SessionOptions { IntraOpNumThreads = 1, InterOpNumThreads = 1 };
        _session = new InferenceSession(options.Value.ModelPath, sessionOptions);
    }

    public ValueTask<IReadOnlyList<NliScores>> ScoreAsync(
        string premise,
        IReadOnlyList<string> hypotheses,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tokenizer = _tokenizer ?? throw new InvalidOperationException("The NLI model is disabled.");
        var session = _session ?? throw new InvalidOperationException("The NLI model is disabled.");
        var rows = hypotheses.Select(hypothesis => EncodePair(tokenizer, premise, hypothesis))
            .ToArray();
        var width = rows.Max(row => row.Ids.Length);
        var inputIds = new long[rows.Length * width];
        var tokenTypes = new long[rows.Length * width];
        var attentionMask = new long[rows.Length * width];
        for (var index = 0; index < rows.Length; index++)
        {
            var offset = index * width;
            Array.Copy(rows[index].Ids, 0, inputIds, offset, rows[index].Ids.Length);
            Array.Copy(rows[index].Types, 0, tokenTypes, offset, rows[index].Types.Length);
            Array.Fill(attentionMask, 1L, offset, rows[index].Ids.Length);
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(inputIds, [rows.Length, width])),
            NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(attentionMask, [rows.Length, width])),
            NamedOnnxValue.CreateFromTensor("token_type_ids", new DenseTensor<long>(tokenTypes, [rows.Length, width])),
        };
        using var output = session.Run(inputs);
        var logits = output.Single().AsTensor<float>().ToArray();
        var scores = new NliScores[hypotheses.Count];
        for (var index = 0; index < rows.Length; index++)
        {
            var notEntailmentLogit = logits[index * 2];
            var entailmentLogit = logits[(index * 2) + 1];
            var maximum = Math.Max(notEntailmentLogit, entailmentLogit);
            var notEntailment = Math.Exp(notEntailmentLogit - maximum);
            var entailment = Math.Exp(entailmentLogit - maximum);
            var total = notEntailment + entailment;
            scores[index] = new NliScores(entailment / total, notEntailment / total);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<NliScores>>(scores);

        static (long[] Ids, long[] Types) EncodePair(BertTokenizer tokenizer, string premise, string hypothesis)
        {
            var second = tokenizer.EncodeToIds(hypothesis, false, true, true).Take(254).ToArray();
            var first = tokenizer.EncodeToIds(premise, false, true, true).Take(512 - second.Length - 3).ToArray();
            var ids = tokenizer.BuildInputsWithSpecialTokens(first, second).Select(value => (long)value).ToArray();
            var types = tokenizer.CreateTokenTypeIdsFromSequences(first, second).Select(value => (long)value).ToArray();
            return (ids, types);
        }
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(!_enabled || _session is not null
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The NLI model is unavailable."));

    public void Dispose() => _session?.Dispose();
}
