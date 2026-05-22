namespace NoodleStudios.Flecs;

/// <summary>
///     Maps managed component types to the component ids assigned to them by a
///     single world. One instance lives per world, reachable through that
///     world's binding context (see <see cref="BindingContext"/>).
/// </summary>
/// <remarks>
///     Component ids are world-specific: the same managed type can be assigned a
///     different id in two different worlds, so the mapping cannot be a single
///     process-wide static. Registration is assumed to happen on a single thread
///     (the typical Flecs usage), so no locking is performed.
/// </remarks>
internal sealed class ComponentRegistry
{
    private readonly Dictionary<Type, Id> _ids = [];

    /// <summary>
    ///     Look up the id previously registered for <paramref name="type"/>.
    /// </summary>
    public bool TryGetId(Type type, out Id id)
    {
        if (_ids.TryGetValue(type, out Id value))
        {
            id = value;
            return true;
        }

        id = Id.None;
        return false;
    }

    /// <summary>
    ///     Record the id assigned to <paramref name="type"/>.
    /// </summary>
    public void Store(Type type, Id id) => _ids[type] = id;
}
