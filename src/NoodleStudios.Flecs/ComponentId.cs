using System.Reflection;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     Resolves the component id for a managed type <typeparamref name="T"/> in a
///     given world, registering it with Flecs on first use and caching the result.
/// </summary>
/// <remarks>
///     <para>
///         A per-type static "fast slot" caches the most recently used
///         <c>(world, id)</c> pair so the common single-world case is a couple of
///         field reads. On a world mismatch it falls back to the world's
///         <see cref="ComponentRegistry"/>, which is authoritative; the fast slot
///         is only ever a hint, so a world pointer reused after <c>ecs_fini</c>
///         cannot return a stale id for a different world.
///     </para>
///     <para>
///         Only blittable (<c>unmanaged</c>) types are supported. A field-less
///         struct registers as a one-byte component (C# has no zero-sized
///         structs); use a plain entity as a tag if a true zero-sized tag is
///         required.
///     </para>
/// </remarks>
internal static unsafe class ComponentId<T> where T : unmanaged
{
    // The cache is keyed by the world's registry instance, not its native
    // pointer: an ecs_world_t* address is recycled by ecs_init after ecs_fini, so
    // a pointer cannot distinguish a disposed world from a new one at the same
    // address. Each live world has its own registry, created fresh in its binding
    // context, so a reference comparison reliably detects a different world while
    // still letting the common single-world case skip the dictionary lookup.
    private static ComponentRegistry? _cachedRegistry;
    private static Id _cachedId;

    private static readonly ComponentTraitAttribute[] Traits =
        typeof(T).GetCustomAttributes<ComponentTraitAttribute>(inherit: false).ToArray();

    /// <summary>
    ///     Get the id of <typeparamref name="T"/> in <paramref name="world"/>,
    ///     registering it if necessary.
    /// </summary>
    public static Id GetId(ecs_world_t* world)
    {
        ComponentRegistry registry = BindingContext.GetRegistry(world);
        if (ReferenceEquals(registry, _cachedRegistry) && _cachedId != Id.None)
            return _cachedId;

        if (!registry.TryGetId(typeof(T), out var id))
            id = Register(world, registry);

        _cachedRegistry = registry;
        _cachedId = id;
        return _cachedId;
    }

    /// <summary>
    ///     Get the id of <typeparamref name="T"/> in <paramref name="world"/> only
    ///     if it has already been registered, without registering it. Used by
    ///     read-only paths that must not mutate the world.
    /// </summary>
    public static bool TryGetId(ecs_world_t* world, out Id id)
    {
        ComponentRegistry? registry = BindingContext.TryGetRegistry(world);
        if (registry == null)
        {
            id = Id.None;
            return false;
        }

        if (ReferenceEquals(registry, _cachedRegistry) && _cachedId != Id.None)
        {
            id = _cachedId;
            return true;
        }

        if (registry.TryGetId(typeof(T), out id))
        {
            _cachedRegistry = registry;
            _cachedId = id;
            return true;
        }

        id = Id.None;
        return false;
    }

    private static Id Register(ecs_world_t* world, ComponentRegistry registry)
    {
        // The symbol (fully-qualified type name) is Flecs's stable dedup key: if
        // an entity with this symbol already exists, ecs_entity_init reuses it and
        // ecs_component_init asserts the size/alignment match, making registration
        // idempotent per world regardless of the path it was reached through.
        byte* name = Utf8.Encode(typeof(T).Name);
        byte* symbol = Utf8.Encode(typeof(T).FullName);
        try
        {
            ecs_entity_desc_t entityDesc = default;
            entityDesc.name = name;
            entityDesc.symbol = symbol;
            entityDesc.use_low_id = true;
            ulong entity = ecs_entity_init(world, &entityDesc);

            ecs_component_desc_t componentDesc = default;
            componentDesc.entity = entity;
            componentDesc.type.size = sizeof(T);
            componentDesc.type.alignment = AlignOf();
            Id id = ecs_component_init(world, &componentDesc);

            // Record the finalized id before applying traits to avoid endless recursion.
            registry.Store(typeof(T), id);

            ComponentTraits.Apply(World.FromNativeWorldHandle(world), new Entity(id), Traits);
            return id;
        }
        finally
        {
            Utf8.Free(name);
            Utf8.Free(symbol);
        }
    }

    /// <summary>
    ///     The alignment of <typeparamref name="T"/>, derived from the offset the
    ///     CLR gives a <typeparamref name="T"/> field placed after a single byte.
    /// </summary>
    private static int AlignOf() => sizeof(AlignHelper) - sizeof(T);

    // Fields exist purely to measure T's alignment via the offset of Value; they
    // are never read or written.
#pragma warning disable CS0649
    private struct AlignHelper
    {
        public byte Pad;
        public T Value;
    }
#pragma warning restore CS0649
}
