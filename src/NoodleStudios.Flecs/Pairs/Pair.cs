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
        public WithRelationIdAndTargetId Target(Id target)
        {
            return new WithRelationIdAndTargetId(_relation, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithRelationIdAndTargetId Target(Entity target)
        {
            return new WithRelationIdAndTargetId(_relation, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithRelationIdAndUnknownTargetValue<TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new WithRelationIdAndUnknownTargetValue<TTarget>(_relation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithRelationIdAndKnownTargetValue<TTarget> Target<TTarget>(TTarget target)
            where TTarget : unmanaged
        {
            return new WithRelationIdAndKnownTargetValue<TTarget>(_relation, target);
        }
    }

    public readonly ref struct WithRelationType<TRelation>
        where TRelation : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithUnknownRelationValueAndTargetId<TRelation> Target(Id target)
        {
            return new WithUnknownRelationValueAndTargetId<TRelation>(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithUnknownRelationValueAndTargetId<TRelation> Target(Entity target)
        {
            return new WithUnknownRelationValueAndTargetId<TRelation>(target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithRelationTypeAndKnownTargetValue<TRelation, TTarget> Target<TTarget>(TTarget targetValue)
            where TTarget : unmanaged
        {
            return new WithRelationTypeAndKnownTargetValue<TRelation, TTarget>(targetValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentResolver<TRelation, TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new ComponentResolver<TRelation, TTarget>();
        }
    }

    public readonly ref struct WithKnownRelationValue<TRelation>(TRelation relation)
        where TRelation : unmanaged
    {
        private readonly TRelation _relation = relation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithKnownRelationValueAndTargetId<TRelation> Target(Id target)
        {
            return new WithKnownRelationValueAndTargetId<TRelation>(_relation, target);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WithKnownRelationValueAndTargetId<TRelation, TTarget> Target<TTarget>()
            where TTarget : unmanaged
        {
            return new WithKnownRelationValueAndTargetId<TRelation, TTarget>(_relation);
        }
    }

    /// <summary>
    ///     Builder for a pair with a relation value but no value provided.
    ///     Indicates a pair component should be resolved by reading the
    ///     relation value type.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The known data type of the relation value.
    /// </typeparam>
    public readonly ref struct WithUnknownRelationValue<TRelation>
        where TRelation : unmanaged
    {
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
    public readonly ref struct WithRelationIdAndTargetId(Id relation, Id target)
    {
        public readonly Id Relation = relation;
        public readonly Id Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(this World world, Entity entity, WithRelationIdAndTargetId resolver)
    {
        var pairId = Ecs.MakePair(resolver.Relation, resolver.Target);
        ecs_add_id(world.Handle, entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(this World world, Entity entity, WithRelationIdAndTargetId resolver)
    {
        // For a Tag pair, Add and Set are effectively the same since there is
        // no data to set.
        Add(world, entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove(this World world, Entity entity, WithRelationIdAndTargetId resolver)
    {
        var pairId = Ecs.MakePair(resolver.Relation, resolver.Target);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(this ReadOnlyWorld world, Entity entity, WithRelationIdAndTargetId resolver)
    {
        var pairId = Ecs.MakePair(resolver.Relation, resolver.Target);
        bool result = world.Has(entity, pairId);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(this World world, Entity entity, WithRelationIdAndTargetId resolver)
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
    public readonly ref struct WithRelationTypeAndTargetType<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation, TTarget>(this World world, Entity entity, WithRelationTypeAndTargetType<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Add(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation, TTarget>(this World world, Entity entity, WithRelationTypeAndTargetType<TRelation, TTarget> resolver)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        // For a Tag pair, Add and Set are effectively the same since there is no
        // data to set.
        Add(world, entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<TRelation, TTarget>(this World world, Entity entity, WithRelationTypeAndTargetType<TRelation, TTarget> resolver)
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
        WithRelationTypeAndTargetType<TRelation, TTarget> resolver
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
    public static bool Has<TRelation, TTarget>(this World world, Entity entity, WithRelationTypeAndTargetType<TRelation, TTarget> resolver)
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
    public readonly ref struct WithUnknownRelationValueAndTargetId<TRelation>(Id target)
        where TRelation : unmanaged
    {
        public readonly Id Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation>(this World world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Add(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TRelation>(this World world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<TRelation>(this World world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver)
        where TRelation : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(world.Handle);
        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TRelation>(this ReadOnlyWorld world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver)
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
    public static bool Has<TRelation>(this World world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver)
        where TRelation : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TRelation Get<TRelation>(this ReadOnlyWorld world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver)
        where TRelation : unmanaged
    {
        if (!world.TryGetId<TRelation>(out var relationId))
            relationId = Id.None;

        var targetId = resolver.Target;
        var pairId = Ecs.MakePair(relationId, targetId);
        return ref world.Get<TRelation>(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TRelation Get<TRelation>(this World world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver)
        where TRelation : unmanaged
    {
        return ref world.AsReadOnly().Get(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TRelation>(this ReadOnlyWorld world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver, out TRelation relation)
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
    public static bool TryGet<TRelation>(this World world, Entity entity, WithUnknownRelationValueAndTargetId<TRelation> resolver, out TRelation relation)
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
    public readonly ref struct WithRelationIdAndUnknownTargetValue<TTarget>(Id relation)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TTarget>(this World world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Add(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set<TTarget>(this World world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Set(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Remove<TTarget>(this World world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;
        var targetId = ComponentId<TTarget>.GetId(world.Handle);
        var pairId = Ecs.MakePair(relationId, targetId);
        world.Remove(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has<TTarget>(this ReadOnlyWorld world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver)
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
    public static bool Has<TTarget>(this World world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver)
        where TTarget : unmanaged
    {
        return world.AsReadOnly().Has(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TTarget Get<TTarget>(this ReadOnlyWorld world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver)
        where TTarget : unmanaged
    {
        var relationId = resolver.Relation;

        if (!world.TryGetId<TTarget>(out var targetId))
            targetId = Id.None;

        var pairId = Ecs.MakePair(relationId, targetId);
        return ref world.Get<TTarget>(entity, pairId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref readonly TTarget Get<TTarget>(this World world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver)
        where TTarget : unmanaged
    {
        return ref world.AsReadOnly().Get(entity, resolver);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TTarget>(this World world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver, out TTarget target)
        where TTarget : unmanaged
    {
        return world.AsReadOnly().TryGet(entity, resolver, out target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGet<TTarget>(this ReadOnlyWorld world, Entity entity, WithRelationIdAndUnknownTargetValue<TTarget> resolver, out TTarget target)
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

    public readonly ref struct WithUnknownRelationValueAndTargetType<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
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
    public readonly ref struct WithKnownRelationValueAndTargetId<TRelation>(TRelation relation, Id target)
        where TRelation : unmanaged
    {
        public readonly TRelation Relation = relation;
        public readonly Id Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation>(this World world, Entity entity, WithKnownRelationValueAndTargetId<TRelation> pair)
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
    public static void Set<TRelation>(this World world, Entity entity, WithKnownRelationValueAndTargetId<TRelation> pair)
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
    public readonly ref struct WithKnownRelationValueAndTargetId<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TRelation Relation = relation;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation, TTarget>(this World world, Entity entity, WithKnownRelationValueAndTargetId<TRelation, TTarget> pair)
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
    public static void Set<TRelation, TTarget>(this World world, Entity entity, WithKnownRelationValueAndTargetId<TRelation, TTarget> pair)
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
    public readonly ref struct WithRelationIdAndKnownTargetValue<TTarget>(Id relation, TTarget target)
        where TTarget : unmanaged
    {
        public readonly Id Relation = relation;
        public readonly TTarget Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TTarget>(this World world, Entity entity, WithRelationIdAndKnownTargetValue<TTarget> pair)
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
    public static void Set<TTarget>(this World world, Entity entity, WithRelationIdAndKnownTargetValue<TTarget> pair)
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
    public readonly ref struct WithRelationTypeAndKnownTargetValue<TRelation, TTarget>(TTarget target)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        public readonly TTarget Target = target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add<TRelation, TTarget>(this World world, Entity entity, WithRelationTypeAndKnownTargetValue<TRelation, TTarget> pair)
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
    public static void Set<TRelation, TTarget>(this World world, Entity entity, WithRelationTypeAndKnownTargetValue<TRelation, TTarget> pair)
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
    public static WithKnownRelationValue<TRelation> Relation<TRelation>(TRelation relation)
        where TRelation : unmanaged
    {
        return new WithKnownRelationValue<TRelation>(relation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationIdAndTargetId Tag(Id relation, Id target)
    {
        return new WithRelationIdAndTargetId(relation, target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationTypeAndTargetType<TRelation, TTarget> Tag<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new WithRelationTypeAndTargetType<TRelation, TTarget>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithKnownRelationValueAndTargetId<TRelation, TTarget> Component<TRelation, TTarget>(TRelation relation)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new WithKnownRelationValueAndTargetId<TRelation, TTarget>(relation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationTypeAndKnownTargetValue<TRelation, TTarget> Component<TRelation, TTarget>(TTarget target)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new WithRelationTypeAndKnownTargetValue<TRelation, TTarget>(target);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ComponentResolver<TRelation, TTarget> Component<TRelation, TTarget>()
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        return new ComponentResolver<TRelation, TTarget>();
    }

    /// <summary>
    ///     Shortcut to create a <see cref="WithRelationIdAndTargetId"/> for the built-in
    ///     <c>IsA</c> relationship.
    /// </summary>
    /// <param name="superType">
    ///     The "parent" entity of the inheritance relationship.
    /// </param>
    /// <returns>
    ///     A <see cref="WithRelationIdAndTargetId"/> for the <c>IsA</c> relationship.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationIdAndTargetId IsA(Entity superType) => new(Ecs.IsA, superType);

    /// <summary>
    ///     Shortcut to create a <see cref="WithRelationIdAndTargetId"/> for the built-in
    ///     <c>ChildOf</c> relationship.
    /// </summary>
    /// <param name="entity">
    ///     The "parent" entity of the child relationship.
    /// </param>
    /// <returns>
    ///     A <see cref="WithRelationIdAndTargetId"/> for the <c>ChildOf</c> relationship.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WithRelationIdAndTargetId ChildOf(Entity entity) => new(Ecs.ChildOf, entity);
}
