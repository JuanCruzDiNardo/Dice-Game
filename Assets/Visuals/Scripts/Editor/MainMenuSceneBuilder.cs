using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/Main Menu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";
    private const string FontPath = "Assets/Fonts/Dice/Neythal-Regular SDF.asset";
    private const string PaperTexturePath = "Assets/Textures/UI/Paper.jpg";
    private const string WizardSpritePath = "Assets/Sprites/UI/MainMenu/Mago.png";
    private const string DiceSpritePath = "Assets/Sprites/UI/MainMenu/Dados.png";

    [MenuItem("Tools/Project/Build Main Menu")]
    public static void BuildMainMenu()
    {
        TMP_FontAsset font = LoadRequiredAsset<TMP_FontAsset>(FontPath);
        Texture paperTexture = LoadRequiredAsset<Texture>(PaperTexturePath);
        Sprite wizardSprite = LoadRequiredAsset<Sprite>(WizardSpritePath);
        Sprite diceSprite = LoadRequiredAsset<Sprite>(DiceSpritePath);

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene existingMenuScene = SceneManager.GetSceneByPath(MenuScenePath);
        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(menuScene);

        if (existingMenuScene.IsValid() && existingMenuScene.isLoaded)
        {
            if (existingMenuScene.isDirty)
            {
                EditorSceneManager.CloseScene(menuScene, true);
                throw new InvalidOperationException(
                    "Main Menu is open with unsaved changes. Save or discard them before rebuilding it.");
            }

            EditorSceneManager.CloseScene(existingMenuScene, true);
        }

        var rootObject = new GameObject("Main Menu Controller");
        MainMenuController controller = rootObject.AddComponent<MainMenuController>();
        var viewFactory = new MainMenuViewFactory(
            font,
            paperTexture,
            wizardSprite,
            diceSprite);
        MainMenuViewReferences view = viewFactory.Build(rootObject.transform);

        controller.Configure(
            view.MainActionsPanel,
            view.OptionsPanel,
            view.PlayButton,
            view.BackButton,
            GameplayScenePath);

        UnityEventTools.AddPersistentListener(view.PlayButton.onClick, controller.PlayGame);
        UnityEventTools.AddPersistentListener(view.OptionsButton.onClick, controller.ShowOptions);
        UnityEventTools.AddPersistentListener(view.BackButton.onClick, controller.ShowMainMenu);
        UnityEventTools.AddPersistentListener(view.ExitButton.onClick, controller.ExitGame);
        EditorUtility.SetDirty(controller);

        EditorSceneManager.MarkSceneDirty(menuScene);
        if (!EditorSceneManager.SaveScene(menuScene, MenuScenePath))
            throw new InvalidOperationException($"Could not save main menu scene at '{MenuScenePath}'.");

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();

        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousActiveScene);
            EditorSceneManager.CloseScene(menuScene, true);
        }
        else
        {
            Selection.activeGameObject = rootObject;
        }

        Debug.Log($"Editable main menu scene rebuilt successfully at '{MenuScenePath}'.");
    }

    private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Required asset was not found at '{path}'.");

        return asset;
    }

    private static void ConfigureBuildSettings()
    {
        var prioritizedScenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(GameplayScenePath, true)
        };

        EditorBuildSettingsScene[] remainingScenes = EditorBuildSettings.scenes
            .Where(scene => !string.Equals(scene.path, MenuScenePath, StringComparison.OrdinalIgnoreCase))
            .Where(scene => !string.Equals(scene.path, GameplayScenePath, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        EditorBuildSettings.scenes = prioritizedScenes.Concat(remainingScenes).ToArray();
    }
}
