using UnityEngine;

public class ExplosiveAbility : IBallAbility
{
    public BallAbilityType AbilityType =>
        BallAbilityType.Explosive;

    public bool TryActivate(BallRuntime owner, ImpactContext impact)
    {
        if (owner == null || impact == null)
            return false;

        if (impact.Source != owner && impact.Target != owner)
            return false;

        return owner.TryScheduleEffect();
    }

    public bool CanCancelPendingEffect(BallRuntime owner)
    {
        return owner != null &&
               owner.State == BallRuntimeState.PendingEffect;
    }
}
