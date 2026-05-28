using System.Runtime.CompilerServices;

namespace NoodleStudios.Flecs;

/// <summary>
///     Predicates over an aspect field reference. Only facts answerable from the
///     reference alone live here, every other per-field question (storage shape,
///     matched id, pair target) goes through <see cref="TableView"/>.
/// </summary>
public static class Field
{
    /// <summary>
    ///     Test whether an optional aspect field is bound on the current row. An
    ///     optional field whose component is not present on the table reads as a
    ///     null reference, and dereferencing it is undefined behavior, so guard
    ///     every read with this predicate first.
    /// </summary>
    public static bool HasValue<T>(in T field) where T : allows ref struct
        => !Unsafe.IsNullRef(in field);
}
