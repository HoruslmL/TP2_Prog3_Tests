using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BallSpawnPointData
{
    [SerializeField] private string id;
    [SerializeField] private Vector2 localPosition;
    [SerializeField] private List<BallSpawnOption> spawnOptions =
        new List<BallSpawnOption>();

    public string Id => id;
    public Vector2 LocalPosition => localPosition;
    public IReadOnlyList<BallSpawnOption> SpawnOptions => spawnOptions;
}
