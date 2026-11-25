using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class BallDataAssetTests
{
    [Test]
    public void ToBallData_ConvertsExplosiveConfigurationCorrectly()
    {
        var asset = ScriptableObject.CreateInstance<BallDataAsset>();

        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("id").stringValue = "Explosive";
        serializedAsset.FindProperty("displayName").stringValue = "Explosive Ball";
        serializedAsset.FindProperty("mass").floatValue = 2f;
        serializedAsset.FindProperty("abilityType").enumValueIndex =
            (int)BallAbilityType.Explosive;
        serializedAsset.FindProperty("effectRadius").floatValue = 3f;
        serializedAsset.FindProperty("effectDelay").floatValue = 1f;
        Color expectedColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        serializedAsset.FindProperty("color").colorValue = expectedColor;
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();

        BallData data = asset.ToBallData();

        Assert.That(data.Id, Is.EqualTo("Explosive"));
        Assert.That(data.DisplayName, Is.EqualTo("Explosive Ball"));
        Assert.That(data.Mass, Is.EqualTo(2f));
        Assert.That(data.AbilityType, Is.EqualTo(BallAbilityType.Explosive));
        Assert.That(data.EffectRadius, Is.EqualTo(3f));
        Assert.That(data.EffectDelay, Is.EqualTo(1f));
        Assert.That(data.Color, Is.EqualTo(expectedColor));

        Object.DestroyImmediate(asset);
    }

    [Test]
    public void ToBallData_WhenDisplayNameIsEmpty_FallsBackToId()
    {
        var asset = ScriptableObject.CreateInstance<BallDataAsset>();

        var serializedAsset = new SerializedObject(asset);
        serializedAsset.FindProperty("id").stringValue = "Normal";
        serializedAsset.FindProperty("displayName").stringValue = " ";
        serializedAsset.ApplyModifiedPropertiesWithoutUndo();

        BallData data = asset.ToBallData();

        Assert.That(data.DisplayName, Is.EqualTo("Normal"));
        Assert.That(data.Color, Is.EqualTo(Color.white));

        Object.DestroyImmediate(asset);
    }
}
