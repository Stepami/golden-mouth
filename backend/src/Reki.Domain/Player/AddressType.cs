namespace Reki.Domain.Player;

// Standard enum serializers use these names; preserve existing lowercase JSON/BSON values without a migration.
#pragma warning disable SA1300
public enum AddressType
{
    she,
    he,
    nick,
}
#pragma warning restore SA1300
