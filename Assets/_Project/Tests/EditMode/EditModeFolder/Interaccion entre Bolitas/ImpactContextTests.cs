using NUnit.Framework;

public class ImpactContextTests
{
    [Test]
    public void Constructor_StoresBallsAndImpactInformation()
    {
        var sourceData = new BallData(
            "Normal", 1f, BallAbilityType.None);

        var targetData = new BallData(
            "Explosive", 1f, BallAbilityType.Explosive, 3f, 1f);

        var source = new BallRuntime("ball-A", sourceData);
        var target = new BallRuntime("ball-B", targetData);

        var impact = new ImpactContext(
            source, target, 2f, 4f, 1f, 0f, 5f);

        Assert.That(impact.Source, Is.SameAs(source));
        Assert.That(impact.Target, Is.SameAs(target));

        Assert.That(impact.PositionX, Is.EqualTo(2f));
        Assert.That(impact.PositionY, Is.EqualTo(4f));
        Assert.That(impact.DirectionX, Is.EqualTo(1f));
        Assert.That(impact.DirectionY, Is.EqualTo(0f));
        Assert.That(impact.Strength, Is.EqualTo(5f));
    }
}
