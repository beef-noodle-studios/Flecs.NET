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
///         <see cref="InOut"/>, or <see cref="None"/>, combine it with the next
///         term using <see cref="Or"/>, or source it from an ancestor or a fixed
///         entity with <see cref="Self()"/>, <see cref="Up()"/>, <see cref="UpAncestorsFirst()"/>,
///         <see cref="UpDescendantsFirst()"/>, or <see cref="Source(Entity)"/>. Finish with a terminal verb
///         that chooses the query's lifetime: <see cref="BuildCached"/>,
///         <see cref="BuildUncached"/>, or <see cref="BuildDisposable"/>.
///
///         A builder is single-use, so create a new one for each query.
///     </para>
/// </remarks>
public unsafe ref struct QueryBuilder
{
    private readonly ecs_world_t* _world;
    private ecs_query_desc_t _desc;
    private int _termCount;
    private bool _built;

    internal QueryBuilder(ecs_world_t* world)
    {
        _world = world;
        _desc = default;
        _termCount = 0;
        _built = false;
    }

    /// <summary>
    ///     The number of terms added so far. Lets a wrapping builder capture each
    ///     seeded term's index immediately after the verb that adds it.
    /// </summary>
    internal readonly int TermCount => _termCount;

    /// <summary>
    ///     The world this builder targets. Exposed so a wrapping builder can read
    ///     the world without holding its own copy.
    /// </summary>
    internal readonly ecs_world_t* World => _world;

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

    // --- Operator refiners ---

    /// <summary>
    ///     Combine the most recently added term with the next one as an OR group so an
    ///     entity matches if it has either. Chain further <see cref="Or"/> calls to extend
    ///     the group. OR members of differing types collapse to a single dataless field.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Or()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].oper = (short)EcsOr;
        return ref this;
    }

    // --- Traversal refiners ---

    /// <summary>
    ///     Source the most recently added term from the matched entity itself. A term is
    ///     already self-sourced by default. Combine with <see cref="Up()"/> as
    ///     <c>.Self().Up()</c> to match either the entity or an ancestor.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Self()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].src.id |= EcsSelf;
        return ref this;
    }

    /// <summary>
    ///     Source the most recently added term from an ancestor reached by following the
    ///     <c>ChildOf</c> relationship upward, not from the matched entity itself. Use
    ///     <c>.Self().Up()</c> to match either the entity or an ancestor.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Up()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].src.id |= EcsUp;
        return ref this;
    }

    /// <summary>
    ///     As <see cref="Up()"/>, but traverse <paramref name="relationship"/> upward
    ///     instead of <c>ChildOf</c>. The relationship must be traversable for the term's
    ///     component.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder Up(Id relationship)
    {
        RequireTerm();
        GuardTrav(relationship);
        ref ecs_term_t term = ref _desc.terms[_termCount - 1];
        term.src.id |= EcsUp;
        term.trav = relationship;
        return ref this;
    }

    /// <summary>
    ///     Source the most recently added term by traversing <c>ChildOf</c> upward, like
    ///     <see cref="Up()"/>, and additionally order the matched tables by hierarchy
    ///     depth so an entity's ancestors are always iterated before the entity itself
    ///     (flecs calls this breadth-first iteration). The textbook use is a transform
    ///     system, where each parent must be processed before its children.
    /// </summary>
    /// <remarks>
    ///     This ordering is only available on a cached query (<see cref="BuildCached"/>).
    ///     Building it uncached fails.
    /// </remarks>
    [UnscopedRef]
    public ref QueryBuilder UpAncestorsFirst()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].src.id |= EcsUp | EcsCascade;
        return ref this;
    }

    /// <summary>
    ///     As <see cref="UpAncestorsFirst()"/>, but traverse <paramref name="relationship"/>
    ///     upward instead of <c>ChildOf</c>. The relationship must be traversable.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder UpAncestorsFirst(Id relationship)
    {
        RequireTerm();
        GuardTrav(relationship);
        ref ecs_term_t term = ref _desc.terms[_termCount - 1];
        term.src.id |= EcsUp | EcsCascade;
        term.trav = relationship;
        return ref this;
    }

    /// <summary>
    ///     The reverse of <see cref="UpAncestorsFirst()"/>: source by traversing <c>ChildOf</c>
    ///     upward and order the matched tables so an entity's descendants are iterated
    ///     before the entity itself.
    /// </summary>
    /// <remarks>
    ///     This ordering is only available on a cached query (<see cref="BuildCached"/>).
    ///     Building it uncached fails.
    /// </remarks>
    [UnscopedRef]
    public ref QueryBuilder UpDescendantsFirst()
    {
        RequireTerm();
        _desc.terms[_termCount - 1].src.id |= EcsUp | EcsCascade | EcsDesc;
        return ref this;
    }

    /// <summary>
    ///     As <see cref="UpDescendantsFirst()"/>, but traverse <paramref name="relationship"/>
    ///     upward instead of <c>ChildOf</c>. The relationship must be traversable.
    /// </summary>
    [UnscopedRef]
    public ref QueryBuilder UpDescendantsFirst(Id relationship)
    {
        RequireTerm();
        GuardTrav(relationship);
        ref ecs_term_t term = ref _desc.terms[_termCount - 1];
        term.src.id |= EcsUp | EcsCascade | EcsDesc;
        term.trav = relationship;
        return ref this;
    }

    /// <summary>
    ///     Source the most recently added term from the fixed entity
    ///     <paramref name="source"/> for every matched row, rather than from the matched
    ///     entity. The field reads as shared (one value for the whole table) and is
    ///     read-only by default. Mark the term <see cref="InOut()"/> or <see cref="Out()"/>
    ///     to write through it.
    /// </summary>
    /// <remarks>
    ///     A fixed source overwrites the term's source id, clearing any flags a prior
    ///     traversal refiner (e.g. <see cref="Self()"/> or <see cref="Up()"/>) set, so call
    ///     <c>Source</c> before any traversal verb on the same term: a verb called after it
    ///     (e.g. <c>.Source(e).Up()</c>) composes, traversing up from the fixed entity,
    ///     while one called before it (<c>.Up().Source(e)</c>) is silently overwritten.
    /// </remarks>
    [UnscopedRef]
    public ref QueryBuilder Source(Entity source)
    {
        RequireTerm();
        GuardSrc(source);
        _desc.terms[_termCount - 1].src.id = (ulong)source | EcsIsEntity;
        return ref this;
    }

    /// <summary>
    ///     As <see cref="Source(Entity)"/>, but source from the id <paramref name="source"/>.
    /// </summary>
    /// <remarks>
    ///     <paramref name="source"/> must be a plain entity id, not a pair. Passing a
    ///     pair throws in Debug and is undefined behavior in Release.
    /// </remarks>
    [UnscopedRef]
    public ref QueryBuilder Source(Id source)
    {
        RequireTerm();
        GuardSrc(source);
        _desc.terms[_termCount - 1].src.id = (ulong)source | EcsIsEntity;
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
        GuardNotBuilt();
        GuardTermCapacity();
        GuardTermId(id);
        ref ecs_term_t term = ref _desc.terms[_termCount];
        term.id = id;
        term.oper = oper;
        _termCount++;
    }

    private Query Build(ecs_query_cache_kind_t kind)
    {
        GuardNotBuilt();
        _built = true;

        Debug.Assert(_desc._canary == 0);

        fixed (ecs_query_desc_t* d = &_desc)
        {
            d->cache_kind = kind;

            ulong entity = d->entity = kind == EcsQueryCacheAuto ? ecs_new(_world) : 0;

            ecs_query_t* handle = ecs_query_init(_world, d);

            if (handle == null)
            {
                // flecs zeroes the public entity on failure, so the caller owns the
                // pre-created entity and must delete it to avoid leaking it.
                if (entity != 0)
                    ecs_delete(_world, entity);

                throw new InvalidOperationException(GetBuildFailureMessage());
            }

            return new Query(_world, handle);
        }
    }

    // Embed flecs's last error code in the message when one is set. 
    private static string GetBuildFailureMessage()
    {
        // Returns 0 and an empty message for failures that flecs routes through ecs_err
        // without setting log_last_error_ (e.g. cascade on an uncached query)
        // so the message degrades to a bare "Failed to build query." in those
        // cases. ecs_strerror maps a non-zero code to a short symbolic name like
        // INVALID_PARAMETER.
        int errCode = ecs_log_last_error();
        if (errCode == 0)
            return "Failed to build query.";

        string? codeName = Utf8.Decode(ecs_strerror(errCode));
        return string.IsNullOrEmpty(codeName)
            ? $"Failed to build query (flecs error code {errCode})."
            : $"Failed to build query: {codeName} (flecs error code {errCode}).";
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

    [Conditional("DEBUG")]
    private readonly void GuardNotBuilt()
    {
        if (_built)
            throw new InvalidOperationException(
                "This builder has already built a query. Create a new builder per query.");
    }

    [Conditional("DEBUG")]
    private readonly void GuardSrc(Id source)
    {
        if (source.Value == 0)
            throw new InvalidOperationException(
                "A query term has a zero source. Check for a failed Lookup or an Entity.None/Id.None.");
        if (source.IsPair)
            throw new InvalidOperationException(
                "A query term source must be a plain entity, not a pair.");
    }

    [Conditional("DEBUG")]
    private readonly void GuardTrav(Id relationship)
    {
        if (relationship.Value == 0)
            throw new InvalidOperationException(
                "A traversal relationship is zero. Check for a failed Lookup or an Id.None. " +
                "Use a parameterless Up()/UpAncestorsFirst()/UpDescendantsFirst() to default to ChildOf.");
        if (!ecs_has_id(_world, relationship, EcsTraversable))
            throw new InvalidOperationException(
                "A traversal relationship must have the Traversable trait. Mark it [Traversable] " +
                "(ChildOf, IsA, and DependsOn already are).");
    }

    private readonly void RequireTerm()
    {
        GuardNotBuilt();
        if (_termCount == 0)
            throw new InvalidOperationException(
                "Add a term with With/Without/Optional before refining it with In/Out/InOut/None/Or/Self/Up/UpAncestorsFirst/UpDescendantsFirst/Source.");
    }
}
