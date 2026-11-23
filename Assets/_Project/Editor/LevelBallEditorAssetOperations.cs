using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

public static class LevelBallEditorAssetOperations
{
    private const string SpawnPointIdPrefix = "spawn-point-";

    public static bool TryGetLocalPosition(
        Transform levelRoot,
        Ray worldRay,
        out Vector2 localPosition)
    {
        localPosition = Vector2.zero;
        if (levelRoot == null ||
            !IsFinite(worldRay.origin) ||
            !IsFinite(worldRay.direction))
            return false;

        Vector3 worldOrigin = levelRoot.TransformPoint(Vector3.zero);
        Vector3 worldXAxis = levelRoot.TransformVector(Vector3.right);
        Vector3 worldYAxis = levelRoot.TransformVector(Vector3.up);
        Vector3 planeNormal = Vector3.Cross(worldXAxis, worldYAxis);

        float normalMagnitude = planeNormal.sqrMagnitude;
        if (!IsFinite(worldOrigin) ||
            !IsFinite(planeNormal) ||
            !IsFinite(normalMagnitude) ||
            normalMagnitude <= 0f)
            return false;

        var plane = new Plane(planeNormal.normalized, worldOrigin);
        if (!plane.Raycast(worldRay, out float distance) || !IsFinite(distance))
            return false;

        Vector3 worldPosition = worldRay.GetPoint(distance);
        Vector3 localPoint = levelRoot.InverseTransformPoint(worldPosition);
        if (!IsFinite(localPoint))
            return false;

        localPosition = new Vector2(localPoint.x, localPoint.y);
        return true;
    }

    public static string AddSpawnPoint(
        LevelDataAsset levelData,
        Vector2 localPosition)
    {
        if (levelData == null)
            throw new ArgumentNullException(nameof(levelData));

        var serializedLevel = new SerializedObject(levelData);
        serializedLevel.Update();

        SerializedProperty spawnPoints =
            serializedLevel.FindProperty("spawnPoints");
        if (spawnPoints == null || !spawnPoints.isArray)
        {
            throw new InvalidOperationException(
                "No se encontró la lista serializada de puntos de aparición.");
        }

        string id = GenerateUniqueId(spawnPoints);
        int newIndex = spawnPoints.arraySize;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Agregar punto de aparición");

        try
        {
            spawnPoints.InsertArrayElementAtIndex(newIndex);
            SerializedProperty newPoint =
                spawnPoints.GetArrayElementAtIndex(newIndex);
            newPoint.FindPropertyRelative("id").stringValue = id;
            newPoint.FindPropertyRelative("localPosition").vector2Value =
                localPosition;
            newPoint.FindPropertyRelative("spawnOptions").ClearArray();

            if (!serializedLevel.ApplyModifiedProperties())
            {
                throw new InvalidOperationException(
                    "No se pudo guardar el nuevo punto de aparición.");
            }

            EditorUtility.SetDirty(levelData);
            return id;
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    public static bool TrySetSpawnPointLocalPosition(
        LevelDataAsset levelData,
        string spawnPointId,
        Vector2 localPosition)
    {
        if (levelData == null ||
            string.IsNullOrEmpty(spawnPointId) ||
            !IsFinite(localPosition.x) ||
            !IsFinite(localPosition.y))
            return false;

        var serializedLevel = new SerializedObject(levelData);
        serializedLevel.Update();

        SerializedProperty spawnPoints =
            serializedLevel.FindProperty("spawnPoints");
        if (spawnPoints == null || !spawnPoints.isArray)
            return false;

        int matchingIndex = -1;
        for (int i = 0; i < spawnPoints.arraySize; i++)
        {
            string id = spawnPoints
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("id")
                .stringValue;
            if (!string.Equals(id, spawnPointId, StringComparison.Ordinal))
                continue;

            if (matchingIndex >= 0)
                return false;

            matchingIndex = i;
        }

        if (matchingIndex < 0)
            return false;

        SerializedProperty position = spawnPoints
            .GetArrayElementAtIndex(matchingIndex)
            .FindPropertyRelative("localPosition");
        position.vector2Value = localPosition;
        if (!serializedLevel.ApplyModifiedProperties())
            return false;

        EditorUtility.SetDirty(levelData);
        return true;
    }

    private static string GenerateUniqueId(SerializedProperty spawnPoints)
    {
        var existingIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < spawnPoints.arraySize; i++)
        {
            string existingId = spawnPoints
                .GetArrayElementAtIndex(i)
                .FindPropertyRelative("id")
                .stringValue;
            existingIds.Add(existingId);
        }

        for (long suffix = 1; ; suffix++)
        {
            string candidate = SpawnPointIdPrefix +
                suffix.ToString("D3", CultureInfo.InvariantCulture);
            if (!existingIds.Contains(candidate))
                return candidate;
        }
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x) &&
            IsFinite(value.y) &&
            IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
