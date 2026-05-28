namespace NoodleStudios.Flecs.Tests;

public sealed class FindFieldTests
{
    [Test]
    public void FirstMatch_returns_first_index_of_the_type()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { X = 2 });

        Query query = world.CreateQuery().With<Position>().With<Velocity>().BuildUncached();

        var found = new List<(int, int)>();
        foreach (TableView table in query)
        {
            found.Add((
                table.FindField<Position>(),
                table.FindField<Velocity>()));
        }

        Assert.That(found, Is.EquivalentTo(new[] { (0, 1) }));
        world.DestroyQuery(query);
    }

    [Test]
    public void Unregistered_type_returns_minus_one_in_every_mode()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        var modes = new[]
        {
            FieldScanMode.FirstMatch, FieldScanMode.PreferSelf,
            FieldScanMode.SelfOnly, FieldScanMode.SharedOnly,
        };

        var results = new List<int>();
        foreach (TableView table in query)
            foreach (FieldScanMode mode in modes)
                results.Add(table.FindField<Velocity>(mode));

        Assert.That(results, Is.All.EqualTo(-1));
        world.DestroyQuery(query);
    }

    [Test]
    public void Type_registered_but_not_in_query_returns_minus_one()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });
        world.Set(e, new Velocity { X = 2 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        var results = new List<int>();
        foreach (TableView table in query)
        {
            results.Add(table.FindField<Velocity>(FieldScanMode.FirstMatch));
            results.Add(table.FindField<Velocity>(FieldScanMode.PreferSelf));
            results.Add(table.FindField<Velocity>(FieldScanMode.SelfOnly));
            results.Add(table.FindField<Velocity>(FieldScanMode.SharedOnly));
        }

        Assert.That(results, Is.All.EqualTo(-1));
        world.DestroyQuery(query);
    }

    [Test]
    public void Self_only_returns_minus_one_on_an_inherited_only_table()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });

        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        // Single With<Inherited>() term matches both the base (self) and the
        // instance (inherited). Per table the field's binding shape differs.
        Query query = world.CreateQuery().With<Inherited>().BuildUncached();

        var bySelfOnly = new List<(bool isSelfTable, int idx)>();
        var bySharedOnly = new List<(bool isSelfTable, int idx)>();
        foreach (TableView table in query)
        {
            bool isSelfTable = !table.IsFieldShared(0);
            bySelfOnly.Add((isSelfTable, table.FindField<Inherited>(FieldScanMode.SelfOnly)));
            bySharedOnly.Add((isSelfTable, table.FindField<Inherited>(FieldScanMode.SharedOnly)));
        }

        Assert.Multiple(() =>
        {
            // SelfOnly: returns 0 on the base's table, -1 on the instance's table.
            Assert.That(bySelfOnly, Does.Contain((true, 0)));
            Assert.That(bySelfOnly, Does.Contain((false, -1)));
            // SharedOnly is the mirror.
            Assert.That(bySharedOnly, Does.Contain((true, -1)));
            Assert.That(bySharedOnly, Does.Contain((false, 0)));
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void Prefer_self_falls_back_to_first_match_on_an_inherited_only_table()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });

        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        Query query = world.CreateQuery().With<Inherited>().BuildUncached();

        var idxByMode = new List<int>();
        foreach (TableView table in query)
        {
            if (!table.IsFieldShared(0))
                continue; // only inspect the instance's (inherited) table

            idxByMode.Add(table.FindField<Inherited>(FieldScanMode.FirstMatch));
            idxByMode.Add(table.FindField<Inherited>(FieldScanMode.PreferSelf));
        }

        // On the inherited-only table, FirstMatch returns 0 and PreferSelf
        // falls back to the same first match rather than returning -1.
        Assert.That(idxByMode, Is.EqualTo(new[] { 0, 0 }));
        world.DestroyQuery(query);
    }

    [Test]
    public void Same_type_terms_split_by_mode_when_self_is_declared_first()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Inherited { Value = 7 });

        Entity child = world.CreateEntity();
        world.AddPair(child, world.ChildOf, parent);
        world.Set(child, new Inherited { Value = 1 });

        // Self declared first (idx 0), Up declared second (idx 1).
        Query query = world.CreateQuery()
            .With<Inherited>()              // idx 0 — self
            .With<Inherited>().Up()         // idx 1 — sourced from parent
            .BuildUncached();

        var modes = new List<(FieldScanMode mode, int idx)>();
        foreach (TableView table in query)
        {
            modes.Add((FieldScanMode.FirstMatch, table.FindField<Inherited>(FieldScanMode.FirstMatch)));
            modes.Add((FieldScanMode.PreferSelf, table.FindField<Inherited>(FieldScanMode.PreferSelf)));
            modes.Add((FieldScanMode.SelfOnly, table.FindField<Inherited>(FieldScanMode.SelfOnly)));
            modes.Add((FieldScanMode.SharedOnly, table.FindField<Inherited>(FieldScanMode.SharedOnly)));
        }

        Assert.That(modes, Is.EquivalentTo(new[]
        {
            (FieldScanMode.FirstMatch, 0),
            (FieldScanMode.PreferSelf, 0),
            (FieldScanMode.SelfOnly, 0),
            (FieldScanMode.SharedOnly, 1),
        }));
        world.DestroyQuery(query);
    }

    [Test]
    public void Same_type_terms_split_by_mode_when_shared_is_declared_first()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Inherited { Value = 7 });

        Entity child = world.CreateEntity();
        world.AddPair(child, world.ChildOf, parent);
        world.Set(child, new Inherited { Value = 1 });

        // Up declared first (idx 0), Self declared second (idx 1) — the H2
        // case the plan calls out by name.
        Query query = world.CreateQuery()
            .With<Inherited>().Up()         // idx 0 — sourced from parent
            .With<Inherited>()              // idx 1 — self
            .BuildUncached();

        var modes = new List<(FieldScanMode mode, int idx)>();
        foreach (TableView table in query)
        {
            modes.Add((FieldScanMode.FirstMatch, table.FindField<Inherited>(FieldScanMode.FirstMatch)));
            modes.Add((FieldScanMode.PreferSelf, table.FindField<Inherited>(FieldScanMode.PreferSelf)));
            modes.Add((FieldScanMode.SelfOnly, table.FindField<Inherited>(FieldScanMode.SelfOnly)));
            modes.Add((FieldScanMode.SharedOnly, table.FindField<Inherited>(FieldScanMode.SharedOnly)));
        }

        Assert.That(modes, Is.EquivalentTo(new[]
        {
            (FieldScanMode.FirstMatch, 0),  // first index, regardless of shape
            (FieldScanMode.PreferSelf, 1),  // self at idx 1 wins
            (FieldScanMode.SelfOnly, 1),
            (FieldScanMode.SharedOnly, 0),
        }));
        world.DestroyQuery(query);
    }

    [Test]
    public void FindField_by_id_routes_through_the_same_mode_logic()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });

        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        Id inheritedId = world.Component<Inherited>();
        Query query = world.CreateQuery().With<Inherited>().BuildUncached();

        var bySelfOnly = new List<(bool isSelfTable, int idx)>();
        foreach (TableView table in query)
        {
            bool isSelfTable = !table.IsFieldShared(0);
            bySelfOnly.Add((isSelfTable, table.FindField(inheritedId, FieldScanMode.SelfOnly)));
        }

        Assert.Multiple(() =>
        {
            Assert.That(bySelfOnly, Does.Contain((true, 0)));
            Assert.That(bySelfOnly, Does.Contain((false, -1)));
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void HasSelfField_distinguishes_self_bound_from_inherited()
    {
        using World world = new();
        Entity baseEntity = world.CreateEntity();
        world.Set(baseEntity, new Inherited { Value = 42 });

        Entity instance = world.CreateEntity();
        world.AddPair(instance, world.IsA, baseEntity);

        Id inheritedId = world.Component<Inherited>();
        Query query = world.CreateQuery().With<Inherited>().BuildUncached();

        var byTable = new List<(bool isSelfTable, bool hasSelfByType, bool hasSelfById)>();
        foreach (TableView table in query)
        {
            bool isSelfTable = !table.IsFieldShared(0);
            byTable.Add((isSelfTable, table.HasSelfField<Inherited>(), table.HasSelfField(inheritedId)));
        }

        Assert.Multiple(() =>
        {
            // Base's table: self-bound — HasSelfField is true via both overloads.
            Assert.That(byTable, Does.Contain((true, true, true)));
            // Instance's table: inherited — HasSelfField is false via both overloads.
            Assert.That(byTable, Does.Contain((false, false, false)));
        });
        world.DestroyQuery(query);
    }

    [Test]
    public void HasSelfField_returns_false_for_a_type_not_in_the_query()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        var results = new List<bool>();
        foreach (TableView table in query)
            results.Add(table.HasSelfField<Velocity>());

        Assert.That(results, Is.All.False);
        world.DestroyQuery(query);
    }

    [Test]
    public void GetFieldSource_returns_None_when_the_query_has_no_sourced_fields()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        var sources = new List<Entity>();
        foreach (TableView table in query)
            sources.Add(table.GetFieldSource(0));

        Assert.That(sources, Is.All.EqualTo(Entity.None));
        world.DestroyQuery(query);
    }

    [Test]
    public void GetFieldSource_returns_the_supplying_entity_for_an_Up_sourced_field()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Inherited { Value = 7 });

        Entity child = world.CreateEntity();
        world.AddPair(child, world.ChildOf, parent);
        world.Set(child, new Position { X = 1 });

        // Field 0 = Position (self), field 1 = Inherited (sourced from parent).
        Query query = world.CreateQuery()
            .With<Position>()
            .With<Inherited>().Up()
            .BuildUncached();

        var seen = new List<(Entity selfSource, Entity upSource)>();
        foreach (TableView table in query)
            seen.Add((table.GetFieldSource(0), table.GetFieldSource(1)));

        // The self field's source is None (self-bound has no external source);
        // the Up field's source is the parent.
        Assert.That(seen, Is.EquivalentTo(new[] { (Entity.None, parent) }));
        world.DestroyQuery(query);
    }

    [Test]
    public void GetFieldSource_by_type_routes_through_FindField_mode()
    {
        using World world = new();
        Entity parent = world.CreateEntity();
        world.Set(parent, new Inherited { Value = 7 });

        Entity child = world.CreateEntity();
        world.AddPair(child, world.ChildOf, parent);
        world.Set(child, new Position { X = 1 });

        Query query = world.CreateQuery()
            .With<Position>()
            .With<Inherited>().Up()
            .BuildUncached();

        var seen = new List<(Entity firstMatch, Entity sharedOnly, Entity selfOnly)>();
        foreach (TableView table in query)
        {
            seen.Add((
                table.GetFieldSource<Inherited>(),                                
                table.GetFieldSource<Inherited>(FieldScanMode.SharedOnly),        
                table.GetFieldSource<Inherited>(FieldScanMode.SelfOnly)));       
        }

        Assert.That(seen, Is.EquivalentTo(new[] { (parent, parent, Entity.None) }));
        world.DestroyQuery(query);
    }

    [Test]
    public void GetFieldTarget_by_type_returns_None_when_the_slot_is_not_a_pair()
    {
        using World world = new();
        Entity e = world.CreateEntity();
        world.Set(e, new Position { X = 1 });

        Query query = world.CreateQuery().With<Position>().BuildUncached();

        // Self-only mode finds no shared slot, so the by-type form returns None
        // without ever indexing into the (non-pair) field. Discriminating: the
        // positional GetFieldTarget(int) would throw in Debug on a non-pair id.
        var results = new List<Entity>();
        foreach (TableView table in query)
            results.Add(table.GetFieldTarget<Position>(FieldScanMode.SharedOnly));

        Assert.That(results, Is.All.EqualTo(Entity.None));
        world.DestroyQuery(query);
    }

    [Test]
    public void GetFieldTarget_by_id_resolves_a_concrete_pair_field()
    {
        using World world = new();
        Entity apples = world.CreateEntity();
        Entity likes = world.CreateEntity();
        Id likesApples = world.Pair(likes, apples);
        Entity e = world.CreateEntity();
        world.AddPair(e, likes, apples);

        Query query = world.CreateQuery().With(likes, apples).BuildUncached();

        var targets = new List<Entity>();
        foreach (TableView table in query)
            targets.Add(table.GetFieldTarget(likesApples));

        Assert.That(targets, Is.EquivalentTo(new[] { apples }));
        world.DestroyQuery(query);
    }

    // --- test components ---

    private struct Position { public int X; }
    private struct Velocity { public int X; }

    [OnInstantiate(Instantiate.Inherit)]
    private struct Inherited { public int Value; }
}
