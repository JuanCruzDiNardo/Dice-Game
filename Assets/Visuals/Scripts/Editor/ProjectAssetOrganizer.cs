using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ProjectAssetOrganizer
{
    private readonly struct AssetMove
    {
        public AssetMove(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }

        public string Source { get; }
        public string Destination { get; }
    }

    private static readonly AssetMove[] Moves =
    {
        new("Assets/Dice/Fonts", "Assets/Fonts/Dice"),
        new("Assets/Dice/Generated", "Assets/Generated/Dice"),
        new("Assets/Dice/Materials", "Assets/Materials/Dice"),
        new("Assets/Dice/Model", "Assets/Models/Dice"),
        new("Assets/Dice/Prefabs", "Assets/Prefabs/Dice"),
        new("Assets/Dice/Scripts", "Assets/Scripts/Dice"),
        new("Assets/Dice/Shaders", "Assets/Shaders/Dice"),
        new("Assets/Dice/Textures", "Assets/Textures/Dice"),
        new("Assets/Dice/README.md", "Assets/Documentation/Dice/README.md"),

        new("Assets/Enemy_Orc_Visual/Prefab", "Assets/Prefabs/Enemies/Orc"),
        new("Assets/Enemy_Orc_Visual/Scripts", "Assets/Scripts/Enemies/Orc"),
        new("Assets/Enemy_Orc_Visual/Sprites", "Assets/Sprites/Enemies/Orc"),

        new("Assets/Ground/Materials", "Assets/Materials/Ground"),
        new("Assets/Ground/Prefabs", "Assets/Prefabs/Environment/Ground"),
        new("Assets/Ground/Scripts", "Assets/Scripts/Environment/Ground"),
        new("Assets/Ground/Shaders", "Assets/Shaders/Ground"),
        new("Assets/Ground/Textures", "Assets/Textures/Ground"),

        new("Assets/Materials/Ground.mat", "Assets/Materials/Ground/Ground.mat"),
        new("Assets/Materials/M_HandDrawnSprite.mat", "Assets/Materials/UI/M_HandDrawnSprite.mat"),
        new("Assets/Materials/M_TowerHandDrawn.mat", "Assets/Materials/Environment/M_TowerHandDrawn.mat"),
        new("Assets/Materials/Orco_PlaceHolder.mat", "Assets/Materials/Enemies/Orc/Orco_PlaceHolder.mat"),
        new("Assets/Materials/Sin título-1.mat", "Assets/Materials/Ground/M_GrassWind_Prototype.mat"),

        new("Assets/Brick.jpg", "Assets/Textures/Environment/Brick.jpg"),
        new("Assets/Paper.jpg", "Assets/Textures/UI/Paper.jpg"),
        new("Assets/Orco_PlaceHolder.png", "Assets/Sprites/Enemies/Orc/Orco_PlaceHolder.png"),
        new("Assets/Mago.png", "Assets/Sprites/UI/MainMenu/Mago.png"),
        new("Assets/Dados.png", "Assets/Sprites/UI/MainMenu/Dados.png"),

        new("Assets/Entrance.fbx", "Assets/Models/Environment/Entrance.fbx"),
        new("Assets/Tower.fbx", "Assets/Models/Environment/Tower.fbx"),
        new("Assets/Wall.fbx", "Assets/Models/Environment/Wall.fbx"),
        new("Assets/InputSystem_Actions.inputactions", "Assets/Settings/Input/InputSystem_Actions.inputactions")
    };

    [MenuItem("Tools/Project/Organize Assets")]
    public static void OrganizeAssets()
    {
        var failures = new List<string>();

        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (AssetMove move in Moves)
                MoveAsset(move, failures);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        RemoveEmptyLegacyFolder("Assets/Dice");
        RemoveEmptyLegacyFolder("Assets/Enemy_Orc_Visual");
        RemoveEmptyLegacyFolder("Assets/Ground");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Asset organization completed with errors:\n" + string.Join("\n", failures));
        }

        Debug.Log("Assets organized successfully. Unity GUID references were preserved.");
    }

    private static void MoveAsset(AssetMove move, ICollection<string> failures)
    {
        if (AssetDatabase.LoadMainAssetAtPath(move.Source) == null &&
            !AssetDatabase.IsValidFolder(move.Source))
        {
            if (AssetDatabase.LoadMainAssetAtPath(move.Destination) == null &&
                !AssetDatabase.IsValidFolder(move.Destination))
            {
                failures.Add($"Missing source: {move.Source}");
            }

            return;
        }

        if (AssetDatabase.LoadMainAssetAtPath(move.Destination) != null ||
            AssetDatabase.IsValidFolder(move.Destination))
        {
            return;
        }

        EnsureParentFolder(move.Destination);
        string error = AssetDatabase.MoveAsset(move.Source, move.Destination);

        if (!string.IsNullOrEmpty(error))
            failures.Add($"{move.Source} -> {move.Destination}: {error}");
    }

    private static void EnsureParentFolder(string assetPath)
    {
        string parent = assetPath[..assetPath.LastIndexOf('/')];
        string[] segments = parent.Split('/');
        string current = segments[0];

        for (int index = 1; index < segments.Length; index++)
        {
            string next = $"{current}/{segments[index]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, segments[index]);

            current = next;
        }
    }

    private static void RemoveEmptyLegacyFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        bool containsAssets = AssetDatabase.FindAssets(string.Empty, new[] { folderPath })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Any(path => !string.Equals(path, folderPath, StringComparison.Ordinal));

        if (!containsAssets)
            AssetDatabase.DeleteAsset(folderPath);
    }
}
