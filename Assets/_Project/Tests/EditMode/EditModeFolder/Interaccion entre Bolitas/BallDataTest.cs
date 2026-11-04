using NUnit.Framework;

public class BallDataTest
{
    [Test]
    public void NormalBall_StoresBasicConfiguration()
    {
        var data = new BallData(
            "Normal",
            5f,
            BallAbilityType.None
        );

        Assert.That(data.Id, Is.EqualTo("Normal"));
        Assert.That(data.Mass, Is.EqualTo(5f));
        Assert.That(data.AbilityType, Is.EqualTo(BallAbilityType.None));
        Assert.That(data.EffectRadius, Is.EqualTo(0f));
    }

    [Test]
    public void ExplosiveBall_StoresExplosionConfiguration()
    {
        var data = new BallData(
            "Explosive",
            1f,
            BallAbilityType.Explosive,
            3f,
            1f
        );

        Assert.That(data.AbilityType, Is.EqualTo(BallAbilityType.Explosive));
        Assert.That(data.EffectRadius, Is.EqualTo(3f));
        Assert.That(data.EffectDelay, Is.EqualTo(1f));
    }
}
