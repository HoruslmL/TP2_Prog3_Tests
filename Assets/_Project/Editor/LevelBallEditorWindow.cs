using UnityEditor;
using UnityEngine;

public sealed class LevelBallEditorWindow : EditorWindow
{
    [SerializeField] private LevelDataAsset levelDataAsset;
    [SerializeField] private Transform levelRoot;
    [SerializeField] private bool isAddingPoints;

    [MenuItem("Tools/Level Ball Editor")]
    private static void Open()
    {
        GetWindow<LevelBallEditorWindow>("Level Ball Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        Undo.undoRedoPerformed -= OnUndoRedo;
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
            SceneView.RepaintAll();

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
        Handles.color = new Color(1f, 0.75f, 0.1f, 1f);

        try
        {
            foreach (BallSpawnPointData spawnPoint in levelDataAsset.SpawnPoints)
            {
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
        Repaint();
        SceneView.RepaintAll();
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
