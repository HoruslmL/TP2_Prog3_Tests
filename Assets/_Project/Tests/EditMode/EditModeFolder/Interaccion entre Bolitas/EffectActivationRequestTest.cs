using NUnit.Framework;

public class EffectActivationRequestTest
{
    [Test]
    public void Constructor_StoresOwnerAndConfiguration()
    {
        BallData data = new BallData(
            "Explosive",
            1f,
            BallAbilityType.Explosive,
            3f,
            2f
        );

        BallRuntime owner = new BallRuntime("Ball_01", data);

        EffectActivationRequest request =
            new EffectActivationRequest(owner);

        Assert.AreSame(owner, request.Owner);
        Assert.AreEqual(2f, request.Delay);
        Assert.AreEqual(BallAbilityType.Explosive, request.AbilityType);
    }
}
