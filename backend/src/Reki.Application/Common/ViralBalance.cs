namespace Reki.Application.Common;

public sealed record ViralBalance(
    double BaseChance,
    double ChancePerQuality,
    int GuaranteedPost,
    int PityPosts,
    double Multiplier);
