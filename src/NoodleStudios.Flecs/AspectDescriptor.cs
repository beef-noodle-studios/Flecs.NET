using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs;

/// <summary>
///     Per-<typeparamref name="TAspect"/> reflection cache. Building the
///     descriptor reflects on <typeparamref name="TAspect"/> once and stores the
///     result on a generic static field, so the cold path runs exactly once per
///     aspect type and the hot path reads from <see cref="Instance"/> with no
///     further reflection.
/// </summary>
internal static class AspectDescriptor<
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
    TAspect>
    where TAspect : struct, IAspect, allows ref struct
{
    /// <summary>
    ///     The shared descriptor for <typeparamref name="TAspect"/>, built once
    ///     on first access.
    /// </summary>
    public static readonly AspectDescriptor Instance =
        AspectDescriptor.Build(typeof(TAspect), Unsafe.SizeOf<TAspect>());
}

/// <summary>
///     Discriminates an aspect slot's role. A slot is either the matched 
///     <see cref="Entity"/>, the per-table <see cref="NoodleStudios.Flecs.TableView"/> 
///     handle, or a component accessor field bound to a row's data.
/// </summary>
internal enum AspectSlotKind
{
    Entity,
    TableView,
    ComponentAccessor,
}

/// <summary>
///     The access of a component accessor slot. <see cref="In"/> mirrors a
///     <c>ref readonly</c> field (a read), <see cref="InOut"/> a <c>ref</c>
///     field (read + write). 
/// </summary>
internal enum AspectRefKind
{
    In,
    InOut,
}

/// <summary>
///     The sourcing of a component accessor slot. <see cref="Self"/> is the
///     default (the term reads from the matched row's own entity). The other
///     values mirror the field-level sourcing attributes.
/// </summary>
internal enum SourcingKind
{
    Self,
    Up,
    UpAncestorsFirst,
    UpDescendantsFirst,
    Singleton,
}

/// <summary>
///     One field slot in an aspect's reflection descriptor. 
/// </summary>
internal readonly struct AspectSlot(int slotIndex, int offset, AspectSlotKind kind)
{
    /// <summary>
    ///     The slot's position in the aspect's declared field order. Doubles as
    ///     the index in <see cref="AspectDescriptor.Slots"/>.
    /// </summary>
    public int SlotIndex { get; } = slotIndex;

    /// <summary>
    ///     The slot's byte offset from the aspect's base pointer
    ///     (<c>slotIndex * 8</c> on the 64-bit-only target)
    /// </summary>
    public int Offset { get; } = offset;

    /// <summary>
    ///     The slot's role.
    /// </summary>
    public AspectSlotKind Kind { get; } = kind;

    /// <summary>
    ///     The declaring field's name. Used in diagnostics that point back at the
    ///     specific aspect field that triggered them.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    ///     The component type of a <see cref="AspectSlotKind.ComponentAccessor"/>
    ///     slot. Null on every other slot kind.
    /// </summary>
    public Type? ComponentType { get; init; }

    /// <summary>
    ///     The ref kind of a component accessor slot. Meaningful only when
    ///     <see cref="Kind"/> is <see cref="AspectSlotKind.ComponentAccessor"/>.
    /// </summary>
    public AspectRefKind RefKind { get; init; }

    /// <summary>
    ///     True if the accessor field carries <c>[Optional]</c>.
    /// </summary>
    public bool Optional { get; init; }

    /// <summary>
    ///     True if the accessor field carries <c>[Self]</c>.
    /// </summary>
    public bool Self { get; init; }

    /// <summary>
    ///     The accessor field's sourcing.
    /// </summary>
    public SourcingKind Sourcing { get; init; }

    /// <summary>
    ///     The relationship traversed by an <c>[Up*]</c> sourced field. Null
    ///     means "default relationship" (<c>ChildOf</c>, resolved at lowering
    ///     time). Always null on a non-accessor slot, on a <c>Self</c>-sourced
    ///     slot, and on a <c>Singleton</c>-sourced slot.
    /// </summary>
    public Type? Relationship { get; init; }

    /// <summary>
    ///     The accessor's index among accessor slots (0..N-1, in declaration
    ///     order over accessor slots only). 
    /// </summary>
    public int SeedTermIndex { get; init; } = -1;
}

/// <summary>
///     The reflection descriptor for one aspect type. 
/// </summary>
internal sealed class AspectDescriptor
{
    /// <summary>
    ///     All slots in declaration order. Includes the <see cref="Entity"/>
    ///     and <see cref="NoodleStudios.Flecs.TableView"/> slots, if any.
    /// </summary>
    public required ImmutableArray<AspectSlot> Slots { get; init; }

    /// <summary>
    ///     The count of <see cref="AspectSlotKind.ComponentAccessor"/> slots.
    /// </summary>
    public required int FieldSlotCount { get; init; }

    /// <summary>
    ///     The aspect's size in bytes (<see cref="Unsafe.SizeOf{T}"/> at the
    ///     generic site). Equal to <c>Slots.Length * 8</c> for our 64-bit target.
    /// </summary>
    public required int Size { get; init; }

    /// <summary>
    ///     Reflect <paramref name="aspectType"/> into an immutable descriptor.
    /// </summary>
    /// <param name="aspectType">
    ///     The aspect type to describe. Must be a <c>ref struct</c> implementing
    ///     <see cref="IAspect"/> with sequential layout and pointer-width fields.
    /// </param>
    /// <param name="sizeOf">
    ///     <see cref="Unsafe.SizeOf{T}"/> evaluated at the generic site. Passed
    ///     down because <c>Marshal.SizeOf</c> rejects ref structs. 
    /// </param>
    internal static AspectDescriptor Build(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
        Type aspectType,
        int sizeOf)
    {
        // Slot offsets are computed at expected pointer-width (8 bytes).
        if (IntPtr.Size != 8)
            throw new PlatformNotSupportedException(
                "Aspects are supported only on 64-bit targets: their field slots are laid "
                + "out at 8-byte (pointer-width) offsets.");

        ValidateLayout(aspectType);

        FieldInfo[] fields = aspectType.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        var slots = ImmutableArray.CreateBuilder<AspectSlot>(fields.Length);
        int accessorCount = 0;

        for (int i = 0; i < fields.Length; i++)
        {
            FieldInfo field = fields[i];

            // Each field is one pointer-width slot, placed at its declaration index.
            // The per-row binder writes raw pointers at these offsets, so it is
            // memory-safety-critical that the runtime lays the fields out in declaration 
            // order. 
            //
            // [StructLayout(LayoutKind.Sequential)] (enforced by ValidateLayout) is what 
            // guarantees that. If a field were ever reordered, every binding past it would 
            // land at the wrong offset.
            int offset = i * sizeof(long); // 64-bit-only target
            Type fieldType = field.FieldType;

            if (fieldType == typeof(Entity))
            {
                EnsureNoAccessorAttributes(aspectType, field, "Entity");
                slots.Add(new AspectSlot(i, offset, AspectSlotKind.Entity) { Name = field.Name });
            }
            else if (fieldType == typeof(TableView))
            {
                EnsureNoAccessorAttributes(aspectType, field, "TableView");
                slots.Add(new AspectSlot(i, offset, AspectSlotKind.TableView) { Name = field.Name });
            }
            else if (fieldType.IsByRef)
            {
                slots.Add(BuildAccessorSlot(aspectType, field, i, offset, accessorCount));
                accessorCount++;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Aspect '{aspectType.FullName}' field '{field.Name}' has unsupported type " +
                    $"'{fieldType.FullName}'. An aspect field must be a by-value Entity, a by-value " +
                    "TableView, or a ref / ref readonly to a non-ref-struct component value type.");
            }
        }

        int expectedSize = slots.Count * sizeof(long);
        if (sizeOf != expectedSize)
        {
            throw new InvalidOperationException(
                $"Aspect '{aspectType.FullName}' size mismatch: Unsafe.SizeOf reports {sizeOf} bytes, " +
                $"expected {expectedSize} ({slots.Count} slots × 8 bytes). An aspect must carry only " +
                "pointer-width fields under [StructLayout(LayoutKind.Sequential)] on 64-bit-only.");
        }

        if (accessorCount > FLECS_TERM_COUNT_MAX)
        {
            throw new InvalidOperationException(
                $"Aspect '{aspectType.FullName}' declares {accessorCount} component accessor fields, " +
                $"but a query is limited to {FLECS_TERM_COUNT_MAX} terms. Reduce the number of accessor fields.");
        }

        return new AspectDescriptor
        {
            Slots = slots.MoveToImmutable(),
            FieldSlotCount = accessorCount,
            Size = sizeOf,
        };
    }

    private static void ValidateLayout(Type aspectType)
    {
        // GetCustomAttributes / IsDefined return empty for 
        // SequentialLayout / ExplicitLayout. Type.StructLayoutAttribute 
        // reads the type-def's intrinsic layout flag and works in JIT and AOT.
        StructLayoutAttribute? layout = aspectType.StructLayoutAttribute;
        if (layout is null || layout.Value != LayoutKind.Sequential)
        {
            string found = layout is null ? "none" : layout.Value.ToString();
            throw new InvalidOperationException(
                $"Aspect '{aspectType.FullName}' must be declared with " +
                $"[StructLayout(LayoutKind.Sequential)]. Found: {found}. ");
        }
    }

    private static AspectSlot BuildAccessorSlot(
        Type aspectType,
        FieldInfo field,
        int slotIndex,
        int offset,
        int accessorOrdinal)
    {
        Type elementType = field.FieldType.GetElementType()!;

        // Reject ref-to-{reference type, ref struct, Entity, TableView}. The
        // whitelist is "ref / ref readonly to a non-byref value type that
        // isn't one of our by-value sentinels."
        if (!elementType.IsValueType || elementType.IsByRefLike
            || elementType == typeof(Entity) || elementType == typeof(TableView))
        {
            throw new InvalidOperationException(
                $"Aspect '{aspectType.FullName}' field '{field.Name}' is a ref to " +
                $"'{elementType.FullName}', which is not a component type. Only ref / " +
                "ref readonly to an unmanaged component value type is supported. " +
                "Bind Entity and TableView by value, not by ref.");
        }

        // A managed value type (a struct that holds object references) is stored
        // by flecs as raw bytes, so its references would not survive the round
        // trip. Only unmanaged components are bindable.
        if (!IsUnmanaged(elementType))
        {
            throw new InvalidOperationException(
                $"Aspect '{aspectType.FullName}' field '{field.Name}' is a ref to " +
                $"'{elementType.FullName}', which contains managed references. A " +
                "component accessor must be a ref / ref readonly to an unmanaged value type.");
        }

        AspectRefKind refKind = field.CustomAttributes
            .Any(a => a.AttributeType == typeof(IsReadOnlyAttribute))
            ? AspectRefKind.In
            : AspectRefKind.InOut;

        bool optional = field.IsDefined(typeof(OptionalAttribute), inherit: false);
        bool self = field.IsDefined(typeof(SelfAttribute), inherit: false);

        var up = (UpAttribute?)field.GetCustomAttribute(typeof(UpAttribute), inherit: false);
        var upAncestors = (UpAncestorsFirstAttribute?)field.GetCustomAttribute(
            typeof(UpAncestorsFirstAttribute), inherit: false);
        var upDescendants = (UpDescendantsFirstAttribute?)field.GetCustomAttribute(
            typeof(UpDescendantsFirstAttribute), inherit: false);
        bool singleton = field.IsDefined(typeof(SingletonAttribute), inherit: false);

        int sourcingCount = (up is not null ? 1 : 0)
            + (upAncestors is not null ? 1 : 0)
            + (upDescendants is not null ? 1 : 0)
            + (singleton ? 1 : 0);

        if (sourcingCount > 1)
        {
            throw new InvalidOperationException(
                $"Aspect '{aspectType.FullName}' field '{field.Name}' combines more than one " +
                "sourcing attribute. Use at most one of [Up], [UpAncestorsFirst], " +
                "[UpDescendantsFirst], [Singleton].");
        }

        if (self && sourcingCount > 0)
        {
            throw new InvalidOperationException(
                $"Aspect '{aspectType.FullName}' field '{field.Name}' combines [Self] with " +
                "[Up*] or [Singleton]. [Self] restricts a self-sourced term and is mutually " +
                "exclusive with explicit sourcing.");
        }

        SourcingKind sourcing = SourcingKind.Self;
        Type? relationship = null;
        if (up is not null)
        {
            sourcing = SourcingKind.Up;
            relationship = up.Relationship;
        }
        else if (upAncestors is not null)
        {
            sourcing = SourcingKind.UpAncestorsFirst;
            relationship = upAncestors.Relationship;
        }
        else if (upDescendants is not null)
        {
            sourcing = SourcingKind.UpDescendantsFirst;
            relationship = upDescendants.Relationship;
        }
        else if (singleton)
        {
            sourcing = SourcingKind.Singleton;
        }

        return new AspectSlot(slotIndex, offset, AspectSlotKind.ComponentAccessor)
        {
            Name = field.Name,
            ComponentType = elementType,
            RefKind = refKind,
            Optional = optional,
            Self = self,
            Sourcing = sourcing,
            Relationship = relationship,
            SeedTermIndex = accessorOrdinal,
        };
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "A component whose fields are trimmed degrades to a permissive result.")]
    private static bool IsUnmanaged(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type.IsPointer || type.IsFunctionPointer)
            return true;

        if (!type.IsValueType)
            return false;

        foreach (FieldInfo field in type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!IsUnmanaged(field.FieldType))
                return false;
        }

        return true;
    }

    private static readonly Type[] s_accessorOnlyAttributes =
    [
        typeof(OptionalAttribute),
        typeof(SelfAttribute),
        typeof(UpAttribute),
        typeof(UpAncestorsFirstAttribute),
        typeof(UpDescendantsFirstAttribute),
        typeof(SingletonAttribute),
    ];

    private static void EnsureNoAccessorAttributes(Type aspectType, FieldInfo field, string slotKind)
    {
        foreach (Type attrType in s_accessorOnlyAttributes)
        {
            if (field.IsDefined(attrType, inherit: false))
            {
                throw new InvalidOperationException(
                    $"Aspect '{aspectType.FullName}' field '{field.Name}' is a {slotKind} slot but " +
                    $"carries the accessor-only attribute [{TrimAttributeSuffix(attrType.Name)}]. " +
                    "Move the attribute to a component accessor field.");
            }
        }
    }

    private static string TrimAttributeSuffix(string typeName) =>
        typeName.EndsWith("Attribute", StringComparison.Ordinal)
            ? typeName[..^"Attribute".Length]
            : typeName;
}
