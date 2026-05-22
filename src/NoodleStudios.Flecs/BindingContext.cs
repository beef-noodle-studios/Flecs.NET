using System.Runtime.InteropServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     Stores and retrieves the managed <see cref="ComponentRegistry"/> attached
///     to a world, using Flecs's binding-context slot.
/// </summary>
/// <remarks>
///     <para>
///         The registry is held alive by a <see cref="GCHandle"/>; the handle's
///         <see cref="GCHandle.ToIntPtr(GCHandle)"/> token (not the object's
///         address) is what is stored natively, so the object is free to move.
///     </para>
///     <para>
///         The handle is released by <see cref="Free"/>, which Flecs invokes
///         during <c>ecs_fini</c>. It must not also be freed by managed code, or
///         the handle would be double-freed. A world that is abandoned without
///         being disposed leaks the handle, just as it leaks the native world.
///     </para>
///     <para>
///         The <em>binding</em> context slot is used (never the application
///         context slot), so this never collides with user code that calls
///         <c>ecs_set_ctx</c>.
///     </para>
/// </remarks>
internal static unsafe class BindingContext
{
    /// <summary>
    ///     Get the world's <see cref="ComponentRegistry"/>, creating and
    ///     attaching one on first use.
    /// </summary>
    public static ComponentRegistry GetRegistry(ecs_world_t* world)
    {
        void* ctx = ecs_get_binding_ctx(world);
        if (ctx != null)
            return (ComponentRegistry)GCHandle.FromIntPtr((nint)ctx).Target!;

        var registry = new ComponentRegistry();
        GCHandle handle = GCHandle.Alloc(registry, GCHandleType.Normal);
        ecs_set_binding_ctx(world, (void*)GCHandle.ToIntPtr(handle), &Free);
        return registry;
    }

    /// <summary>
    ///     Get the world's <see cref="ComponentRegistry"/> if one has been
    ///     attached, without creating one. Used by read-only paths that must not
    ///     mutate the world.
    /// </summary>
    public static ComponentRegistry? TryGetRegistry(ecs_world_t* world)
    {
        void* ctx = ecs_get_binding_ctx(world);
        return ctx != null ? (ComponentRegistry)GCHandle.FromIntPtr((nint)ctx).Target! : null;
    }

    [UnmanagedCallersOnly]
    private static void Free(void* ptr)
    {
        if (ptr != null)
            GCHandle.FromIntPtr((nint)ptr).Free();
    }
}
