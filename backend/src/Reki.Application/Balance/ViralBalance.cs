namespace Reki.Application.Balance;

public sealed record ViralBalance(double BaseChance, double ChancePerQuality, int Multiplier, double EmotionBonus = 0.05);
