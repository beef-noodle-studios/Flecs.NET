using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A read-write wrapper around a native Flecs world.
/// </summary>
public unsafe readonly struct World : IDisposable
{
    private readonly ecs_world_t* _handle;

    private World(ecs_world_t* handle)
    {
        _handle = handle;
    }

    /// <summary>
    ///     The native Flecs world handle.
    /// </summary>
    internal ecs_world_t* Handle => _handle;

    /// <summary>
    ///     Create a new Flecs world.
    /// </summary>
    /// <returns>
    ///     A <see cref="World"/> instance that wraps a newly created native
    ///     Flecs world.
    /// </returns>
    public static World New()
    {
        ecs_world_t* handle = ecs_init();
        var world = FromNativeWorldHandle(handle);
        return world;
    }

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
    public Entity CreateEmptyEntity()
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
