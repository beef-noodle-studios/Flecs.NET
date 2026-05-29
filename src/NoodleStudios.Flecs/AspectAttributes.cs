namespace NoodleStudios.Flecs;

// Attributes that describe how an aspect's terms are seeded into the query. The
// field-level attributes refine the term seeded for the field they decorate. The
// struct-level attributes add extra matching-only terms to the query.
//
// All attributes take Type / typeof(...) arguments rather than generics so that
// the runtime path resolves them through ComponentRegistry.TryGetId without
// MakeGenericMethod, which is AOT-hostile.

// --- Field-level refinements ---

/// <summary>
///     Match the field's component optionally. The aspect field is bound to a
///     null reference on tables where the component is absent. Guard the read
///     with <see cref="Field.HasValue{T}(in T)"/> before dereferencing.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class OptionalAttribute : Attribute
{
}

/// <summary>
///     Source this field by traversing <paramref name="relationship"/> upward
///     from the matched entity. The default <paramref name="relationship"/> is
///     <c>ChildOf</c>, matching the field from an ancestor. Lowers to
///     <see cref="QueryBuilder.Up()"/> (or its <c>Id</c> overload) on the term.
/// </summary>
/// <remarks>
///     Mutually exclusive with <see cref="SelfAttribute"/> and
///     <see cref="SingletonAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class UpAttribute : Attribute
{
    /// <summary>
    ///     The relationship traversed upward. Null defaults to <c>ChildOf</c> at
    ///     lowering time.
    /// </summary>
    public Type? Relationship { get; }

    /// <summary>
    ///     Traverse <c>ChildOf</c> upward.
    /// </summary>
    public UpAttribute() => Relationship = null;

    /// <summary>
    ///     Traverse <paramref name="relationship"/> upward. The relationship must
    ///     be traversable for the field's component.
    /// </summary>
    public UpAttribute(Type relationship) => Relationship = relationship;
}

/// <summary>
///     As <see cref="UpAttribute"/>, but additionally order the matched tables
///     by hierarchy depth so an entity's ancestors are iterated before the
///     entity itself. Lowers to <see cref="QueryBuilder.UpAncestorsFirst()"/>.
/// </summary>
/// <remarks>
///     This ordering is only available on a cached query
///     (<see cref="QueryBuilder.BuildCached"/>). Building it uncached fails.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class UpAncestorsFirstAttribute : Attribute
{
    /// <summary>
    ///     The relationship traversed upward. Null defaults to <c>ChildOf</c> at
    ///     lowering time.
    /// </summary>
    public Type? Relationship { get; }

    /// <summary>
    ///     Traverse <c>ChildOf</c> upward, ancestors first.
    /// </summary>
    public UpAncestorsFirstAttribute() => Relationship = null;

    /// <summary>
    ///     Traverse <paramref name="relationship"/> upward, ancestors first.
    /// </summary>
    public UpAncestorsFirstAttribute(Type relationship) => Relationship = relationship;
}

/// <summary>
///     The reverse of <see cref="UpAncestorsFirstAttribute"/>: traverse upward
///     but order the matched tables so an entity's descendants are iterated
///     before the entity itself. Lowers to
///     <see cref="QueryBuilder.UpDescendantsFirst()"/>.
/// </summary>
/// <remarks>
///     This ordering is only available on a cached query
///     (<see cref="QueryBuilder.BuildCached"/>). Building it uncached fails.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class UpDescendantsFirstAttribute : Attribute
{
    /// <summary>
    ///     The relationship traversed upward. Null defaults to <c>ChildOf</c> at
    ///     lowering time.
    /// </summary>
    public Type? Relationship { get; }

    /// <summary>
    ///     Traverse <c>ChildOf</c> upward, descendants first.
    /// </summary>
    public UpDescendantsFirstAttribute() => Relationship = null;

    /// <summary>
    ///     Traverse <paramref name="relationship"/> upward, descendants first.
    /// </summary>
    public UpDescendantsFirstAttribute(Type relationship) => Relationship = relationship;
}

/// <summary>
///     Source this field from the component's singleton entity, the entity that
///     shares the component's id. Every matched row reads the same value. Lowers
///     to <see cref="QueryBuilder.Source(Entity)"/> at the component's id.
/// </summary>
/// <remarks>
///     Mutually exclusive with <see cref="UpAttribute"/>,
///     <see cref="UpAncestorsFirstAttribute"/>,
///     <see cref="UpDescendantsFirstAttribute"/>, and
///     <see cref="SelfAttribute"/>.
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SingletonAttribute : Attribute
{
}

/// <summary>
///     Restrict this field's term to self matches, excluding any shared-source
///     match. Lowers to <see cref="QueryBuilder.Self()"/> on the term.
/// </summary>
/// <remarks>
///     <para>
///         Use <c>[Self]</c> on a writable (<c>ref</c>) field when the query may
///         match tables where the component is inherited or otherwise shared.
///         Writing through a shared-source field would mutate the source entity
///         shared across every inheriting row, so <c>[Self]</c> opts that table
///         out at match time.
///     </para>
///     <para>
///         Mutually exclusive with <see cref="UpAttribute"/>,
///         <see cref="UpAncestorsFirstAttribute"/>,
///         <see cref="UpDescendantsFirstAttribute"/>, and
///         <see cref="SingletonAttribute"/>.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class SelfAttribute : Attribute
{
}

// --- Struct-level matching constraints ---

/// <summary>
///     Add a matching-only term to the aspect's query that requires the matched
///     entity to also have <paramref name="component"/> or pair
///     <c>(first, target)</c>. The term carries no data, it only filters which
///     entities the aspect matches.
/// </summary>
/// <remarks>
///     <para>
///         Multiple <c>[With]</c> attributes stack. The order they apply
///         relative to accessor field terms follows the aspect's lowering rule:
///         every accessor term is seeded first, then struct-level attributes
///         apply, so the slot-to-field-index map is unchanged.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class WithAttribute : Attribute
{
    /// <summary>
    ///     The first element of the term: a component type, or the relationship
    ///     when <see cref="Target"/> is set.
    /// </summary>
    public Type Component { get; }

    /// <summary>
    ///     The target of the matched pair, or null for a non-pair term.
    /// </summary>
    public Type? Target { get; }

    /// <summary>
    ///     Match entities that have <paramref name="component"/>.
    /// </summary>
    public WithAttribute(Type component)
    {
        Component = component;
        Target = null;
    }

    /// <summary>
    ///     Match entities that have the pair
    ///     <c>(<paramref name="first"/>, <paramref name="target"/>)</c>.
    /// </summary>
    public WithAttribute(Type first, Type target)
    {
        Component = first;
        Target = target;
    }
}

/// <summary>
///     Add a matching-only term to the aspect's query that excludes entities
///     that have <paramref name="component"/>.
/// </summary>
/// <remarks>
///     Multiple <c>[Without]</c> attributes stack. As <see cref="WithAttribute"/>,
///     the term applies after every accessor term is seeded so it does not
///     disturb the slot-to-field-index map.
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class WithoutAttribute : Attribute
{
    /// <summary>
    ///     The component the matched entity must not have.
    /// </summary>
    public Type Component { get; }

    /// <summary>
    ///     Exclude entities that have <paramref name="component"/>.
    /// </summary>
    public WithoutAttribute(Type component) => Component = component;
}

/// <summary>
///     Add an OR group of matching-only terms to the aspect's query: the
///     matched entity must have at least one of <paramref name="components"/>.
/// </summary>
/// <remarks>
///     <para>
///         Multiple <c>[Any]</c> attributes stack, each contributing an
///         independent OR group. As <see cref="WithAttribute"/>, every accessor
///         term is seeded first so the slot-to-field-index map is unchanged.
///     </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class AnyAttribute : Attribute
{
    /// <summary>
    ///     The OR-group members. At least one must be present on the matched
    ///     entity.
    /// </summary>
    public Type[] Components { get; }

    /// <summary>
    ///     Require the matched entity to have at least one of
    ///     <paramref name="components"/>.
    /// </summary>
    public AnyAttribute(params Type[] components) => Components = components;
}
