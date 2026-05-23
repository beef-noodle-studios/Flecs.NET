using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A read-write wrapper around a native Flecs world.
/// </summary>
public unsafe readonly partial struct World : IDisposable
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
    ///     Try to get the id of a component type, returning false if the
    ///     component is not registered in this world.
    /// </summary>
    /// <typeparam name="T">
    ///     The component type whose id to get. Must be unmanaged.
    /// </typeparam>
    /// <param name="id">
    ///     The output parameter to receive the component id, if it's registered.
    /// </param>
    /// <returns>
    ///     True if the component is registered in this world.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetId<T>(out Id id) where T : unmanaged
    {
        bool result = ComponentId<T>.TryGetId(_handle, out id);
        return result;
    }
}
