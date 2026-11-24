using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class LevelDataAssetTests
{
    private LevelDataAsset levelDataAsset;
    private BallDataAsset ballDataAsset;

    [SetUp]
    public void SetUp()
    {
        levelDataAsset = ScriptableObject.CreateInstance<LevelDataAsset>();
        ballDataAsset = ScriptableObject.CreateInstance<BallDataAsset>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(levelDataAsset);
        Object.DestroyImmediate(ballDataAsset);
    }

    [Test]
    public void CreateInstance_CreatesLevelDataAsset()
    {
        Assert.That(levelDataAsset, Is.Not.Null);
        Assert.That(levelDataAsset, Is.InstanceOf<LevelDataAsset>());
    }

    [Test]
    public void SerializedLevelSettings_CanBeRetrieved()
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        serializedAsset.FindProperty("seed").intValue = 7341;
        serializedAsset.FindProperty("enableDynamicBalance").boolValue = true;
        serializedAsset.FindProperty("minimumSpawnPointClearance").floatValue =
            1.25f;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(levelDataAsset.Seed, Is.EqualTo(7341));
        Assert.That(levelDataAsset.EnableDynamicBalance, Is.True);
        Assert.That(
            levelDataAsset.MinimumSpawnPointClearance,
            Is.EqualTo(1.25f));

        serializedAsset.FindProperty("enableDynamicBalance").boolValue = false;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(levelDataAsset.EnableDynamicBalance, Is.False);
    }

    [Test]
    public void MinimumSpawnPointClearance_DefaultsToHalfWorldUnit()
    {
        Assert.That(
            levelDataAsset.MinimumSpawnPointClearance,
            Is.EqualTo(0.5f));
    }

    [Test]
    public void SerializedSpawnPoint_CanBeRetrievedWithLocalPositionAndOptions()
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        var spawnPoints = serializedAsset.FindProperty("spawnPoints");
        spawnPoints.arraySize = 1;

        var spawnPoint = spawnPoints.GetArrayElementAtIndex(0);
        spawnPoint.FindPropertyRelative("id").stringValue = "west-entry";
        spawnPoint.FindPropertyRelative("localPosition").vector2Value =
            new Vector2(-4.25f, 2.5f);

        var spawnOptions = spawnPoint.FindPropertyRelative("spawnOptions");
        spawnOptions.arraySize = 1;
        var spawnOption = spawnOptions.GetArrayElementAtIndex(0);
        spawnOption.FindPropertyRelative("ballData").objectReferenceValue =
            ballDataAsset;
        spawnOption.FindPropertyRelative("spawnProbability").floatValue = 37.5f;

        serializedAsset.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(levelDataAsset.SpawnPoints, Has.Count.EqualTo(1));

        var storedSpawnPoint = levelDataAsset.SpawnPoints[0];
        Assert.That(storedSpawnPoint.Id, Is.EqualTo("west-entry"));
        Assert.That(
            storedSpawnPoint.LocalPosition,
            Is.EqualTo(new Vector2(-4.25f, 2.5f)));
        Assert.That(storedSpawnPoint.SpawnOptions, Has.Count.EqualTo(1));
        Assert.That(
            storedSpawnPoint.SpawnOptions[0].BallData,
            Is.SameAs(ballDataAsset));
        Assert.That(
            storedSpawnPoint.SpawnOptions[0].SpawnProbability,
            Is.EqualTo(37.5f));
    }
}
