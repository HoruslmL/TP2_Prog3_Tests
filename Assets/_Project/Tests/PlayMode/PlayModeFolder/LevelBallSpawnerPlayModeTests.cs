using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class LevelBallSpawnerPlayModeTests
{
    private GameObject factoryObject;
    private GameObject prefabObject;
    private GameObject poolObject;
    private GameObject rootObject;
    private GameObject spawnerObject;

    private BallFactory factory;
    private BallController prefab;
    private BallPool pool;
    private Transform levelRoot;
    private LevelBallSpawner levelBallSpawner;
    private LevelDataAsset levelData;
    private BallDataAsset firstBallData;
    private BallDataAsset secondBallData;

    [SetUp]
    public void SetUp()
    {
        factoryObject = new GameObject("BallFactory");
        factory = factoryObject.AddComponent<BallFactory>();

        prefabObject = new GameObject("BallPrefab");
        prefabObject.SetActive(false);
        prefabObject.AddComponent<Rigidbody2D>();
        prefabObject.AddComponent<CircleCollider2D>();
        prefab = prefabObject.AddComponent<BallController>();

        firstBallData = CreateBallDataAsset("First", prefabObject);
        secondBallData = CreateBallDataAsset("Second", prefabObject);

        poolObject = new GameObject("BallPool");
        pool = poolObject.AddComponent<BallPool>();
        pool.Initialize(factory, prefab, 10);

        rootObject = new GameObject("LevelRoot");
        levelRoot = rootObject.transform;

        spawnerObject = new GameObject("LevelBallSpawner");
        levelBallSpawner = spawnerObject.AddComponent<LevelBallSpawner>();

        levelData = ScriptableObject.CreateInstance<LevelDataAsset>();
        SetSpawnerReferences(levelData, levelRoot, pool);
    }

    [UnityTest]
    public IEnumerator TrySpawnLevel_UsesSelectedDataAndTransformedPositions()
    {
        levelRoot.position = new Vector3(3f, -2f, 0f);
        levelRoot.rotation = Quaternion.Euler(0f, 0f, 90f);
        levelRoot.localScale = new Vector3(2f, 3f, 1f);
        SetSpawnPoints(
            (new Vector2(1f, 0f), firstBallData),
            (new Vector2(0f, 2f), secondBallData));

        bool success = levelBallSpawner.TrySpawnLevel(out string error);

        Assert.IsTrue(success, error);
        Assert.IsNull(error);
        Assert.AreEqual(2, pool.CreatedCount);

        Vector3 expectedFirst = levelRoot.TransformPoint(new Vector3(1f, 0f, 0f));
        Vector3 expectedSecond = levelRoot.TransformPoint(new Vector3(0f, 2f, 0f));
        BallController[] balls = Object.FindObjectsByType<BallController>(
            FindObjectsSortMode.None);

        Assert.AreEqual(2, balls.Length);
        AssertBallAt(balls, expectedFirst, "First", "First Display Name");
        AssertBallAt(balls, expectedSecond, "Second", "Second Display Name");
        Assert.AreEqual(0, levelData.Seed);
        Assert.AreEqual(100f, levelData.SpawnPoints[0].SpawnOptions[0].SpawnProbability);
        Assert.AreSame(firstBallData, levelData.SpawnPoints[0].SpawnOptions[0].BallData);

        yield return null;
    }

    [UnityTest]
    public IEnumerator TrySpawnLevel_WhenAlreadySpawned_RejectsWithoutChangingBalls()
    {
        SetSpawnPoints(
            (new Vector2(0f, 0f), firstBallData),
            (new Vector2(10f, 0f), secondBallData));

        bool firstSuccess = levelBallSpawner.TrySpawnLevel(out string firstError);

        Assert.IsTrue(firstSuccess, firstError);
        BallController[] originalBalls = Object.FindObjectsByType<BallController>(
            FindObjectsSortMode.None);
        Vector3[] originalPositions = new Vector3[originalBalls.Length];
        for (int i = 0; i < originalBalls.Length; i++)
            originalPositions[i] = originalBalls[i].transform.position;

        int originalCreatedCount = pool.CreatedCount;
        int originalActiveCount = GetActiveSpawnedBallCount();

        bool secondSuccess = levelBallSpawner.TrySpawnLevel(out string secondError);

        Assert.IsFalse(secondSuccess);
        StringAssert.Contains("ya generó el nivel", secondError);
        Assert.AreEqual(originalCreatedCount, pool.CreatedCount);
        Assert.AreEqual(originalActiveCount, GetActiveSpawnedBallCount());
        for (int i = 0; i < originalBalls.Length; i++)
        {
            Assert.IsTrue(originalBalls[i].gameObject.activeSelf);
            Assert.AreEqual(originalPositions[i], originalBalls[i].transform.position);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator TrySpawnLevel_WhenValidationFails_DoesNotRequestBalls()
    {
        SetSpawnPoints((Vector2.zero, firstBallData));
        SetSpawnerReferences(levelData, null, pool);

        bool success = levelBallSpawner.TrySpawnLevel(out string error);

        Assert.IsFalse(success);
        StringAssert.Contains(
            LevelDataValidationErrorCode.MissingLevelRoot.ToString(),
            error);
        Assert.AreEqual(0, pool.CreatedCount);

        yield return null;
    }

    [UnityTest]
    public IEnumerator TrySpawnLevel_WhenPoolRunsOut_RollsBackAndAllowsRetry()
    {
        pool.Initialize(factory, prefab, 1);
        SetSpawnPoints(
            (new Vector2(0f, 0f), firstBallData),
            (new Vector2(10f, 0f), secondBallData));

        bool success = levelBallSpawner.TrySpawnLevel(out string error);

        Assert.IsFalse(success);
        StringAssert.Contains("punto-2", error);
        StringAssert.Contains("BallPool", error);
        Assert.AreEqual(1, pool.CreatedCount);
        Assert.AreEqual(0, GetActiveSpawnedBallCount());

        while (pool.AvailableCount == 0)
            yield return null;

        Assert.AreEqual(1, pool.AvailableCount);

        pool.Initialize(factory, prefab, 10);
        bool retrySuccess = levelBallSpawner.TrySpawnLevel(out string retryError);

        Assert.IsTrue(retrySuccess, retryError);
        Assert.AreEqual(2, GetActiveSpawnedBallCount());
    }

    [UnityTest]
    public IEnumerator TrySpawnLevel_WhenFactoryRejectsPrefab_LeavesPoolAndSceneUnchanged()
    {
        prefabObject.SetActive(true);
        SetSpawnPoints((Vector2.zero, firstBallData));
        LogAssert.Expect(
            LogType.Error,
            "BallFactory: el prefab debe estar desactivado.");

        bool success = levelBallSpawner.TrySpawnLevel(out string error);

        Assert.IsFalse(success);
        StringAssert.Contains("no pudo proporcionar una bolita", error);
        Assert.AreEqual(0, pool.CreatedCount);
        Assert.AreEqual(0, pool.AvailableCount);
        Assert.AreEqual(0, GetActiveSpawnedBallCount());

        yield return null;
    }

    private BallDataAsset CreateBallDataAsset(string id, GameObject ballPrefab)
    {
        BallDataAsset asset = ScriptableObject.CreateInstance<BallDataAsset>();
        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("id").stringValue = id;
        serializedAsset.FindProperty("displayName").stringValue =
            id + " Display Name";
        serializedAsset.FindProperty("mass").floatValue = 1f;
        serializedAsset.FindProperty("abilityType").enumValueIndex =
            (int)BallAbilityType.None;
        serializedAsset.FindProperty("effectRadius").floatValue = 0f;
        serializedAsset.FindProperty("effectDelay").floatValue = 0f;
        serializedAsset.FindProperty("scale").floatValue = 1f;
        serializedAsset.FindProperty("ballPrefab").objectReferenceValue = ballPrefab;
        serializedAsset.ApplyModifiedProperties();
        return asset;
    }

    private void SetSpawnerReferences(
        LevelDataAsset data,
        Transform root,
        BallPool ballPool)
    {
        var serializedSpawner = new SerializedObject(levelBallSpawner);
        serializedSpawner.FindProperty("levelData").objectReferenceValue = data;
        serializedSpawner.FindProperty("levelRoot").objectReferenceValue = root;
        serializedSpawner.FindProperty("ballPool").objectReferenceValue = ballPool;
        serializedSpawner.ApplyModifiedProperties();
    }

    private void SetSpawnPoints(
        params (Vector2 position, BallDataAsset data)[] points)
    {
        var serializedLevel = new SerializedObject(levelData);
        SerializedProperty spawnPoints =
            serializedLevel.FindProperty("spawnPoints");
        spawnPoints.arraySize = points.Length;

        for (int i = 0; i < points.Length; i++)
        {
            SerializedProperty point = spawnPoints.GetArrayElementAtIndex(i);
            point.FindPropertyRelative("id").stringValue = "punto-" + (i + 1);
            point.FindPropertyRelative("localPosition").vector2Value =
                points[i].position;

            SerializedProperty options =
                point.FindPropertyRelative("spawnOptions");
            options.arraySize = 1;
            SerializedProperty option = options.GetArrayElementAtIndex(0);
            option.FindPropertyRelative("ballData").objectReferenceValue =
                points[i].data;
            option.FindPropertyRelative("spawnProbability").floatValue = 100f;
        }

        serializedLevel.ApplyModifiedProperties();
    }

    private static void AssertBallAt(
        BallController[] balls,
        Vector3 expectedPosition,
        string expectedDataId,
        string expectedObjectName)
    {
        for (int i = 0; i < balls.Length; i++)
        {
            if (Vector2.Distance(
                    (Vector2)balls[i].transform.position,
                    new Vector2(expectedPosition.x, expectedPosition.y)) > 0.001f)
                continue;

            Assert.AreEqual(expectedDataId, balls[i].Runtime.Data.Id);
            Assert.AreEqual(expectedObjectName, balls[i].gameObject.name);
            return;
        }

        Assert.Fail("No se encontró la bolita esperada en " + expectedPosition + ".");
    }

    private int GetActiveSpawnedBallCount()
    {
        BallController[] balls = Object.FindObjectsByType<BallController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int count = 0;
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i].gameObject != prefabObject &&
                balls[i].gameObject.activeSelf)
                count++;
        }

        return count;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        BallController[] balls = Object.FindObjectsByType<BallController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i] != null && balls[i].gameObject != prefabObject)
                Object.Destroy(balls[i].gameObject);
        }

        if (spawnerObject != null)
            Object.Destroy(spawnerObject);
        if (rootObject != null)
            Object.Destroy(rootObject);
        if (poolObject != null)
            Object.Destroy(poolObject);
        if (factoryObject != null)
            Object.Destroy(factoryObject);
        if (prefabObject != null)
            Object.Destroy(prefabObject);
        if (levelData != null)
            Object.Destroy(levelData);
        if (firstBallData != null)
            Object.Destroy(firstBallData);
        if (secondBallData != null)
            Object.Destroy(secondBallData);

        yield return null;
    }
}
