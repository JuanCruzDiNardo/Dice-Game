using System;
using UnityEngine;

[Serializable]
public sealed class DeathScreenSequenceSettings
{
    [SerializeField, Min(0f)]
    [Tooltip("Duration of the initial fade from transparent to completely black.")]
    private float blackFadeDuration = 1.1f;

    [SerializeField, Min(0f)]
    [Tooltip("Pause after the black fade finishes and before the first button appears.")]
    private float blackScreenPause = 0.45f;

    [SerializeField, Min(0f)]
    [Tooltip("Fade duration used by each button.")]
    private float buttonFadeDuration = 0.4f;

    [SerializeField, Min(0f)]
    [Tooltip("Pause after Retry has appeared and before Exit begins to appear.")]
    private float pauseBetweenButtons = 0.3f;

    [SerializeField, Min(0f)]
    [Tooltip("Pause after both buttons have appeared and before the defeat title.")]
    private float pauseBeforeTitle = 0.45f;

    [SerializeField, Min(0f)]
    [Tooltip("Fade duration of the defeat title.")]
    private float titleFadeDuration = 0.65f;

    [SerializeField, Min(0f)]
    [Tooltip("Pause after the title and before the background, mage, shadow and light.")]
    private float pauseBeforeFinalArtwork = 0.5f;

    public float BlackFadeDuration => blackFadeDuration;
    public float BlackScreenPause => blackScreenPause;
    public float ButtonFadeDuration => buttonFadeDuration;
    public float PauseBetweenButtons => pauseBetweenButtons;
    public float PauseBeforeTitle => pauseBeforeTitle;
    public float TitleFadeDuration => titleFadeDuration;
    public float PauseBeforeFinalArtwork => pauseBeforeFinalArtwork;

    public void Validate()
    {
        blackFadeDuration = Mathf.Max(0f, blackFadeDuration);
        blackScreenPause = Mathf.Max(0f, blackScreenPause);
        buttonFadeDuration = Mathf.Max(0f, buttonFadeDuration);
        pauseBetweenButtons = Mathf.Max(0f, pauseBetweenButtons);
        pauseBeforeTitle = Mathf.Max(0f, pauseBeforeTitle);
        titleFadeDuration = Mathf.Max(0f, titleFadeDuration);
        pauseBeforeFinalArtwork = Mathf.Max(0f, pauseBeforeFinalArtwork);
    }
}
