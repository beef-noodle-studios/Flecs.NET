using NoodleStudios.Flecs.Core;
using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     Class containing utility API methods to simplify working with pairs in
///     Flecs. This includes methods for adding, setting, and checking for pairs
///     on entities and resolving components and tags associated with pairs.
/// </summary>
public unsafe static class Pair
{
    public readonly ref struct WithRelationId(Id relation)
    {
        private readonly Id _relation = relation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TagResolver Target(Id target)
        {
            return new TagResolver(_relation, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TagResolver Target(Entity target)
        {
            return new TagResolver(_relation, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentResolverWithTargetType<TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentResolverWithTargetType<TTarget>(_relation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentInitializerWithTargetValue<TTarget> Target<TTarget>(TTarget target)
            where TTarget : unmanaged
        {
            return new ComponentInitializerWithTargetValue<TTarget>(_relation, target);
        }
    }

    public readonly ref struct WithRelationType<TRelation>
        where TRelation : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentResolverWithRelationType<TRelation> Target(Id target)
        {
            return new ComponentResolverWithRelationType<TRelation>(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentResolverWithRelationType<TRelation> Target(Entity target)
        {
            return new ComponentResolverWithRelationType<TRelation>(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentInitializerWithTargetValue<TRelation, TTarget> Target<TTarget>(TTarget targetValue)
            where TTarget : unmanaged
        {
            return new ComponentInitializerWithTargetValue<TRelation, TTarget>(targetValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentInitializerWithRelationValue<TRelation> Target(Id target)
        {
            return new ComponentInitializerWithRelationValue<TRelation>(_relation, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentInitializerWithRelationValue<TRelation, TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentInitializerWithRelationValue<TRelation, TTarget>(_relation);
        }
    }

    /// <summary>
    ///     Value type used to resolve pair tags via Add/Set/Remove/Has
    ///     extension methods.
    /// </summary>
    /// <param name="relation">
    ///     The relation id of the pair tag to resolve.
    /// </param>
    /// <param name="target">
    ///     The target id of the pair tag to resolve.
    /// </param>
    public readonly ref struct TagResolver(Id relation, Id target)
    {
        public readonly Id Relation = relation;
        public readonly Id Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(this World world, Entity entity, TagResolver resolver)
    {
        var pairId = Ecs.MakePair(resolver.Relation, resolver.Target);
        ecs_add_id(world.Handle, entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(this World world, Entity entity, TagResolver resolver)
    {
        // For a Tag pair, Add and Set are effectively the same since there is
        // no data to set.
        Add(world, entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove(this World world, Entity entity, TagResolver resolver)
    {
        var pairId = Ecs.MakePair(resolver.Relation, resolver.Target);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(this ReadOnlyWorld world, Entity entity, TagResolver resolver)
    {
        var pairId = Ecs.MakePair(resolver.Relation, resolver.Target);
        bool result = world.Has(entity, pairId);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(this World world, Entity entity, TagResolver resolver)
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    /// <summary>
    ///     Value type used to resolve pair tags via Add/Set/Remove/Has
    ///     extension methods.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The type of the relation id of the pair tag to resolve.
    /// </typeparam>
    /// <typeparam name="TTarget">
    ///     The type of the target id of the pair tag to resolve.
    /// </typeparam>
    public readonly ref struct TagResolver<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation, TTarget>(this World world, Entity entity, TagResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Add(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation, TTarget>(this World world, Entity entity, TagResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        // For a Tag pair, Add and Set are effectively the same since there is no
        // data to set.
        Add(world, entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<TRelation, TTarget>(this World world, Entity entity, TagResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TRelation, TTarget>(
        this ReadOnlyWorld world,
        Entity entity,
        TagResolver<TRelation, TTarget> resolver
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            return false;

        if (!world.TryGetId<TTarget>(out var targetId))
            return false;

        var pairId = Ecs.MakePair(relationId, targetId);
        bool result = world.Has(entity, pairId);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TRelation, TTarget>(this World world, Entity entity, TagResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    /// <summary>
    ///     Value type for resolving pair components with a known relation type
    ///     and unknown target type via Add/Set/Remove/Has/Get/TryGet
    ///     extension methods.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The type of the relation id of the pair component to resolve.
    /// </typeparam>
    /// <param name="target">
    ///     The target id of the pair component to resolve.
    /// </param>
    public readonly ref struct ComponentResolverWithRelationType<TRelation>(Id target)
        where TRelation : unmanaged
    {
        public readonly Id Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Add(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TRelation Get<TRelation>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            relationId = Id.None;

        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        return ref world.Get<TRelation>(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TRelation Get<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver)
        where TRelation : unmanaged
    {
        return ref world.AsReadOnly().Get(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TRelation>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver, out TRelation relation)
        where TRelation : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
        {
            relation = default;
            return false;
        }

        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        bool result = world.TryGet(entity, pairId, out relation);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TRelation>(this World world, Entity entity, ComponentResolverWithRelationType<TRelation> resolver, out TRelation relation)
        where TRelation : unmanaged
    {
        return world.AsReadOnly().TryGet(entity, resolver, out relation);
    }

    /// <summary>
    ///     Value type for resolving pair components with a known target type
    ///     and unknown relation type via Add/Set/Remove/Has/Get/TryGet
    ///     extension methods.
    /// </summary>
    /// <typeparam name="TTarget">
    ///     The type of the target id of the pair component to resolve.
    /// </typeparam>
    /// <param name="relation">
    ///     The relation id of the pair component to resolve.
    /// </param>
    public readonly ref struct ComponentResolverWithTargetType<TTarget>(Id relation)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Add(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TTarget Get<TTarget>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;

        if (!world.TryGetId<TTarget>(out var targetId))
            targetId = Id.None;

        var pairId = Ecs.MakePair(relationId, targetId);
        return ref world.Get<TTarget>(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TTarget Get<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver)
        where TTarget : unmanaged
    {
        return ref world.AsReadOnly().Get(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TTarget>(this World world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver, out TTarget target)
        where TTarget : unmanaged
    {
        return world.AsReadOnly().TryGet(entity, resolver, out target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TTarget>(this ReadOnlyWorld world, Entity entity, ComponentResolverWithTargetType<TTarget> resolver, out TTarget target)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        if (!world.TryGetId<TTarget>(out var targetId))
        {
            target = default;
            return false;
        }

        var pairId = Ecs.MakePair(relationId, targetId);
        bool result = world.TryGet(entity, pairId, out target);
        return result;
    }

    /// <summary>
    ///     Value type for resolving pair components with known relation and
    ///     target types via Add/Set/Remove/Has/Get/TryGet extension methods.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The type of the relation id of the pair component to resolve.
    /// </typeparam>
    /// <typeparam name="TTarget">
    ///     The type of the target id of the pair component to resolve.
    /// </typeparam>
    public readonly ref struct ComponentResolver<TRelation, TTarget>
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Add(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TRelation, TTarget>(
        this ReadOnlyWorld world,
        Entity entity,
        ComponentResolver<TRelation, TTarget> resolver
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            return false;

        if (!world.TryGetId<TTarget>(out var targetId))
            return false;

        var pairId = Ecs.MakePair(relationId, targetId);
        bool result = world.Has(entity, pairId);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TRelation Get<TRelation, TTarget>(this World world, Entity entity, ComponentResolver<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Get(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TRelation Get<TRelation, TTarget>(
        this ReadOnlyWorld world,
        Entity entity,
        ComponentResolver<TRelation, TTarget> resolver
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            relationId = Id.None;

        if (!world.TryGetId<TTarget>(out var targetId))
            targetId = Id.None;

        var pairId = ecs_make_pair(relationId, targetId);
        return ref world.Get<TRelation>(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TRelation, TTarget>(
        this ReadOnlyWorld world,
        Entity entity,
        ComponentResolver<TRelation, TTarget> resolver,
        out TRelation relation
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
        {
            relation = default;
            return false;
        }

        if (!world.TryGetId<TTarget>(out var targetId))
        {
            relation = default;
            return false;
        }

        var pairId = ecs_make_pair(relationId, targetId);
        bool result = world.TryGet(entity, pairId, out relation);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TRelation, TTarget>(
        this World world,
        Entity entity,
        ComponentResolver<TRelation, TTarget> resolver,
        out TRelation relation
        )
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return world.AsReadOnly().TryGet(entity, resolver, out relation);
    }

    /// <summary>
    ///     Value type for setting pair components with a known relation value
    ///     and unknown target type via Add/Set extension methods.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The type of the relation id of the pair component to set.
    /// </typeparam>
    /// <param name="relation">
    ///     The value of the relation component to set on the pair component.
    /// </param>
    /// <param name="target">
    ///     The target id of the pair component to set.
    /// </param>
    public readonly ref struct ComponentInitializerWithRelationValue<TRelation>(TRelation relation, Id target)
        where TRelation : unmanaged
    {
        public readonly TRelation Relation = relation;
        public readonly Id Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation>(this World world, Entity entity, ComponentInitializerWithRelationValue<TRelation> pair)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = pair.Target;
        var pairId = Ecs.MakePair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Relation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation>(this World world, Entity entity, ComponentInitializerWithRelationValue<TRelation> pair)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = pair.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId, pair.Relation);
    }

    /// <summary>
    ///     Value type for setting pair components with a known target type and
    ///     relation value via Add/Set extension methods.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The type of the relation id of the pair component to set.
    /// </typeparam>
    /// <typeparam name="TTarget">
    ///     The type of the target id of the pair component to set.
    /// </typeparam>
    /// <param name="relation">
    ///     The value of the relation component to set on the pair component.
    /// </param>
    public readonly ref struct ComponentInitializerWithRelationValue<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TRelation Relation = relation;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation, TTarget>(this World world, Entity entity, ComponentInitializerWithRelationValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Relation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation, TTarget>(this World world, Entity entity, ComponentInitializerWithRelationValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId, pair.Relation);
    }

    /// <summary>
    ///     Value for setting pair components with a known target value and
    ///     unknown relation type via Add/Set extension methods.
    /// </summary>
    /// <typeparam name="TTarget">
    ///     The type of the target id of the pair component to set.
    /// </typeparam>
    /// <param name="relation">
    ///     The id of the relation component to set on the pair component.
    /// </param>
    /// <param name="target">
    ///     The value of the target component to set on the pair component.
    /// </param>
    public readonly ref struct ComponentInitializerWithTargetValue<TTarget>(Id relation, TTarget target)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
        public readonly TTarget Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TTarget>(this World world, Entity entity, ComponentInitializerWithTargetValue<TTarget> pair)
        where TTarget : unmanaged
    {
        var relationId = pair.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TTarget>(this World world, Entity entity, ComponentInitializerWithTargetValue<TTarget> pair)
        where TTarget : unmanaged
    {
        var relationId = pair.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId, pair.Target);
    }

    /// <summary>
    ///     Value for setting pair components with a known target value and
    ///     relation type via Add/Set extension methods.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The type of the relation id of the pair component to set.
    /// </typeparam>
    /// <typeparam name="TTarget">
    ///     The type of the target id of the pair component to set.
    /// </typeparam>
    /// <param name="target">
    ///     The value of the target component to set on the pair component.
    /// </param>
    public readonly ref struct ComponentInitializerWithTargetValue<TRelation, TTarget>(TTarget target)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TTarget Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation, TTarget>(this World world, Entity entity, ComponentInitializerWithTargetValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);

        if (world.Has(entity, pairId))
            return;

        world.Set(entity, pairId, pair.Target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation, TTarget>(this World world, Entity entity, ComponentInitializerWithTargetValue<TRelation, TTarget> pair)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId, pair.Target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationId Relation(Id relation)
    {
        return new WithRelationId(relation);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationId Relation(Entity relation)
    {
        return new WithRelationId(relation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationType<TRelation> Relation<TRelation>()
        where TRelation : unmanaged
    {
        return new WithRelationType<TRelation>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationValue<TRelation> Relation<TRelation>(TRelation relation)
        where TRelation : unmanaged
    {
        return new WithRelationValue<TRelation>(relation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TagResolver Tag(Id relation, Id target)
    {
        return new TagResolver(relation, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TagResolver<TRelation, TTarget> Tag<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new TagResolver<TRelation, TTarget>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInitializerWithRelationValue<TRelation, TTarget> Component<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentInitializerWithRelationValue<TRelation, TTarget>(relation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentInitializerWithTargetValue<TRelation, TTarget> Component<TRelation, TTarget>(TTarget target)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentInitializerWithTargetValue<TRelation, TTarget>(target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentResolver<TRelation, TTarget> Component<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentResolver<TRelation, TTarget>();
    }

    /// <summary>
    ///     Shortcut to create a <see cref="TagResolver"/> for the built-in
    ///     <c>IsA</c> relationship.
    /// </summary>
    /// <param name="superType">
    ///     The "parent" entity of the inheritance relationship.
    /// </param>
    /// <returns>
    ///     A <see cref="TagResolver"/> for the <c>IsA</c> relationship.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TagResolver IsA(Entity superType) => new(Ecs.IsA, superType);

    /// <summary>
    ///     Shortcut to create a <see cref="TagResolver"/> for the built-in
    ///     <c>ChildOf</c> relationship.
    /// </summary>
    /// <param name="entity">
    ///     The "parent" entity of the child relationship.
    /// </param>
    /// <returns>
    ///     A <see cref="TagResolver"/> for the <c>ChildOf</c> relationship.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TagResolver ChildOf(Entity entity) => new(Ecs.ChildOf, entity);
}
