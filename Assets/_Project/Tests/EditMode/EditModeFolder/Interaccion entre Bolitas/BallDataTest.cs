using NUnit.Framework;
using UnityEngine;

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
        Assert.That(data.Color, Is.EqualTo(Color.white));
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

    [Test]
    public void BallData_StoresConfiguredColor()
    {
        Color expectedColor = new Color(0.3f, 0.6f, 0.9f, 0.75f);
        var data = new BallData(
            "Colored",
            1f,
            BallAbilityType.None,
            0f,
            1f,
            1f,
            "Colored",
            expectedColor
        );

        Assert.That(data.Color, Is.EqualTo(expectedColor));
    }
}
