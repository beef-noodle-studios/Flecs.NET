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
}
