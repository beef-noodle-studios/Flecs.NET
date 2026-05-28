namespace NoodleStudios.Flecs;

/// <summary>
///     Marker interface that identifies a type as an aspect, a <c>ref struct</c>
///     whose byref fields are bound per row during typed query iteration.
/// </summary>
/// <remarks>
///     <para>
///         An aspect is declared as a <c>ref struct</c> implementing this
///         interface. Its public and non-public instance fields describe the
///         per-row binding: an <c>Entity</c> field binds the matched entity, a
///         <see cref="TableView"/> field binds the per-table iteration handle,
///         and a <c>ref T</c> or <c>ref readonly T</c> field binds the matched
///         row's <c>T</c> component. Use the aspect as the type argument to
///         <see cref="World.CreateQuery{TAspect}"/> and iterate with
///         <c>foreach (ref readonly TAspect aspect in query)</c>.
///     </para>
///     <para>
///         The interface itself carries no members. Aspect shape is enforced by 
///         analyzer rules at compile time and by <c>AspectPlan</c> at runtime.
///     </para>
/// </remarks>
public interface IAspect
{
}
