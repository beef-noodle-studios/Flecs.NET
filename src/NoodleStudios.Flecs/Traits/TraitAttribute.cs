namespace NoodleStudios.Flecs;

/// <summary>
///     Add the id of <typeparamref name="T"/> to the component entity as a trait.
/// </summary>
public sealed class TraitAttribute<T> : ComponentTraitAttribute where T : unmanaged
{
    internal override void Apply(World world, Entity component) =>
        world.Add(component, world.Component<T>());
}

/// <summary>
///     Add the pair <c>(<typeparamref name="TRel"/>, <typeparamref name="TTarget"/>)</c>
///     to the component entity as a trait.
/// </summary>
public sealed class TraitAttribute<TRel, TTarget> : ComponentTraitAttribute
    where TRel : unmanaged
    where TTarget : unmanaged
{
    internal override void Apply(World world, Entity component) =>
        world.AddPair(component, world.Component<TRel>(), world.Component<TTarget>());
}
