using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class LevelBallEditorWindow : EditorWindow
{
    [SerializeField] private LevelDataAsset levelDataAsset;
    [SerializeField] private Transform levelRoot;
    [SerializeField] private bool isAddingPoints;

    private readonly List<SpawnPointGeometry> spawnPointGeometry =
        new List<SpawnPointGeometry>();
    private bool spawnPointGeometryDirty = true;

    [MenuItem("Tools/Level Ball Editor")]
    private static void Open()
    {
        GetWindow<LevelBallEditorWindow>("Level Ball Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
        Undo.undoRedoPerformed += OnUndoRedo;
        Undo.postprocessModifications -= OnPostprocessModifications;
        Undo.postprocessModifications += OnPostprocessModifications;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.projectChanged += OnProjectChanged;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
        Undo.postprocessModifications -= OnPostprocessModifications;
        EditorApplication.projectChanged -= OnProjectChanged;
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        levelDataAsset = (LevelDataAsset)EditorGUILayout.ObjectField(
            "Level Data",
            levelDataAsset,
            typeof(LevelDataAsset),
            false);
        levelRoot = (Transform)EditorGUILayout.ObjectField(
            "Level Root",
            levelRoot,
            typeof(Transform),
            true);

        if (EditorGUI.EndChangeCheck())
        {
            InvalidateSpawnPointGeometry();
            SceneView.RepaintAll();
        }

        if (levelDataAsset == null || levelRoot == null)
        {
            isAddingPoints = false;
            EditorGUILayout.HelpBox(
                "Asigná un LevelDataAsset y el Transform de LevelRoot.",
                MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(
                   levelDataAsset == null || levelRoot == null))
        {
            string buttonText = isAddingPoints
                ? "Detener colocación"
                : "Agregar punto";
            bool newMode = GUILayout.Toggle(
                isAddingPoints,
                buttonText,
                "Button");

            if (newMode != isAddingPoints)
            {
                isAddingPoints = newMode;
                SceneView.RepaintAll();
            }
        }

        if (isAddingPoints)
        {
            EditorGUILayout.HelpBox(
                "Hacé clic en el plano XY de LevelRoot para agregar puntos. " +
                "El modo permanece activo hasta desactivarlo.",
                MessageType.Info);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (levelDataAsset == null || levelRoot == null)
            return;

        Event currentEvent = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (isAddingPoints && currentEvent.type == EventType.Layout)
            HandleUtility.AddDefaultControl(controlId);

        DrawSpawnPointMarkers();
        if (!isAddingPoints)
            return;

        if (currentEvent.GetTypeForControl(controlId) != EventType.MouseDown ||
            currentEvent.button != 0 ||
            currentEvent.alt)
            return;

        Ray worldRay = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        if (!LevelBallEditorAssetOperations.TryGetLocalPosition(
                levelRoot,
                worldRay,
                out Vector2 localPosition))
        {
            currentEvent.Use();
            return;
        }

        LevelBallEditorAssetOperations.AddSpawnPoint(
            levelDataAsset,
            localPosition);
        InvalidateSpawnPointGeometry();
        currentEvent.Use();
        Repaint();
        SceneView.RepaintAll();
    }

    private void DrawSpawnPointMarkers()
    {
        Vector3 worldXAxis = levelRoot.TransformVector(Vector3.right);
        Vector3 worldYAxis = levelRoot.TransformVector(Vector3.up);
        Vector3 planeNormal = Vector3.Cross(worldXAxis, worldYAxis);
        float normalMagnitude = planeNormal.sqrMagnitude;
        Vector3 labelOffsetDirection = levelRoot.up;
        if (!IsFinite(worldXAxis) ||
            !IsFinite(worldYAxis) ||
            !IsFinite(planeNormal) ||
            !IsFinite(normalMagnitude) ||
            normalMagnitude <= 0f ||
            !IsFinite(labelOffsetDirection))
            return;

        planeNormal.Normalize();
        Color previousColor = Handles.color;
        Color markerColor = new Color(1f, 0.75f, 0.1f, 1f);
        Color radiusColor = new Color(0.25f, 0.8f, 1f, 0.9f);
        RefreshSpawnPointGeometryIfNeeded();

        try
        {
            IReadOnlyList<BallSpawnPointData> spawnPoints =
                levelDataAsset.SpawnPoints;
            if (spawnPoints == null)
                return;

            for (int pointIndex = 0;
                 pointIndex < spawnPoints.Count;
                 pointIndex++)
            {
                BallSpawnPointData spawnPoint = spawnPoints[pointIndex];
                if (spawnPoint == null)
                    continue;

                Vector2 localPosition = spawnPoint.LocalPosition;
                if (!IsFinite(localPosition.x) || !IsFinite(localPosition.y))
                    continue;

                Vector3 worldPosition = levelRoot.TransformPoint(
                    new Vector3(localPosition.x, localPosition.y, 0f));
                if (!IsFinite(worldPosition))
                    continue;

                float handleSize = HandleUtility.GetHandleSize(worldPosition);
                Vector3 labelPosition = worldPosition +
                    labelOffsetDirection * (handleSize * 0.1f);
                float markerRadius = handleSize * 0.06f;
                if (!IsFinite(handleSize) ||
                    handleSize <= 0f ||
                    !IsFinite(labelPosition) ||
                    !IsFinite(markerRadius))
                    continue;

                SpawnPointGeometry geometry =
                    spawnPointGeometry[pointIndex];
                if (geometry.HasValidRadius)
                {
                    Vector3 colliderCenter = new Vector3(
                        worldPosition.x + geometry.CenterOffset.x,
                        worldPosition.y + geometry.CenterOffset.y,
                        0f);
                    if (geometry.Radius > 0f &&
                        IsFinite(geometry.Radius) &&
                        IsFinite(colliderCenter))
                    {
                        Handles.color = radiusColor;
                        Handles.DrawWireDisc(
                            colliderCenter,
                            Vector3.forward,
                            geometry.Radius);
                    }
                }

                Handles.color = markerColor;
                Handles.DrawWireDisc(
                    worldPosition,
                    planeNormal,
                    markerRadius);
                Handles.Label(
                    labelPosition,
                    spawnPoint.Id);
            }
        }
        finally
        {
            Handles.color = previousColor;
        }
    }

    private void OnUndoRedo()
    {
        InvalidateSpawnPointGeometry();
        Repaint();
        SceneView.RepaintAll();
    }

    private UndoPropertyModification[] OnPostprocessModifications(
        UndoPropertyModification[] modifications)
    {
        if (modifications == null)
            return modifications;

        for (int i = 0; i < modifications.Length; i++)
        {
            PropertyModification currentValue =
                modifications[i].currentValue;
            if (currentValue == null ||
                !IsRelevantModifiedObject(currentValue.target))
                continue;

            InvalidateSpawnPointGeometry();
            SceneView.RepaintAll();
            break;
        }

        return modifications;
    }

    private void OnProjectChanged()
    {
        InvalidateSpawnPointGeometry();
        SceneView.RepaintAll();
    }

    private void InvalidateSpawnPointGeometry()
    {
        spawnPointGeometryDirty = true;
    }

    private void RefreshSpawnPointGeometryIfNeeded()
    {
        if (!spawnPointGeometryDirty)
            return;

        spawnPointGeometry.Clear();
        IReadOnlyList<BallSpawnPointData> spawnPoints =
            levelDataAsset.SpawnPoints;
        if (spawnPoints != null)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                bool hasValidRadius =
                    BallColliderGeometry.TryGetMaximumSelectableGeometry(
                        spawnPoints[i],
                        out float radius,
                        out Vector2 centerOffset);
                spawnPointGeometry.Add(
                    new SpawnPointGeometry(
                        hasValidRadius,
                        radius,
                        centerOffset));
            }
        }

        spawnPointGeometryDirty = false;
    }

    private bool IsRelevantModifiedObject(Object modifiedObject)
    {
        if (modifiedObject == null)
            return false;

        if (modifiedObject == levelDataAsset)
            return true;

        IReadOnlyList<BallSpawnPointData> spawnPoints =
            levelDataAsset == null ? null : levelDataAsset.SpawnPoints;
        if (spawnPoints == null)
            return false;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[i];
            if (spawnPoint == null || spawnPoint.SpawnOptions == null)
                continue;

            IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
            for (int optionIndex = 0;
                 optionIndex < options.Count;
                 optionIndex++)
            {
                BallSpawnOption option = options[optionIndex];
                BallDataAsset ballData = option == null ? null : option.BallData;
                if (ballData != null && modifiedObject == ballData)
                    return true;
            }
        }

        Transform changedTransform = GetTransform(modifiedObject);
        if (changedTransform == null)
            return false;

        if (levelRoot != null &&
            (changedTransform == levelRoot ||
             levelRoot.IsChildOf(changedTransform)))
            return true;

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[i];
            if (spawnPoint == null || spawnPoint.SpawnOptions == null)
                continue;

            IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
            for (int optionIndex = 0;
                 optionIndex < options.Count;
                 optionIndex++)
            {
                BallSpawnOption option = options[optionIndex];
                BallDataAsset ballData = option == null ? null : option.BallData;
                if (ballData == null)
                    continue;

                GameObject prefab = ballData.BallPrefab;
                if (prefab != null &&
                    (changedTransform == prefab.transform ||
                     changedTransform.IsChildOf(prefab.transform)))
                    return true;
            }
        }

        return false;
    }

    private static Transform GetTransform(Object modifiedObject)
    {
        Transform transform = modifiedObject as Transform;
        if (transform != null)
            return transform;

        Component component = modifiedObject as Component;
        if (component != null)
            return component.transform;

        GameObject gameObject = modifiedObject as GameObject;
        return gameObject == null ? null : gameObject.transform;
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

    private readonly struct SpawnPointGeometry
    {
        public readonly bool HasValidRadius;
        public readonly float Radius;
        public readonly Vector2 CenterOffset;

        public SpawnPointGeometry(
            bool hasValidRadius,
            float radius,
            Vector2 centerOffset)
        {
            HasValidRadius = hasValidRadius;
            Radius = radius;
            CenterOffset = centerOffset;
        }
    }
}
