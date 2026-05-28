using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A compiled query whose per-row iteration binds the byref fields of an
///     aspect <typeparamref name="TAspect"/> to the matched row's component
///     values. Use the untyped <see cref="Flecs.Query"/> accessible through
///     <see cref="Untyped"/> for lifetime management.
/// </summary>
public unsafe readonly struct Query<TAspect> where TAspect : IAspect, allows ref struct
{
    private readonly Query _inner;
    private readonly int[] _slotToTermIndex;

    internal Query(Query inner, int[] slotToTermIndex)
    {
        _inner = inner;
        _slotToTermIndex = slotToTermIndex;
    }

    /// <summary>
    ///     The underlying untyped query. Pass it to
    ///     <see cref="World.DestroyQuery(Query)"/> to free a cached or uncached
    ///     typed query, or to APIs that take a plain <see cref="Flecs.Query"/>.
    /// </summary>
    public Query Untyped => _inner;

    /// <summary>
    ///     For each accessor slot (indexed by its position among accessor slots in
    ///     the aspect's descriptor), the index of the term that was seeded for it
    ///     at build time.
    /// </summary>
    internal int[] SlotToTermIndex => _slotToTermIndex;
}

/// <summary>
///     An owning, self-freeing wrapper around an uncached
///     <see cref="Query{TAspect}"/>. Free it with <c>using</c>. Mirrors
///     <see cref="DisposableQuery"/> for typed queries.
/// </summary>
public unsafe ref struct DisposableQuery<TAspect> where TAspect : IAspect, allows ref struct
{
    private readonly Query<TAspect> _query;

    private bool _disposed;

    internal DisposableQuery(Query<TAspect> query) => _query = query;

    /// <summary>
    ///     The wrapped typed query. Iterate the <see cref="DisposableQuery{TAspect}"/>
    ///     directly, or use this to pass the typed query to APIs that take a
    ///     <see cref="Query{TAspect}"/>. Do not free it separately, the wrapper
    ///     owns it.
    /// </summary>
    public Query<TAspect> Query => _query;

    /// <summary>
    ///     Free the underlying query.
    /// </summary>
    public void Dispose()
    {
        if (_disposed || _query.Untyped.Handle == null)
            return;

        _disposed = true;
        ecs_query_fini(_query.Untyped.Handle);
    }
}
