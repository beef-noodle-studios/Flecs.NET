namespace NoodleStudios.Flecs.Facades;

public static class FluentApi
{
    public static FluentEntityApi Wrap(World world, Entity entity) => new(world, entity);
    public static ReadOnlyFluentEntityApi Wrap(ReadOnlyWorld world, Entity entity) => new(world, entity);
}
