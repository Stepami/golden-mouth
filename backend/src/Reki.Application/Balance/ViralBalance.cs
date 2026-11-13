namespace Reki.Application.Balance;

public sealed record ViralBalance(double BaseChance, double ChancePerQuality, double Multiplier, double EmotionBonus = 0.05);
