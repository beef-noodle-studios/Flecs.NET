using System.Runtime.CompilerServices;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    public readonly ref struct Adder(World world)
    {
        private readonly World _world = world;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Component<TComponent>(Entity entity, TComponent component)
            where TComponent : unmanaged
        {
            var componentId = ComponentId<TComponent>.GetId(_world._handle);
            if (_world.Has(entity, componentId))
                return;

            _world.Set(entity, componentId, component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tag(Entity entity, Id tag)
        {
            _world.Add(entity, tag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tag<TTag>(Entity entity)
            where TTag : unmanaged
        {
            var tagId = ComponentId<TTag>.GetId(_world._handle);
            _world.Add(entity, tagId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Relationship<TRelation, TTarget>(Entity entity)
            where TRelation : unmanaged
            where TTarget : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, targetId);
            _world.Add(entity, pairId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Relationship<TRelation>(Entity entity, Id target)
            where TRelation : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, target);
            _world.Add(entity, pairId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Relationship(Entity entity, Id relationship, Id target)
        {
            var pairId = _world.Pair(relationship, target);
            _world.Add(entity, pairId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RelationshipComponent<TRelation>(Entity entity, TRelation relationship, Id target)
            where TRelation : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, target);
            _world.Set(entity, pairId, relationship);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RelationshipComponent<TTarget>(Entity entity, Id relationship, TTarget target)
            where TTarget : unmanaged
        {
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relationship, targetId);
            _world.Set(entity, pairId, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RelationshipComponent<TRelation, TTarget>(Entity entity, TRelation relationship)
            where TRelation : unmanaged
            where TTarget : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, targetId);
            _world.Set(entity, pairId, relationship);
        }
    }

    /// <summary>
    ///     Fluent API object for resolving a 'get' for a component or pair
    ///     with friendly syntax.
    /// </summary>
    /// <param name="world">
    ///     The <see cref="World"/> to operate on.
    /// </param>
    public readonly ref struct Getter(World world)
    {
        private readonly World _world = world;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Component<T>(Entity entity)
            where T : unmanaged
        {
            return ref _world.Get<T>(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TRelation RelationshipTowardsTarget<TRelation>(Entity entity, Id target)
            where TRelation : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, target);
            return ref _world.Get<TRelation>(entity, pairId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TTarget TargetOfRelationship<TTarget>(Entity entity, Id relation)
            where TTarget : unmanaged
        {
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relation, targetId);
            return ref _world.Get<TTarget>(entity, pairId);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly TTarget TargetOfRelationship<TRelation, TTarget>(Entity entity)
            where TRelation : unmanaged
            where TTarget : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, targetId);
            return ref _world.Get<TTarget>(entity, pairId);
        }
    }

    public readonly ref struct Setter(World world)
    {
        private readonly World _world = world;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Component<T>(Entity entity, in T data)
            where T : unmanaged
        {
            var id = ComponentId<T>.GetId(_world._handle);
            _world.Set(entity, id, in data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RelationshipComponent<TRelation>(Entity entity, TRelation relationship, Id target)
            where TRelation : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, target);
            _world.Set(entity, pairId, relationship);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RelationshipComponent<TRelation, TTarget>(Entity entity, TRelation relationship)
            where TRelation : unmanaged
            where TTarget : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, targetId);
            _world.Set(entity, pairId, relationship);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Relationship<TTarget>(Entity entity, Id relation, TTarget target)
            where TTarget : unmanaged
        {
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relation, targetId);
            _world.Set(entity, pairId, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Relationship<TRelation, TTarget>(Entity entity, TTarget target)
            where TRelation : unmanaged
            where TTarget : unmanaged
        {
            var relationId = ComponentId<TRelation>.GetId(_world._handle);
            var targetId = ComponentId<TTarget>.GetId(_world._handle);
            var pairId = _world.Pair(relationId, targetId);
            _world.Set(entity, pairId, target);
        }
    }

    public Adder Add() => new(this);
    public Getter Get() => new(this);
    public Setter Set() => new(this);

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

        var entity = world.CreateEntity();
        world.Add().Relationship(entity, likes, apples);
        world.Add().Relationship<Eats, Pears>(entity);
        world.Add().Relationship<Eats>(entity, apples);
        world.Add().Component(entity, new Position(1, 2));
        world.Add().RelationshipComponent(entity, Disposition.Likes, apples);
        world.Add().RelationshipComponent(entity, likes, new Position(0, 0));
        world.Add().Tag(entity, mechanical);
        world.Add().Tag<Biological>(entity);

        world.Set().RelationshipComponent(entity, Disposition.Likes, apples);
        world.Set().RelationshipComponent<Disposition, Pears>(entity, Disposition.Dislikes);
        world.Set().Relationship(entity, likes, apples);
        world.Set().Relationship(entity, IsA, robot);

        world.Get().RelationshipTowardsTarget<Disposition>(entity, apples);
    }
}
