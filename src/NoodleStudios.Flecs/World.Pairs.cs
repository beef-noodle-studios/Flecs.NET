using NoodleStudios.Flecs.Pairs;
using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    // Pairs and relationships.
    // https://www.flecs.dev/flecs/group__relationships.html

    /// <summary>
    ///     The built-in <c>ChildOf</c> relationship, used to build entity
    ///     hierarchies.
    /// </summary>
    public Id ChildOf => new(EcsChildOf);

    /// <summary>
    ///     The built-in <c>IsA</c> relationship, used to express that one entity
    ///     is a kind of another (inheritance).
    /// </summary>
    public Id IsA => new(EcsIsA);

    /// <summary>
    ///     Add a basic tag pair to <paramref name="entity"/>, comprised of
    ///     <paramref name="relationship"/> and <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    ///     This method creates a basic tag pair, which is a combination of a
    ///     relationship and a target entity. Note that this method does not
    ///     support component pairs (pairs with data).
    /// </remarks>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to which the pair will be added.
    /// </param>
    /// <param name="relationship">
    ///     The relationship tag of the pair.
    /// </param>
    /// <param name="target">
    ///     The target tag of the pair.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddPair(Entity entity, Id relationship, Id target)
    {
        var pairId = ecs_make_pair(relationship, target);
        ecs_add_id(_handle, entity, pairId);
    }

    /// <summary>
    ///     Add a basic tag pair to <paramref name="entity"/>, comprised of
    ///     <typeparamref name="TRelation"/> and <typeparamref name="TTarget"/>.
    /// </summary>
    /// <remarks>
    ///     This method creates a basic tag pair, which is a combination of a
    ///     relationship and a target entity. Note that this method does not
    ///     support component pairs (pairs with data).
    /// </remarks>
    /// <typeparam name="TRelation">
    ///     The relationship tag type of the pair.
    /// </typeparam>
    /// <typeparam name="TTarget">
    ///     The target tag type of the pair.
    /// </typeparam>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to which the pair will be added.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddPair<TRelation, TTarget>(Entity entity)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        var relationId = ComponentId<TRelation>.GetId(_handle);
        var targetId = ComponentId<TTarget>.GetId(_handle);
        AddPair(entity, relationId, targetId);
    }

    /// <summary>
    ///     Evaluate whether <paramref name="entity"/> has the pair comprised
    ///     of <paramref name="relationship"/> and <paramref name="target"/>.
    /// </summary>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to check for the presence of the pair.
    /// </param>
    /// <param name="relationship">
    ///     The relationship tag of the pair to check for.
    /// </param>
    /// <param name="target">
    ///     The target tag of the pair to check for.
    /// </param>
    /// <returns>
    ///     True if the entity has the specified pair, false otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasPair(Entity entity, Id relationship, Id target)
    {
        var pairId = ecs_make_pair(relationship, target);
        return ecs_has_id(_handle, entity, pairId);
    }

    /// <summary>
    ///     Evaluate whether <paramref name="entity"/> has the pair comprised
    ///     <typeparamref name="TRelation"/> and <typeparamref name="TTarget"/>.
    /// </summary>
    /// <typeparam name="TRelation">
    ///     The relationship tag type of the pair.
    /// </typeparam>
    /// <typeparam name="TTarget">
    ///     The target tag type of the pair.
    /// </typeparam>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to check for the presence of the pair.
    /// </param>
    /// <returns>
    ///     True if the entity has the specified pair, false otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasPair<TRelation, TTarget>(Entity entity)
        where TRelation : unmanaged
        where TTarget : unmanaged
    {
        if (!ComponentId<TRelation>.TryGetId(_handle, out var relationId))
            return false;

        if (!ComponentId<TTarget>.TryGetId(_handle, out var targetId))
            return false;

        return HasPair(entity, relationId, targetId);
    }

    /// <summary>
    ///     Make <paramref name="child"/> a child of <paramref name="parent"/> by
    ///     adding the <see cref="ChildOf"/> pair.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddChildOf(Entity child, Entity parent)
    {
        AddPair(child, ChildOf, parent);
    }

    /// <summary>
    ///     Get the parent (the <see cref="ChildOf"/> target) of an entity.
    /// </summary>
    /// <returns>
    ///     The parent entity, or <see cref="Entity.None"/> if the entity has no
    ///     parent.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetParent(Entity entity) => new(ecs_get_parent(_handle, entity));

    /// <summary>
    ///     Get the target of a relationship on an entity.
    /// </summary>
    /// <param name="entity">The entity to inspect.</param>
    /// <param name="relationship">The relationship whose target to retrieve.</param>
    /// <param name="index">
    ///     The index of the target, for when an entity has the relationship with
    ///     more than one target. Defaults to the first.
    /// </param>
    /// <returns>
    ///     The target entity, or <see cref="Entity.None"/> if there is none at the
    ///     given index.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity GetTarget(Entity entity, Id relationship, int index = 0) =>
        new(ecs_get_target(_handle, entity, relationship, index));
}
