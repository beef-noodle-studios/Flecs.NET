using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     An owning, self-freeing wrapper around an uncached <see cref="Flecs.Query"/>.
///     Free it with using.
/// </summary>
/// <remarks>
///     <para>
///         Use this when a query is scoped to a block and should be torn down when
///         the block exits, rather than persisted in the world. For a query that
///         outlives its creating scope, use <see cref="QueryBuilder.BuildCached"/> or
///         <see cref="QueryBuilder.BuildUncached"/> and free it with
///         <see cref="World.DestroyQuery(Query)"/>.
///     </para>
/// </remarks>
public unsafe ref struct DisposableQuery
{
    private readonly Query _query;

    private bool _disposed;

    internal DisposableQuery(Query query) => _query = query;

    /// <summary>
    ///     The wrapped query. Iterate the <see cref="DisposableQuery"/> directly, or
    ///     use this to pass the query to APIs that take a <see cref="Flecs.Query"/>.
    ///     Do not free it separately, the wrapper owns it.
    /// </summary>
    public Query Query => _query;

    /// <inheritdoc cref="Query.GetEnumerator"/>
    public Query.Enumerator GetEnumerator() => _query.GetEnumerator();

    /// <summary>
    ///     Free the underlying query. 
    /// </summary>
    public void Dispose()
    {
        if (_disposed || _query.Handle == null)
            return;

        _disposed = true;
        ecs_query_fini(_query.Handle);
    }
}
