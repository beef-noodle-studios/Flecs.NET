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

    struct UnregisteredTag;
}
