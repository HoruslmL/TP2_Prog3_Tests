using UnityEngine;

public sealed class BallData
{
    public string Id { get; }
    public float Mass { get; }
    public BallAbilityType AbilityType { get; }
    public float EffectRadius { get; }
    public float EffectDelay { get; }
    public float Scale { get; }

    public BallData(
        string id,
        float mass,
        BallAbilityType abilityType,
        float effectRadius = 0f,
        float effectDelay = 1f,
        float scale = 1f)
    {
        Id = id;
        Mass = mass;
        AbilityType = abilityType;
        EffectRadius = effectRadius;
        EffectDelay = effectDelay;
        Scale = scale;
    }
}
