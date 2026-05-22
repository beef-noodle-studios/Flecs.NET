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
///         Every entity is also a valid <see cref="Flecs.Id"/> (it can be added to
///         another entity as a tag), so <see cref="Entity"/> converts implicitly
///         to <see cref="Flecs.Id"/>.
///     </para>
/// </remarks>
/// <param name="id">
///     The <see cref="Flecs.Id"/> value of the entity.
/// </param>
public readonly struct Entity(Id id) : IEquatable<Entity>
{
    /// <summary>
    ///     The <see cref="Flecs.Id"/> value of the entity.
    /// </summary>
    public readonly Id Id = id;

    /// <summary>
    ///     A handle that refers to no entity.
    /// </summary>
    public static Entity None => default;

    public override bool Equals(object? obj) => obj is Entity other && Equals(other);
    public bool Equals(Entity other) => Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();

    /// <summary>
    ///     Get the raw 64-bit value of an entity.
    /// </summary>
    public static implicit operator ulong(Entity entity) => entity.Id.Value;

    /// <summary>
    ///     View an entity as an <see cref="Flecs.Id"/> so it can be added to another
    ///     entity as a tag or used as a relationship or target.
    /// </summary>
    public static implicit operator Id(Entity entity) => new(entity.Id);

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}
