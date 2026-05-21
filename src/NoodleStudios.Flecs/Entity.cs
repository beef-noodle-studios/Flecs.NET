namespace NoodleStudios.Flecs;

/// <summary>
///     Value type that represents an entity in a Flecs world.
///     This is a thin wrapper around an <see cref="EntityId"/> with support
///     for implicit conversion.
/// </summary>
/// <param name="id">
///     The <see cref="EntityId"/> that uniquely identifies this entity.
/// </param>
public readonly struct Entity(EntityId id) : IEquatable<Entity>
{
    /// <summary>
    ///     The <see cref="EntityId"/> that uniquely identifies this entity.
    /// </summary>
    public readonly EntityId Id = id;

    /// <summary>
    ///     Value representing an invalid entity.
    /// </summary>
    public static Entity None => new(EntityId.Invalid);

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other)
            return false;

        return Equals(other);
    }

    public bool Equals(Entity other) => Id.Equals(other.Id);
    public override int GetHashCode() => Id.GetHashCode();

    public static implicit operator EntityId(Entity entity) => entity.Id;

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}
