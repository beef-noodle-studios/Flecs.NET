using System.Runtime.CompilerServices;

namespace NoodleStudios.Flecs;

/// <summary>
///     A read-only wrapper around a native Flecs world.
/// </summary>
/// <param name="world">
///     A mutable wrapper around a native Flecs world.
/// </param>
public readonly struct ReadOnlyWorld(World world)
{
    private readonly World _world = world;

    /// <summary>
    ///     Implicitly convert from a read-write <see cref="World"/> wrapper to
    ///     a <see cref="ReadOnlyWorld"/> wrapper.
    /// </summary>
    /// <param name="world">
    ///     The read-write world wrapper to convert.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyWorld(World world) => new(world);
}
