using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A compiled query over a <see cref="World"/>, iterated one matched table at a
///     time.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Query"/> is an inert value, like <see cref="Entity"/>. It is a
///         copyable handle that holds no ownership, so copying it does not duplicate
///         the underlying query and it is not <see cref="IDisposable"/>. 
///     </para>
///     <para>
///         Iterate with <c>foreach</c>. Each step yields a <see cref="TableView"/>
///         for one matched table, over which the caller writes the inner row loop.
///         In-place mutation of component values during iteration is safe;
///         structural changes (adding/removing components, creating/deleting
///         entities) during iteration are not.
///     </para>
/// </remarks>
public unsafe readonly struct Query
{
    private readonly ecs_world_t* _world;
    private readonly ecs_query_t* _handle;

    internal Query(ecs_world_t* world, ecs_query_t* handle)
    {
        _world = world;
        _handle = handle;
    }

    /// <summary>
    ///     The native query handle, or null for a default (uninitialized) query.
    /// </summary>
    internal ecs_query_t* Handle => _handle;

    /// <summary>
    ///     Begin iterating the tables matched by this query.
    /// </summary>
    public Enumerator GetEnumerator() => new(ecs_query_iter(_world, _handle));

    /// <summary>
    ///     Iterates the tables matched by a <see cref="Query"/>, yielding one
    ///     <see cref="TableView"/> per table.
    /// </summary>
    /// <remarks>
    ///     The iterator holds native resources that are released when iteration runs
    ///     to completion, or by <see cref="Dispose"/> on an early <c>break</c>;
    ///     <c>foreach</c> calls <see cref="Dispose"/> automatically. 
    /// </remarks>
    public ref struct Enumerator
    {
        private ecs_iter_t _it;

        private bool _finished;

        internal Enumerator(ecs_iter_t it)
        {
            _it = it;
            _finished = false;
        }

        public TableView Current => new((ecs_iter_t*)Unsafe.AsPointer(ref _it));

        public bool MoveNext()
        {
            if (_finished)
                return false;

            bool has = ecs_query_next((ecs_iter_t*)Unsafe.AsPointer(ref _it));
            if (!has)
                _finished = true;

            return has;
        }

        /// <summary>
        ///     Release the iterator's native resources if iteration has not already
        ///     finalized them. Safe to call more than once.
        /// </summary>
        public void Dispose()
        {
            if (_finished)
                return;

            ecs_iter_fini((ecs_iter_t*)Unsafe.AsPointer(ref _it));
            _finished = true;
        }
    }
}
