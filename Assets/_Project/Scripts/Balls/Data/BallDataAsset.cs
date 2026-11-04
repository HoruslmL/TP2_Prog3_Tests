using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "BallData",
    menuName = "TP2/Balls/Ball Data"
)]

public class BallDataAsset : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField, Min(0.01f)] private float mass = 1f;
    [SerializeField] private BallAbilityType abilityType;

    [FormerlySerializedAs("explosionRadius")]
    [SerializeField, Min(0f)] private float effectRadius = 3f;

    [FormerlySerializedAs("explosionDelay")]
    [SerializeField, Min(0f)] private float effectDelay = 1f;

    [SerializeField, Min(0.1f)]
    private float scale = 1f;

    public BallData ToBallData()
    {
        return new BallData(
            id,
            mass,
            abilityType,
            effectRadius,
            effectDelay,
            scale
        );
    }
}
