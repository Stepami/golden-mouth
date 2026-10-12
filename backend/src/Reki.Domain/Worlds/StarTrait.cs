namespace Reki.Domain.Worlds;

public enum StarTrait
{
    /// <summary>Дерзкость.</summary>
    Boldness,

    /// <summary>Поддержка.</summary>
    Support,

    /// <summary>Загадка.</summary>
    Mystery,

    /// <summary>Воображение.</summary>
    Imagination,

    /// <summary>Любопытство.</summary>
    Curiosity,

    /// <summary>Энергия.</summary>
    Energy,

    /// <summary>Ирония и расчёт.</summary>
    // This is one named persona trait, matching the content catalog.
#pragma warning disable AV1115
    IronyAndCalculation,
#pragma warning restore AV1115

    /// <summary>Сухой юмор.</summary>
    DryHumor,

    /// <summary>Самоирония.</summary>
    SelfIrony,

    /// <summary>Азарт.</summary>
    Excitement,

    /// <summary>Внимание.</summary>
    Attention,
}