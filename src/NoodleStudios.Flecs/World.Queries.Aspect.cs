using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace NoodleStudios.Flecs;

public unsafe readonly partial struct World
{
    /// <summary>
    ///     Begin building a typed query whose iteration binds the byref fields
    ///     of <typeparamref name="TAspect"/> per row.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The aspect's descriptor seeds one accessor term per byref field
    ///         in declaration order, then the aspect's struct-level
    ///         <see cref="WithAttribute"/>, <see cref="WithoutAttribute"/>, and
    ///         <see cref="AnyAttribute"/> apply as matching-only terms. Each
    ///         component used by the aspect (and any relationship referenced by
    ///         an <c>[Up*]</c> attribute) must already be registered in this
    ///         world. 
    ///     </para>
    /// </remarks>
    public QueryBuilder<TAspect> CreateQuery<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        TAspect>()
        where TAspect : IAspect, allows ref struct
    {
        AspectDescriptor descriptor = AspectDescriptor<TAspect>.Instance;
        ComponentRegistry registry = BindingContext.GetRegistry(_handle);
        QueryBuilder inner = CreateQuery();
        int[] slotToTerm = new int[descriptor.FieldSlotCount];

        // Accessor terms are seeded first so each accessor slot's SeedTermIndex
        // matches its position in the resulting query's term array. Adding
        // struct-level matching-only terms afterwards keeps the captured map
        // valid no matter how many [With]/[Without]/[Any] terms follow.
        foreach (AspectSlot slot in descriptor.Slots)
        {
            if (slot.Kind != AspectSlotKind.ComponentAccessor)
                continue;

            Id componentId = ResolveComponentId(registry, slot.ComponentType!);
            SeedAccessor(ref inner, slot, componentId, registry);
            slotToTerm[slot.SeedTermIndex] = inner.TermCount - 1;
        }

        ApplyStructAttributes(ref inner, registry, typeof(TAspect));

        return new QueryBuilder<TAspect>(inner, slotToTerm, descriptor);
    }

    private static void SeedAccessor(
        ref QueryBuilder inner,
        AspectSlot slot,
        Id componentId,
        ComponentRegistry registry)
    {
        if (slot.Optional)
            inner.Optional(componentId);
        else
            inner.With(componentId);

        switch (slot.Sourcing)
        {
            case SourcingKind.Self:
                if (slot.Self)
                    inner.Self();
                break;
            case SourcingKind.Up:
                if (slot.Relationship is null)
                    inner.Up();
                else
                    inner.Up(ResolveRelationshipId(registry, slot.Relationship));
                break;
            case SourcingKind.UpAncestorsFirst:
                if (slot.Relationship is null)
                    inner.UpAncestorsFirst();
                else
                    inner.UpAncestorsFirst(ResolveRelationshipId(registry, slot.Relationship));
                break;
            case SourcingKind.UpDescendantsFirst:
                if (slot.Relationship is null)
                    inner.UpDescendantsFirst();
                else
                    inner.UpDescendantsFirst(ResolveRelationshipId(registry, slot.Relationship));
                break;
            case SourcingKind.Singleton:
                // A singleton sources from the entity that shares the component's
                // id, so every matched row reads the same value.
                inner.Source(new Entity(componentId.Value));
                break;
        }

        switch (slot.RefKind)
        {
            case AspectRefKind.In:
                inner.In();
                break;
            case AspectRefKind.InOut:
                inner.InOut();
                break;
        }
    }

    private static void ApplyStructAttributes(
        ref QueryBuilder inner,
        ComponentRegistry registry,
        Type aspectType)
    {
        foreach (WithAttribute with in aspectType.GetCustomAttributes<WithAttribute>(inherit: false))
        {
            Id first = ResolveComponentId(registry, with.Component);
            if (with.Target is null)
                inner.With(first).None();
            else
                inner.With(first, ResolveComponentId(registry, with.Target)).None();
        }

        foreach (WithoutAttribute without in aspectType.GetCustomAttributes<WithoutAttribute>(inherit: false))
            inner.Without(ResolveComponentId(registry, without.Component));

        foreach (AnyAttribute any in aspectType.GetCustomAttributes<AnyAttribute>(inherit: false))
        {
            Type[] components = any.Components;
            if (components.Length == 0)
                continue;

            for (int i = 0; i < components.Length; i++)
            {
                Id id = ResolveComponentId(registry, components[i]);
                inner.With(id);
                if (i < components.Length - 1)
                    inner.Or();
            }
        }
    }

    private static Id ResolveComponentId(ComponentRegistry registry, Type type)
    {
        if (registry.TryGetId(type, out Id id))
            return id;

        throw new InvalidOperationException(
            $"Component '{type.Name}' is not registered in this world.");
    }

    private static Id ResolveRelationshipId(ComponentRegistry registry, Type type)
    {
        if (registry.TryGetId(type, out Id id))
            return id;

        throw new InvalidOperationException(
            $"Relationship '{type.Name}' is not registered in this world.");
    }
}
