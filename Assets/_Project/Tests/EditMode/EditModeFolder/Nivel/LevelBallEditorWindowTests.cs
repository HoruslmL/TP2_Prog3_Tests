using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class LevelBallEditorWindowTests
{
    private readonly List<Object> objectsToDestroy = new List<Object>();
    private LevelDataAsset levelDataAsset;

    [SetUp]
    public void SetUp()
    {
        levelDataAsset = ScriptableObject.CreateInstance<LevelDataAsset>();
        objectsToDestroy.Add(levelDataAsset);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
        {
            Undo.ClearUndo(objectsToDestroy[i]);
            Object.DestroyImmediate(objectsToDestroy[i]);
        }

        objectsToDestroy.Clear();
    }

    [Test]
    public void AddSpawnPoint_PreservesExistingPoints()
    {
        SetSpawnPoints(
            new SpawnPointSpec(
                "existing",
                new Vector2(1f, 2f),
                new SpawnOptionSpec[0]));

        string newId = LevelBallEditorAssetOperations.AddSpawnPoint(
            levelDataAsset,
            new Vector2(3f, 4f));

        Assert.That(levelDataAsset.SpawnPoints, Has.Count.EqualTo(2));
        Assert.That(levelDataAsset.SpawnPoints[0].Id, Is.EqualTo("existing"));
        Assert.That(
            levelDataAsset.SpawnPoints[0].LocalPosition,
            Is.EqualTo(new Vector2(1f, 2f)));
        Assert.That(levelDataAsset.SpawnPoints[1].Id, Is.EqualTo(newId));
        Assert.That(
            levelDataAsset.SpawnPoints[1].LocalPosition,
            Is.EqualTo(new Vector2(3f, 4f)));
    }

    [Test]
    public void AddSpawnPoint_StoresLocalPositionAndCreatesEmptyOptions()
    {
        Vector2 localPosition = new Vector2(-2.5f, 4.25f);

        LevelBallEditorAssetOperations.AddSpawnPoint(
            levelDataAsset,
            localPosition);

        BallSpawnPointData addedPoint = levelDataAsset.SpawnPoints[0];
        Assert.That(addedPoint.LocalPosition, Is.EqualTo(localPosition));
        Assert.That(addedPoint.SpawnOptions, Is.Empty);
    }

    [Test]
    public void AddSpawnPoint_GeneratesUniqueIdsAroundExistingIds()
    {
        SetSpawnPoints(
            new SpawnPointSpec(
                "spawn-point-001",
                Vector2.zero,
                new SpawnOptionSpec[0]),
            new SpawnPointSpec(
                "spawn-point-003",
                Vector2.one,
                new SpawnOptionSpec[0]));

        string firstGeneratedId =
            LevelBallEditorAssetOperations.AddSpawnPoint(
                levelDataAsset,
                Vector2.zero);
        string secondGeneratedId =
            LevelBallEditorAssetOperations.AddSpawnPoint(
                levelDataAsset,
                Vector2.one);

        Assert.That(firstGeneratedId, Is.EqualTo("spawn-point-002"));
        Assert.That(secondGeneratedId, Is.EqualTo("spawn-point-004"));
        Assert.That(
            levelDataAsset.SpawnPoints[2].Id,
            Is.EqualTo(firstGeneratedId));
        Assert.That(
            levelDataAsset.SpawnPoints[3].Id,
            Is.EqualTo(secondGeneratedId));
    }

    [Test]
    public void TryGetLocalPosition_HandlesTranslatedRotatedAndScaledRoot()
    {
        var rootObject = new GameObject("LevelRoot");
        objectsToDestroy.Add(rootObject);
        Transform root = rootObject.transform;
        root.position = new Vector3(7f, -3f, 2f);
        root.rotation = Quaternion.Euler(17f, 31f, -23f);
        root.localScale = new Vector3(2f, 3f, 0.5f);

        Vector3 expectedLocalPosition = new Vector3(1.75f, -2.25f, 0f);
        Vector3 worldPosition = root.TransformPoint(expectedLocalPosition);
        Vector3 planeNormal = Vector3.Cross(
            root.TransformVector(Vector3.right),
            root.TransformVector(Vector3.up)).normalized;
        var ray = new Ray(
            worldPosition + planeNormal * 10f,
            -planeNormal);

        bool success = LevelBallEditorAssetOperations.TryGetLocalPosition(
            root,
            ray,
            out Vector2 actualLocalPosition);

        Assert.That(success, Is.True);
        Assert.That(
            actualLocalPosition.x,
            Is.EqualTo(expectedLocalPosition.x).Within(0.0001f));
        Assert.That(
            actualLocalPosition.y,
            Is.EqualTo(expectedLocalPosition.y).Within(0.0001f));
    }

    [Test]
    public void TryGetLocalPosition_RejectsRayParallelToLevelPlane()
    {
        var rootObject = new GameObject("LevelRoot");
        objectsToDestroy.Add(rootObject);
        Transform root = rootObject.transform;
        Vector3 worldXAxis = root.TransformVector(Vector3.right).normalized;
        var ray = new Ray(Vector3.zero, worldXAxis);

        bool success = LevelBallEditorAssetOperations.TryGetLocalPosition(
            root,
            ray,
            out Vector2 localPosition);

        Assert.That(success, Is.False);
        Assert.That(localPosition, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void TryGetLocalPosition_RejectsNonFiniteRayOrigin()
    {
        var rootObject = new GameObject("LevelRoot");
        objectsToDestroy.Add(rootObject);
        var ray = new Ray(
            new Vector3(float.NaN, 0f, 1f),
            Vector3.down);

        bool success = LevelBallEditorAssetOperations.TryGetLocalPosition(
            rootObject.transform,
            ray,
            out Vector2 localPosition);

        Assert.That(success, Is.False);
        Assert.That(localPosition, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void TryGetLocalPosition_RejectsNonFiniteRayDirection()
    {
        var rootObject = new GameObject("LevelRoot");
        objectsToDestroy.Add(rootObject);
        var ray = new Ray(
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, float.PositiveInfinity));

        bool success = LevelBallEditorAssetOperations.TryGetLocalPosition(
            rootObject.transform,
            ray,
            out Vector2 localPosition);

        Assert.That(success, Is.False);
        Assert.That(localPosition, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void AddSpawnPoint_SupportsUndoAndRedoAndMarksAssetDirty()
    {
        EditorUtility.ClearDirty(levelDataAsset);
        Assert.That(EditorUtility.IsDirty(levelDataAsset), Is.False);

        string addedId = LevelBallEditorAssetOperations.AddSpawnPoint(
            levelDataAsset,
            new Vector2(2f, 5f));

        Assert.That(levelDataAsset.SpawnPoints, Has.Count.EqualTo(1));
        Assert.That(EditorUtility.IsDirty(levelDataAsset), Is.True);

        Undo.PerformUndo();
        Assert.That(levelDataAsset.SpawnPoints, Is.Empty);

        Undo.PerformRedo();
        Assert.That(levelDataAsset.SpawnPoints, Has.Count.EqualTo(1));
        Assert.That(levelDataAsset.SpawnPoints[0].Id, Is.EqualTo(addedId));
        Assert.That(
            levelDataAsset.SpawnPoints[0].LocalPosition,
            Is.EqualTo(new Vector2(2f, 5f)));
    }

    [Test]
    public void TryGetMaximumSelectableGeometry_UsesLargestPositiveRadiusAndOffset()
    {
        BallDataAsset smallBall =
            CreateBallDataAsset("small", 0.5f, 1f, new Vector2(0.1f, 0f));
        BallDataAsset largeBall =
            CreateBallDataAsset("large", 0.4f, 2f, new Vector2(0.25f, -0.5f));
        BallDataAsset zeroProbabilityBall =
            CreateBallDataAsset("unused", 2f, 1f, Vector2.zero);
        SetSpawnPoints(
            new SpawnPointSpec(
                "entry",
                Vector2.zero,
                new[]
                {
                    new SpawnOptionSpec(smallBall, 50f),
                    new SpawnOptionSpec(largeBall, 50f),
                    new SpawnOptionSpec(zeroProbabilityBall, 0f),
                    new SpawnOptionSpec(null, 0f)
                }));

        bool success =
            BallColliderGeometry.TryGetMaximumSelectableGeometry(
                levelDataAsset.SpawnPoints[0],
                out float radius,
                out Vector2 centerOffset);

        Assert.That(success, Is.True);
        Assert.That(radius, Is.EqualTo(0.8f).Within(0.0001f));
        Assert.That(centerOffset, Is.EqualTo(new Vector2(0.5f, -1f)));
    }

    [Test]
    public void TryGetMaximumSelectableGeometry_RejectsMissingOptionsOrInvalidPositiveGeometry()
    {
        SetSpawnPoints(
            new SpawnPointSpec(
                "empty",
                Vector2.zero,
                new SpawnOptionSpec[0]));
        Assert.That(
            BallColliderGeometry.TryGetMaximumSelectableGeometry(
                levelDataAsset.SpawnPoints[0],
                out _,
                out _),
            Is.False);

        BallDataAsset validBall =
            CreateBallDataAsset("valid", 0.5f, 1f, Vector2.zero);
        BallDataAsset invalidBall = CreateBallDataAssetWithoutCollider();
        SetSpawnPoints(
            new SpawnPointSpec(
                "invalid",
                Vector2.zero,
                new[]
                {
                    new SpawnOptionSpec(validBall, 50f),
                    new SpawnOptionSpec(invalidBall, 50f)
                }));

        Assert.That(
            BallColliderGeometry.TryGetMaximumSelectableGeometry(
                levelDataAsset.SpawnPoints[0],
                out float radius,
                out Vector2 centerOffset),
            Is.False);
        Assert.That(radius, Is.EqualTo(0f));
        Assert.That(centerOffset, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void TryGetMaximumSelectableGeometry_ReflectsOptionAndSizeChanges()
    {
        BallDataAsset ballData =
            CreateBallDataAsset("ball", 0.5f, 1f, Vector2.zero);
        GameObject prefab = ballData.BallPrefab;
        SetSpawnPoints(
            new SpawnPointSpec(
                "entry",
                Vector2.zero,
                new[] { new SpawnOptionSpec(ballData, 100f) }));

        Assert.That(
            BallColliderGeometry.TryGetMaximumSelectableGeometry(
                levelDataAsset.SpawnPoints[0],
                out float initialRadius,
                out _),
            Is.True);
        Assert.That(initialRadius, Is.EqualTo(0.5f));

        var serializedBallData = new SerializedObject(ballData);
        serializedBallData.FindProperty("scale").floatValue = 2f;
        Assert.That(serializedBallData.ApplyModifiedPropertiesWithoutUndo(), Is.True);
        prefab.GetComponent<CircleCollider2D>().radius = 0.75f;

        Assert.That(
            BallColliderGeometry.TryGetMaximumSelectableGeometry(
                levelDataAsset.SpawnPoints[0],
                out float updatedRadius,
                out _),
            Is.True);
        Assert.That(updatedRadius, Is.EqualTo(1.5f));
    }

    private void SetSpawnPoints(params SpawnPointSpec[] points)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        SerializedProperty spawnPoints =
            serializedAsset.FindProperty("spawnPoints");
        spawnPoints.arraySize = points.Length;

        for (int pointIndex = 0; pointIndex < points.Length; pointIndex++)
        {
            SpawnPointSpec pointSpec = points[pointIndex];
            SerializedProperty spawnPoint =
                spawnPoints.GetArrayElementAtIndex(pointIndex);
            spawnPoint.FindPropertyRelative("id").stringValue = pointSpec.Id;
            spawnPoint.FindPropertyRelative("localPosition").vector2Value =
                pointSpec.LocalPosition;

            SerializedProperty options =
                spawnPoint.FindPropertyRelative("spawnOptions");
            options.arraySize = pointSpec.Options.Length;
            for (int optionIndex = 0;
                 optionIndex < pointSpec.Options.Length;
                 optionIndex++)
            {
                SpawnOptionSpec optionSpec = pointSpec.Options[optionIndex];
                SerializedProperty option =
                    options.GetArrayElementAtIndex(optionIndex);
                option.FindPropertyRelative("ballData").objectReferenceValue =
                    optionSpec.BallData;
                option.FindPropertyRelative("spawnProbability").floatValue =
                    optionSpec.Probability;
            }
        }

        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
    }

    private BallDataAsset CreateBallDataAsset(
        string id,
        float colliderRadius,
        float scale,
        Vector2 colliderOffset)
    {
        var prefab = new GameObject(id + "Prefab");
        objectsToDestroy.Add(prefab);
        CircleCollider2D collider = prefab.AddComponent<CircleCollider2D>();
        collider.radius = colliderRadius;
        collider.offset = colliderOffset;

        BallDataAsset asset = ScriptableObject.CreateInstance<BallDataAsset>();
        objectsToDestroy.Add(asset);
        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("id").stringValue = id;
        serializedAsset.FindProperty("scale").floatValue = scale;
        serializedAsset.FindProperty("ballPrefab").objectReferenceValue = prefab;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    private BallDataAsset CreateBallDataAssetWithoutCollider()
    {
        var prefab = new GameObject("TestBallPrefabWithoutCircleCollider");
        objectsToDestroy.Add(prefab);

        BallDataAsset asset = ScriptableObject.CreateInstance<BallDataAsset>();
        objectsToDestroy.Add(asset);
        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("id").stringValue = "invalid";
        serializedAsset.FindProperty("scale").floatValue = 1f;
        serializedAsset.FindProperty("ballPrefab").objectReferenceValue = prefab;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    private sealed class SpawnPointSpec
    {
        public readonly string Id;
        public readonly Vector2 LocalPosition;
        public readonly SpawnOptionSpec[] Options;

        public SpawnPointSpec(
            string id,
            Vector2 localPosition,
            SpawnOptionSpec[] options)
        {
            Id = id;
            LocalPosition = localPosition;
            Options = options;
        }
    }

    private sealed class SpawnOptionSpec
    {
        public readonly BallDataAsset BallData;
        public readonly float Probability;

        public SpawnOptionSpec(BallDataAsset ballData, float probability)
        {
            BallData = ballData;
            Probability = probability;
        }
    }
}
