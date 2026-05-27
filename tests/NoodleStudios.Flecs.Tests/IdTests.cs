namespace NoodleStudios.Flecs.Tests;

public sealed class IdTests
{
    [Test]
    public void TryGetPair_splits_a_pair_into_its_relationship_and_target()
    {
        using World world = new();
        Entity likes = world.CreateEntity();
        Entity apples = world.CreateEntity();

        Id pair = world.Pair(likes, apples);

        Assert.That(pair.TryGetPair(out Id first, out Id second), Is.True);
        Assert.Multiple(() =>
        {
            // First/Second strip generation, so compare against the low 32 bits.
            Assert.That(first.Value, Is.EqualTo((uint)(ulong)likes));
            Assert.That(second.Value, Is.EqualTo((uint)(ulong)apples));
        });
    }

    [Test]
    public void TryGetPair_returns_false_and_None_outputs_for_a_non_pair_id()
    {
        using World world = new();
        Id entity = world.CreateEntity();

        Assert.That(entity.TryGetPair(out Id first, out Id second), Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(Id.None));
            Assert.That(second, Is.EqualTo(Id.None));
        });
    }
}
