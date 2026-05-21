using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A read-write wrapper around a native Flecs world.
/// </summary>
public unsafe readonly struct World : IDisposable
{
    private readonly ecs_world_t* _handle;

    /// <summary>
    ///     Create a new Flecs world.
    /// </summary>
    public World()
    {
        _handle = ecs_init();
    }

    private World(ecs_world_t* handle)
    {
        _handle = handle;
    }

    /// <summary>
    ///     The native Flecs world handle.
    /// </summary>
    internal ecs_world_t* Handle => _handle;

    /// <summary>
    ///     Create a <see cref="World"/> from <paramref name="handle"/>.
    /// </summary>
    /// <param name="handle">
    ///     A pointer to a native Flecs world.
    /// </param>
    /// <returns>
    ///     A <see cref="ReadOnlyWorld"/> instance that wraps the provided
    ///     native world handle.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="handle"/> is null.
    /// </exception>
    public static World FromNativeWorldHandle(ecs_world_t* handle)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));

        // TODO: Validate that the provided handle is actually a valid Flecs world handle.

        var world = new World(handle);
        return world;
    }

    /// <summary>
    ///     Convert this world into a read-only wrapper.
    /// </summary>
    /// <returns>
    ///     A <see cref="ReadOnlyWorld"/> instance that wraps this world.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyWorld AsReadOnly() => new(this);

    public void Dispose()
    {
        if (_handle == null)
            return;

        int result = ecs_fini(_handle);
        if (result != 0)
        {
            Console.WriteLine("Failed to finalize Flecs world. Error code: " + result);
        }
    }

    /// <summary>
    ///     Signal exit.
    /// </summary>
    /// <remarks>
    ///     This operation signals that the application should quit.
    ///     It will cause <see cref="Update(float)"/> to return false.
    /// </remarks>
    public void Quit()
    {
        ecs_quit(_handle);
    }

    /// <summary>
    ///     Return whether a quit has been requested.
    /// </summary>
    /// <returns>
    ///     Whether a quit has been requested.
    /// </returns>
    public bool ShouldQuit()
    {
        return ecs_should_quit(_handle);
    }

    /// <summary>
    ///     Progress a world.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This operation progresses the world by running all systems that
    ///         are both enabled and periodic on their matching entities.
    ///     </para>
    ///     <para>
    ///         An application can pass a delta_time into the function, which
    ///         is the time passed since the last frame. This value is passed
    ///         to systems so they can update entity values proportional to the
    ///         elapsed time since their last invocation.
    ///     </para>
    ///     <para>
    ///         When an application passes 0 to delta_time, ecs_progress() will
    ///         automatically measure the time passed since the last frame.
    ///         If an application does not use time management, it should pass
    ///         a non-zero value for delta_time (1.0 is recommended).
    ///         That way, no time will be wasted measuring the time.
    ///     </para>
    /// </remarks>
    /// <param name="deltaTime">
    ///     The time passed since the last frame.
    /// </param>
    /// <returns>
    ///     False if <see cref="Quit"/> has been called, true otherwise.
    /// </returns>
    public bool Update(float deltaTime)
    {
        return ecs_progress(_handle, deltaTime);
    }

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
         EntityId entityId = ecs_new(_handle);
         return new Entity(entityId);
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
        ecs_delete(_handle, entity.Id);
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
        return ecs_exists(_handle, entity.Id);
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
    ///         Unlike ecs_is_valid(), this operation will panic if the passed-in entity ID is 0 or has an invalid bit pattern.
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
        return ecs_is_alive(_handle, entity.Id);
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
        return ecs_is_valid(_handle, entity.Id);
    }
}
