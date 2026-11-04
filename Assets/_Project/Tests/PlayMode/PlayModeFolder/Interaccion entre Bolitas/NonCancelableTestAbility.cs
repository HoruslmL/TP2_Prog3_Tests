using UnityEngine;

public class NonCancelableTestAbility : IBallAbility
{
    public BallAbilityType AbilityType =>
    BallAbilityType.Explosive;

    public bool TryActivate(BallRuntime owner, ImpactContext impact)
    {
        return owner != null &&
               owner.TryScheduleEffect();
    }

    public bool CanCancelPendingEffect(BallRuntime owner)
    {
        return false;
    }
}
