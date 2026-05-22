using NoodleStudios.Flecs.Facades;
using System.Runtime.CompilerServices;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    public readonly ref struct RelationshipComponentResolver(World world, Entity entity, Id relation)
    {
        private readonly World _world = world;
        private readonly Entity _entity = entity;
        private readonly Id _relation = relation;

        public ref readonly TTarget WithTarget<TTarget>()
            where TTarget : unmanaged
        {
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(_relation, targetId);
            return ref _world.Get<TTarget>(_entity, pairId);
        }
    }

    public readonly ref struct RelationshipComponentResolver<TRelation>(World world, Entity entity)
        where TRelation : unmanaged
    {
        private readonly World _world = world;
        private readonly Entity _entity = entity;

        public ref readonly TRelation Towards(Id target)
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, target);
            return ref _world.Get<TRelation>(_entity, pairId);
        }

        public ref readonly TRelation Towards<TTarget>()
            where TTarget : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, targetId);
            return ref _world.Get<TRelation>(_entity, pairId);
        }
    }

    private struct Eats;
    private struct Pears;
    private record struct Requires(int Amount);
    private record struct Position(float X, float Y);
    private struct Biological;
    private enum Disposition
    {
        Dislikes,
        Likes,
    }

    private void Foo(World world)
    {
        var likes = world.CreateEntity();
        var apples = world.CreateEntity();
        var robot = world.CreateEntity();
        var mechanical = world.CreateEntity();

        var bob = world.CreateEntity();

        FluentApi.Wrap(world, bob)
            .AddTag<Biological>()
            .AddRelationship(likes, apples)
            .AddRelationship(IsA, robot);
    }
}
