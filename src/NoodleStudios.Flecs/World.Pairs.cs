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
    ///     Make <paramref name="child"/> a child of <paramref name="parent"/> by
    ///     adding the <see cref="ChildOf"/> pair.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddChildOf(Entity child, Entity parent) => Add(child, Pair.Relation(ChildOf).Target(parent));

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
