using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class LevelDataAssetEditorTests
{
    private readonly List<Object> objectsToDestroy = new List<Object>();
    private LevelDataAsset levelDataAsset;
    private LevelDataAssetEditor editor;

    [SetUp]
    public void SetUp()
    {
        levelDataAsset = ScriptableObject.CreateInstance<LevelDataAsset>();
        objectsToDestroy.Add(levelDataAsset);
    }

    [TearDown]
    public void TearDown()
    {
        if (editor != null)
            Object.DestroyImmediate(editor);

        for (int i = objectsToDestroy.Count - 1; i >= 0; i--)
        {
            Undo.ClearUndo(objectsToDestroy[i]);
            Object.DestroyImmediate(objectsToDestroy[i]);
        }

        objectsToDestroy.Clear();
    }

    [Test]
    public void SeedChange_RefreshesDistribution()
    {
        BallDataAsset firstBall = CreateBallDataAsset("first");
        BallDataAsset secondBall = CreateBallDataAsset("second");
        int firstSeed = FindSeedForDraw(0, 0d, 0.5d);
        int secondSeed = FindSeedForDraw(0, 0.5d, 1d);
        SetLevel(
            firstSeed,
            false,
            Point(
                "entry",
                Vector2.zero,
                Option(firstBall, 50f),
                Option(secondBall, 50f)));

        editor = CreateLevelEditor();
        AssertSelectedBall(
            0,
            firstBall,
            "Initial seed");
        LevelDataValidationResult validationBeforeSeedChange =
            editor.CurrentValidationResult;

        SetSeed(secondSeed);
        editor.CheckForChanges();

        Assert.That(
            editor.CurrentValidationResult,
            Is.SameAs(validationBeforeSeedChange),
            "Changing only the seed should not rerun validation.");
        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.True);
        AssertSelectedBall(
            0,
            secondBall,
            "Changed seed");
    }

    [Test]
    public void EnableDynamicBalanceChange_RefreshesDistribution()
    {
        BallDataAsset repeatedBall = CreateBallDataAsset("repeated");
        BallDataAsset alternativeBall = CreateBallDataAsset("alternative");
        int seed = FindSeedForDraw(1, 0.375d, 0.5d);
        SetLevel(
            seed,
            false,
            Point("first", Vector2.zero, Option(repeatedBall, 100f)),
            Point(
                "second",
                Vector2.one,
                Option(repeatedBall, 50f),
                Option(alternativeBall, 50f)));

        editor = CreateLevelEditor();
        AssertSelectedBall(
            1,
            repeatedBall,
            "Balance disabled");
        LevelDataValidationResult validationBeforeBalanceChange =
            editor.CurrentValidationResult;

        SetDynamicBalance(true);
        editor.CheckForChanges();

        Assert.That(
            editor.CurrentValidationResult,
            Is.SameAs(validationBeforeBalanceChange),
            "Changing only dynamic balance should not rerun validation.");
        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.True);
        AssertSelectedBall(
            1,
            alternativeBall,
            "Balance enabled");
    }

    [Test]
    public void OptionAndProbabilityChanges_RefreshDistribution()
    {
        BallDataAsset firstBall = CreateBallDataAsset("first");
        BallDataAsset secondBall = CreateBallDataAsset("second");
        SetLevel(
            12,
            false,
            Point("entry", Vector2.zero, Option(firstBall, 100f)));

        editor = CreateLevelEditor();
        AssertSelectedBall(
            0,
            firstBall,
            "Initial options");
        LevelDataValidationResult validationBeforeOptionChange =
            editor.CurrentValidationResult;

        SetOptions(
            Option(firstBall, 0f),
            Option(secondBall, 100f));
        editor.CheckForChanges();

        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.True);
        Assert.That(
            editor.CurrentValidationResult,
            Is.Not.SameAs(validationBeforeOptionChange),
            "Changing options should refresh validation.");
        AssertSelectedBall(
            0,
            secondBall,
            "Changed options");
    }

    [Test]
    public void ReferencedBallDataChange_RefreshesValidationOnly()
    {
        BallDataAsset ballData = CreateBallDataAsset("ball");
        SetLevel(
            7,
            false,
            Point("entry", Vector2.zero, Option(ballData, 100f)));
        editor = CreateLevelEditor();
        LevelDataValidationResult validationBeforeChange =
            editor.CurrentValidationResult;
        BallDistributionResult distributionBeforeChange =
            editor.CurrentDistributionResult;

        var serializedBallData = new SerializedObject(ballData);
        serializedBallData.FindProperty("scale").floatValue = 2f;
        Assert.That(serializedBallData.ApplyModifiedProperties(), Is.True);
        editor.CheckForChanges();

        Assert.That(
            editor.CurrentValidationResult,
            Is.Not.SameAs(validationBeforeChange));
        Assert.That(
            editor.CurrentDistributionResult,
            Is.SameAs(distributionBeforeChange),
            "Changing physical scale should not regenerate logical choices.");
    }

    [Test]
    public void ClearanceWarnings_RefreshForMarginPositionsOptionsAndBallScale()
    {
        BallDataAsset baseBall =
            CreateBallDataAssetWithCollider("base", 0.5f, 1f);
        BallDataAsset largerBall =
            CreateBallDataAssetWithCollider("larger", 0.3f, 2f);
        SetLevel(
            7,
            false,
            Point("west", Vector2.zero, Option(baseBall, 100f)),
            Point(
                "east",
                new Vector2(1.4f, 0f),
                Option(baseBall, 100f)));

        var rootObject = new GameObject("ValidationRoot");
        objectsToDestroy.Add(rootObject);
        editor = CreateLevelEditor();
        SetValidationRoot(rootObject.transform);
        editor.CheckForChanges();

        Assert.That(editor.CurrentValidationResult.IsValid, Is.True);
        Assert.That(editor.CurrentValidationResult.Warnings, Has.Count.EqualTo(1));
        Assert.That(
            editor.CurrentValidationResult.Warnings[0].Clearance,
            Is.EqualTo(0.4f).Within(0.0001f));

        LevelDataValidationResult beforeMarginChange =
            editor.CurrentValidationResult;
        SetMinimumSpawnPointClearance(0.3f);
        editor.CheckForChanges();
        Assert.That(
            editor.CurrentValidationResult,
            Is.Not.SameAs(beforeMarginChange));
        Assert.That(editor.CurrentValidationResult.Warnings, Is.Empty);

        SetPointPosition(1, new Vector2(1.2f, 0f));
        editor.CheckForChanges();
        Assert.That(editor.CurrentValidationResult.Warnings, Has.Count.EqualTo(1));
        Assert.That(
            editor.CurrentValidationResult.Warnings[0].Clearance,
            Is.EqualTo(0.2f).Within(0.0001f));

        SetPointOption(1, largerBall, 100f);
        editor.CheckForChanges();
        Assert.That(editor.CurrentValidationResult.Warnings, Has.Count.EqualTo(1));
        Assert.That(
            editor.CurrentValidationResult.Warnings[0].Clearance,
            Is.EqualTo(0.1f).Within(0.0001f));

        var serializedLargerBall = new SerializedObject(largerBall);
        serializedLargerBall.FindProperty("scale").floatValue = 2.5f;
        Assert.That(serializedLargerBall.ApplyModifiedProperties(), Is.True);
        editor.CheckForChanges();
        Assert.That(editor.CurrentValidationResult.Warnings, Is.Empty);
        Assert.That(editor.CurrentValidationResult.Issues, Has.Count.EqualTo(1));
        Assert.That(
            editor.CurrentValidationResult.Issues[0].Code,
            Is.EqualTo(LevelDataValidationErrorCode.OverlappingSpawnPoints));
    }

    [Test]
    public void AddSpawnPoint_UndoAndRedoRefreshDistribution()
    {
        BallDataAsset ballData = CreateBallDataAsset("ball");
        SetLevel(
            31,
            false,
            Point("existing", Vector2.zero, Option(ballData, 100f)));
        editor = CreateLevelEditor();

        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.True);
        Assert.That(editor.CurrentDistributionResult.Entries, Has.Count.EqualTo(1));
        LevelDataValidationResult validationBeforeAdd =
            editor.CurrentValidationResult;

        LevelBallEditorAssetOperations.AddSpawnPoint(
            levelDataAsset,
            new Vector2(2f, 3f));
        editor.CheckForChanges();

        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.False);
        Assert.That(
            editor.CurrentDistributionResult.Error.Code,
            Is.EqualTo(BallDistributionErrorCode.NoSpawnOptions));
        Assert.That(
            editor.CurrentDistributionResult.Error.SpawnPointIndex,
            Is.EqualTo(1));
        Assert.That(
            editor.CurrentValidationResult,
            Is.Not.SameAs(validationBeforeAdd));

        Undo.PerformUndo();
        editor.CheckForChanges();

        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.True);
        Assert.That(editor.CurrentDistributionResult.Entries, Has.Count.EqualTo(1));

        Undo.PerformRedo();
        editor.CheckForChanges();

        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.False);
        Assert.That(
            editor.CurrentDistributionResult.Error.Code,
            Is.EqualTo(BallDistributionErrorCode.NoSpawnOptions));
    }

    [Test]
    public void RefreshingDistribution_DoesNotModifyOriginalAssetData()
    {
        BallDataAsset firstBall = CreateBallDataAsset("first");
        BallDataAsset secondBall = CreateBallDataAsset("second");
        SetLevel(
            5821,
            true,
            Point(
                "entry",
                new Vector2(-1.5f, 3.25f),
                Option(firstBall, 60f),
                Option(secondBall, 40f)));
        string originalJson = EditorJsonUtility.ToJson(levelDataAsset);

        editor = CreateLevelEditor();

        Assert.That(editor.CurrentDistributionResult.IsSuccess, Is.True);
        Assert.That(
            EditorJsonUtility.ToJson(levelDataAsset),
            Is.EqualTo(originalJson));
        Assert.That(
            editor.CurrentValidationResult.Issues[0].Code,
            Is.EqualTo(LevelDataValidationErrorCode.MissingLevelRoot));
    }

    private LevelDataAssetEditor CreateLevelEditor()
    {
        return (LevelDataAssetEditor)UnityEditor.Editor.CreateEditor(
            levelDataAsset);
    }

    private void AssertSelectedBall(
        int entryIndex,
        BallDataAsset expectedBall,
        string context)
    {
        BallDistributionResult result = editor.CurrentDistributionResult;
        Assert.That(result, Is.Not.Null, context + ": missing result.");
        Assert.That(
            result.IsSuccess,
            Is.True,
            context + ": " + (result.Error == null
                ? "generation failed without an error"
                : result.Error.Code + " - " + result.Error.Message));
        Assert.That(
            result.Entries,
            Has.Count.GreaterThan(entryIndex),
            context + ": no entry at index " + entryIndex + ".");

        BallDataAsset actualBall = result.Entries[entryIndex].BallData;
        Assert.That(
            actualBall,
            Is.SameAs(expectedBall),
            context + ": expected instance " + expectedBall.GetInstanceID() +
            ", actual instance " +
            (actualBall == null ? "null" : actualBall.GetInstanceID().ToString()) +
            ".");
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

    private BallDataAsset CreateBallDataAssetWithCollider(
        string id,
        float colliderRadius,
        float scale)
    {
        var prefab = new GameObject(id + "Prefab");
        prefab.AddComponent<CircleCollider2D>().radius = colliderRadius;
        objectsToDestroy.Add(prefab);

        BallDataAsset asset = CreateBallDataAsset(id);
        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("ballPrefab").objectReferenceValue = prefab;
        serializedAsset.FindProperty("scale").floatValue = scale;
        Assert.That(serializedAsset.ApplyModifiedPropertiesWithoutUndo(), Is.True);
        return asset;
    }

    private void SetLevel(
        int seed,
        bool enableDynamicBalance,
        params SpawnPointSpec[] pointSpecs)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
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
                SetOption(
                    options.GetArrayElementAtIndex(optionIndex),
                    pointSpec.Options[optionIndex]);
            }
        }

        serializedAsset.ApplyModifiedPropertiesWithoutUndo();
    }

    private void SetSeed(int seed)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        serializedAsset.FindProperty("seed").intValue = seed;
        Assert.That(serializedAsset.ApplyModifiedProperties(), Is.True);
    }

    private void SetDynamicBalance(bool enabled)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        serializedAsset.FindProperty("enableDynamicBalance").boolValue =
            enabled;
        Assert.That(serializedAsset.ApplyModifiedProperties(), Is.True);
    }

    private void SetMinimumSpawnPointClearance(float clearance)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        serializedAsset.FindProperty("minimumSpawnPointClearance").floatValue =
            clearance;
        Assert.That(serializedAsset.ApplyModifiedProperties(), Is.True);
    }

    private void SetPointPosition(int pointIndex, Vector2 position)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        serializedAsset.FindProperty("spawnPoints")
            .GetArrayElementAtIndex(pointIndex)
            .FindPropertyRelative("localPosition")
            .vector2Value = position;
        Assert.That(serializedAsset.ApplyModifiedProperties(), Is.True);
    }

    private void SetPointOption(
        int pointIndex,
        BallDataAsset ballData,
        float probability)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        SerializedProperty option = serializedAsset
            .FindProperty("spawnPoints")
            .GetArrayElementAtIndex(pointIndex)
            .FindPropertyRelative("spawnOptions")
            .GetArrayElementAtIndex(0);
        SetOption(option, Option(ballData, probability));
        Assert.That(serializedAsset.ApplyModifiedProperties(), Is.True);
    }

    private void SetValidationRoot(Transform root)
    {
        FieldInfo field = typeof(LevelDataAssetEditor).GetField(
            "validationRoot",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(editor, root);
    }

    private void SetOptions(params OptionSpec[] optionSpecs)
    {
        var serializedAsset = new SerializedObject(levelDataAsset);
        SerializedProperty options = serializedAsset
            .FindProperty("spawnPoints")
            .GetArrayElementAtIndex(0)
            .FindPropertyRelative("spawnOptions");
        options.arraySize = optionSpecs.Length;
        for (int optionIndex = 0;
             optionIndex < optionSpecs.Length;
             optionIndex++)
        {
            SetOption(
                options.GetArrayElementAtIndex(optionIndex),
                optionSpecs[optionIndex]);
        }

        Assert.That(serializedAsset.ApplyModifiedProperties(), Is.True);
    }

    private static void SetOption(
        SerializedProperty option,
        OptionSpec optionSpec)
    {
        option.FindPropertyRelative("ballData").objectReferenceValue =
            optionSpec.BallData;
        option.FindPropertyRelative("spawnProbability").floatValue =
            optionSpec.Probability;
    }

    private static SpawnPointSpec Point(
        string id,
        Vector2 localPosition,
        params OptionSpec[] options)
    {
        return new SpawnPointSpec(id, localPosition, options);
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
