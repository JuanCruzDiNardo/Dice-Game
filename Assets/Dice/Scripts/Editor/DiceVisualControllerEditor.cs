using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DiceVisualController))]
public class DiceVisualControllerEditor : Editor
{
    private DiceVisualController controller;

    // Editor lifecycle ------------------------------------------------------

    private void OnEnable()
    {
        controller =
            (DiceVisualController)target;
    }
    // Inspector presentation -----------------------------------------------


    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        bool inspectorChanged =
            EditorGUI.EndChangeCheck();

        serializedObject.ApplyModifiedProperties();


        EditorGUILayout.Space(12);

        EditorGUILayout.HelpBox(
            "Default Face Texture is the shared base surface. Per-face Texture is " +
            "composited above it as a transparent overlay and below the label. " +
            "Face Tint multiplies the composed result; None disables the overlay. " +
            "In Play Mode, inspector edits stay pending until an Apply button is used.",
            MessageType.Info
        );

        if (GUILayout.Button(
                "Apply Face Overlay / Tint Changes",
                GUILayout.Height(24)
            ))
        {
            controller.ApplyFaceAppearanceChanges();
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button(
                "Apply Label Changes",
                GUILayout.Height(24)
            ))
        {
            controller.ApplyLabelChanges();
            EditorUtility.SetDirty(controller);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Dice Setup",
            EditorStyles.boldLabel
        );


        if (GUILayout.Button(
                "Auto Setup Faces",
                GUILayout.Height(28)
            ))
        {
            Undo.RecordObject(
                controller,
                "Auto Setup Dice Faces"
            );

            controller.AutoSetup();

            EditorUtility.SetDirty(controller);

            SceneView.RepaintAll();
        }


        if (GUILayout.Button(
                "Refresh Visuals",
                GUILayout.Height(24)
            ))
        {
            controller.RefreshVisuals();

            EditorUtility.SetDirty(controller);

            SceneView.RepaintAll();
        }


        EditorGUILayout.Space(4);


        if (GUILayout.Button(
                "Remove Generated Labels",
                GUILayout.Height(22)
            ))
        {
            controller.RemoveGeneratedLabels();

            EditorUtility.SetDirty(controller);

            SceneView.RepaintAll();
        }


        if (inspectorChanged)
        {
            controller.InvalidateSetupCache();

            if (!Application.isPlaying)
                controller.RefreshVisuals();

            EditorUtility.SetDirty(controller);

            SceneView.RepaintAll();
        }
    }
}
