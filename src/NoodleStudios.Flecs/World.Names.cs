using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

public unsafe partial struct World
{
    // Naming and lookup.
    // https://www.flecs.dev/flecs/group__entities.html

    /// <summary>
    ///     Set the name of an entity.
    /// </summary>
    /// <remarks>
    ///     The name is used to look the entity up by path and is shown in
    ///     tooling. Passing null clears the name.
    /// </remarks>
    public void SetName(Entity entity, string? name)
    {
        byte* native = Utf8.Encode(name);
        try
        {
            ecs_set_name(_handle, entity, native);
        }
        finally
        {
            Utf8.Free(native);
        }
    }

    /// <summary>
    ///     Get the name of an entity.
    /// </summary>
    /// <returns>
    ///     The entity's name, or null if it has none.
    /// </returns>
    public string? GetName(Entity entity) => Utf8.Decode(ecs_get_name(_handle, entity));

    /// <summary>
    ///     Get the symbol of an entity.
    /// </summary>
    /// <remarks>
    ///     Components are registered with their fully-qualified type name as their
    ///     symbol; this is the stable, scope-independent identifier Flecs uses to
    ///     deduplicate registrations.
    /// </remarks>
    /// <returns>
    ///     The entity's symbol, or null if it has none.
    /// </returns>
    public string? GetSymbol(Entity entity) => Utf8.Decode(ecs_get_symbol(_handle, entity));

    /// <summary>
    ///     Look up an entity by name or path.
    /// </summary>
    /// <param name="path">
    ///     The name, or a path of names separated by <c>.</c> for nested entities.
    /// </param>
    /// <returns>
    ///     The matching entity, or <see cref="Entity.None"/> if none was found.
    /// </returns>
    public Entity Lookup(string path)
    {
        byte* native = Utf8.Encode(path);
        try
        {
            return new Entity(ecs_lookup(_handle, native));
        }
        finally
        {
            Utf8.Free(native);
        }
    }
}
