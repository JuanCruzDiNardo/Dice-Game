using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditableMainMenuBootstrap
{
    private const string MenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string EditableHierarchyMarker = "m_Name: Main Menu Canvas";
    private const string StatusLogPath = "Logs/codex-editable-menu-setup.log";

    static EditableMainMenuBootstrap()
    {
        EditorApplication.delayCall += BuildWhenReady;
    }

    private static void BuildWhenReady()
    {
        if (EditorApplication.isCompiling ||
            EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += BuildWhenReady;
            return;
        }

        try
        {
            if (!HasEditableHierarchy())
            {
                WriteStatus("Building serialized Main Menu hierarchy...");
                MainMenuSceneBuilder.BuildMainMenu();
            }

            WriteStatus("Editable Main Menu hierarchy is ready.");
        }
        catch (Exception exception)
        {
            WriteStatus($"ERROR: {exception}");
            Debug.LogException(exception);
        }
    }

    private static bool HasEditableHierarchy()
    {
        if (!File.Exists(MenuScenePath))
            return false;

        return File.ReadAllText(MenuScenePath)
            .Contains(EditableHierarchyMarker, StringComparison.Ordinal);
    }

    private static void WriteStatus(string message)
    {
        File.AppendAllText(
            StatusLogPath,
            $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
        Debug.Log($"Codex editable menu: {message}");
    }
}
