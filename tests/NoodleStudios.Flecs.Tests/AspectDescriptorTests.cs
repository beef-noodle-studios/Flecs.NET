using System.Linq;
using System.Runtime.InteropServices;

#pragma warning disable CS0649 // component fields never assigned 

namespace NoodleStudios.Flecs.Tests;

public sealed class AspectDescriptorTests
{
    // --- Positive coverage on a representative aspect ---

    [Test]
    public void Movement_descriptor_classifies_slot_kinds_in_declaration_order()
    {
        AspectDescriptor descriptor = AspectDescriptor<Movement>.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Slots, Has.Length.EqualTo(3));
            Assert.That(descriptor.Slots[0].Kind, Is.EqualTo(AspectSlotKind.Entity));
            Assert.That(descriptor.Slots[1].Kind, Is.EqualTo(AspectSlotKind.ComponentAccessor));
            Assert.That(descriptor.Slots[2].Kind, Is.EqualTo(AspectSlotKind.ComponentAccessor));
            Assert.That(descriptor.FieldSlotCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void Movement_descriptor_assigns_pointer_packed_offsets()
    {
        AspectDescriptor descriptor = AspectDescriptor<Movement>.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Slots[0].Offset, Is.EqualTo(0));
            Assert.That(descriptor.Slots[1].Offset, Is.EqualTo(8));
            Assert.That(descriptor.Slots[2].Offset, Is.EqualTo(16));

            // Size == Slots.Length * 8 on 64-bit target
            Assert.That(descriptor.Size, Is.EqualTo(24));
        });
    }

    [Test]
    public void Movement_descriptor_classifies_ref_readonly_as_In_and_ref_as_InOut()
    {
        AspectDescriptor descriptor = AspectDescriptor<Movement>.Instance;
        AspectSlot pos = descriptor.Slots.Single(s => s.ComponentType == typeof(Position));
        AspectSlot vel = descriptor.Slots.Single(s => s.ComponentType == typeof(Velocity));

        Assert.Multiple(() =>
        {
            Assert.That(pos.RefKind, Is.EqualTo(AspectRefKind.In),
                "ref readonly Position must classify as In.");
            Assert.That(vel.RefKind, Is.EqualTo(AspectRefKind.InOut),
                "ref Velocity must classify as InOut.");
        });
    }

    [Test]
    public void Accessor_slots_get_seed_term_indices_in_declaration_order()
    {
        AspectDescriptor descriptor = AspectDescriptor<Movement>.Instance;
        AspectSlot[] accessors = descriptor.Slots
            .Where(s => s.Kind == AspectSlotKind.ComponentAccessor)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(accessors[0].ComponentType, Is.EqualTo(typeof(Position)));
            Assert.That(accessors[0].SeedTermIndex, Is.EqualTo(0));
            Assert.That(accessors[1].ComponentType, Is.EqualTo(typeof(Velocity)));
            Assert.That(accessors[1].SeedTermIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void Non_accessor_slots_have_default_accessor_fields()
    {
        AspectDescriptor descriptor = AspectDescriptor<Movement>.Instance;
        AspectSlot entitySlot = descriptor.Slots[0];

        Assert.Multiple(() =>
        {
            Assert.That(entitySlot.ComponentType, Is.Null);
            Assert.That(entitySlot.Optional, Is.False);
            Assert.That(entitySlot.Self, Is.False);
            Assert.That(entitySlot.Sourcing, Is.EqualTo(SourcingKind.Self));
            Assert.That(entitySlot.Relationship, Is.Null);
            Assert.That(entitySlot.SeedTermIndex, Is.EqualTo(-1));
        });
    }

    [Test]
    public void TableView_field_classifies_as_TableView_slot()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithTableView>.Instance;

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Slots[0].Kind, Is.EqualTo(AspectSlotKind.Entity));
            Assert.That(descriptor.Slots[1].Kind, Is.EqualTo(AspectSlotKind.TableView));
            Assert.That(descriptor.Slots[2].Kind, Is.EqualTo(AspectSlotKind.ComponentAccessor));
            Assert.That(descriptor.FieldSlotCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Optional_attribute_lands_on_accessor_slot()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithOptional>.Instance;
        AspectSlot mass = descriptor.Slots.Single(s => s.ComponentType == typeof(Mass));

        Assert.That(mass.Optional, Is.True);
    }

    [Test]
    public void Self_attribute_lands_on_accessor_slot()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithSelf>.Instance;
        AspectSlot vel = descriptor.Slots.Single(s => s.ComponentType == typeof(Velocity));

        Assert.Multiple(() =>
        {
            Assert.That(vel.Self, Is.True);
            Assert.That(vel.Sourcing, Is.EqualTo(SourcingKind.Self));
        });
    }

    [Test]
    public void Up_default_relationship_is_null()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithUpDefault>.Instance;
        AspectSlot slot = descriptor.Slots.Single(s => s.Kind == AspectSlotKind.ComponentAccessor);

        Assert.Multiple(() =>
        {
            Assert.That(slot.Sourcing, Is.EqualTo(SourcingKind.Up));
            Assert.That(slot.Relationship, Is.Null);
        });
    }

    [Test]
    public void Up_with_explicit_relationship_records_relationship_type()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithUpRelationship>.Instance;
        AspectSlot slot = descriptor.Slots.Single(s => s.Kind == AspectSlotKind.ComponentAccessor);

        Assert.Multiple(() =>
        {
            Assert.That(slot.Sourcing, Is.EqualTo(SourcingKind.Up));
            Assert.That(slot.Relationship, Is.EqualTo(typeof(ParentLink)));
        });
    }

    [Test]
    public void UpAncestorsFirst_records_sourcing_and_relationship()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithUpAncestors>.Instance;
        AspectSlot slot = descriptor.Slots.Single(s => s.Kind == AspectSlotKind.ComponentAccessor);

        Assert.Multiple(() =>
        {
            Assert.That(slot.Sourcing, Is.EqualTo(SourcingKind.UpAncestorsFirst));
            Assert.That(slot.Relationship, Is.EqualTo(typeof(ParentLink)));
        });
    }

    [Test]
    public void UpDescendantsFirst_records_sourcing_and_relationship()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithUpDescendants>.Instance;
        AspectSlot slot = descriptor.Slots.Single(s => s.Kind == AspectSlotKind.ComponentAccessor);

        Assert.Multiple(() =>
        {
            Assert.That(slot.Sourcing, Is.EqualTo(SourcingKind.UpDescendantsFirst));
            Assert.That(slot.Relationship, Is.EqualTo(typeof(ParentLink)));
        });
    }

    [Test]
    public void Singleton_records_sourcing_and_no_relationship()
    {
        AspectDescriptor descriptor = AspectDescriptor<WithSingleton>.Instance;
        AspectSlot slot = descriptor.Slots.Single(s => s.Kind == AspectSlotKind.ComponentAccessor);

        Assert.Multiple(() =>
        {
            Assert.That(slot.Sourcing, Is.EqualTo(SourcingKind.Singleton));
            Assert.That(slot.Relationship, Is.Null);
        });
    }

    [Test]
    public void Generic_static_field_wraps_build_failure_in_TypeInitializationException()
    {
        TypeInitializationException ex = Assert.Throws<TypeInitializationException>(
            () => _ = AspectDescriptor<ExplicitLayoutAspect>.Instance)!;
        Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(ex.InnerException!.Message, Does.Contain("[StructLayout(LayoutKind.Sequential)]"));
    }

    [Test]
    public void Explicit_layout_aspect_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(ExplicitLayoutAspect), 16))!;
        Assert.That(ex.Message, Does.Contain("[StructLayout(LayoutKind.Sequential)]"));
    }

    [Test]
    public void Auto_layout_aspect_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(AutoLayoutAspect), 16))!;
        Assert.That(ex.Message, Does.Contain("[StructLayout(LayoutKind.Sequential)]"));
    }

    [Test]
    public void Non_whitelisted_by_value_field_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(NonWhitelistedField), 8))!;
        Assert.That(ex.Message, Does.Contain("unsupported type"));
    }

    [Test]
    public void Ref_to_managed_type_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(RefToManagedField), 8))!;
        Assert.That(ex.Message, Does.Contain("not a component type"));
    }

    [Test]
    public void Ref_to_managed_value_type_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(RefToManagedValueField), 8))!;
        Assert.That(ex.Message, Does.Contain("managed references"));
    }

    [Test]
    public void Ref_to_Entity_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(RefToEntityField), 8))!;
        Assert.That(ex.Message, Does.Contain("not a component type"));
    }

    [Test]
    public void Size_mismatch_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(Movement), 100))!;
        Assert.That(ex.Message, Does.Contain("size mismatch"));
    }

    [Test]
    public void Optional_on_Entity_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(OptionalOnEntity), 16))!;
        Assert.That(ex.Message, Does.Contain("Entity"));
        Assert.That(ex.Message, Does.Contain("accessor-only attribute"));
    }

    [Test]
    public void Optional_on_TableView_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(OptionalOnTableView), 16))!;
        Assert.That(ex.Message, Does.Contain("TableView"));
        Assert.That(ex.Message, Does.Contain("accessor-only attribute"));
    }

    [Test]
    public void Singleton_plus_Up_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(SingletonPlusUp), 8))!;
        Assert.That(ex.Message, Does.Contain("more than one sourcing attribute"));
    }

    [Test]
    public void Up_plus_UpAncestorsFirst_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(UpPlusUpAncestors), 8))!;
        Assert.That(ex.Message, Does.Contain("more than one sourcing attribute"));
    }

    [Test]
    public void Self_on_Up_field_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(SelfOnUp), 8))!;
        Assert.That(ex.Message, Does.Contain("[Self]"));
        Assert.That(ex.Message, Does.Contain("mutually exclusive"));
    }

    [Test]
    public void Self_on_Singleton_field_throws()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            AspectDescriptor.Build(typeof(SelfOnSingleton), 8))!;
        Assert.That(ex.Message, Does.Contain("[Self]"));
        Assert.That(ex.Message, Does.Contain("mutually exclusive"));
    }

    // --- Test components and aspect shapes ---

    internal struct Position { public int X; public int Y; }
    internal struct Velocity { public int X; public int Y; }
    internal struct Mass { public float Value; }
    internal struct Config { public int Level; }

    internal struct ParentLink { }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct Movement : IAspect
    {
        public Entity Entity;
        public ref readonly Position Position;
        public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithTableView : IAspect
    {
        public Entity Entity;
        public TableView Table;
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithOptional : IAspect
    {
        [Optional]
        public ref readonly Mass Mass;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithSelf : IAspect
    {
        [Self]
        public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithUpDefault : IAspect
    {
        [Up]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithUpRelationship : IAspect
    {
        [Up(typeof(ParentLink))]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithUpAncestors : IAspect
    {
        [UpAncestorsFirst(typeof(ParentLink))]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithUpDescendants : IAspect
    {
        [UpDescendantsFirst(typeof(ParentLink))]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct WithSingleton : IAspect
    {
        [Singleton]
        public ref readonly Config Config;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal ref struct ExplicitLayoutAspect : IAspect
    {
        [FieldOffset(0)] public ref readonly Position Position;
        [FieldOffset(8)] public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Auto)]
    internal ref struct AutoLayoutAspect : IAspect
    {
        public ref readonly Position Position;
        public ref Velocity Velocity;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct NonWhitelistedField : IAspect
    {
        public int NotAllowed;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct RefToManagedField : IAspect
    {
        public ref string NotAllowed;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct RefToEntityField : IAspect
    {
        public ref Entity NotAllowed;
    }

    internal struct ManagedValueComponent { public string Name; }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct RefToManagedValueField : IAspect
    {
        public ref ManagedValueComponent NotAllowed;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct OptionalOnEntity : IAspect
    {
        [Optional]
        public Entity Entity;
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct OptionalOnTableView : IAspect
    {
        [Optional]
        public TableView Table;
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SingletonPlusUp : IAspect
    {
        [Singleton]
        [Up]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct UpPlusUpAncestors : IAspect
    {
        [Up]
        [UpAncestorsFirst]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SelfOnUp : IAspect
    {
        [Self]
        [Up]
        public ref readonly Position Position;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal ref struct SelfOnSingleton : IAspect
    {
        [Self]
        [Singleton]
        public ref readonly Config Config;
    }
}

#pragma warning restore CS0649
