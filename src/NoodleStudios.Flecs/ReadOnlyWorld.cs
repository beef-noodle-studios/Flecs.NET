using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     A read-only wrapper around a native Flecs world.
/// </summary>
public unsafe readonly struct ReadOnlyWorld
{
    private readonly ecs_world_t* _handle;

    private ReadOnlyWorld(ecs_world_t* handle)
    {
        _handle = handle;
    }

    /// <summary>
    ///     The native Flecs world handle.
    /// </summary>
    internal ecs_world_t* Handle => _handle;

    /// <summary>
    ///     Create a <see cref="ReadOnlyWorld"/> from <paramref name="handle"/>.
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
    public static ReadOnlyWorld FromNativeWorldHandle(ecs_world_t* handle)
    {
        if (handle == null)
            throw new ArgumentNullException(nameof(handle));

        var world = new ReadOnlyWorld(handle);
        return world;
    }

    /// <summary>
    ///     Implicitly convert from a read-write <see cref="World"/> wrapper to
    ///     a <see cref="ReadOnlyWorld"/> wrapper.
    /// </summary>
    /// <param name="world">
    ///     The read-write world wrapper to convert.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyWorld(World world) => FromNativeWorldHandle(world.Handle);
}
