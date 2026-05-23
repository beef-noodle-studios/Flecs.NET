using System.Diagnostics;

namespace NoodleStudios.Flecs;

/// <summary>
///     Applies the <see cref="ComponentTraitAttribute"/>s declared on a component
///     type to that component's entity during registration.
/// </summary>
internal static class ComponentTraits
{
    /// <summary>
    ///     Add every declared trait in <paramref name="traits"/> to
    ///     <paramref name="component"/>'s entity.
    /// </summary>
    public static void Apply(World world, Entity component, ComponentTraitAttribute[] traits)
    {
        Validate(traits);
        foreach (ComponentTraitAttribute trait in traits)
            trait.Apply(world, component);
    }

    /// <summary>
    ///     Reject combinations that would violate a native Flecs assert and abort the process.
    ///
    ///     Debug-only
    /// </summary>
    [Conditional("DEBUG")]
    private static void Validate(ComponentTraitAttribute[] traits)
    {
        bool sparse = false;
        bool union = false;
        foreach (ComponentTraitAttribute trait in traits)
        {
            sparse |= trait is SparseAttribute;
            union |= trait is UnionAttribute;
        }

        if (sparse && union)
            throw new InvalidOperationException(
                "A component cannot be both [Sparse] and [Union]; they are mutually " +
                "exclusive storage modes.");
    }
}
