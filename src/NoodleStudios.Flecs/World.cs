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
}
