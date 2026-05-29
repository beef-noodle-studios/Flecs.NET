using System.Diagnostics.CodeAnalysis;

namespace NoodleStudios.Flecs;

/// <summary>
///     A typed builder for a <see cref="Query{TAspect}"/>. Wraps an untyped
///     <see cref="QueryBuilder"/> seeded from the aspect's descriptor, and
///     forwards the dynamic refiners so additional terms or per-term refinements
///     compose with the seeded ones. A builder is single-use, so create a new
///     one for each query.
/// </summary>
public unsafe ref struct QueryBuilder<TAspect> where TAspect : struct, IAspect, allows ref struct
{
    private QueryBuilder _inner;
    private readonly int[] _slotToTermIndex;
    private readonly AspectDescriptor _descriptor;

    internal QueryBuilder(QueryBuilder inner, int[] slotToTermIndex, AspectDescriptor descriptor)
    {
        _inner = inner;
        _slotToTermIndex = slotToTermIndex;
        _descriptor = descriptor;
    }

    /// <inheritdoc cref="QueryBuilder.With{T}()"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> With<T>() where T : unmanaged
    {
        _inner.With<T>();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.With(Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> With(Id id)
    {
        _inner.With(id);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.With(Id, Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> With(Id first, Id second)
    {
        _inner.With(first, second);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Without{T}()"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Without<T>() where T : unmanaged
    {
        _inner.Without<T>();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Without(Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Without(Id id)
    {
        _inner.Without(id);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Optional{T}()"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Optional<T>() where T : unmanaged
    {
        _inner.Optional<T>();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Optional(Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Optional(Id id)
    {
        _inner.Optional(id);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.In"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> In()
    {
        _inner.In();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Out"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Out()
    {
        _inner.Out();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.InOut"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> InOut()
    {
        _inner.InOut();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.None"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> None()
    {
        _inner.None();
        return ref this;
    }

    // --- Operator refiners ---

    /// <inheritdoc cref="QueryBuilder.Or"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Or()
    {
        _inner.Or();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Self"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Self()
    {
        _inner.Self();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Up()"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Up()
    {
        _inner.Up();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Up(Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Up(Id relationship)
    {
        _inner.Up(relationship);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.UpAncestorsFirst()"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> UpAncestorsFirst()
    {
        _inner.UpAncestorsFirst();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.UpAncestorsFirst(Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> UpAncestorsFirst(Id relationship)
    {
        _inner.UpAncestorsFirst(relationship);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.UpDescendantsFirst()"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> UpDescendantsFirst()
    {
        _inner.UpDescendantsFirst();
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.UpDescendantsFirst(Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> UpDescendantsFirst(Id relationship)
    {
        _inner.UpDescendantsFirst(relationship);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Source(Entity)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Source(Entity source)
    {
        _inner.Source(source);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.Source(Id)"/>
    [UnscopedRef]
    public ref QueryBuilder<TAspect> Source(Id source)
    {
        _inner.Source(source);
        return ref this;
    }

    /// <inheritdoc cref="QueryBuilder.BuildCached"/>
    public Query<TAspect> BuildCached() =>
        new(_inner.BuildCached(), _slotToTermIndex, _descriptor);

    /// <inheritdoc cref="QueryBuilder.BuildUncached"/>
    public Query<TAspect> BuildUncached() =>
        new(_inner.BuildUncached(), _slotToTermIndex, _descriptor);

    /// <inheritdoc cref="QueryBuilder.BuildDisposable"/>
    public DisposableQuery<TAspect> BuildDisposable() =>
        new(new Query<TAspect>(_inner.BuildUncached(), _slotToTermIndex, _descriptor));
}
