using NUnit.Framework;

public class BallRuntimeTests
{
    [Test]
    public void ExplosiveBall_CanCompleteLifecycleAndReturnToPool()
    {
        var data = new BallData(
            "Explosive", 1f, BallAbilityType.Explosive, 3f, 1f);

        var ball = new BallRuntime("instance-1", data);

        Assert.That(ball.State, Is.EqualTo(BallRuntimeState.Available));

        Assert.That(ball.TryActivate(), Is.True);
        Assert.That(ball.TryScheduleEffect(), Is.True);
        Assert.That(ball.TryMarkEffectExecuted(), Is.True);
        Assert.That(ball.TryReturnToPool(), Is.True);

        Assert.That(ball.State, Is.EqualTo(BallRuntimeState.Available));
    }

    [Test]
    public void NormalBall_CannotScheduleExplosion()
    {
        var data = new BallData(
            "Normal", 1f, BallAbilityType.None);

        var ball = new BallRuntime("instance-1", data);

        ball.TryActivate();

        Assert.That(ball.TryScheduleEffect(), Is.False);
        Assert.That(ball.State, Is.EqualTo(BallRuntimeState.Active));
    }
}
