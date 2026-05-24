namespace NoodleStudios.Flecs.Tests;

public class ComponentApiTests
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
    // It's important for the functionality of ReadOnlyWorld that Has<T> does
    // not mutate the world by registering the component type T.
    public void Has_does_not_register_component()
    {
        var alice = _world.CreateEntity();
        Assert.That(_world.Has<UnregisteredTag>(alice), Is.False);
        Assert.That(_world.TryGetId<UnregisteredTag>(out _), Is.False);
    }

    [Test]
    public unsafe void Get_id_throws_when_the_id_is_a_tag()
    {
        var alice = _world.CreateEntity();
        _world.Add(alice, tag);

        Assert.That(() => _world.Get<TestTag>(alice), Throws.InvalidOperationException);
        Assert.That(() => _world.GetMut<TestTag>(alice), Throws.InvalidOperationException);
    }

    struct TestTag;
    struct UnregisteredTag;
}
