using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

// Attributes for each built-in flecs trait. 

// --- Storage / behavior markers ---

/// <summary>Store this component in a sparse set rather than in tables (<c>EcsSparse</c>).</summary>
public sealed class SparseAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsSparse);
}

/// <summary>Use union storage for this relationship (<c>EcsUnion</c>).</summary>
public sealed class UnionAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsUnion);
}

/// <summary>Allow this component to be enabled/disabled without a table move (<c>EcsCanToggle</c>).</summary>
public sealed class CanToggleAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsCanToggle);
}

/// <summary>Forbid this entity from being inherited/specialized (<c>EcsFinal</c>).</summary>
public sealed class FinalAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsFinal);
}

/// <summary>Treat pairs with this relationship as tags carrying no data (<c>EcsPairIsTag</c>).</summary>
public sealed class PairIsTagAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsPairIsTag);
}

/// <summary>Mark this component as inheritable by instances (<c>EcsInheritable</c>).</summary>
public sealed class InheritableAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsInheritable);
}

// --- Relationship-property markers ---

/// <summary>Mark this relationship transitive (<c>EcsTransitive</c>).</summary>
public sealed class TransitiveAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsTransitive);
}

/// <summary>Mark this relationship reflexive (<c>EcsReflexive</c>).</summary>
public sealed class ReflexiveAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsReflexive);
}

/// <summary>Mark this relationship acyclic (<c>EcsAcyclic</c>).</summary>
public sealed class AcyclicAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsAcyclic);
}

/// <summary>Mark this relationship traversable by queries (<c>EcsTraversable</c>).</summary>
public sealed class TraversableAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsTraversable);
}

/// <summary>Mark this relationship symmetric (<c>EcsSymmetric</c>).</summary>
public sealed class SymmetricAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsSymmetric);
}

/// <summary>Allow an entity only one instance of this relationship at a time (<c>EcsExclusive</c>).</summary>
public sealed class ExclusiveAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsExclusive);
}

// --- Identity markers ---

/// <summary>Mark this type as itself a trait (<c>EcsTrait</c>).</summary>
public sealed class IsTraitAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsTrait);
}

/// <summary>Mark this type as a relationship (<c>EcsRelationship</c>).</summary>
public sealed class IsRelationshipAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsRelationship);
}

/// <summary>Mark this type as a relationship target (<c>EcsTarget</c>).</summary>
public sealed class IsTargetAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsTarget);
}

// --- Parameterized built-ins ---

/// <summary>
/// How a component is propagated to instances created from a prefab/base.
/// </summary>
public enum Instantiate
{
    /// <summary>
    /// Instances inherit the component from the base (<c>EcsInherit</c>).
    /// </summary>
    Inherit,

    /// <summary>
    /// Instances get their own copy of the component (<c>EcsOverride</c>).
    /// </summary>
    Override,

    /// <summary>
    /// Instances do not receive the component (<c>EcsDontInherit</c>).
    /// </summary>
    DontInherit,
}

/// <summary>
/// What happens to entities/components when a (target) entity is deleted.
/// </summary>
public enum CleanupAction
{
    /// <summary>
    /// Remove the instance of the relationship (<c>EcsRemove</c>).
    /// </summary>
    Remove,

    /// <summary>
    /// Delete entities holding the relationship (<c>EcsDelete</c>).
    /// </summary>
    Delete,

    /// <summary>
    /// Throw/panic if anything still holds the relationship (<c>EcsPanic</c>).
    /// </summary>
    Panic,
}

/// <summary>
///     Control how this component propagates to instances on instantiation
///     (<c>(EcsOnInstantiate, …)</c>).
/// </summary>
public sealed class OnInstantiateAttribute(Instantiate mode) : ComponentTraitAttribute
{
    private readonly Instantiate _mode = mode;

    internal override void Apply(World world, Entity component) =>
        world.AddPair(component, EcsOnInstantiate, _mode switch
        {
            Instantiate.Inherit => EcsInherit,
            Instantiate.Override => EcsOverride,
            Instantiate.DontInherit => EcsDontInherit,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), _mode, "Unknown instantiate mode."),
        });
}

/// <summary>
///     Set the cleanup policy applied when an instance of this relationship's
///     target is deleted (<c>(EcsOnDelete, …)</c>).
/// </summary>
public sealed class OnDeleteAttribute(CleanupAction action) : ComponentTraitAttribute
{
    private readonly CleanupAction _action = action;

    internal override void Apply(World world, Entity component) =>
        world.AddPair(component, EcsOnDelete, CleanupId(_action));

    internal static ulong CleanupId(CleanupAction action) => action switch
    {
        CleanupAction.Remove => EcsRemove,
        CleanupAction.Delete => EcsDelete,
        CleanupAction.Panic => EcsPanic,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown cleanup action."),
    };
}

/// <summary>
///     Set the cleanup policy applied to the targets of this relationship when
///     the relationship entity is deleted (<c>(EcsOnDeleteTarget, …)</c>).
/// </summary>
public sealed class OnDeleteTargetAttribute(CleanupAction action) : ComponentTraitAttribute
{
    private readonly CleanupAction _action = action;

    internal override void Apply(World world, Entity component) =>
        world.AddPair(component, EcsOnDeleteTarget, OnDeleteAttribute.CleanupId(_action));
}

/// <summary>
///     Require entities that get this component to also get <typeparamref name="TTarget"/>
///     (<c>(EcsWith, TTarget)</c>).
/// </summary>
public sealed class WithAttribute<TTarget> : ComponentTraitAttribute where TTarget : unmanaged
{
    internal override void Apply(World world, Entity component) =>
        world.AddPair(component, EcsWith, world.Component<TTarget>());
}

/// <summary>
///     Constrain this relationship's targets to children of the relationship
///     itself (<c>EcsOneOf</c>). Use <see cref="OneOfAttribute{TScope}"/> to scope
///     targets to a different entity instead.
/// </summary>
public sealed class OneOfAttribute : ComponentTraitAttribute
{
    internal override void Apply(World world, Entity component) => world.Add(component, EcsOneOf);
}

/// <summary>
///     Constrain this relationship's targets to children of <typeparamref name="TScope"/>
///     (<c>(EcsOneOf, TScope)</c>).
/// </summary>
public sealed class OneOfAttribute<TScope> : ComponentTraitAttribute where TScope : unmanaged
{
    internal override void Apply(World world, Entity component) =>
        world.AddPair(component, EcsOneOf, world.Component<TScope>());
}
