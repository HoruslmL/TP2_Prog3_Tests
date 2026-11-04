using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class BallFactoryPlayModeTests
{
    private GameObject factoryObject;
    private GameObject prefabObject;

    private BallFactory factory;
    private BallController prefab;
    private BallController createdBall;

    private BallData data;

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

        data = new BallData(
            "TestBall",
            1f,
            BallAbilityType.None,
            0f,
            1f,
            1.5f
        );
    }

    [UnityTest]
    public IEnumerator Create_InitializesBallCorrectly()
    {
        Vector2 expectedPosition = new Vector2(3f, 5f);

        createdBall = factory.Create(
            prefab,
            data,
            expectedPosition
        );

        Assert.IsNotNull(createdBall);
        Assert.IsNotNull(createdBall.Runtime);

        Assert.IsTrue(createdBall.gameObject.activeSelf);

        Assert.AreEqual(
            BallRuntimeState.Active,
            createdBall.Runtime.State
        );

        Assert.AreEqual(
            expectedPosition,
            (Vector2)createdBall.transform.position
        );

        Assert.AreEqual(
            "TestBall",
            createdBall.Runtime.Data.Id
        );

        Assert.AreEqual(
            1.5f,
            createdBall.transform.localScale.x
        );

        Assert.AreEqual(
            1f,
            createdBall.GetComponent<Rigidbody2D>().mass
        );

        yield return null;
    }

    [Test]
    public void Create_WithNullPrefab_ReturnsNull()
    {
        LogAssert.Expect(
            LogType.Error,
            "BallFactory: falta asignar el prefab."
        );

        createdBall = factory.Create(
            null,
            data,
            Vector2.zero
        );

        Assert.IsNull(createdBall);
    }

    [Test]
    public void Create_WithNullData_ReturnsNull()
    {
        LogAssert.Expect(
            LogType.Error,
            "BallFactory: BallData es null."
        );

        createdBall = factory.Create(
            prefab,
            null,
            Vector2.zero
        );

        Assert.IsNull(createdBall);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (createdBall != null)
            Object.Destroy(createdBall.gameObject);

        if (factoryObject != null)
            Object.Destroy(factoryObject);

        if (prefabObject != null)
            Object.Destroy(prefabObject);

        yield return null;
    }
}
