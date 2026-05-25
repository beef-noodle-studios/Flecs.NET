using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    // A query is built fluently with QueryBuilder and matches a set of tables.
    //
    // The builder's terminal verb chooses the lifetime of the query: 
    //
    // - BuildCached/BuildUncached return a Query that persists until DestroyQuery is called
    // - BuildDisposable returns a DisposableQuery that is self-freed when paired with a using statement 

    /// <summary>
    ///     Begin building a query against this world.
    /// </summary>
    public QueryBuilder QueryBuilder() => new(_handle);

    /// <summary>
    ///     Free a query created by <see cref="Flecs.QueryBuilder.BuildCached"/> or
    ///     <see cref="Flecs.QueryBuilder.BuildUncached"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Freeing a query more than once, or freeing one whose world is already disposed,
    ///         is undefined. A query built with <see cref="Flecs.QueryBuilder.BuildDisposable"/> 
    ///         is owned by its <see cref="DisposableQuery"/> and should not be freed here.
    ///     </para>
    ///     <para>
    ///         Any still-live query is also freed when the world is disposed, so this
    ///         is only needed to free a query earlier than the world.
    ///     </para>
    /// </remarks>
    public void DestroyQuery(Query query)
    {
        if (query.Handle != null)
            ecs_query_fini(query.Handle);
    }
}
