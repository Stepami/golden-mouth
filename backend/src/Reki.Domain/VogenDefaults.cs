using Vogen;

[assembly: VogenDefaults(
    fromPrimitiveCasting: CastOperator.None,
    toPrimitiveCasting: CastOperator.Explicit,
    explicitlySpecifyTypeInValueObject: true,
    primitiveEqualityGeneration: PrimitiveEqualityGeneration.Omit)]
