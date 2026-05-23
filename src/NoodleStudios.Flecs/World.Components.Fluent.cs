using NoodleStudios.Flecs.Core;
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
    private struct LivesAt;
    private readonly Entity Bob;
    private readonly Id Likes;
    private readonly Id Apples;
    private readonly Id SpawnsAt;
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

    private void AddTest(World world)
    {
        // Pair Tag (Ids)
        world.Add(Bob, Pair.Get(Likes, Apples));

        // Pair Tag (Relation Generic)
        world.Add(Bob, Pair.Relation<Eats>.Target(Apples));

        // Pair Tag (Target Generic)
        world.Add(Bob, Pair.Relation(Likes).Target<Pears>());

        // Pair Tag (Relation generic + Target generic)
        world.Add(Bob, Pair.Get<Eats, Pears>());

        // Pair Component (Relation generic)
        world.Add(Bob, Pair.Relation(new Requires(5)).Target(Apples));

        // Pair Component (Target generic)
        world.Add(Bob, Pair.Relation(SpawnsAt).Target(new Position(10, 20)));

        // Pair Component (Relation generic + Target generic)
        world.Add(Bob, Pair.Get<Requires, Position>(new Requires(5)));
        world.Add(Bob, Pair.Relation(new Requires(5)).Target<Position>());
        world.Add(Bob, Pair.Relation<LivesAt>().Target(new Position(10, 20)));
    }

    private void HasTest(World world)
    {
        // Pair Tag (Ids)

        // Pair Tag (Relation Generic)

        // Pair Tag (Target Generic)

        // Pair Tag (Relation generic + Target generic)

        // Pair Component (Relation generic)

        // Pair Component (Target generic)

        // Pair Component (Relation generic + Target generic)
    }

    private void SetTest(World world)
    {
        // Pair Tag (Ids)

        // Pair Tag (Relation Generic)

        // Pair Tag (Target Generic)

        // Pair Tag (Relation generic + Target generic)

        // Pair Component (Relation generic)

        // Pair Component (Target generic)

        // Pair Component (Relation generic + Target generic)
    }

    private void GetTest(World world)
    {
        // Pair Component (Relation generic)
        var relation = world.Get(Bob, Pair.Relation<Requires>.Target(Apples));

        // Pair Component (Target generic)
        var target = world.Get(Bob, Pair.Relation(SpawnsAt).Target<Position>());

        // Pair Component (Relation generic + Target generic)
        var relation = world.Get(Bob, Pair.Get<Requires, Position>());
        var home = world.Get(Bob, Pair.Relation<LivesAt>().Target<Position>());
    }
}
