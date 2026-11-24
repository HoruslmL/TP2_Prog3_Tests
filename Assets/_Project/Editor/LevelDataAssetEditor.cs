using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelDataAsset))]
public sealed class LevelDataAssetEditor : Editor
{
    private const double ChangePollIntervalSeconds = 0.25d;

    private Transform validationRoot;
    private LevelDataValidationResult validationResult;
    private BallDistributionResult distributionResult;
    private bool applyingInspectorChanges;
    private string lastValidationSignature;
    private string lastDistributionSignature;
    private string lastPrefabDependencySignature;
    private double nextChangePollTime;

    internal LevelDataValidationResult CurrentValidationResult =>
        validationResult;

    internal BallDistributionResult CurrentDistributionResult =>
        distributionResult;

    private void OnEnable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        Undo.undoRedoPerformed += OnUndoRedo;
        Undo.postprocessModifications -= OnPostprocessModifications;
        Undo.postprocessModifications += OnPostprocessModifications;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        EditorApplication.update -= PollForChanges;
        EditorApplication.update += PollForChanges;
        UpdateSignatures();
        RefreshValidation();
        RefreshDistribution();
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
        Undo.postprocessModifications -= OnPostprocessModifications;
        EditorApplication.projectChanged -= OnProjectChanged;
        EditorApplication.update -= PollForChanges;
    }

    public override void OnInspectorGUI()
    {
        var levelData = (LevelDataAsset)target;
        serializedObject.Update();

        EditorGUILayout.LabelField("Configuración del nivel", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        Transform assignedRoot = (Transform)EditorGUILayout.ObjectField(
            "Level Root (validación)",
            validationRoot,
            typeof(Transform),
            true);
        bool rootChanged = EditorGUI.EndChangeCheck();
        if (rootChanged)
            validationRoot = assignedRoot;

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("seed"),
            new GUIContent("Seed"));
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("enableDynamicBalance"),
            new GUIContent("Balance dinámico"));
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("minimumSpawnPointClearance"),
            new GUIContent("Margen mínimo entre puntos"));

        SerializedProperty spawnPoints =
            serializedObject.FindProperty("spawnPoints");
        DrawSpawnPoints(spawnPoints);
        bool serializedChanged;
        applyingInspectorChanges = true;
        try
        {
            serializedChanged = serializedObject.ApplyModifiedProperties();
        }
        finally
        {
            applyingInspectorChanges = false;
        }

        if (GUILayout.Button("Agregar punto"))
        {
            LevelBallEditorAssetOperations.AddSpawnPoint(
                levelData,
                Vector2.zero);
            serializedObject.Update();
            CheckForChanges();
        }

        if (rootChanged || serializedChanged)
            CheckForChanges();

        DrawValidationResults();
        DrawDistributionPreview(levelData);
    }

    private void DrawSpawnPoints(SerializedProperty spawnPoints)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Puntos de aparición", EditorStyles.boldLabel);

        if (spawnPoints == null || !spawnPoints.isArray)
        {
            EditorGUILayout.HelpBox(
                "No se encontró la lista serializada de puntos.",
                MessageType.Error);
            return;
        }

        for (int pointIndex = 0;
             pointIndex < spawnPoints.arraySize;
             pointIndex++)
        {
            SerializedProperty spawnPoint =
                spawnPoints.GetArrayElementAtIndex(pointIndex);
            SerializedProperty id =
                spawnPoint.FindPropertyRelative("id");
            string pointId = id.stringValue;
            string label = string.IsNullOrWhiteSpace(pointId)
                ? "Punto " + (pointIndex + 1)
                : "Punto " + (pointIndex + 1) + " - " + pointId;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            spawnPoint.isExpanded = EditorGUILayout.Foldout(
                spawnPoint.isExpanded,
                label,
                true);

            bool removePoint = false;
            if (spawnPoint.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(
                    id,
                    new GUIContent("Identificador"));
                EditorGUILayout.PropertyField(
                    spawnPoint.FindPropertyRelative("localPosition"),
                    new GUIContent("Posición local"));
                DrawSpawnOptions(
                    spawnPoint.FindPropertyRelative("spawnOptions"));
                removePoint = GUILayout.Button("Eliminar punto");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            if (!removePoint)
                continue;

            spawnPoints.DeleteArrayElementAtIndex(pointIndex);
            break;
        }
    }

    private void DrawSpawnOptions(SerializedProperty options)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Opciones de aparición", EditorStyles.boldLabel);

        if (options == null || !options.isArray)
        {
            EditorGUILayout.HelpBox(
                "No se encontró la lista serializada de opciones.",
                MessageType.Error);
            return;
        }

        for (int optionIndex = 0;
             optionIndex < options.arraySize;
             optionIndex++)
        {
            SerializedProperty option =
                options.GetArrayElementAtIndex(optionIndex);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.PropertyField(
                option.FindPropertyRelative("ballData"),
                new GUIContent("Bolita"));
            EditorGUILayout.PropertyField(
                option.FindPropertyRelative("spawnProbability"),
                new GUIContent("Probabilidad (%)"));
            bool removeOption = GUILayout.Button("Eliminar opción");
            EditorGUILayout.EndVertical();

            if (!removeOption)
                continue;

            options.DeleteArrayElementAtIndex(optionIndex);
            break;
        }

        if (!GUILayout.Button("Agregar opción"))
            return;

        int newIndex = options.arraySize;
        options.InsertArrayElementAtIndex(newIndex);
        SerializedProperty newOption =
            options.GetArrayElementAtIndex(newIndex);
        newOption.FindPropertyRelative("ballData").objectReferenceValue = null;
        newOption.FindPropertyRelative("spawnProbability").floatValue = 0f;
    }

    private void DrawValidationResults()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Validación", EditorStyles.boldLabel);

        if (validationResult == null)
            return;

        if (validationResult.IsValid)
        {
            EditorGUILayout.HelpBox(
                "La configuración del nivel es válida.",
                MessageType.Info);
        }
        else
        {
            foreach (LevelDataValidationIssue issue in validationResult.Issues)
            {
                string location = issue.SpawnPointIndex.HasValue
                    ? "Punto " + (issue.SpawnPointIndex.Value + 1) +
                      (string.IsNullOrEmpty(issue.SpawnPointId)
                          ? string.Empty
                          : " (" + issue.SpawnPointId + ")") +
                      ": "
                    : string.Empty;
                EditorGUILayout.HelpBox(
                    issue.Code + " — " + location + issue.Message,
                    MessageType.Error);
            }
        }

        foreach (LevelDataSpacingWarning warning in validationResult.Warnings)
        {
            EditorGUILayout.HelpBox(
                "Separación reducida entre '" + warning.SpawnPointId +
                "' y '" + warning.RelatedSpawnPointId +
                "': " + warning.Clearance.ToString("F2", CultureInfo.CurrentCulture) +
                " unidades libres. Esto es una advertencia; no garantiza " +
                "ausencia de colisiones durante el movimiento.",
                MessageType.Warning);
        }
    }

    private void DrawDistributionPreview(LevelDataAsset levelData)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            "Previsualización de distribución",
            EditorStyles.boldLabel);

        if (validationResult != null && !validationResult.IsValid)
        {
            EditorGUILayout.HelpBox(
                "Simulación lógica: los resultados no representan una " +
                "distribución físicamente válida mientras haya errores de validación.",
                MessageType.Warning);
        }

        if (distributionResult == null)
            return;

        if (!distributionResult.IsSuccess)
        {
            EditorGUILayout.HelpBox(
                distributionResult.Error.Code + " — " +
                distributionResult.Error.Message,
                MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField(
            "Seed: " + levelData.Seed +
            "    Balance dinámico: " +
            (levelData.EnableDynamicBalance ? "Activado" : "Desactivado"));

        foreach (BallDistributionEntry entry in distributionResult.Entries)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                "Punto " + (entry.SpawnPointIndex + 1) +
                " (" + entry.SpawnPointId + ")",
                GUILayout.Width(180f));
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    entry.BallData,
                    typeof(BallDataAsset),
                    false);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void RefreshValidation()
    {
        LevelDataAsset levelData = (LevelDataAsset)target;
        validationResult = LevelDataValidator.Validate(
            levelData,
            validationRoot);
        Repaint();
    }

    private void RefreshDistribution()
    {
        LevelDataAsset levelData = (LevelDataAsset)target;
        distributionResult = BallDistributionGenerator.Generate(levelData);
        Repaint();
    }

    private void PollForChanges()
    {
        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime < nextChangePollTime)
            return;

        nextChangePollTime = currentTime + ChangePollIntervalSeconds;
        CheckForChanges();
    }

    internal void CheckForChanges()
    {
        CheckForChanges(false);
    }

    private void CheckForChanges(bool checkPrefabDependencies)
    {
        LevelDataAsset levelData = (LevelDataAsset)target;
        string validationSignature = BuildValidationSignature(levelData);
        string distributionSignature = BuildDistributionSignature(levelData);
        bool validationChanged =
            validationSignature != lastValidationSignature;
        bool distributionChanged =
            distributionSignature != lastDistributionSignature;
        bool prefabDependenciesChanged = checkPrefabDependencies &&
            HasPrefabDependenciesChanged(levelData);

        if (!validationChanged &&
            !distributionChanged &&
            !prefabDependenciesChanged)
            return;

        lastValidationSignature = validationSignature;
        lastDistributionSignature = distributionSignature;
        if (validationChanged || prefabDependenciesChanged)
            lastPrefabDependencySignature =
                BuildPrefabDependencySignature(levelData);

        if (validationChanged || prefabDependenciesChanged)
            RefreshValidation();
        if (distributionChanged)
            RefreshDistribution();
    }

    private void UpdateSignatures()
    {
        LevelDataAsset levelData = (LevelDataAsset)target;
        lastValidationSignature = BuildValidationSignature(levelData);
        lastDistributionSignature = BuildDistributionSignature(levelData);
        lastPrefabDependencySignature =
            BuildPrefabDependencySignature(levelData);
    }

    private string BuildValidationSignature(LevelDataAsset levelData)
    {
        var signature = new StringBuilder(128);
        AppendFloat(signature, levelData.MinimumSpawnPointClearance);
        IReadOnlyList<BallSpawnPointData> spawnPoints = levelData.SpawnPoints;
        if (spawnPoints == null)
        {
            signature.Append("null-points|");
        }
        else
        {
            signature.Append(spawnPoints.Count).Append('|');
            for (int pointIndex = 0;
                 pointIndex < spawnPoints.Count;
                 pointIndex++)
            {
                BallSpawnPointData spawnPoint = spawnPoints[pointIndex];
                if (spawnPoint == null)
                {
                    signature.Append("null-point|");
                    continue;
                }

                AppendString(signature, spawnPoint.Id);
                AppendFloat(signature, spawnPoint.LocalPosition.x);
                AppendFloat(signature, spawnPoint.LocalPosition.y);
                IReadOnlyList<BallSpawnOption> options =
                    spawnPoint.SpawnOptions;
                if (options == null)
                {
                    signature.Append("null-options|");
                    continue;
                }

                signature.Append(options.Count).Append('|');
                for (int optionIndex = 0;
                     optionIndex < options.Count;
                     optionIndex++)
                {
                    BallSpawnOption option = options[optionIndex];
                    if (option == null)
                    {
                        signature.Append("null-option|");
                        continue;
                    }

                    AppendFloat(signature, option.SpawnProbability);
                    BallDataAsset ballData = option.BallData;
                    signature.Append(
                        ballData == null ? 0 : ballData.GetInstanceID());
                    signature.Append('|');
                    if (ballData != null)
                    {
                        AppendFloat(
                            signature,
                            ballData.ToBallData().Scale);
                        GameObject prefab = ballData.BallPrefab;
                        signature.Append(
                            prefab == null ? 0 : prefab.GetInstanceID());
                        signature.Append('|');
                    }
                }
            }
        }

        AppendRootTransform(signature);
        return signature.ToString();
    }

    private static string BuildDistributionSignature(LevelDataAsset levelData)
    {
        var signature = new StringBuilder(128);
        signature.Append(levelData.Seed).Append('|');
        signature.Append(levelData.EnableDynamicBalance).Append('|');

        IReadOnlyList<BallSpawnPointData> spawnPoints = levelData.SpawnPoints;
        if (spawnPoints == null)
        {
            signature.Append("null-points|");
            return signature.ToString();
        }

        signature.Append(spawnPoints.Count).Append('|');
        for (int pointIndex = 0;
             pointIndex < spawnPoints.Count;
             pointIndex++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[pointIndex];
            if (spawnPoint == null)
            {
                signature.Append("null-point|");
                continue;
            }

            AppendString(signature, spawnPoint.Id);
            AppendFloat(signature, spawnPoint.LocalPosition.x);
            AppendFloat(signature, spawnPoint.LocalPosition.y);
            IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
            if (options == null)
            {
                signature.Append("null-options|");
                continue;
            }

            signature.Append(options.Count).Append('|');
            for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
            {
                BallSpawnOption option = options[optionIndex];
                if (option == null)
                {
                    signature.Append("null-option|");
                    continue;
                }

                AppendFloat(signature, option.SpawnProbability);
                BallDataAsset ballData = option.BallData;
                signature.Append(
                    ballData == null ? 0 : ballData.GetInstanceID());
                signature.Append('|');
            }
        }

        return signature.ToString();
    }

    private static void AppendString(StringBuilder signature, string value)
    {
        if (value == null)
        {
            signature.Append("-1:");
            return;
        }

        signature.Append(value.Length).Append(':').Append(value);
    }

    private static void AppendFloat(StringBuilder signature, float value)
    {
        signature.Append(
            value.ToString("R", CultureInfo.InvariantCulture));
        signature.Append('|');
    }

    private void AppendRootTransform(StringBuilder signature)
    {
        signature.Append("root|");
        if (validationRoot == null)
        {
            signature.Append("null|");
            return;
        }

        signature.Append(validationRoot.GetInstanceID()).Append('|');
        Matrix4x4 localToWorld = validationRoot.localToWorldMatrix;
        for (int i = 0; i < 16; i++)
            AppendFloat(signature, localToWorld[i]);
    }

    private bool HasPrefabDependenciesChanged(LevelDataAsset levelData)
    {
        return BuildPrefabDependencySignature(levelData) !=
            lastPrefabDependencySignature;
    }

    private static string BuildPrefabDependencySignature(
        LevelDataAsset levelData)
    {
        var signature = new StringBuilder(128);
        var seenAssets = new HashSet<int>();
        IReadOnlyList<BallSpawnPointData> spawnPoints = levelData.SpawnPoints;
        if (spawnPoints == null)
            return string.Empty;

        for (int pointIndex = 0; pointIndex < spawnPoints.Count; pointIndex++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[pointIndex];
            if (spawnPoint == null || spawnPoint.SpawnOptions == null)
                continue;

            IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
            for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
            {
                BallSpawnOption option = options[optionIndex];
                BallDataAsset ballData = option == null ? null : option.BallData;
                if (ballData == null || !seenAssets.Add(ballData.GetInstanceID()))
                    continue;

                signature.Append(ballData.GetInstanceID()).Append('|');
                GameObject prefab = ballData.BallPrefab;
                if (prefab == null)
                {
                    signature.Append("null|");
                    continue;
                }

                string assetPath = AssetDatabase.GetAssetPath(prefab);
                AppendString(signature, assetPath);
                if (!string.IsNullOrEmpty(assetPath))
                    signature.Append(
                        AssetDatabase.GetAssetDependencyHash(assetPath));
                signature.Append('|');
            }
        }

        return signature.ToString();
    }

    private bool IsRelevantModifiedObject(
        Object modifiedObject,
        out bool prefabGeometryChanged)
    {
        prefabGeometryChanged = false;
        if (modifiedObject == null)
            return false;

        if (modifiedObject == target || modifiedObject == validationRoot)
            return true;

        Transform changedTransform = GetTransform(modifiedObject);
        if (validationRoot != null &&
            changedTransform != null &&
            (changedTransform == validationRoot ||
             validationRoot.IsChildOf(changedTransform)))
            return true;

        IReadOnlyList<BallSpawnPointData> spawnPoints =
            ((LevelDataAsset)target).SpawnPoints;
        if (spawnPoints == null)
            return false;

        for (int pointIndex = 0; pointIndex < spawnPoints.Count; pointIndex++)
        {
            BallSpawnPointData spawnPoint = spawnPoints[pointIndex];
            if (spawnPoint == null || spawnPoint.SpawnOptions == null)
                continue;

            IReadOnlyList<BallSpawnOption> options = spawnPoint.SpawnOptions;
            for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
            {
                BallSpawnOption option = options[optionIndex];
                BallDataAsset ballData = option == null ? null : option.BallData;
                if (ballData == null)
                    continue;

                if (modifiedObject == ballData)
                    return true;

                GameObject prefab = ballData.BallPrefab;
                if (prefab != null &&
                    changedTransform != null &&
                    (changedTransform == prefab.transform ||
                     changedTransform.IsChildOf(prefab.transform)))
                {
                    prefabGeometryChanged = true;
                    return true;
                }
            }
        }

        return false;
    }

    private static Transform GetTransform(Object modifiedObject)
    {
        var transform = modifiedObject as Transform;
        if (transform != null)
            return transform;

        var component = modifiedObject as Component;
        if (component != null)
            return component.transform;

        var gameObject = modifiedObject as GameObject;
        return gameObject == null ? null : gameObject.transform;
    }

    private void OnUndoRedo()
    {
        serializedObject.Update();
        CheckForChanges(true);
        Repaint();
    }

    private UndoPropertyModification[] OnPostprocessModifications(
        UndoPropertyModification[] modifications)
    {
        if (modifications == null)
            return modifications;

        if (applyingInspectorChanges)
            return modifications;

        for (int i = 0; i < modifications.Length; i++)
        {
            PropertyModification currentValue = modifications[i].currentValue;
            bool prefabGeometryChanged;
            if (currentValue == null ||
                !IsRelevantModifiedObject(
                    currentValue.target,
                    out prefabGeometryChanged))
                continue;

            serializedObject.Update();
            if (prefabGeometryChanged)
            {
                RefreshValidation();
                lastPrefabDependencySignature =
                    BuildPrefabDependencySignature((LevelDataAsset)target);
                lastValidationSignature =
                    BuildValidationSignature((LevelDataAsset)target);
            }
            else
            {
                CheckForChanges(false);
            }
            break;
        }

        return modifications;
    }

    private void OnProjectChanged()
    {
        CheckForChanges(true);
    }
}
