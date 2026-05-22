using System.Runtime.CompilerServices;

namespace NoodleStudios.Flecs.Facades;

public unsafe readonly ref struct FluentEntityApi(World world, Entity entity) 
{
    private readonly World _world = world;
    private readonly Entity _entity = entity;

    public FluentHasApi Has => new(_world, _entity);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentEntityApi AddComponent<TComponent>(TComponent component)
        where TComponent : unmanaged
    {
        var componentId = ComponentId<TComponent>.GetId(_world.Handle);
        if (_world.Has(_entity, componentId))
            return this;

        _world.Set(_entity, componentId, component);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentEntityApi AddTag(Id tag)
    {
        _world.Add(_entity, tag);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentEntityApi AddTag<TTag>()
        where TTag : unmanaged
    {
        var tagId = ComponentId<TTag>.GetId(_world.Handle);
        _world.Add(_entity, tagId);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentEntityApi AddRelationship<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(_world.Handle);
        var targetId = ComponentId<TTarget>.GetId(_world.Handle);
        var pairId = _world.Pair(relationId, targetId);
        _world.Add(_entity, pairId);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentEntityApi AddRelationship<TRelation>(Id target)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(_world.Handle);
        var pairId = _world.Pair(relationId, target);
        _world.Add(_entity, pairId);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public FluentEntityApi AddRelationship(Id relationship, Id target)
    {
        var pairId = _world.Pair(relationship, target);
        _world.Add(_entity, pairId);
        return this;
    }
}
