namespace NoodleStudios.Flecs.Facades;

public unsafe static class FluentRelationshipIdResolver
{
    public readonly ref struct RelationshipResolver(ReadOnlyWorld world, Id relation)
    {
        private readonly ReadOnlyWorld _world = world;
        private readonly Id _relation = relation;

        public Id WithTarget<TTarget>()
            where TTarget : unmanaged
        {
            if (!_world.TryGetId<TTarget>(out var targetId))
                return Id.None;

            var pairId = _world.Pair(_relation, targetId);
            return pairId;
        }
    }

    public readonly ref struct RelationshipResolver<TRelation>(ReadOnlyWorld world)
        where TRelation : unmanaged
    {
        private readonly ReadOnlyWorld _world = world;

        public Id Towards(Id target)
        {
            if (!_world.TryGetId<TRelation>(out var relationId))
                return Id.None;

            var pairId = _world.Pair(relationId, target);
            return pairId;
        }

        public Id Towards<TTarget>()
            where TTarget : unmanaged
        {
            if (!_world.TryGetId<TRelation>(out var relationId))
                return Id.None;

            if (!_world.TryGetId<TTarget>(out var targetId))
                return Id.None;

            var pairId = _world.Pair(relationId, targetId);
            return pairId;
        }
    }
}
