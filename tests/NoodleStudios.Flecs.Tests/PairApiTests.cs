using NoodleStudios.Flecs.Pairs;

namespace NoodleStudios.Flecs.Tests;

public sealed class PairApiTests
{
    private World _world;

    [SetUp]
    public void Setup()
    {
        _world = new World();
    }

    [TearDown]
    public void TearDown()
    {
        _world.Dispose();
    }

    [Test]
    public void Add_pair_tag_by_id()
    {
        var alice = _world.CreateEntity();
        var likes = _world.CreateEntity();
        var apples = _world.CreateEntity();
        var likesApples = Pair.Relation(likes).Target(apples);

        _world.Add(alice, likesApples);
        Assert.That(_world.Has(alice, likesApples), Is.True);
    }

    [Test]
    public void Add_pair_tag_by_shorthand()
    {
        var alice = _world.CreateEntity();
        var bob = _world.CreateEntity();
        var likes = _world.CreateEntity();
        var apples = _world.CreateEntity();
        var likesApples = Pair.Tag(likes, apples);
        var likesOranges = Pair.Tag<Likes, Oranges>();

        _world.Add(alice, likesApples);
        Assert.That(_world.Has(alice, likesApples), Is.True);

        _world.Add(bob, likesOranges);
        Assert.That(_world.Has(bob, likesOranges), Is.True);
    }

    [Test]
    public void Add_pair_tag_by_generic()
    {
        var alice = _world.CreateEntity();
        var likesOranges = Pair.Relation<Likes>().Target<Oranges>();

        _world.Add(alice, likesOranges);
        Assert.That(_world.Has(alice, likesOranges), Is.True);
    }

    [Test]
    public void Add_pair_tag_with_mixed_id_and_generic_arguments()
    {
        var alice = _world.CreateEntity();
        var bob = _world.CreateEntity();
        var likes = _world.CreateEntity();
        var apples = _world.CreateEntity();
        var likesApples = Pair.Relation<Likes>().Target(apples);
        var likesOranges = Pair.Relation(likes).Target<Oranges>();

        _world.Add(alice, likesApples);
        Assert.That(_world.Has(alice, likesApples), Is.True);

        _world.Add(bob, likesOranges);
        Assert.That(_world.Has(bob, likesOranges), Is.True);
    }

    [Test]
    public void Add_pair_component_with_relation_value()
    {
        var apples = _world.CreateEntity();
        var appleQuest = _world.CreateEntity();
        var orangeQuest = _world.CreateEntity();

        const int AppleAmount = 10;
        _world.Add(appleQuest, Pair.Relation(new Requires(AppleAmount)).Target(apples));
        Assert.That(_world.Get(appleQuest, Pair.Relation<Requires>().Target(apples)).Amount, Is.EqualTo(AppleAmount));

        const int OrangeAmount = 5;
        _world.Add(orangeQuest, Pair.Relation(new Requires(OrangeAmount)).Target<Oranges>());
        Assert.That(_world.Get(orangeQuest, Pair.Relation<Requires>().Target<Oranges>()).Amount, Is.EqualTo(OrangeAmount));
    }

    [Test]
    public void Add_pair_component_with_target_value()
    {
        var alice = _world.CreateEntity();
        var bob = _world.CreateEntity();
        var livesAt = _world.CreateEntity();

        _world.Add(alice, Pair.Relation(livesAt).Target(new Position(1, 2)));
        var alicePosition = _world.Get(alice, Pair.Relation(livesAt).Target<Position>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(alicePosition.X, Is.EqualTo(1));
            Assert.That(alicePosition.Y, Is.EqualTo(2));
        }

        _world.Add(bob, Pair.Relation<SpawnsAt>().Target(new Position(3, 4)));
        var bobPosition = _world.Get(bob, Pair.Relation<SpawnsAt>().Target<Position>());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(bobPosition.X, Is.EqualTo(3));
            Assert.That(bobPosition.Y, Is.EqualTo(4));
        }
    }

    struct Likes;
    struct Oranges;
    struct SpawnsAt;
    record struct Requires(int Amount);
    record struct Position(float X, float Y);
}
