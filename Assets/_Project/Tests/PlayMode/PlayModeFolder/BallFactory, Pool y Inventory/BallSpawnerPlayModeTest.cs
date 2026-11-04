using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class BallSpawnerPlayModeTest
{
    private GameObject factoryObject;
    private GameObject prefabObject;
    private GameObject poolObject;
    private GameObject spawnerObject;

    private BallFactory factory;
    private BallController prefab;
    private BallPool pool;
    private BallSpawner spawner;

    private BallDataAsset normalAsset;
    private Inventory inventory;

    [SetUp]
    public void SetUp()
    {
        // Creamos la Factory.
        factoryObject = new GameObject("BallFactory");
        factory = factoryObject.AddComponent<BallFactory>();

        // Creamos el prefab neutral.
        prefabObject = new GameObject("BallPrefab");
        prefabObject.SetActive(false);

        prefabObject.AddComponent<Rigidbody2D>();
        prefabObject.AddComponent<CircleCollider2D>();

        prefab = prefabObject.AddComponent<BallController>();

        // Creamos los datos de la bolita normal.
        normalAsset = ScriptableObject.CreateInstance<BallDataAsset>();

        SerializedObject serializedAsset =
            new SerializedObject(normalAsset);

        serializedAsset.FindProperty("id").stringValue = "NormalTest";
        serializedAsset.FindProperty("mass").floatValue = 1f;

        serializedAsset.FindProperty("abilityType").enumValueIndex =
            (int)BallAbilityType.None;

        serializedAsset.FindProperty("effectRadius").floatValue = 0f;
        serializedAsset.FindProperty("effectDelay").floatValue = 1f;
        serializedAsset.FindProperty("scale").floatValue = 1.5f;

        serializedAsset.ApplyModifiedProperties();

        // Creamos e inicializamos el Pool.
        poolObject = new GameObject("BallPool");
        pool = poolObject.AddComponent<BallPool>();

        pool.Initialize(factory, prefab, 2);

        // Creamos el Spawner y asignamos sus referencias.
        spawnerObject = new GameObject("BallSpawner");
        spawner = spawnerObject.AddComponent<BallSpawner>();

        SerializedObject serializedSpawner =
            new SerializedObject(spawner);

        serializedSpawner.FindProperty("ballPool").objectReferenceValue = pool;
        serializedSpawner.FindProperty("normalBallData").objectReferenceValue = normalAsset;

        serializedSpawner.ApplyModifiedProperties();

        inventory = new Inventory();
        spawner.Initialize(inventory);
    }

    [UnityTest]
    public IEnumerator SpawnBall_WithAvailableInventory_CreatesBallAndConsumesInventory()
    {
        inventory.AddBall(BallAbilityType.None, 1);

        BallController ball = spawner.SpawnBall(
            BallAbilityType.None,
            new Vector2(3f, 5f)
        );

        Assert.IsNotNull(ball);
        Assert.IsTrue(ball.gameObject.activeSelf);

        Assert.AreEqual(
            "NormalTest",
            ball.Runtime.Data.Id
        );

        Assert.AreEqual(
            new Vector2(3f, 5f),
            (Vector2)ball.transform.position
        );

        Assert.AreEqual(
            0,
            inventory.GetBallCount(BallAbilityType.None)
        );

        Assert.AreEqual(1, pool.CreatedCount);

        yield return null;
    }

    [UnityTest]
    public IEnumerator SpawnBall_WithEmptyInventory_ReturnsNull()
    {
        BallController ball = spawner.SpawnBall(
            BallAbilityType.None,
            Vector2.zero
        );

        Assert.IsNull(ball);

        Assert.AreEqual(
            0,
            inventory.GetBallCount(BallAbilityType.None)
        );

        Assert.AreEqual(0, pool.CreatedCount);

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        BallController[] balls =
            Object.FindObjectsByType<BallController>(
                FindObjectsSortMode.None
            );

        foreach (BallController ball in balls)
        {
            if (ball != null && ball.gameObject != prefabObject)
                Object.Destroy(ball.gameObject);
        }

        if (spawnerObject != null)
            Object.Destroy(spawnerObject);

        if (poolObject != null)
            Object.Destroy(poolObject);

        if (factoryObject != null)
            Object.Destroy(factoryObject);

        if (prefabObject != null)
            Object.Destroy(prefabObject);

        if (normalAsset != null)
            Object.Destroy(normalAsset);

        yield return null;
    }
}
