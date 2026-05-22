using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    // Pairs and relationships.
    // https://www.flecs.dev/flecs/group__relationships.html
    //
    // A pair (relationship, target) is just an Id, built with Pair(...). It is
    // added/removed/queried with the ordinary component operations on World, so a
    // pair is not a special kind of thing - it is an id like any other. The
    // *Pair convenience methods below build the id for you. The relationship and
    // target are always ordinary arguments, never type arguments, so it is always
    // clear which is which.

    /// <summary>
    ///     Build a pair id from a relationship and a target.
    /// </summary>
    /// <remarks>
    ///     The result is an <see cref="Id"/> that can be added to an entity or set
    ///     to a value, but it is not a valid standalone entity.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Id Pair(Id relationship, Id target) => new(ecs_make_pair(relationship, target));

    /// <summary>
    ///     Add a (relationship, target) pair to an entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddPair(Entity entity, Id relationship, Id target) =>
        Add(entity, Pair(relationship, target));

    /// <summary>
    ///     Remove a (relationship, target) pair from an entity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemovePair(Entity entity, Id relationship, Id target) =>
        Remove(entity, Pair(relationship, target));

    /// <summary>
    ///     Test whether an entity has a (relationship, target) pair.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasPair(Entity entity, Id relationship, Id target) =>
        Has(entity, Pair(relationship, target));

    /// <summary>
    ///     Set the value carried by a (relationship, target) pair on an entity,
    ///     adding the pair if the entity does not already have it.
    /// </summary>
    /// <remarks>
    ///     The data type <typeparamref name="T"/> must match the component the
    ///     pair stores (the relationship's or target's type, per the pair's
    ///     definition).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPair<T>(Entity entity, Id relationship, Id target, in T data) where T : unmanaged =>
        Set(entity, Pair(relationship, target), in data);

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
    ///     Make <paramref name="child"/> a child of <paramref name="parent"/> by
    ///     adding the <see cref="ChildOf"/> pair.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddChildOf(Entity child, Entity parent) => AddPair(child, ChildOf, parent);

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
