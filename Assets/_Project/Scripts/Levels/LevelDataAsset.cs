using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LevelData",
    menuName = "TP3/Levels/Level Data"
)]
public class LevelDataAsset : ScriptableObject
{
    [SerializeField] private int seed;
    [SerializeField] private List<BallSpawnPointData> spawnPoints =
        new List<BallSpawnPointData>();
    [SerializeField] private bool enableDynamicBalance;

    public int Seed => seed;
    public IReadOnlyList<BallSpawnPointData> SpawnPoints => spawnPoints;
    public bool EnableDynamicBalance => enableDynamicBalance;
}
