using System;
using UnityEngine;

[Serializable]
public class BallSpawnOption
{
    [SerializeField] private BallDataAsset ballData;
    [SerializeField] private float spawnProbability;

    public BallDataAsset BallData => ballData;
    public float SpawnProbability => spawnProbability;
}
