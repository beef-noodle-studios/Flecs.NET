using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A read-only wrapper around a native Flecs world.
/// </summary>
/// <remarks>
///     Exposes only operations that do not change the world. Because registering
///     a component is itself a (structural) change, the generic accessors here
///     resolve component ids lookup-only and throw if the component has not
///     already been registered through a read-write <see cref="World"/>.
/// </remarks>
/// <param name="world">
///     A mutable wrapper around a native Flecs world.
/// </param>
public readonly unsafe partial struct ReadOnlyWorld(World world)
{
    private readonly World _world = world;

    /// <summary>
    ///     Implicitly convert from a read-write <see cref="World"/> wrapper to
    ///     a <see cref="ReadOnlyWorld"/> wrapper.
    /// </summary>
    /// <param name="world">
    ///     The read-write world wrapper to convert.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyWorld(World world) => new(world);

    /// <inheritdoc cref="World.TryGetId{T}(out Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetId<T>(out Id id) where T : unmanaged => _world.TryGetId<T>(out id);

    private Id ResolveId<T>() where T : unmanaged
    {
        if (!ComponentId<T>.TryGetId(_world.Handle, out var id))
            throw new InvalidOperationException(
                $"Component '{typeof(T).Name}' is not registered in this world.");

        return id;
    }

    /// <inheritdoc cref="World.ShouldQuit"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldQuit() => _world.ShouldQuit();

    /// <inheritdoc cref="World.EntityExists(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EntityExists(Entity entity) => _world.EntityExists(entity);

    /// <inheritdoc cref="World.EntityIsAlive(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EntityIsAlive(Entity entity) => _world.EntityIsAlive(entity);

    /// <inheritdoc cref="World.EntityIsValid(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EntityIsValid(Entity entity) => _world.EntityIsValid(entity);

    /// <inheritdoc cref="World.Has(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Entity entity, Id id) => _world.Has(entity, id);

    /// <inheritdoc cref="World.Get{T}(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T Get<T>(Entity entity, Id id) where T : unmanaged => ref _world.Get<T>(entity, id);

    /// <inheritdoc cref="World.TryGet{T}(Entity, Id, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(Entity entity, Id id, out T value) where T : unmanaged =>
        _world.TryGet(entity, id, out value);

    /// <inheritdoc cref="World.Has{T}(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>(Entity entity) where T : unmanaged => _world.Has(entity, ResolveId<T>());

    /// <inheritdoc cref="World.HasPair(Entity, Id, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasPair(Entity entity, Id relationship, Id target) =>
        _world.HasPair(entity, relationship, target);

    /// <inheritdoc cref="World.HasPair{TRelation, TTarget}(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasPair<TRelation, TTarget>(Entity entity)
        where TRelation : unmanaged
        where TTarget : unmanaged => _world.HasPair<TRelation, TTarget>(entity);

    /// <inheritdoc cref="World.Get{T}(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T Get<T>(Entity entity) where T : unmanaged => ref _world.Get<T>(entity, ResolveId<T>());

    /// <inheritdoc cref="World.TryGet{T}(Entity, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(Entity entity, out T value) where T : unmanaged =>
        _world.TryGet(entity, ResolveId<T>(), out value);

    /// <inheritdoc cref="World.GetParent(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetParent(Entity entity) => _world.GetParent(entity);

    /// <inheritdoc cref="World.GetTarget(Entity, Id, int)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetTarget(Entity entity, Id relationship, int index = 0) =>
        _world.GetTarget(entity, relationship, index);

    /// <inheritdoc cref="World.GetName(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetName(Entity entity) => _world.GetName(entity);

    /// <inheritdoc cref="World.GetSymbol(Entity)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string? GetSymbol(Entity entity) => _world.GetSymbol(entity);

    /// <inheritdoc cref="World.Lookup(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Lookup(string path) => _world.Lookup(path);
}
