using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class LevelDataValidatorTests
{
    private readonly List<Object> objectsToDestroy = new List<Object>();
    private LevelDataAsset levelDataAsset;
    private GameObject levelRoot;

    [SetUp]
    public void SetUp()
    {
        levelDataAsset = ScriptableObject.CreateInstance<LevelDataAsset>();
        objectsToDestroy.Add(levelDataAsset);
        levelRoot = new GameObject("LevelRoot");
        objectsToDestroy.Add(levelRoot);
    }

    [TearDown]
    public void TearDown()
    {
        for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(objectsToDestroy[i]);

        objectsToDestroy.Clear();
    }

    [Test]
    public void Validate_NullLevel_ReturnsIssueWithoutThrowing()
    {
        LevelDataValidationResult result =
            LevelDataValidator.Validate(null, levelRoot.transform);

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.MissingLevelData);
    }

    [Test]
    public void Validate_NullLevelRoot_ReturnsIssue()
    {
        LevelDataValidationResult result =
            LevelDataValidator.Validate(levelDataAsset, null);

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.MissingLevelRoot);
    }

    [Test]
    public void Validate_LevelWithoutSpawnPoints_ReturnsIssue()
    {
        LevelDataValidationResult result = Validate();

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.NoSpawnPoints);
    }

    [Test]
    public void Validate_SpawnPointWithoutOptions_ReturnsIssue()
    {
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.NoSpawnOptions);
    }

    [Test]
    public void Validate_ValidLevel_ReturnsNoIssues()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballData, 100f)),
            Point("entry-b", new Vector2(1f, 0f), Option(ballData, 100f)));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Issues, Is.Empty);
    }

    [Test]
    public void ExistingBallDataAssets_ReferenceTheirPhysicalPrefabs()
    {
        BallDataAsset normalBallData = AssetDatabase.LoadAssetAtPath<BallDataAsset>(
            "Assets/_Project/Scripts/Balls/ScriptableData/Ball_Normal.asset");
        BallDataAsset explosiveBallData = AssetDatabase.LoadAssetAtPath<BallDataAsset>(
            "Assets/_Project/Scripts/Balls/ScriptableData/Ball_Explosive.asset");
        GameObject normalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Scripts/Balls/Prefabs/NormalBall.prefab");
        GameObject explosivePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Scripts/Balls/Prefabs/ExplosiveBall.prefab");

        Assert.That(normalBallData, Is.Not.Null);
        Assert.That(explosiveBallData, Is.Not.Null);
        Assert.That(normalBallData.BallPrefab, Is.SameAs(normalPrefab));
        Assert.That(explosiveBallData.BallPrefab, Is.SameAs(explosivePrefab));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_EmptyOrWhitespaceIdentifier_ReturnsIssue(string id)
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(id, Vector2.zero, Option(ballData, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.EmptySpawnPointId);
    }

    [Test]
    public void Validate_DuplicateIdentifiers_ReturnsIssueForRepeatedPoint()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(ballData, 100f)),
            Point("entry", new Vector2(2f, 0f), Option(ballData, 100f)));

        LevelDataValidationIssue issue = result.Issues.First(
            candidate =>
                candidate.Code ==
                LevelDataValidationErrorCode.DuplicateSpawnPointId);

        Assert.That(issue.SpawnPointIndex, Is.EqualTo(1));
        Assert.That(issue.SpawnPointId, Is.EqualTo("entry"));
    }

    [Test]
    public void Validate_NullBallDataAsset_ReturnsIssue()
    {
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(null, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.NullBallDataAsset);
    }

    [Test]
    public void Validate_ProbabilityAtZeroIsAllowedWithAnotherPositiveOption()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(
                "entry",
                Vector2.zero,
                Option(ballData, 0f),
                Option(ballData, 100f)));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_ProbabilityAtOneHundredIsValid()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(ballData, 100f)));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase(-0.01f)]
    [TestCase(100.01f)]
    public void Validate_ProbabilityOutsideRangeReturnsIssue(float probability)
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(ballData, probability)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.ProbabilityOutOfRange);
    }

    [TestCase(49.995f, 50f)]
    [TestCase(50f, 50.005f)]
    public void Validate_ProbabilityTotalWithinToleranceIsValid(
        float probabilityA,
        float probabilityB)
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(
                "entry",
                Vector2.zero,
                Option(ballData, probabilityA),
                Option(ballData, probabilityB)));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_ProbabilityTotalOutsideToleranceReturnsIssue()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(
                "entry",
                Vector2.zero,
                Option(ballData, 49.98f),
                Option(ballData, 50f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.ProbabilityTotalNot100);
    }

    [Test]
    public void Validate_NoPositiveProbabilityOptionReturnsIssue()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(
                "entry",
                Vector2.zero,
                Option(ballData, 0f),
                Option(ballData, 0f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.NoPositiveProbabilityOption);
    }

    [Test]
    public void Validate_PositiveOptionWithoutPrefabReturnsIssue()
    {
        BallDataAsset ballData = CreateBallDataAssetWithoutPrefab();
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(ballData, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.MissingBallPrefab);
    }

    [Test]
    public void Validate_PrefabWithoutCircleColliderReturnsIssue()
    {
        BallDataAsset ballData = CreateBallDataAssetWithoutCollider();
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(ballData, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.MissingCircleCollider);
    }

    [Test]
    public void Validate_MultipleRootCircleCollidersReturnUnsupportedConfigurationIssue()
    {
        BallDataAsset ballData =
            CreateBallDataAssetWithMultipleRootColliders(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(ballData, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.UnsupportedMultipleRootCircleColliders);
    }

    [Test]
    public void Validate_CircleColliderRadiusRatherThanEffectRadiusDeterminesOverlap()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballData, 100f)),
            Point("entry-b", new Vector2(1f, 0f), Option(ballData, 100f)));

        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_PointsAtContactBoundaryDoNotOverlap()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballData, 100f)),
            Point("entry-b", new Vector2(1f, 0f), Option(ballData, 100f)));

        AssertIssueAbsent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_UnitScaleLevelRootPreservesContactBoundary()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        levelRoot.transform.position = new Vector3(5f, -3f, 0f);
        levelRoot.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        levelRoot.transform.localScale = Vector3.one;

        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballData, 100f)),
            Point("entry-b", new Vector2(1f, 0f), Option(ballData, 100f)));

        AssertIssueAbsent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [TestCase(0.6f, 0f, false)]
    [TestCase(0f, 0.6f, true)]
    public void Validate_TransformsSpawnPositionsThroughNonUnitLevelRoot(
        float localX,
        float localY,
        bool overlaps)
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        levelRoot.transform.position = new Vector3(5f, -3f, 0f);
        levelRoot.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        levelRoot.transform.localScale = new Vector3(2f, 1f, 1f);

        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballData, 100f)),
            Point(
                "entry-b",
                new Vector2(localX, localY),
                Option(ballData, 100f)));

        Assert.That(
            HasIssue(
                result,
                LevelDataValidationErrorCode.OverlappingSpawnPoints),
            Is.EqualTo(overlaps));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void Validate_NonFiniteLocalPositionReturnsIssueWithoutOverlap(
        float localX)
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(
                "invalid",
                new Vector2(localX, 0f),
                Option(ballData, 100f)),
            Point(
                "valid",
                Vector2.zero,
                Option(ballData, 100f)));

        LevelDataValidationIssue issue = result.Issues.First(
            candidate =>
                candidate.Code ==
                LevelDataValidationErrorCode.InvalidSpawnPointWorldPosition);

        Assert.That(issue.SpawnPointIndex, Is.EqualTo(0));
        Assert.That(issue.SpawnPointId, Is.EqualTo("invalid"));
        AssertIssueAbsent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_NonFiniteLevelRootTransformReturnsIssueWithoutOverlap()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        levelRoot.transform.localScale =
            new Vector3(float.MaxValue, 1f, 1f);

        LevelDataValidationResult result = Validate(
            Point("entry-a", new Vector2(2f, 0f), Option(ballData, 100f)),
            Point("entry-b", new Vector2(3f, 0f), Option(ballData, 100f)));

        LevelDataValidationIssue issue = result.Issues.First(
            candidate =>
                candidate.Code ==
                LevelDataValidationErrorCode.InvalidSpawnPointWorldPosition);

        Assert.That(issue.SpawnPointIndex, Is.EqualTo(0));
        Assert.That(issue.SpawnPointId, Is.EqualTo("entry-a"));
        AssertIssueAbsent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_CollidersWithSameDirectionOffsetsUseTheirPhysicalCenters()
    {
        BallDataAsset ballDataA =
            CreateBallDataAsset(0.5f, new Vector2(0.2f, 0f));
        BallDataAsset ballDataB =
            CreateBallDataAsset(0.5f, new Vector2(0.2f, 0f));
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballDataA, 100f)),
            Point("entry-b", new Vector2(1f, 0f), Option(ballDataB, 100f)));

        AssertIssueAbsent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_CollidersWithOppositeOffsetsOverlap()
    {
        BallDataAsset ballDataA =
            CreateBallDataAsset(0.5f, new Vector2(0.2f, 0f));
        BallDataAsset ballDataB =
            CreateBallDataAsset(0.5f, new Vector2(-0.2f, 0f));
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballDataA, 100f)),
            Point("entry-b", new Vector2(1f, 0f), Option(ballDataB, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_SeparatedPointsDoNotOverlap()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballData, 100f)),
            Point("entry-b", new Vector2(1.25f, 0f), Option(ballData, 100f)));

        AssertIssueAbsent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_OverlappingPointsReturnsIssueForBothPoints()
    {
        BallDataAsset ballData = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(ballData, 100f)),
            Point("entry-b", new Vector2(0.99f, 0f), Option(ballData, 100f)));

        LevelDataValidationIssue issue = result.Issues.First(
            candidate =>
                candidate.Code ==
                LevelDataValidationErrorCode.OverlappingSpawnPoints);

        Assert.That(issue.SpawnPointIndex, Is.EqualTo(0));
        Assert.That(issue.SpawnPointId, Is.EqualTo("entry-a"));
        Assert.That(issue.RelatedSpawnPointIndex, Is.EqualTo(1));
        Assert.That(issue.RelatedSpawnPointId, Is.EqualTo("entry-b"));
    }

    [TestCase(1.125f, false)]
    [TestCase(1.12f, true)]
    public void Validate_UsesRuntimeBallScaleForEffectiveRadius(
        float distance,
        bool overlaps)
    {
        BallDataAsset scaledBall = CreateBallDataAsset(0.5f, 1.25f);
        BallDataAsset normalBall = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry-a", Vector2.zero, Option(scaledBall, 100f)),
            Point("entry-b", new Vector2(distance, 0f), Option(normalBall, 100f)));

        Assert.That(
            HasIssue(
                result,
                LevelDataValidationErrorCode.OverlappingSpawnPoints),
            Is.EqualTo(overlaps));
    }

    [Test]
    public void Validate_UsesLargestPositiveOptionRadius()
    {
        BallDataAsset smallBall = CreateBallDataAsset(0.25f);
        BallDataAsset largeBall = CreateBallDataAsset(0.5f, 1.25f);
        BallDataAsset otherBall = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(
                "entry-a",
                Vector2.zero,
                Option(smallBall, 50f),
                Option(largeBall, 50f)),
            Point(
                "entry-b",
                new Vector2(1.1f, 0f),
                Option(otherBall, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_MultipleOptionsUseCombinationWithMinimumClearance()
    {
        BallDataAsset offsetSmallBall =
            CreateBallDataAsset(0.2f, new Vector2(0.4f, 0f));
        BallDataAsset centeredLargeBall = CreateBallDataAsset(0.5f);
        BallDataAsset oppositeOffsetSmallBall =
            CreateBallDataAsset(0.2f, new Vector2(-0.4f, 0f));
        BallDataAsset centeredMediumBall = CreateBallDataAsset(0.3f);
        LevelDataValidationResult result = Validate(
            Point(
                "entry-a",
                Vector2.zero,
                Option(offsetSmallBall, 50f),
                Option(centeredLargeBall, 50f)),
            Point(
                "entry-b",
                new Vector2(1.1f, 0f),
                Option(oppositeOffsetSmallBall, 50f),
                Option(centeredMediumBall, 50f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
    }

    [Test]
    public void Validate_ZeroProbabilityOptionDoesNotIncreaseMaximumRadius()
    {
        BallDataAsset activeBall = CreateBallDataAsset(0.25f);
        BallDataAsset inactiveLargeBall =
            CreateBallDataAsset(2f, new Vector2(0.8f, 0f));
        BallDataAsset otherBall = CreateBallDataAsset(0.5f);
        LevelDataValidationResult result = Validate(
            Point(
                "entry-a",
                Vector2.zero,
                Option(activeBall, 100f),
                Option(inactiveLargeBall, 0f)),
            Point(
                "entry-b",
                new Vector2(0.8f, 0f),
                Option(otherBall, 100f)));

        AssertIssueAbsent(
            result,
            LevelDataValidationErrorCode.OverlappingSpawnPoints);
        Assert.That(result.IsValid, Is.True);
    }

    [Test]
    public void Validate_ChildCircleColliderReturnsUnsupportedConfigurationIssue()
    {
        BallDataAsset ballData = CreateBallDataAssetWithChildCollider(0.5f);
        LevelDataValidationResult result = Validate(
            Point("entry", Vector2.zero, Option(ballData, 100f)));

        AssertIssuePresent(
            result,
            LevelDataValidationErrorCode.UnsupportedChildCircleCollider);
    }

    private LevelDataValidationResult Validate(params SpawnPointSpec[] points)
    {
        SetSpawnPoints(points);
        return LevelDataValidator.Validate(levelDataAsset, levelRoot.transform);
    }

    private void SetSpawnPoints(SpawnPointSpec[] pointSpecs)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        SerializedProperty spawnPoints =
            serializedAsset.FindProperty("spawnPoints");
        spawnPoints.arraySize = pointSpecs.Length;

        for (int i = 0; i < pointSpecs.Length; i++)
        {
            SpawnPointSpec pointSpec = pointSpecs[i];
            SerializedProperty spawnPoint =
                spawnPoints.GetArrayElementAtIndex(i);
            spawnPoint.FindPropertyRelative("id").stringValue = pointSpec.Id;
            spawnPoint.FindPropertyRelative("localPosition").vector2Value =
                pointSpec.LocalPosition;

            SerializedProperty options =
                spawnPoint.FindPropertyRelative("spawnOptions");
            options.arraySize = pointSpec.Options.Length;
            for (int j = 0; j < pointSpec.Options.Length; j++)
            {
                SpawnOptionSpec optionSpec = pointSpec.Options[j];
                SerializedProperty option =
                    options.GetArrayElementAtIndex(j);
                option.FindPropertyRelative("ballData").objectReferenceValue =
                    optionSpec.BallData;
                option.FindPropertyRelative("spawnProbability").floatValue =
                    optionSpec.Probability;
            }
        }

        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
    }

    private BallDataAsset CreateBallDataAsset(
        float colliderRadius,
        float scale = 1f)
    {
        return CreateBallDataAsset(colliderRadius, Vector2.zero, scale);
    }

    private BallDataAsset CreateBallDataAsset(
        float colliderRadius,
        Vector2 colliderOffset,
        float scale = 1f)
    {
        var prefab = new GameObject("TestBallPrefab");
        CircleCollider2D collider = prefab.AddComponent<CircleCollider2D>();
        collider.radius = colliderRadius;
        collider.offset = colliderOffset;
        objectsToDestroy.Add(prefab);

        return CreateBallDataAsset(prefab, scale);
    }

    private BallDataAsset CreateBallDataAssetWithChildCollider(float radius)
    {
        var prefab = new GameObject("TestBallPrefabWithChildCollider");
        var child = new GameObject("ChildCollider");
        child.transform.SetParent(prefab.transform, false);
        child.AddComponent<CircleCollider2D>().radius = radius;
        objectsToDestroy.Add(prefab);
        objectsToDestroy.Add(child);

        return CreateBallDataAsset(prefab, 1f);
    }

    private BallDataAsset CreateBallDataAssetWithMultipleRootColliders(
        float radius)
    {
        var prefab = new GameObject("TestBallPrefabWithMultipleColliders");
        prefab.AddComponent<CircleCollider2D>().radius = radius;
        prefab.AddComponent<CircleCollider2D>().radius = radius;
        objectsToDestroy.Add(prefab);

        return CreateBallDataAsset(prefab, 1f);
    }

    private BallDataAsset CreateBallDataAssetWithoutPrefab()
    {
        return CreateBallDataAsset(null, 1f);
    }

    private BallDataAsset CreateBallDataAssetWithoutCollider()
    {
        var prefab = new GameObject("TestBallPrefabWithoutCollider");
        objectsToDestroy.Add(prefab);
        return CreateBallDataAsset(prefab, 1f);
    }

    private BallDataAsset CreateBallDataAsset(
        GameObject prefab,
        float scale)
    {
        var ballDataAsset = ScriptableObject.CreateInstance<BallDataAsset>();
        objectsToDestroy.Add(ballDataAsset);

        var serializedAsset = new SerializedObject(ballDataAsset);
        serializedAsset.FindProperty("ballPrefab").objectReferenceValue =
            prefab;
        serializedAsset.FindProperty("scale").floatValue = scale;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        return ballDataAsset;
    }

    private static SpawnPointSpec Point(
        string id,
        Vector2 localPosition,
        params SpawnOptionSpec[] options)
    {
        return new SpawnPointSpec(id, localPosition, options);
    }

    private static SpawnOptionSpec Option(
        BallDataAsset ballData,
        float probability)
    {
        return new SpawnOptionSpec(ballData, probability);
    }

    private static bool HasIssue(
        LevelDataValidationResult result,
        LevelDataValidationErrorCode code)
    {
        return result.Issues.Any(issue => issue.Code == code);
    }

    private static void AssertIssuePresent(
        LevelDataValidationResult result,
        LevelDataValidationErrorCode code)
    {
        Assert.That(HasIssue(result, code), Is.True);
    }

    private static void AssertIssueAbsent(
        LevelDataValidationResult result,
        LevelDataValidationErrorCode code)
    {
        Assert.That(HasIssue(result, code), Is.False);
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

        public SpawnOptionSpec(
            BallDataAsset ballData,
            float probability)
        {
            BallData = ballData;
            Probability = probability;
        }
    }
}
