using UnityEngine;

public readonly struct DiceFaceRenderData
{
    // Immutable snapshot consumed by the runtime renderer optimizer.

    public DiceFaceRenderData(
        string faceId,
        int materialIndex,
        Texture2D baseTexture,
        Texture2D overlayTexture,
        Color tint)
    {
        FaceId = faceId;
        MaterialIndex = materialIndex;
        BaseTexture = baseTexture;
        OverlayTexture = overlayTexture;
        Tint = tint;
    }

    // Kept for source compatibility. Texture now represents the optional
    // transparent face overlay instead of replacing the base surface.
    public DiceFaceRenderData(
        string faceId,
        int materialIndex,
        Texture2D texture,
        Color tint)
        : this(
            faceId,
            materialIndex,
            null,
            texture,
            tint)
    {
    }

    public string FaceId { get; }

    public int MaterialIndex { get; }

    public Texture2D BaseTexture { get; }

    public Texture2D OverlayTexture { get; }

    public Texture2D Texture => OverlayTexture;

    public Color Tint { get; }
}
