using System.Collections.Generic;
using NUnit.Framework;

public class InteractionResolverTests
{
    private InteractionResolver resolver;

    [SetUp]
    public void SetUp()
    {
        resolver = new InteractionResolver(new ExplosiveAbility());
    }

    [Test]
    public void NormalHitsExplosive_ReturnsOneRequestForExplosive()
    {
        BallRuntime normal = CreateBall(
            "Normal", BallAbilityType.None);

        BallRuntime explosive = CreateBall(
            "Explosive", BallAbilityType.Explosive, 2f);

        ImpactContext impact = CreateImpact(normal, explosive);

        List<EffectActivationRequest> requests =
            resolver.ResolveImpact(impact);

        Assert.AreEqual(1, requests.Count);
        Assert.AreSame(explosive, requests[0].Owner);
        Assert.AreEqual(2f, requests[0].Delay);

        Assert.AreEqual(BallRuntimeState.Active, normal.State);
        Assert.AreEqual(BallRuntimeState.PendingEffect, explosive.State);
    }

    [Test]
    public void TwoExplosiveBallsImpact_ReturnsTwoRequests()
    {
        BallRuntime explosiveA = CreateBall(
            "ExplosiveA", BallAbilityType.Explosive, 1f);

        BallRuntime explosiveB = CreateBall(
            "ExplosiveB", BallAbilityType.Explosive, 3f);

        ImpactContext impact = CreateImpact(explosiveA, explosiveB);

        List<EffectActivationRequest> requests =
            resolver.ResolveImpact(impact);

        Assert.AreEqual(2, requests.Count);

        Assert.AreSame(explosiveA, requests[0].Owner);
        Assert.AreSame(explosiveB, requests[1].Owner);

        Assert.AreEqual(1f, requests[0].Delay);
        Assert.AreEqual(3f, requests[1].Delay);

        Assert.AreEqual(BallRuntimeState.PendingEffect, explosiveA.State);
        Assert.AreEqual(BallRuntimeState.PendingEffect, explosiveB.State);
    }

    [Test]
    public void RepeatedImpact_DoesNotCreateDuplicateRequest()
    {
        BallRuntime normal = CreateBall(
            "Normal", BallAbilityType.None);

        BallRuntime explosive = CreateBall(
            "Explosive", BallAbilityType.Explosive);

        ImpactContext impact = CreateImpact(normal, explosive);

        List<EffectActivationRequest> firstRequests =
            resolver.ResolveImpact(impact);

        List<EffectActivationRequest> secondRequests =
            resolver.ResolveImpact(impact);

        Assert.AreEqual(1, firstRequests.Count);
        Assert.AreEqual(0, secondRequests.Count);
    }

    private BallRuntime CreateBall(
        string id,
        BallAbilityType abilityType,
        float effectDelay = 1f)
    {
        BallData data = new BallData(
            id,
            1f,
            abilityType,
            3f,
            effectDelay
        );

        BallRuntime ball = new BallRuntime(id + "_Instance", data);

        ball.TryActivate();

        return ball;
    }

    private ImpactContext CreateImpact(
        BallRuntime source,
        BallRuntime target)
    {
        return new ImpactContext(
            source,
            target,
            0f,
            0f,
            1f,
            0f,
            1f
        );
    }
}
