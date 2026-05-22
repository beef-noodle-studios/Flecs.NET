using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoodleStudios.Flecs.Facades;

public unsafe static class FluentRelationshipValueResolver
{
    public readonly ref struct RelationshipResolver(World world, Entity entity, Id relation)
    {
        private readonly World _world = world;
        private readonly Entity _entity = entity;
        private readonly Id _relation = relation;

        public ref readonly TTarget WithTarget<TTarget>()
            where TTarget : unmanaged
        {
            var targetId = ComponentId<TTarget>.GetId(_world.Handle);
            var pairId = _world.Pair(_relation, targetId);
            return ref _world.Get<TTarget>(_entity, pairId);
        }
    }
}
