using System.Runtime.CompilerServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    // Component and tag operations.
    // https://www.flecs.dev/flecs/group__adding__removing.html
    //
    // Each operation comes in two forms: a generic form whose type argument names
    // the component (its id is resolved and registered on first use), and an
    // id-based form that takes an explicit Id. The id form is what makes pairs and
    // runtime-resolved ids work; the generic form is sugar over it. The component
    // is always a single type argument and the entity is always a normal argument,
    // so there is never ambiguity about which is which.

    // --- Id-based core -----------------------------------------------------

    /// <summary>
    ///     Add an id to an entity. Adding an id the entity already has is a no-op.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Entity entity, Id id) => ecs_add_id(_handle, entity, id);

    /// <summary>
    ///     Remove an id from an entity. Removing an id the entity does not have is
    ///     a no-op.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove(Entity entity, Id id) => ecs_remove_id(_handle, entity, id);

    /// <summary>
    ///     Test whether an entity has an id.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Entity entity, Id id) => ecs_has_id(_handle, entity, id);

    /// <summary>
    ///     Set the value of a component (identified by <paramref name="id"/>) on an
    ///     entity, adding it if the entity does not already have it. The value is
    ///     copied into the entity's storage.
    /// </summary>
    /// <remarks>
    ///     The data type <typeparamref name="T"/> must match the component the id
    ///     refers to; Flecs validates the size against the registered layout.
    /// </remarks>
    internal void Set<T>(Entity entity, Id id, in T data) where T : unmanaged
    {
        fixed (T* ptr = &data)
        {
            ecs_set_id(_handle, entity, id, (nint)sizeof(T), ptr);
        }
    }

    /// <summary>
    ///     Get a read-only reference to a component value on an entity.
    /// </summary>
    /// <remarks>
    ///     The reference points directly into the entity's storage and is
    ///     invalidated by any structural change (adding/removing components,
    ///     deleting entities). Do not hold it across such operations.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///     The entity does not have the component.
    /// </exception>
    public ref readonly T Get<T>(Entity entity, Id id) where T : unmanaged
    {
        void* ptr = ecs_get_id(_handle, entity, id);
        if (ptr == null)
            throw new InvalidOperationException(
                $"Entity {(ulong)entity} does not have component '{typeof(T).Name}'.");

        return ref Unsafe.AsRef<T>(ptr);
    }

    /// <summary>
    ///     Get a mutable reference to a component value on an entity, adding the
    ///     component (default-initialized) if the entity does not already have it.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The reference points directly into the entity's storage and is
    ///         invalidated by any structural change. Do not hold it across such
    ///         operations.
    ///     </para>
    ///     <para>
    ///         This does not signal a change to Flecs. Call
    ///         <see cref="Modified(Entity, Id)"/> afterwards if change detection or
    ///         observers should be notified.
    ///     </para>
    /// </remarks>
    public ref T GetMut<T>(Entity entity, Id id) where T : unmanaged
    {
        void* ptr = ecs_ensure_id(_handle, entity, id);
        return ref Unsafe.AsRef<T>(ptr);
    }

    /// <summary>
    ///     Copy out a component value if the entity has it.
    /// </summary>
    /// <returns>
    ///     True and the value if present; otherwise false and the default value.
    /// </returns>
    public bool TryGet<T>(Entity entity, Id id, out T value) where T : unmanaged
    {
        void* ptr = ecs_get_id(_handle, entity, id);
        if (ptr == null)
        {
            value = default;
            return false;
        }

        value = Unsafe.Read<T>(ptr);
        return true;
    }

    /// <summary>
    ///     Signal that a component on an entity has been modified, triggering
    ///     change detection and observers.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Modified(Entity entity, Id id) => ecs_modified_id(_handle, entity, id);

    // --- Generic convenience ----------------------------------------------

    /// <inheritdoc cref="Add(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(Entity entity) where T : unmanaged =>
        ecs_add_id(_handle, entity, ComponentId<T>.GetId(_handle));

    /// <summary>
    ///     Add an <see cref="Id"/> with value <typeparamref name="T"/> to
    ///     <paramref name="entity"/>. Does nothing if the entity already has
    ///     the id.
    /// </summary>
    /// <remarks>
    ///     This function has no effects if the entity already has the id.
    /// </remarks>
    /// <typeparam name="T">
    ///     The type of component to add to the entity.
    /// </typeparam>
    /// <param name="entity">
    ///     The entity to which the component should be added.
    /// </param>
    /// <param name="value">
    ///     The value to set on the component.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(Entity entity, T value)
        where T : unmanaged
    {
        var id = ComponentId<T>.GetId(_handle);
        if (Has(entity, id))
            return;

        Set(entity, id, value);
    }

    /// <inheritdoc cref="Remove(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Remove<T>(Entity entity) where T : unmanaged =>
        ecs_remove_id(_handle, entity, ComponentId<T>.GetId(_handle));

    /// <inheritdoc cref="Has(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has<T>(Entity entity) where T : unmanaged =>
        ecs_has_id(_handle, entity, ComponentId<T>.GetId(_handle));

    /// <summary>
    ///     Set the value of component <typeparamref name="T"/> on an entity, adding
    ///     it if the entity does not already have it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(Entity entity, in T data) where T : unmanaged =>
        Set(entity, new Id(ComponentId<T>.GetId(_handle)), in data);

    /// <inheritdoc cref="Get{T}(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T Get<T>(Entity entity) where T : unmanaged =>
        ref Get<T>(entity, new Id(ComponentId<T>.GetId(_handle)));

    /// <inheritdoc cref="GetMut{T}(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetMut<T>(Entity entity) where T : unmanaged =>
        ref GetMut<T>(entity, new Id(ComponentId<T>.GetId(_handle)));

    /// <inheritdoc cref="TryGet{T}(Entity, Id, out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet<T>(Entity entity, out T value) where T : unmanaged =>
        TryGet(entity, new Id(ComponentId<T>.GetId(_handle)), out value);

    /// <inheritdoc cref="Modified(Entity, Id)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Modified<T>(Entity entity) where T : unmanaged =>
        ecs_modified_id(_handle, entity, ComponentId<T>.GetId(_handle));

    /// <summary>
    ///     Get the component entity for <typeparamref name="T"/>, registering it
    ///     in this world on first use.
    /// </summary>
    /// <remarks>
    ///     A component is itself an entity, so the returned <see cref="FluentApi"/>
    ///     can be named, annotated, or used as a relationship or target in a pair.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Component<T>() where T : unmanaged => new(ComponentId<T>.GetId(_handle));
}
