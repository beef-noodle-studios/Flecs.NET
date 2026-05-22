using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    // TODO: Add World entity API methods here.
    // https://www.flecs.dev/flecs/group__entities.html

    // Create, destroy, validate, copy, etc.

    /// <summary>
    ///     Create new entity ID.
    /// </summary>
    /// <remarks>
    ///     This operation returns an unused entity ID.
    ///     This operation is guaranteed to return an empty entity as it does
    ///     not use values set by <see cref="ecs_set_scope(ecs_world_t*, ulong)"/>
    ///     or <see cref="ecs_set_with(ecs_world_t*, ulong)"/>.
    /// </remarks>
    /// <returns>
    ///     The new entity ID.
    /// </returns>
    public Entity CreateEntity()
    {
        return new Entity(ecs_new(_handle));
    }

    /// <summary>
    ///     Delete an entity.
    /// </summary>
    /// <remarks>
    ///     This operation will delete an entity and all of its components.
    ///     The entity ID will be made available for recycling.
    ///     If the entity passed to <see cref="DestroyEntity(Entity)"/> is not
    ///     alive, the operation will have no side effects.
    /// </remarks>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to destroy.
    /// </param>
    public void DestroyEntity(Entity entity)
    {
        ecs_delete(_handle, entity);
    }

    /// <summary>
    ///     Test whether <paramref name="entity"/> exists.
    /// </summary>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to test.
    /// </param>
    /// <returns>
    ///     True if the entity exists, false if the entity does not exist.
    /// </returns>
    public bool EntityExists(Entity entity)
    {
        return ecs_exists(_handle, entity);
    }

    /// <summary>
    ///     Test whether an entity is alive.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Entities are alive after they are created, and become not alive
    ///         when they are deleted. Operations that return alive IDs are
    ///         (amongst others) <see cref="CreateEntity"/>,
    ///         ecs_new_low_id() and ecs_entity_init().
    ///         IDs can be made alive with the ecs_make_alive() function.
    ///     </para>
    ///     <para>
    ///         After an ID is deleted it can be recycled.
    ///         Recycled IDs are different from the original ID in that they
    ///         have a different generation count. This makes it possible for
    ///         the API to distinguish between the two. An example:
    ///         <code>
    ///             ecs_entity_t e1 = ecs_new(world);
    ///             ecs_is_alive(world, e1);             // true
    ///             ecs_delete(world, e1);
    ///             ecs_is_alive(world, e1);             // false
    ///
    ///             ecs_entity_t e2 = ecs_new(world);    // recycles e1
    ///             ecs_is_alive(world, e2);             // true
    ///             ecs_is_alive(world, e1);             // false
    ///         </code>
    ///     </para>
    ///     <para>
    ///         Unlike <see cref="EntityIsValid(Entity)"/>, this operation will
    ///         panic if the passed-in entity ID is 0 or has an invalid bit pattern.
    ///     </para>
    /// </remarks>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to test.
    /// </param>
    /// <returns>
    ///     True if the entity is alive, false otherwise.
    /// </returns>
    /// <seealso cref="EntityIsValid(Entity)"/>
    public bool EntityIsAlive(Entity entity)
    {
        return ecs_is_alive(_handle, entity);
    }

    /// <summary>
    ///     Test whether an entity is valid.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This operation tests whether the entity ID:
    ///         <list type="bullet">
    ///             <item>
    ///                 Is not 0
    ///             </item>
    ///             <item>
    ///                 Has a valid bit pattern
    ///             </item>
    ///             <item>
    ///                 is alive (see <see cref="EntityIsAlive"/>)
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         If this operation returns true, it is safe to use the entity
    ///         with other operations.
    ///     </para>
    ///     <para>
    ///         This operation should only be used if an application cannot be
    ///         sure that an entity is initialized with a valid value.
    ///         In all other cases where an entity was initialized with a valid
    ///         value, but the application wants to check if the entity is
    ///         (still) alive, use <see cref="EntityIsAlive(Entity)"/>.
    ///     </para>
    /// </remarks>
    /// <param name="entity">
    ///     The <see cref="Entity"/> to test.
    /// </param>
    /// <returns>
    ///     True if the entity is valid, false otherwise.
    /// </returns>
    /// <seealso cref="EntityIsAlive(Entity)"/>
    public bool EntityIsValid(Entity entity)
    {
        return ecs_is_valid(_handle, entity);
    }
}
