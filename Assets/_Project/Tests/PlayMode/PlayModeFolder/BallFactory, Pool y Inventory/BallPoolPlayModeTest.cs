using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BallPoolPlayModeTest
{
    private GameObject factoryObject;
    private GameObject prefabObject;
    private GameObject poolObject;

    private BallFactory factory;
    private BallController prefab;
    private BallPool pool;

    private BallData normalData;
    private BallData explosiveData;

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

        normalData = new BallData(
            "Normal",
            1f,
            BallAbilityType.None,
            0f,
            1f,
            1.5f
        );

        explosiveData = new BallData(
            "Explosive",
            1f,
            BallAbilityType.Explosive,
            0.1f,
            1f,
            1.5f
        );

        poolObject = new GameObject("BallPool");
        pool = poolObject.AddComponent<BallPool>();

        pool.Initialize(factory, prefab, 2);
    }

    [UnityTest]
    public IEnumerator GetBall_CreatesInitializedBall()
    {
        BallController ball = pool.GetBall(
            normalData,
            Vector2.zero
        );

        Assert.IsNotNull(ball);
        Assert.IsTrue(ball.gameObject.activeSelf);

        Assert.AreEqual(
            BallRuntimeState.Active,
            ball.Runtime.State
        );

        Assert.AreEqual(
            "Normal",
            ball.Runtime.Data.Id
        );

        Assert.AreEqual(1, pool.CreatedCount);

        yield return null;
    }

    [UnityTest]
    public IEnumerator ReturnBall_ThenGetBall_ReusesAndResetsInstance()
    {
        BallController firstBall = pool.GetBall(
            normalData,
            Vector2.zero
        );

        Rigidbody2D rb = firstBall.GetComponent<Rigidbody2D>();

        // Simulamos el uso de la bolita.
        rb.linearVelocity = new Vector2(5f, 8f);
        rb.angularVelocity = 10f;
        rb.mass = 99f;

        firstBall.transform.localScale = Vector3.one * 3f;

        // La devolvemos al pool.
        firstBall.Despawn();

        Assert.IsFalse(firstBall.gameObject.activeSelf);

        // Esperamos a que termine la limpieza.
        while (pool.AvailableCount == 0)
            yield return null;

        BallData newData = new BallData(
            "NewNormal",
            2f,
            BallAbilityType.None,
            0f,
            1f,
            2f
        );

        Vector2 newPosition = new Vector2(3f, 5f);

        BallController reusedBall = pool.GetBall(
            newData,
            newPosition
        );

        Assert.AreSame(firstBall, reusedBall);
        Assert.IsTrue(reusedBall.gameObject.activeSelf);

        Assert.AreEqual(
            BallRuntimeState.Active,
            reusedBall.Runtime.State
        );

        Assert.AreEqual("NewNormal", reusedBall.Runtime.Data.Id);
        Assert.AreEqual(newPosition, (Vector2)reusedBall.transform.position);

        Assert.AreEqual(2f, rb.mass);
        Assert.AreEqual(Vector2.zero, rb.linearVelocity);
        Assert.AreEqual(0f, rb.angularVelocity);
        Assert.AreEqual(Vector3.one * 2f, reusedBall.transform.localScale);

        Assert.AreEqual(1, pool.CreatedCount);
        Assert.AreEqual(0, pool.AvailableCount);

        yield return null;
    }

    [UnityTest]
    public IEnumerator GetBall_WhenLimitReached_ReturnsNull()
    {
        BallController firstBall = pool.GetBall(
            normalData,
            Vector2.zero
        );

        BallController secondBall = pool.GetBall(
            normalData,
            Vector2.one
        );

        BallController thirdBall = pool.GetBall(
            normalData,
            Vector2.zero
        );

        Assert.IsNotNull(firstBall);
        Assert.IsNotNull(secondBall);
        Assert.IsNull(thirdBall);

        Assert.AreEqual(2, pool.CreatedCount);

        yield return null;
    }

    [UnityTest]
    public IEnumerator ExplosiveBall_AfterExplosion_CanBeReusedAsNormal()
    {
        BallController explosiveBall = pool.GetBall(
            explosiveData,
            Vector2.zero
        );

        Assert.IsNotNull(explosiveBall);

        ExplosiveEffectExecutor executor =
            explosiveBall.GetComponent<ExplosiveEffectExecutor>();

        Assert.IsNotNull(executor);

        // Simulamos que el efecto está listo para ejecutarse.
        Assert.IsTrue(explosiveBall.Runtime.TryScheduleEffect());
        Assert.IsTrue(explosiveBall.Runtime.TryMarkEffectExecuted());

        executor.Execute(explosiveBall.Runtime);

        // Esperamos a que el pool termine de limpiarla.
        while (pool.AvailableCount == 0)
            yield return null;

        BallController reusedBall = pool.GetBall(
            normalData,
            new Vector2(3f, 5f)
        );

        Assert.AreSame(explosiveBall, reusedBall);
        Assert.IsTrue(reusedBall.gameObject.activeSelf);

        Assert.IsNull(
            reusedBall.GetComponent<ExplosiveEffectExecutor>()
        );

        Assert.AreEqual(
            BallAbilityType.None,
            reusedBall.Runtime.Data.AbilityType
        );

        Assert.AreEqual(
            BallRuntimeState.Active,
            reusedBall.Runtime.State
        );

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

        if (poolObject != null)
            Object.Destroy(poolObject);

        if (factoryObject != null)
            Object.Destroy(factoryObject);

        if (prefabObject != null)
            Object.Destroy(prefabObject);

        yield return null;
    }
}
