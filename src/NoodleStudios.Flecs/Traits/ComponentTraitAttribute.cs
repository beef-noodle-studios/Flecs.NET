namespace NoodleStudios.Flecs;

/// <summary>
///     Base class for attributes that declare a built-in Flecs trait on a component type.
///     Placing one on a component <c>struct</c> causes the corresponding trait to
///     be added to that component's backing entity automatically when the component is
///     registered.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public abstract class ComponentTraitAttribute : Attribute
{
    internal abstract void Apply(World world, Entity component);
}
