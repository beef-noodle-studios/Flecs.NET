namespace NoodleStudios.Flecs;

/// <summary>
///     Selects which slot a by-type field lookup returns when a query has more
///     than one term of the same component type or when the matched table binds
///     the component in a shape the caller wants to filter for.
/// </summary>
public enum FieldScanMode
{
    /// <summary>
    ///     Return the first slot whose id matches, regardless of binding shape.
    ///     This is TableView's default and what the type-keyed accessors use.
    /// </summary>
    FirstMatch,

    /// <summary>
    ///     Return the first self-bound slot (own component on the current
    ///     table). If none is self-bound, fall back to
    ///     <see cref="FirstMatch"/>. Useful when the caller wants the
    ///     row-local copy when present and is happy reading the inherited or
    ///     sourced value otherwise.
    /// </summary>
    PreferSelf,

    /// <summary>
    ///     Return the first self-bound slot only. If no slot of this type is
    ///     self-bound on the current table, return -1. Useful for systems that
    ///     only act on entities that own the component, ignoring inherited or
    ///     sourced bindings.
    /// </summary>
    SelfOnly,

    /// <summary>
    ///     Return the first shared slot only (inherited or sourced from another
    ///     entity). If no slot of this type is shared on the current table,
    ///     return -1. Useful for systems that only act on inherited or
    ///     sourced values, ignoring row-local copies.
    /// </summary>
    SharedOnly,
}
