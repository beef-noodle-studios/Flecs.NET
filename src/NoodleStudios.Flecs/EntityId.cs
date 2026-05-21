namespace NoodleStudios.Flecs;

/// <summary>
///     64-bit value type that represents an entity Id in a Flecs world.
/// </summary>
/// <param name="value">
///     The raw 64-bit value of the entity Id.
/// </param>
public readonly struct EntityId(ulong value) : IEquatable<EntityId>
{
    /// <summary>
    ///     The raw 64-bit value of the entity Id.
    /// </summary>
    public readonly ulong RawValue = value;

    /// <summary>
    ///     Value representing an invalid entity Id.
    /// </summary>
    public static EntityId Invalid => new(0);

    public override bool Equals(object? obj)
    {
        if (obj is not EntityId other)
            return false;

        return Equals(other);
    }

    public bool Equals(EntityId other) => RawValue == other.RawValue;
    public override int GetHashCode() => RawValue.GetHashCode();

    public static implicit operator ulong(EntityId id) => id.RawValue;
    public static implicit operator EntityId(ulong value) => new(value);

    public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
    public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
}
