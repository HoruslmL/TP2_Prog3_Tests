using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class BallDistributionGeneratorTests
{
    private readonly List<Object> objectsToDestroy = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
            Object.DestroyImmediate(objectsToDestroy[i]);

        objectsToDestroy.Clear();
    }

    [Test]
    public void Generate_NullLevelReturnsExplicitErrorWithoutEntries()
    {
        BallDistributionResult result =
            BallDistributionGenerator.Generate(null);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(
            BallDistributionErrorCode.MissingLevelData));
        Assert.That(result.Entries, Is.Empty);
    }

    [Test]
    public void Generate_PreservesPointAndOptionOrder()
    {
        BallDataAsset firstBall = CreateBallDataAsset("first");
        BallDataAsset secondBall = CreateBallDataAsset("second");
        BallDataAsset thirdBall = CreateBallDataAsset("third");
        OptionSpec firstOption = Option(firstBall, 25f);
        OptionSpec secondOption = Option(secondBall, 75f);
        int seed = FindSeedForDraw(0, 0d, 0.25d);
        LevelDataAsset level = CreateLevel(
            seed,
            false,
            Point("point-first", firstOption, secondOption),
            Point("point-second", Option(thirdBall, 100f)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Entries, Has.Count.EqualTo(2));
        Assert.That(result.Entries[0].SpawnPointId, Is.EqualTo("point-first"));
        Assert.That(
            result.Entries[0].SelectedOption,
            Is.SameAs(level.SpawnPoints[0].SpawnOptions[0]));
        Assert.That(result.Entries[1].SpawnPointId, Is.EqualTo("point-second"));
        Assert.That(result.Entries[1].BallData, Is.SameAs(thirdBall));
    }

    [Test]
    public void Generate_SameSeedAndConfigurationReturnsSameDistribution()
    {
        BallDataAsset firstBall = CreateBallDataAsset("first");
        BallDataAsset secondBall = CreateBallDataAsset("second");
        LevelDataAsset level = CreateLevel(
            1973,
            false,
            Point(
                "one",
                Option(firstBall, 45f),
                Option(secondBall, 55f)),
            Point(
                "two",
                Option(firstBall, 20f),
                Option(secondBall, 80f)),
            Point(
                "three",
                Option(firstBall, 70f),
                Option(secondBall, 30f)));

        BallDistributionResult first = BallDistributionGenerator.Generate(level);
        BallDistributionResult second = BallDistributionGenerator.Generate(level);

        Assert.That(first.IsSuccess, Is.True);
        Assert.That(second.IsSuccess, Is.True);
        Assert.That(first.Entries, Has.Count.EqualTo(second.Entries.Count));
        for (int i = 0; i < first.Entries.Count; i++)
        {
            Assert.That(
                second.Entries[i].SelectedOption,
                Is.SameAs(first.Entries[i].SelectedOption));
        }
    }

    [Test]
    public void Generate_ZeroProbabilityOptionIsNeverSelected()
    {
        BallDataAsset zeroProbabilityBall = CreateBallDataAsset("zero");
        BallDataAsset selectableBall = CreateBallDataAsset("selectable");
        LevelDataAsset level = CreateLevel(
            12,
            false,
            Point(
                "entry",
                Option(zeroProbabilityBall, 0f),
                Option(selectableBall, 100f)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Entries[0].BallData, Is.SameAs(selectableBall));
    }

    [Test]
    public void Generate_DynamicBalanceAppliesFactorForOnePreviousAppearance()
    {
        BallDataAsset repeatedBall = CreateBallDataAsset("repeated");
        BallDataAsset alternativeBall = CreateBallDataAsset("alternative");
        int seed = FindSeedForDraw(1, 0.375d, 0.5d);
        LevelDataAsset balanced = CreateLevel(
            seed,
            true,
            Point("first", Option(repeatedBall, 100f)),
            Point(
                "second",
                Option(repeatedBall, 50f),
                Option(alternativeBall, 50f)));
        LevelDataAsset unbalanced = CreateLevel(
            seed,
            false,
            Point("first", Option(repeatedBall, 100f)),
            Point(
                "second",
                Option(repeatedBall, 50f),
                Option(alternativeBall, 50f)));

        BallDistributionResult balancedResult =
            BallDistributionGenerator.Generate(balanced);
        BallDistributionResult unbalancedResult =
            BallDistributionGenerator.Generate(unbalanced);

        Assert.That(balancedResult.IsSuccess, Is.True);
        Assert.That(unbalancedResult.IsSuccess, Is.True);
        Assert.That(
            balancedResult.Entries[1].BallData,
            Is.SameAs(alternativeBall));
        Assert.That(
            unbalancedResult.Entries[1].BallData,
            Is.SameAs(repeatedBall));
    }

    [Test]
    public void Generate_DynamicBalanceCountsFullConsecutiveStreak()
    {
        BallDataAsset repeatedBall = CreateBallDataAsset("repeated");
        BallDataAsset alternativeBall = CreateBallDataAsset("alternative");
        int seed = FindSeedForDraw(2, 18d / 68d, 0.5d);
        LevelDataAsset level = CreateLevel(
            seed,
            true,
            Point("first", Option(repeatedBall, 100f)),
            Point("second", Option(repeatedBall, 100f)),
            Point(
                "third",
                Option(repeatedBall, 50f),
                Option(alternativeBall, 50f)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Entries[2].BallData, Is.SameAs(alternativeBall));
    }

    [Test]
    public void Generate_DifferentAssetsWithSameIdHaveIndependentStreaks()
    {
        BallDataAsset firstAsset = CreateBallDataAsset("same-id");
        BallDataAsset secondAsset = CreateBallDataAsset("same-id");
        int seed = FindSeedForDraw(2, 0.5d, 0.625d);
        LevelDataAsset level = CreateLevel(
            seed,
            true,
            Point("first", Option(firstAsset, 100f)),
            Point("second", Option(secondAsset, 100f)),
            Point(
                "third",
                Option(firstAsset, 50f),
                Option(secondAsset, 50f)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Entries[2].BallData, Is.SameAs(firstAsset));
    }

    [Test]
    public void Generate_UnavailablePreviousAssetResetsItsStreak()
    {
        BallDataAsset firstAsset = CreateBallDataAsset("first");
        BallDataAsset secondAsset = CreateBallDataAsset("second");
        int seed = FindSeedForDraw(2, 0.5d, 0.625d);
        LevelDataAsset level = CreateLevel(
            seed,
            true,
            Point("first", Option(firstAsset, 100f)),
            Point("second", Option(secondAsset, 100f)),
            Point(
                "third",
                Option(firstAsset, 50f),
                Option(secondAsset, 50f)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Entries[2].BallData, Is.SameAs(firstAsset));
    }

    [Test]
    public void Generate_ChangingAssetResetsStreakBeforeAStartsAgain()
    {
        BallDataAsset assetA = CreateBallDataAsset("A");
        BallDataAsset assetB = CreateBallDataAsset("B");
        int seed = FindSeedForStreakResetScenario();
        LevelDataAsset level = CreateLevel(
            seed,
            true,
            Point("point-1", Option(assetA, 100f)),
            Point("point-2", Option(assetA, 100f)),
            Point(
                "point-3",
                Option(assetA, 50f),
                Option(assetB, 50f)),
            Point(
                "point-4",
                Option(assetA, 50f),
                Option(assetB, 50f)),
            Point(
                "point-5",
                Option(assetA, 50f),
                Option(assetB, 50f)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Entries, Has.Count.EqualTo(5));
        Assert.That(result.Entries[0].BallData, Is.SameAs(assetA));
        Assert.That(result.Entries[1].BallData, Is.SameAs(assetA));
        Assert.That(result.Entries[2].BallData, Is.SameAs(assetB));
        Assert.That(result.Entries[3].BallData, Is.SameAs(assetA));
        Assert.That(result.Entries[4].BallData, Is.SameAs(assetA));
    }

    [Test]
    public void Generate_DoesNotModifyOriginalProbabilities()
    {
        BallDataAsset firstBall = CreateBallDataAsset("first");
        BallDataAsset secondBall = CreateBallDataAsset("second");
        OptionSpec firstOption = Option(firstBall, 60f);
        OptionSpec secondOption = Option(secondBall, 40f);
        LevelDataAsset level = CreateLevel(
            22,
            true,
            Point("entry-a", Option(firstBall, 100f)),
            Point("entry-b", firstOption, secondOption));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(
            level.SpawnPoints[1].SpawnOptions[0].SpawnProbability,
            Is.EqualTo(firstOption.Probability));
        Assert.That(
            level.SpawnPoints[1].SpawnOptions[1].SpawnProbability,
            Is.EqualTo(secondOption.Probability));
    }

    [Test]
    public void Generate_NoSelectableOptionReturnsErrorWithoutPartialEntries()
    {
        BallDataAsset ballData = CreateBallDataAsset("ball");
        LevelDataAsset level = CreateLevel(
            41,
            false,
            Point("valid", Option(ballData, 100f)),
            Point("invalid", Option(ballData, 0f)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(
            BallDistributionErrorCode.NoSelectableOptions));
        Assert.That(result.Error.SpawnPointIndex, Is.EqualTo(1));
        Assert.That(result.Error.SpawnPointId, Is.EqualTo("invalid"));
        Assert.That(result.Entries, Is.Empty);
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-0.1f)]
    [TestCase(100.1f)]
    public void Generate_InvalidProbabilityReturnsError(float probability)
    {
        BallDataAsset ballData = CreateBallDataAsset("ball");
        LevelDataAsset level = CreateLevel(
            9,
            false,
            Point("invalid", Option(ballData, probability)));

        BallDistributionResult result = BallDistributionGenerator.Generate(level);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error.Code, Is.EqualTo(
            BallDistributionErrorCode.InvalidProbability));
        Assert.That(result.Error.SpawnPointId, Is.EqualTo("invalid"));
        Assert.That(result.Entries, Is.Empty);
    }

    private BallDataAsset CreateBallDataAsset(string id)
    {
        BallDataAsset asset = ScriptableObject.CreateInstance<BallDataAsset>();
        objectsToDestroy.Add(asset);

        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("id").stringValue = id;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    private LevelDataAsset CreateLevel(
        int seed,
        bool enableDynamicBalance,
        params SpawnPointSpec[] pointSpecs)
    {
        LevelDataAsset level = ScriptableObject.CreateInstance<LevelDataAsset>();
        objectsToDestroy.Add(level);

        var serializedAsset = new SerializedObject(level);
        serializedAsset.FindProperty("seed").intValue = seed;
        serializedAsset.FindProperty("enableDynamicBalance").boolValue =
            enableDynamicBalance;

        SerializedProperty spawnPoints =
            serializedAsset.FindProperty("spawnPoints");
        spawnPoints.arraySize = pointSpecs.Length;
        for (int pointIndex = 0;
             pointIndex < pointSpecs.Length;
             pointIndex++)
        {
            SpawnPointSpec pointSpec = pointSpecs[pointIndex];
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
                OptionSpec optionSpec = pointSpec.Options[optionIndex];
                SerializedProperty option =
                    options.GetArrayElementAtIndex(optionIndex);
                option.FindPropertyRelative("ballData").objectReferenceValue =
                    optionSpec.BallData;
                option.FindPropertyRelative("spawnProbability").floatValue =
                    optionSpec.Probability;
            }
        }

        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
        return level;
    }

    private static SpawnPointSpec Point(
        string id,
        params OptionSpec[] options)
    {
        return new SpawnPointSpec(id, Vector2.zero, options);
    }

    private static OptionSpec Option(
        BallDataAsset ballData,
        float probability)
    {
        return new OptionSpec(ballData, probability);
    }

    private static int FindSeedForDraw(
        int drawIndex,
        double minimumInclusive,
        double maximumExclusive)
    {
        for (int seed = 0; seed < 100000; seed++)
        {
            var random = new System.Random(seed);
            double draw = 0d;
            for (int i = 0; i <= drawIndex; i++)
                draw = random.NextDouble();

            if (draw >= minimumInclusive && draw < maximumExclusive)
                return seed;
        }

        Assert.Fail("No seed found for the requested random draw interval.");
        return 0;
    }

    private static int FindSeedForStreakResetScenario()
    {
        const double probabilityAAfterTwoAppearances = 18d / 68d;
        const double probabilityAAfterOneAppearance = 0.375d;
        const double probabilityAAfterBWasSelected = 0.625d;

        for (int seed = 0; seed < 100000; seed++)
        {
            var random = new System.Random(seed);
            var draws = new double[5];
            for (int pointIndex = 0; pointIndex < draws.Length; pointIndex++)
                draws[pointIndex] = random.NextDouble();

            bool selectsBAtThirdPoint =
                draws[2] >= probabilityAAfterTwoAppearances;
            bool selectsAAtFourthPoint =
                draws[3] < probabilityAAfterBWasSelected;
            bool distinguishesNewAStreak =
                draws[4] >= probabilityAAfterTwoAppearances &&
                draws[4] < probabilityAAfterOneAppearance;

            if (selectsBAtThirdPoint &&
                selectsAAtFourthPoint &&
                distinguishesNewAStreak)
                return seed;
        }

        Assert.Fail("No seed found for the requested streak-reset scenario.");
        return 0;
    }

    private sealed class SpawnPointSpec
    {
        public readonly string Id;
        public readonly Vector2 LocalPosition;
        public readonly OptionSpec[] Options;

        public SpawnPointSpec(
            string id,
            Vector2 localPosition,
            OptionSpec[] options)
        {
            Id = id;
            LocalPosition = localPosition;
            Options = options;
        }
    }

    private sealed class OptionSpec
    {
        public readonly BallDataAsset BallData;
        public readonly float Probability;

        public OptionSpec(BallDataAsset ballData, float probability)
        {
            BallData = ballData;
            Probability = probability;
        }
    }
}
