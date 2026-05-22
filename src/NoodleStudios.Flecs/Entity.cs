namespace NoodleStudios.Flecs;

/// <summary>
///     A handle to an entity in a Flecs world.
/// </summary>
/// <remarks>
///     <para>
///         An entity is a 64-bit value that uniquely identifies something in a
///         world. It is an inert handle: it carries no reference to the world it
///         came from, and all operations on it are performed through the
///         <see cref="World"/> that created it. Using an entity with a world it
///         does not belong to is undefined.
///     </para>
///     <para>
///         Every entity is also a valid <see cref="Id"/> (it can be added to
///         another entity as a tag), so <see cref="Entity"/> converts implicitly
///         to <see cref="Id"/>.
///     </para>
/// </remarks>
/// <param name="value">
///     The raw 64-bit value of the entity.
/// </param>
public readonly struct Entity(ulong value) : IEquatable<Entity>
{
    /// <summary>
    ///     The raw 64-bit value of the entity.
    /// </summary>
    public readonly ulong Value = value;

    /// <summary>
    ///     A handle that refers to no entity.
    /// </summary>
    public static Entity None => default;

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);
    public bool Equals(Entity other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    ///     Get the raw 64-bit value of an entity.
    /// </summary>
    public static implicit operator ulong(Entity entity) => entity.Value;

    /// <summary>
    ///     View an entity as an <see cref="Id"/> so it can be added to another
    ///     entity as a tag or used as a relationship or target.
    /// </summary>
    public static implicit operator Id(Entity entity) => new(entity.Value);

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}
