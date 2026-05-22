using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A 64-bit value naming something that can be added to an entity: a
///     component, a tag, or a pair.
/// </summary>
/// <remarks>
///     <para>
///         Every <see cref="Entity"/> is a valid id, but not every id is a valid
///         entity. In particular a pair (see <see cref="World.Pair(Id, Id)"/>) is
///         an id with relationship and target encoded into it; it cannot be used
///         where a standalone entity is expected (for example with
///         <see cref="World.EntityIsAlive(Entity)"/>).
///     </para>
/// </remarks>
/// <param name="value">
///     The raw 64-bit value of the id.
/// </param>
public readonly struct Id(ulong value) : IEquatable<Id>
{
    /// <summary>
    ///     The raw 64-bit value of the id.
    /// </summary>
    public readonly ulong Value = value;

    /// <summary>
    ///     An id that names nothing.
    /// </summary>
    public static Id None => default;

    /// <summary>
    ///     Test whether this id is a pair.
    /// </summary>
    public bool IsPair => ecs_id_is_pair(Value);

    public override bool Equals(object? obj) => obj is Id other && Equals(other);
    public bool Equals(Id other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    ///     Get the raw 64-bit value of an id.
    /// </summary>
    public static implicit operator ulong(Id id) => id.Value;

    public static bool operator ==(Id left, Id right) => left.Equals(right);
    public static bool operator !=(Id left, Id right) => !left.Equals(right);
}
