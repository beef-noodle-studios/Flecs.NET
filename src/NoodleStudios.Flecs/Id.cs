using System.Diagnostics;
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

    /// <summary>
    ///     The relationship (first element) of a pair id, as a raw id without
    ///     generation. Only valid when <see cref="IsPair"/> is true, otherwise it
    ///     throws in Debug and is undefined behavior in Release.
    /// </summary>
    public Id First
    {
        get
        {
            DebugIsPair();
            return new Id((uint)((Value & ECS_COMPONENT_MASK) >> 32));
        }
    }

    /// <summary>
    ///     The target (second element) of a pair id, as a raw id without
    ///     generation. Only valid when <see cref="IsPair"/> is true, otherwise it
    ///     throws in Debug and is undefined behavior in Release.
    /// </summary>
    public Id Second
    {
        get
        {
            DebugIsPair();
            return new Id((uint)Value);
        }
    }

    /// <summary>
    ///     If this id is a pair, split it into its <see cref="First"/> relationship
    ///     and <see cref="Second"/> target. Returns false for a non-pair id, with
    ///     both outputs set to <see cref="None"/> (unlike <see cref="First"/> and
    ///     <see cref="Second"/>, which throw on a non-pair).
    /// </summary>
    public bool TryGetPair(out Id first, out Id second)
    {
        if (!IsPair)
        {
            first = None;
            second = None;
            return false;
        }

        first = new Id((uint)((Value & ECS_COMPONENT_MASK) >> 32));
        second = new Id((uint)Value);
        return true;
    }

    [Conditional("DEBUG")]
    private void DebugIsPair()
    {
        if (!IsPair)
            throw new InvalidOperationException(
                "This id is not a pair, so it has no First/Second. Test with IsPair first.");
    }

    public override bool Equals(object? obj) => obj is Id other && Equals(other);
    public bool Equals(Id other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();

    /// <summary>
    ///     Get the raw 64-bit value of an id.
    /// </summary>
    public static implicit operator ulong(Id id) => id.Value;

    /// <summary>
    ///     Get an id from a raw 64-bit value.
    /// </summary>
    public static implicit operator Id(ulong value) => new(value);

    public static bool operator ==(Id left, Id right) => left.Equals(right);
    public static bool operator !=(Id left, Id right) => !left.Equals(right);
}
