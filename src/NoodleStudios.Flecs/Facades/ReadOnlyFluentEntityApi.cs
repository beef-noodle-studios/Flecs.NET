using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NoodleStudios.Flecs.Facades;

public unsafe readonly ref struct ReadOnlyFluentEntityApi(ReadOnlyWorld world, Entity entity)
{
    private readonly ReadOnlyWorld _world = world;
    private readonly Entity _entity = entity;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<TComponent>()
        where TComponent : unmanaged
    {
        if (!_world.TryGetId<TComponent>(out var componentId))
            return false;

        return _world.Has(_entity, componentId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Id tag)
    {
        return _world.Has(_entity, tag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasRelationship<TRelation>(Id target)
        where TRelation : unmanaged
    {
        if (!_world.TryGetId<TRelation>(out var relationId))
            return false;
        var pairId = _world.Pair(relationId, target);
        return _world.Has(_entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasRelationship<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!_world.TryGetId<TRelation>(out var relationId))
            return false;

        if (!_world.TryGetId<TTarget>(out var targetId))
            return false;

        var pairId = _world.Pair(relationId, targetId);
        return _world.Has(_entity, pairId);
    }
}
