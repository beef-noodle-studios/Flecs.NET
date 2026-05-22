namespace NoodleStudios.Flecs.Pairs;

/// <summary>
///     Read-only struct for returning references to values in a pair.
/// </summary>
/// <typeparam name="TRelation">
///     The type of the relation component.
///     This is the 'first' element of the pair.
/// </typeparam>
/// <typeparam name="TTarget">
///     The type of the target component.
///     This is the 'second' element of the pair.
/// </typeparam>
public readonly ref struct ReadOnlyPair<TRelation, TTarget>
{
    /// <summary>
    ///     The relation component of the pair.
    /// </summary>
    public readonly ref TRelation Relation;

    /// <summary>
    ///     The target component of the pair.
    /// </summary>
    public readonly ref TTarget Target;

    public ReadOnlyPair(ref TRelation relation, ref TTarget target)
    {
        Relation = ref relation;
        Target = ref target;
    }
}
