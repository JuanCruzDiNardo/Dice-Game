using System;
using TMPro;
using UnityEngine;

[Serializable]
public sealed class DiceFaceData
{
    // Setup-owned references ------------------------------------------------

    [SerializeField, HideInInspector]
    private string faceId;

    [SerializeField, HideInInspector]
    private int materialIndex = -1;

    [SerializeField, HideInInspector]
    private Transform anchor;

    // User configuration ----------------------------------------------------

    [Header("Gameplay")]
    [SerializeField]
    private int value = 1;

    [Header("Visual")]
    [SerializeField]
    [Tooltip("Optional transparent overlay rendered above the default base texture and below the label.")]
    private Texture2D texture;

    [SerializeField]
    private Color faceTint = new Color32(250, 250, 250, 255);

    [Header("Label")]
    [SerializeField]
    private bool useCustomLabel;

    [SerializeField]
    private string customLabel;

    [SerializeField]
    private TMP_FontAsset font;

    [SerializeField]
    private Color textColor = Color.black;

    [SerializeField]
    [Min(0.01f)]
    private float fontSize = 4f;

    [SerializeField]
    private FontStyles fontStyle = FontStyles.Normal;

    [SerializeField]
    [Min(0f)]
    private float textOffset = 0.01f;

    // Read-only public state ------------------------------------------------

    public string FaceId => faceId;

    public int MaterialIndex => materialIndex;

    public Transform Anchor => anchor;

    public int Value => value;

    public Texture2D Texture => texture;

    public Texture2D OverlayTexture => texture;

    public Color FaceTint => faceTint;

    public bool UseCustomLabel => useCustomLabel;

    public string CustomLabel => customLabel;

    public TMP_FontAsset Font => font;

    public Color TextColor => textColor;

    public float FontSize => fontSize;

    public FontStyles FontStyle => fontStyle;

    public float TextOffset => textOffset;


    public string DisplayLabel
    {
        get
        {
            if (useCustomLabel && !string.IsNullOrWhiteSpace(customLabel))
                return customLabel;

            return value.ToString();
        }
    }

    // Controlled mutation API ----------------------------------------------

    public void SetInternalReferences(
        string newFaceId,
        int newMaterialIndex,
        Transform newAnchor)
    {
        faceId = newFaceId;
        materialIndex = newMaterialIndex;
        anchor = newAnchor;
    }

    public void SetTexture(Texture2D newTexture)
    {
        SetOverlayTexture(newTexture);
    }

    public void SetOverlayTexture(Texture2D newTexture)
    {
        texture = newTexture;
    }

    public void SetFaceTint(Color newTint)
    {
        faceTint = newTint;
    }

    public void SetValue(int newValue)
    {
        value = newValue;
    }

    public void SetCustomLabel(string newLabel)
    {
        customLabel = newLabel;
        useCustomLabel = !string.IsNullOrWhiteSpace(newLabel);
    }
}
