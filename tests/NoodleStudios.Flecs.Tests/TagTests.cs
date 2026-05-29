using System.Runtime.InteropServices;
using static Flecs.NET.Bindings.flecs;

namespace NoodleStudios.Flecs.Tests;

public sealed class TagTests
{
    private struct EmptyTag;

    private struct OneByte
    {
        public byte Value;
    }

    [Sparse]
    private struct SparseTag;

    [CanToggle]
    private struct ToggleTag;

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    private struct OpaqueBlob;

    // --- Registration ---

    [Test]
    public unsafe void Field_less_struct_registers_with_no_type_info()
    {
        using World world = new();

        Assert.That((nint)ecs_get_type_info(world.Handle, world.Component<EmptyTag>()), Is.EqualTo((nint)0));
    }

    [Test]
    public unsafe void One_byte_struct_registers_as_a_real_component()
    {
        using World world = new();

        ecs_type_info_t* info = ecs_get_type_info(world.Handle, world.Component<OneByte>());
        Assert.That((nint)info, Is.Not.EqualTo((nint)0));
        Assert.That(info->size, Is.EqualTo(1));
    }

    [Test]
    public unsafe void Explicit_struct_layout_size_keeps_a_field_less_struct_a_component()
    {
        using World world = new();

        ecs_type_info_t* info = ecs_get_type_info(world.Handle, world.Component<OpaqueBlob>());
        Assert.That((nint)info, Is.Not.EqualTo((nint)0));
        Assert.That(info->size, Is.EqualTo(16));
    }

    // --- Storage traits ---

    [Test]
    public void Sparse_on_a_field_less_struct_throws()
    {
        using World world = new();

        Assert.That(world.Component<SparseTag>, Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public unsafe void CanToggle_on_a_field_less_struct_stays_a_tag()
    {
        using World world = new();

        Assert.That((nint)ecs_get_type_info(world.Handle, world.Component<ToggleTag>()), Is.EqualTo((nint)0));
    }

    // --- World ops ---

    [Test]
    public void Set_on_a_tag_adds_it()
    {
        using World world = new();
        Entity e = world.CreateEntity();

        world.Set(e, default(EmptyTag));

        Assert.That(world.Has<EmptyTag>(e), Is.True);
    }

    [Test]
    public void Get_on_a_tag_throws()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Add<EmptyTag>(e);

        Assert.That(() => world.Get<EmptyTag>(e), Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void GetMut_on_a_tag_throws()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Add<EmptyTag>(e);

        Assert.That(() => world.GetMut<EmptyTag>(e), Throws.InstanceOf<InvalidOperationException>());
    }

    [Test]
    public void TryGet_on_a_present_tag_reports_presence_and_default()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Add<EmptyTag>(e);

        Assert.That(world.TryGet<EmptyTag>(e, out _), Is.True);
    }

    [Test]
    public void TryGet_on_an_absent_tag_returns_false()
    {
        using World world = new();
        Entity e = world.CreateEntity();

        Assert.That(world.TryGet<EmptyTag>(e, out _), Is.False);
    }

    // --- Tag is matchable but not readable ---

    [Test]
    public void Tag_matches_a_query_but_exposes_no_field_data()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Add<EmptyTag>(e);

        var matched = 0;
        Query query = world.CreateQuery().With<EmptyTag>().BuildUncached();
        foreach (TableView table in query)
        {
            matched += table.Count;

            Assert.That(table.HasField<EmptyTag>(), Is.False);
        }

        Assert.That(matched, Is.EqualTo(1));
        world.DestroyQuery(query);
    }

    private struct AutoProp
    {
        public int Value { get; set; }
    }

    private struct ConstOnly
    {
        public const int K = 1;
    }

    private struct NestedEmpty
    {
        public EmptyTag Inner;
    }

    [Test]
    public void IsTag_classifies_types_the_same_way_the_analyzer_does()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ComponentId<EmptyTag>.IsTag, Is.True, "field-less struct");
            Assert.That(ComponentId<ConstOnly>.IsTag, Is.True, "only a const field");
            Assert.That(ComponentId<OneByte>.IsTag, Is.False, "one byte field");
            Assert.That(ComponentId<AutoProp>.IsTag, Is.False, "auto-property backing field");
            Assert.That(ComponentId<NestedEmpty>.IsTag, Is.False, "field of empty-struct type");
            Assert.That(ComponentId<OpaqueBlob>.IsTag, Is.False, "explicit StructLayout size");
        });
    }
}
