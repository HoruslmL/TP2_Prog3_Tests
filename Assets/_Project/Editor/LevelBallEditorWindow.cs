using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class LevelBallEditorWindow : EditorWindow
{
    private const float DragThresholdPixels = 4f;
    private const float SelectionAmbiguityPixels = 3f;
    private const float MarkerHitAreaMultiplier = 1.5f;

    [SerializeField] private LevelDataAsset levelDataAsset;
    [SerializeField] private Transform levelRoot;
    [SerializeField] private bool isAddingPoints;

    private readonly List<SpawnPointGeometry> spawnPointGeometry =
        new List<SpawnPointGeometry>();
    private readonly List<SpawnPointHandle> spawnPointHandles =
        new List<SpawnPointHandle>();
    private readonly Dictionary<string, int> spawnPointIdCounts =
        new Dictionary<string, int>(StringComparer.Ordinal);
    private bool spawnPointGeometryDirty = true;
    private LevelDataAsset cachedGeometryAsset;
    private IReadOnlyList<BallSpawnPointData> cachedSpawnPoints;
    private string selectedSpawnPointId;
    private bool isPlacingPoint;
    private bool placementWasDragged;
    private Vector2 placementStartPosition;
    private int placementControlId;
    private bool isDraggingSpawnPoint;
    private bool dragHasChanged;
    private Vector2 dragStartScreenPosition;
    private int dragControlId;
    private int dragUndoGroup = -1;
    private string draggedSpawnPointId;
    private Vector2 dragLocalOffset;

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
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        CancelPointPlacement();
        EndSpawnPointDrag();
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
        Undo.postprocessModifications -= OnPostprocessModifications;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
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
            CancelPointPlacement();
            EndSpawnPointDrag();
            selectedSpawnPointId = null;
            InvalidateSpawnPointGeometry();
            SceneView.RepaintAll();
        }

        if (levelDataAsset == null || levelRoot == null)
        {
            CancelPointPlacement();
            EndSpawnPointDrag();
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
                CancelPointPlacement();
                EndSpawnPointDrag();
                selectedSpawnPointId = null;
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
        else if (levelDataAsset != null && levelRoot != null)
        {
            EditorGUILayout.HelpBox(
                "Hacé clic en un marcador para seleccionarlo y arrastralo " +
                "para moverlo. Los puntos con IDs duplicados no se pueden mover.",
                MessageType.Info);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (levelDataAsset == null || levelRoot == null)
        {
            CancelPointPlacement();
            EndSpawnPointDrag();
            return;
        }

        Event currentEvent = Event.current;
        int placementId = GUIUtility.GetControlID(FocusType.Passive);
        if (isAddingPoints && currentEvent.type == EventType.Layout)
            HandleUtility.AddDefaultControl(placementId);

        DrawSpawnPointMarkers(!isAddingPoints, currentEvent);

        if (isAddingPoints)
            HandlePointPlacement(currentEvent, placementId);
        else
            HandleSpawnPointMovement(currentEvent);
    }

    private void HandlePointPlacement(Event currentEvent, int controlId)
    {
        if (currentEvent.GetTypeForControl(controlId) == EventType.MouseDown &&
            currentEvent.button == 0 &&
            !currentEvent.alt &&
            GUIUtility.hotControl == 0)
        {
            isPlacingPoint = true;
            placementWasDragged = false;
            placementStartPosition = currentEvent.mousePosition;
            placementControlId = controlId;
            GUIUtility.hotControl = controlId;
            currentEvent.Use();
            return;
        }

        if (!isPlacingPoint || GUIUtility.hotControl != placementControlId)
            return;

        if (currentEvent.type == EventType.Ignore)
        {
            CancelPointPlacement();
        }
        else if (currentEvent.type == EventType.MouseDrag)
        {
            if ((currentEvent.mousePosition - placementStartPosition).sqrMagnitude >
                DragThresholdPixels * DragThresholdPixels)
                placementWasDragged = true;

            currentEvent.Use();
        }
        else if (currentEvent.type == EventType.MouseUp &&
                 currentEvent.button == 0)
        {
            if ((currentEvent.mousePosition - placementStartPosition).sqrMagnitude >
                DragThresholdPixels * DragThresholdPixels)
                placementWasDragged = true;

            if (!placementWasDragged)
            {
                Ray worldRay =
                    HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
                if (LevelBallEditorAssetOperations.TryGetLocalPosition(
                        levelRoot,
                        worldRay,
                        out Vector2 localPosition))
                {
                    LevelBallEditorAssetOperations.AddSpawnPoint(
                        levelDataAsset,
                        localPosition);
                    InvalidateSpawnPointGeometry();
                    Repaint();
                    SceneView.RepaintAll();
                }
            }

            currentEvent.Use();
            CancelPointPlacement();
        }
        else if (currentEvent.type == EventType.KeyDown &&
                 currentEvent.keyCode == KeyCode.Escape)
        {
            currentEvent.Use();
            CancelPointPlacement();
        }
    }

    private void HandleSpawnPointMovement(Event currentEvent)
    {
        if (isDraggingSpawnPoint &&
            GUIUtility.hotControl == dragControlId)
        {
            if (currentEvent.type == EventType.Ignore)
            {
                EndSpawnPointDrag();
            }
            else if (currentEvent.type == EventType.MouseDrag)
            {
                if (HasExceededDragThreshold(currentEvent.mousePosition))
                    UpdateDraggedSpawnPoint(currentEvent);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp &&
                     currentEvent.button == 0)
            {
                if (HasExceededDragThreshold(currentEvent.mousePosition))
                    UpdateDraggedSpawnPoint(currentEvent);
                currentEvent.Use();
                EndSpawnPointDrag();
            }
            else if (currentEvent.type == EventType.KeyDown &&
                     currentEvent.keyCode == KeyCode.Escape)
            {
                currentEvent.Use();
                EndSpawnPointDrag();
            }

            return;
        }

        if (currentEvent.type != EventType.MouseDown ||
            currentEvent.button != 0 ||
            currentEvent.alt ||
            GUIUtility.hotControl != 0)
            return;

        if (!TryGetUnambiguousSpawnPointHandle(
                HandleUtility.nearestControl,
                out SpawnPointHandle handle,
                out bool isAmbiguous))
            return;

        if (isAmbiguous)
        {
            selectedSpawnPointId = null;
            currentEvent.Use();
            SceneView.RepaintAll();
            return;
        }

        if (!TryBeginSpawnPointDrag(handle, currentEvent))
        {
            currentEvent.Use();
            return;
        }

        currentEvent.Use();
    }

    private void DrawSpawnPointMarkers(
        bool enablePointInteraction,
        Event currentEvent)
    {
        spawnPointHandles.Clear();

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

        try
        {
            IReadOnlyList<BallSpawnPointData> spawnPoints =
                levelDataAsset.SpawnPoints;
            RefreshSpawnPointGeometryIfNeeded(spawnPoints);
            if (spawnPoints == null)
                return;

            RefreshSpawnPointIdCounts(spawnPoints);
            if (!string.IsNullOrEmpty(selectedSpawnPointId) &&
                (!spawnPointIdCounts.TryGetValue(
                     selectedSpawnPointId,
                     out int selectedIdCount) ||
                 selectedIdCount != 1))
                selectedSpawnPointId = null;

            for (int pointIndex = 0;
                 pointIndex < spawnPoints.Count;
                 pointIndex++)
            {
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
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

                bool hasUniqueId =
                    !string.IsNullOrEmpty(spawnPoint.Id) &&
                    spawnPointIdCounts[spawnPoint.Id] == 1;
                if (enablePointInteraction && hasUniqueId)
                {
                    float hitRadius = markerRadius * MarkerHitAreaMultiplier;
                    float hitDistance =
                        HandleUtility.DistanceToCircle(worldPosition, hitRadius);
                    if (IsFinite(hitDistance))
                    {
                        spawnPointHandles.Add(
                            new SpawnPointHandle(
                                controlId,
                                spawnPoint.Id,
                                hitDistance));
                        if (currentEvent.type == EventType.Layout)
                            HandleUtility.AddControl(controlId, hitDistance);
                    }
                }

                if (pointIndex >= spawnPointGeometry.Count)
                {
                    InvalidateSpawnPointGeometry();
                    continue;
                }

                SpawnPointGeometry geometry = spawnPointGeometry[pointIndex];
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

                if (string.Equals(
                        selectedSpawnPointId,
                        spawnPoint.Id,
                        StringComparison.Ordinal))
                {
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(
                        worldPosition,
                        planeNormal,
                        markerRadius * 1.35f);
                    Handles.color = markerColor;
                }
            }
        }
        finally
        {
            Handles.color = previousColor;
        }
    }

    private bool TryBeginSpawnPointDrag(
        SpawnPointHandle handle,
        Event currentEvent)
    {
        if (!TryGetUniqueSpawnPoint(
                handle.Id,
                out BallSpawnPointData spawnPoint))
            return false;

        selectedSpawnPointId = handle.Id;
        Ray worldRay =
            HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        if (!LevelBallEditorAssetOperations.TryGetLocalPosition(
                levelRoot,
                worldRay,
                out Vector2 pointerLocalPosition))
            return false;

        draggedSpawnPointId = handle.Id;
        dragLocalOffset = spawnPoint.LocalPosition - pointerLocalPosition;
        dragStartScreenPosition = currentEvent.mousePosition;
        dragControlId = handle.ControlId;
        dragHasChanged = false;
        isDraggingSpawnPoint = true;
        Undo.IncrementCurrentGroup();
        dragUndoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Mover punto de aparición");
        GUIUtility.hotControl = dragControlId;
        return true;
    }

    private void UpdateDraggedSpawnPoint(Event currentEvent)
    {
        if (!TryGetUniqueSpawnPoint(
                draggedSpawnPointId,
                out BallSpawnPointData spawnPoint))
        {
            EndSpawnPointDrag();
            return;
        }

        Ray worldRay =
            HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);
        if (!LevelBallEditorAssetOperations.TryGetLocalPosition(
                levelRoot,
                worldRay,
                out Vector2 pointerLocalPosition))
            return;

        Vector2 localPosition = pointerLocalPosition + dragLocalOffset;
        if (localPosition == spawnPoint.LocalPosition)
            return;

        if (LevelBallEditorAssetOperations.TrySetSpawnPointLocalPosition(
                levelDataAsset,
                draggedSpawnPointId,
                localPosition))
        {
            dragHasChanged = true;
            Repaint();
            SceneView.RepaintAll();
        }
    }

    private bool TryGetUnambiguousSpawnPointHandle(
        int nearestControl,
        out SpawnPointHandle selectedHandle,
        out bool isAmbiguous)
    {
        selectedHandle = default(SpawnPointHandle);
        isAmbiguous = false;

        SpawnPointHandle nearestHandle = default(SpawnPointHandle);
        bool foundNearest = false;
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < spawnPointHandles.Count; i++)
        {
            SpawnPointHandle handle = spawnPointHandles[i];
            if (handle.ControlId != nearestControl)
                continue;

            nearestHandle = handle;
            nearestDistance = handle.HitDistance;
            foundNearest = true;
            break;
        }

        if (!foundNearest || nearestDistance > SelectionAmbiguityPixels)
            return false;

        int nearbyControlCount = 0;
        for (int i = 0; i < spawnPointHandles.Count; i++)
        {
            if (spawnPointHandles[i].HitDistance <=
                nearestDistance + SelectionAmbiguityPixels)
                nearbyControlCount++;
        }

        if (nearbyControlCount != 1)
        {
            isAmbiguous = true;
            return true;
        }

        selectedHandle = nearestHandle;
        return true;
    }

    private bool TryGetUniqueSpawnPoint(
        string spawnPointId,
        out BallSpawnPointData spawnPoint)
    {
        spawnPoint = null;
        if (string.IsNullOrEmpty(spawnPointId) ||
            levelDataAsset == null ||
            levelDataAsset.SpawnPoints == null)
            return false;

        IReadOnlyList<BallSpawnPointData> spawnPoints =
            levelDataAsset.SpawnPoints;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            BallSpawnPointData candidate = spawnPoints[i];
            if (candidate == null ||
                !string.Equals(
                    candidate.Id,
                    spawnPointId,
                    StringComparison.Ordinal))
                continue;

            if (spawnPoint != null)
            {
                spawnPoint = null;
                return false;
            }

            spawnPoint = candidate;
        }

        return spawnPoint != null;
    }

    private void RefreshSpawnPointIdCounts(
        IReadOnlyList<BallSpawnPointData> spawnPoints)
    {
        spawnPointIdCounts.Clear();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[i];
            if (spawnPoint == null || string.IsNullOrEmpty(spawnPoint.Id))
                continue;

            if (spawnPointIdCounts.TryGetValue(spawnPoint.Id, out int count))
                spawnPointIdCounts[spawnPoint.Id] = count + 1;
            else
                spawnPointIdCounts.Add(spawnPoint.Id, 1);
        }
    }

    private void CancelPointPlacement()
    {
        if (isPlacingPoint && GUIUtility.hotControl == placementControlId)
            GUIUtility.hotControl = 0;

        isPlacingPoint = false;
        placementWasDragged = false;
        placementControlId = 0;
    }

    private bool HasExceededDragThreshold(Vector2 mousePosition)
    {
        return (mousePosition - dragStartScreenPosition).sqrMagnitude >
            DragThresholdPixels * DragThresholdPixels;
    }

    private void EndSpawnPointDrag()
    {
        if (isDraggingSpawnPoint &&
            GUIUtility.hotControl == dragControlId)
            GUIUtility.hotControl = 0;

        if (dragUndoGroup >= 0 && dragHasChanged)
            Undo.CollapseUndoOperations(dragUndoGroup);

        isDraggingSpawnPoint = false;
        dragHasChanged = false;
        dragControlId = 0;
        dragUndoGroup = -1;
        draggedSpawnPointId = null;
        dragLocalOffset = Vector2.zero;
        dragStartScreenPosition = Vector2.zero;
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

        bool hasRelevantModification = false;
        bool requiresGeometryRefresh = false;
        for (int i = 0; i < modifications.Length; i++)
        {
            PropertyModification currentValue =
                modifications[i].currentValue;
            if (currentValue == null ||
                !IsRelevantModifiedObject(currentValue.target))
                continue;

            hasRelevantModification = true;
            if (!IsSpawnPointPositionModification(currentValue))
                requiresGeometryRefresh = true;
        }

        if (hasRelevantModification)
        {
            if (requiresGeometryRefresh)
                InvalidateSpawnPointGeometry();

            SceneView.RepaintAll();
        }

        return modifications;
    }

    private bool IsSpawnPointPositionModification(
        PropertyModification modification)
    {
        return modification.target == levelDataAsset &&
            !string.IsNullOrEmpty(modification.propertyPath) &&
            modification.propertyPath.IndexOf(
                ".localPosition",
                StringComparison.Ordinal) >= 0;
    }

    private void OnProjectChanged()
    {
        InvalidateSpawnPointGeometry();
        SceneView.RepaintAll();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        CancelPointPlacement();
        EndSpawnPointDrag();
        selectedSpawnPointId = null;
        spawnPointHandles.Clear();
        spawnPointIdCounts.Clear();
        InvalidateSpawnPointGeometry();
        Repaint();
        SceneView.RepaintAll();
    }

    private void InvalidateSpawnPointGeometry()
    {
        spawnPointGeometryDirty = true;
    }

    private void RefreshSpawnPointGeometryIfNeeded(
        IReadOnlyList<BallSpawnPointData> spawnPoints)
    {
        int spawnPointCount = spawnPoints == null ? 0 : spawnPoints.Count;
        if (!spawnPointGeometryDirty &&
            cachedGeometryAsset == levelDataAsset &&
            ReferenceEquals(cachedSpawnPoints, spawnPoints) &&
            spawnPointGeometry.Count == spawnPointCount)
            return;

        spawnPointGeometry.Clear();
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

        cachedGeometryAsset = levelDataAsset;
        cachedSpawnPoints = spawnPoints;
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

    private readonly struct SpawnPointHandle
    {
        public readonly int ControlId;
        public readonly string Id;
        public readonly float HitDistance;

        public SpawnPointHandle(int controlId, string id, float hitDistance)
        {
            ControlId = controlId;
            Id = id;
            HitDistance = hitDistance;
        }
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
