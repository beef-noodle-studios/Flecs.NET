using static Flecs.NET.Bindings.flecs;
using NoodleStudios.Flecs.Core;
using System.Runtime.CompilerServices;

namespace NoodleStudios.Flecs.Pairs;

public unsafe static class Pair
{
    public readonly ref struct WithRelationId(Id relation)
    {
        private readonly Id _relation = relation;

        public TagResolver Target(Id target)
        {
            return new TagResolver(_relation, target);
        }

        public TagResolver Target(Entity target)
        {
            return new TagResolver(_relation, target);
        }

        public ComponentResolverWithTargetType<TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentResolverWithTargetType<TTarget>(_relation);
        }

        public ComponentWithTargetValue<TTarget> Target<TTarget>(TTarget target)
            where TTarget : unmanaged
        {
            return new ComponentWithTargetValue<TTarget>(_relation, target);
        }
    }

    public readonly ref struct WithRelationType<TRelation>
        where TRelation : unmanaged
    {
        public ComponentResolverWithRelationType<TRelation> Target(Id target)
        {
            return new ComponentResolverWithRelationType<TRelation>(target);
        }

        public ComponentResolverWithRelationType<TRelation> Target(Entity target)
        {
            return new ComponentResolverWithRelationType<TRelation>(target);
        }

        public ComponentWithTargetValue<TRelation, TTarget> Target<TTarget>(TTarget targetValue)
            where TTarget : unmanaged
        {
            return new ComponentWithTargetValue<TRelation, TTarget>(targetValue);
        }

        public ComponentResolver<TRelation, TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentResolver<TRelation, TTarget>();
        }
    }

    public readonly ref struct WithRelationValue<TRelation>(TRelation relation)
        where TRelation : unmanaged
    {
        private readonly TRelation _relation = relation;

        public ComponentWithRelationValue<TRelation> Target(Id target)
        {
            return new ComponentWithRelationValue<TRelation>(_relation, target);
        }

        public ComponentWithRelationValue<TRelation, TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentWithRelationValue<TRelation, TTarget>(_relation);
        }
    }

    public readonly ref struct TagResolver(Id relation, Id target)
    {
        public readonly Id Relation = relation;
        public readonly Id Target = target;
    }

    public static void Add(this World world, Entity entity, TagResolver resolver)
    {
        var pairId = ecs_make_pair(resolver.Relation, resolver.Target);
        ecs_add_id(world.Handle, entity, pairId);
    }

    public static void Set(this World world, Entity entity, TagResolver resolver)
    {
        // For a Tag pair, Add and Set are effectively the same since there is
        // no data to set.
        Add(world, entity, resolver);
    }

    public static bool Has(this ReadOnlyWorld world, Entity entity, TagResolver resolver)
    {
        var pairId = ecs_make_pair(resolver.Relation, resolver.Target);
        bool result = ecs_has_id(world.Handle, entity, pairId);
        return result;
    }

    public static bool Has(this World world, Entity entity, TagResolver resolver)
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    public readonly ref struct TagResolver<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
    }

    public static void Add<TRelation, TTarget>(this World world, Entity entity, TagResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        ecs_add_id(world.Handle, entity, pairId);
    }

    public static void Set<TRelation, TTarget>(this World world, Entity entity, TagResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        // For a Tag pair, Add and Set are effectively the same since there is no
        // data to set.
        Add(world, entity, resolver);
    }

    public static bool Has<TRelation, TTarget>(
        this ReadOnlyWorld world,
        Entity entity,
        TagResolver<TRelation, TTarget> resolver = default
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            return false;

        if (!world.TryGetId<TTarget>(out var targetId))
            return false;

        var pairId = ecs_make_pair(relationId, targetId);
        bool result = ecs_has_id(world.Handle, entity, pairId);
        return result;
    }

    public static bool Has<TRelation, TTarget>(this World world, Entity entity, TagResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    public readonly ref struct ComponentResolverWithRelationType<TRelation>(Id target)
        where TRelation : unmanaged
    {
        public readonly Id Target = target;
    }

    public static void Add<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = ecs_make_pair(relationId, targetId);
        world.Add(entity, pairId);
    }

    public static void Set<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = ecs_make_pair(relationId, targetId);
        world.Set(entity, pairId);
    }

    public static bool Has<TRelation>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            return false;

        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        bool result = world.Has(entity, pairId);
        return result;
    }

    public static bool Has<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    public static ref readonly TRelation Get<TRelation>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            relationId = Id.None;

        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        return ref world.Get<TRelation>(entity, pairId);
    }

    public static ref readonly TRelation Get<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        return ref world.AsReadOnly().Get(entity, resolver);
    }

    public readonly ref struct ComponentResolverWithTargetType<TTarget>(Id relation)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
    }

    public static void Add<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        world.Add(entity, pairId);
    }

    public static void Set<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        world.Set(entity, pairId);
    }

    public static bool Has<TTarget>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TTarget>(out var targetId))
            return false;

        var relationId = resolver.Relation;
        var pairId = Ecs.MakePair(relationId, targetId);
        bool result = world.Has(entity, pairId);
        return result;
    }

    public static bool Has<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    public static ref readonly TTarget Get<TTarget>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;

        if (!world.TryGetId<TTarget>(out var targetId))
            targetId = Id.None;

        var pairId = Ecs.MakePair(relationId, targetId);
        return ref world.Get<TTarget>(entity, pairId);
    }

    public static ref readonly TTarget Get<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        return ref world.AsReadOnly().Get(entity, resolver);
    }

    public readonly ref struct ComponentResolver<TRelation, TTarget>
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
    }

    public static void Add<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        world.Add(entity, pairId);
    }

    public static void Set<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        world.Set(entity, pairId);
    }

    public static bool Has<TRelation, TTarget>(
        this ReadOnlyWorld world,
        Entity entity,
        ComponentResolver<TRelation, TTarget> resolver = default
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            return false;

        if (!world.TryGetId<TTarget>(out var targetId))
            return false;

        var pairId = ecs_make_pair(relationId, targetId);
        bool result = world.Has(entity, pairId);
        return result;
    }

    public static bool Has<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    public static TRelation Get<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Get(entity, resolver);
    }

    public static ref readonly TRelation Get<TRelation, TTarget>(
        this ReadOnlyWorld world,
        Entity entity,
        ComponentResolver<TRelation, TTarget> resolver = default
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            relationId = Id.None;

        if (!world.TryGetId<TTarget>(out var targetId))
            targetId = Id.None;

        var pairId = ecs_make_pair(relationId, targetId);
        void* ptr = ecs_get_id(world.Handle, entity, pairId);
        return ref Unsafe.AsRef<TRelation>(ptr);
    }

    public readonly ref struct ComponentWithRelationValue<TRelation>(TRelation relation, Id target)
        where TRelation : unmanaged
    {
        public readonly TRelation Relation = relation;
        public readonly Id Target = target;
    }

    public static void Add<TRelation>(this World world, Entity entity, ComponentWithRelationValue<TRelation> pair)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = pair.Target;
        var pairId = ecs_make_pair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Relation);
    }

    public static void Set<TRelation>(this World world, Entity entity, ComponentWithRelationValue<TRelation> pair)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = pair.Target;
        var pairId = ecs_make_pair(relationId, targetId);
        world.Set(entity, pairId, pair.Relation);
    }

    public readonly ref struct ComponentWithRelationValue<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TRelation Relation = relation;
    }

    public static void Add<TRelation, TTarget>(this World world, Entity entity, ComponentWithRelationValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Relation);
    }

    public static void Set<TRelation, TTarget>(this World world, Entity entity, ComponentWithRelationValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        world.Set(entity, pairId, pair.Relation);
    }

    public readonly ref struct ComponentWithTargetValue<TTarget>(Id relation, TTarget target)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
        public readonly TTarget Target = target;
    }

    public static void Add<TTarget>(this World world, Entity entity, ComponentWithTargetValue<TTarget> pair)
        where TTarget : unmanaged
    {
        var relationId = pair.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Target);
    }

    public static void Set<TTarget>(this World world, Entity entity, ComponentWithTargetValue<TTarget> pair)
        where TTarget : unmanaged
    {
        var relationId = pair.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        world.Set(entity, pairId, pair.Target);
    }

    public readonly ref struct ComponentWithTargetValue<TRelation, TTarget>(TTarget target)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TTarget Target = target;
    }

    public static void Add<TRelation, TTarget>(this World world, Entity entity, ComponentWithTargetValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Target);
    }

    public static void Set<TRelation, TTarget>(this World world, Entity entity, ComponentWithTargetValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = ecs_make_pair(relationId, targetId);
        world.Set(entity, pairId, pair.Target);
    }

    public static WithRelationId Relation(Id relation)
    {
        return new WithRelationId(relation);
    }

    public static WithRelationId Relation(Entity relation)
    {
        return new WithRelationId(relation);
    }

    public static WithRelationType<TRelation> Relation<TRelation>()
        where TRelation : unmanaged
    {
        return new WithRelationType<TRelation>();
    }

    public static WithRelationValue<TRelation> Relation<TRelation>(TRelation relation)
        where TRelation : unmanaged
    {
        return new WithRelationValue<TRelation>(relation);
    }

    public static TagResolver Tag(Id relation, Id target)
    {
        return new TagResolver(relation, target);
    }

    public static TagResolver<TRelation, TTarget> Tag<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new TagResolver<TRelation, TTarget>();
    }

    public static ComponentWithRelationValue<TRelation, TTarget> Component<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentWithRelationValue<TRelation, TTarget>(relation);
    }

    public static ComponentWithTargetValue<TRelation, TTarget> Component<TRelation, TTarget>(TTarget target)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentWithTargetValue<TRelation, TTarget>(target);
    }

    public static ComponentResolver<TRelation, TTarget> Component<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentResolver<TRelation, TTarget>();
    }
}
