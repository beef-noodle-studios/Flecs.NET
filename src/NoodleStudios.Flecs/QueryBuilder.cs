using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A fluent builder for a <see cref="Query"/>. 
/// </summary>
/// <remarks>
///     <para>
///         Add a term with <see cref="With{T}()"/>, <see cref="Without{T}()"/>, or
///         <see cref="Optional{T}()"/>, then optionally refine the most recently
///         added term's access with <see cref="In"/>, <see cref="Out"/>,
///         <see cref="InOut"/>, or <see cref="None"/>. Finish with a terminal verb
///         that chooses the query's lifetime: <see cref="BuildCached"/>,
///         <see cref="BuildUncached"/>, or <see cref="BuildDisposable"/>.
///     </para>
/// </remarks>
public unsafe ref struct QueryBuilder
{
    private readonly ecs_world_t* _world;
    private ecs_query_desc_t _desc;
    private int _termCount;

    internal QueryBuilder(ecs_world_t* world)
    {
        _world = world;
        _desc = default;
        _termCount = 0;
    }

    // --- Term adders ---

    /// <summary>
    ///     Match entities that have component <typeparamref name="T"/>, and read it
    ///     during iteration. 
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder With<T>() where T : unmanaged
    {
        AddTerm(ComponentId<T>.GetId(_world), (short)EcsAnd);
        return ref this;
    }

    /// <summary>
    ///     Match entities that have the id <paramref name="id"/> 
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder With(Id id)
    {
        AddTerm(id, (short)EcsAnd);
        return ref this;
    }

    /// <summary>
    ///     Match entities that have the pair <c>(first, target)</c>. Either element
    ///     may be a wildcard.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder With(Id first, Id second)
    {
        AddTerm(ecs_make_pair(first, second), (short)EcsAnd);
        return ref this;
    }

    /// <summary>
    ///     Exclude entities that have component <typeparamref name="T"/>. 
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Without<T>() where T : unmanaged
    {
        AddTerm(ComponentId<T>.GetId(_world), (short)EcsNot);
        return ref this;
    }

    /// <summary>
    ///     Exclude entities that have the id <paramref name="id"/>.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Without(Id id)
    {
        AddTerm(id, (short)EcsNot);
        return ref this;
    }

    /// <summary>
    ///     Match component <typeparamref name="T"/> optionally. Entities are matched
    ///     whether or not they have it, and the field carries data only on the
    ///     tables that do. 
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Optional<T>() where T : unmanaged
    {
        AddTerm(ComponentId<T>.GetId(_world), (short)EcsOptional);
        return ref this;
    }

    /// <summary>
    ///     Match the id <paramref name="id"/> optionally.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Optional(Id id)
    {
        AddTerm(id, (short)EcsOptional);
        return ref this;
    }

    // --- Access refiners ---

    /// <summary>
    ///     Mark the most recently added term as read-only.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder In()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].inout = (short)EcsIn;
        return ref this;
    }

    /// <summary>
    ///     Mark the most recently added term as write-only.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Out()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].inout = (short)EcsOut;
        return ref this;
    }

    /// <summary>
    ///     Mark the most recently added term as read-write.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder InOut()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].inout = (short)EcsInOut;
        return ref this;
    }

    /// <summary>
    ///     Mark the most recently added term as matched but carrying no data, so it
    ///     constrains matching without producing a readable field.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder None()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].inout = (short)EcsInOutNone;
        return ref this;
    }

    // --- Terminal builders ---

    /// <summary>
    ///     Build an entity-backed query that persists in the world until freed with
    ///     <see cref="World.DestroyQuery(Query)"/>. flecs caches the matched tables
    ///     when the terms allow it, so re-iteration is cheap. A query with no
    ///     cacheable terms stays entity-backed but uncached.
    /// </summary>
    /// <exception cref="InvalidOperationException">The query is invalid.</exception>
    public Query BuildCached() => Build(EcsQueryCacheAuto);

    /// <summary>
    ///     Build an uncached query: it matches tables on each iteration with no
    ///     maintained cache. Persists in the world until freed with
    ///     <see cref="World.DestroyQuery(Query)"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The query is invalid.</exception>
    public Query BuildUncached() => Build(EcsQueryCacheNone);

    /// <summary>
    ///     Build an uncached query wrapped in a <see cref="DisposableQuery"/> that
    ///     frees it on disposal. Free it with <c>using</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The query is invalid.</exception>
    public DisposableQuery BuildDisposable() => new(Build(EcsQueryCacheNone));

    // --- Internals ---

    private void AddTerm(ulong id, short oper)
    {
        GuardTermCapacity();
        GuardTermId(id);
        ref ecs_term_t term = ref _desc.terms[_termCount];
        term.id = id;
        term.oper = oper;
        _termCount++;
    }

    private Query Build(ecs_query_cache_kind_t kind)
    {
        Debug.Assert(_desc._canary == 0);

        fixed (ecs_query_desc_t* d = &_desc)
        {
            d->cache_kind = kind;

            ulong entity = d->entity = kind == EcsQueryCacheAuto ? ecs_new(_world) : 0;

            ecs_query_t* handle = ecs_query_init(_world, d);

            d->entity = 0;

            if (handle == null)
            {
                // flecs zeroes the public entity on failure, so the caller owns the
                // pre-created entity and must delete it to avoid leaking it.
                if (entity != 0)
                    ecs_delete(_world, entity);

                throw new InvalidOperationException("Failed to build query.");
            }

            return new Query(_world, handle);
        }
    }

    [Conditional("DEBUG")]
    private readonly void GuardTermCapacity()
    {
        if (_termCount >= FLECS_TERM_COUNT_MAX)
            throw new InvalidOperationException(
                $"A query cannot have more than {FLECS_TERM_COUNT_MAX} terms.");
    }

    // A zero id (Id.None, or a failed Lookup) reads to flecs as an uninitialized
    // term, which silently drops it and every term after it. Reject it up front.
    [Conditional("DEBUG")]
    private readonly void GuardTermId(ulong id)
    {
        if (id == 0)
            throw new InvalidOperationException(
                "A query term has a zero id. Check for a failed Lookup or an Id.None.");
    }

    private readonly void RequireTerm()
    {
        if (_termCount == 0)
            throw new InvalidOperationException(
                "Add a term with With/Without/Optional before refining it with In/Out/InOut/None.");
    }
}
