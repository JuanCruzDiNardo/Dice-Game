using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class HandDrawnOutlineEffect : BaseMeshEffect
{
    private const int OutlineSampleCount = 8;

    [SerializeField]
    private Color outlineColor = Color.black;

    [SerializeField, Range(0f, 12f)]
    private float thickness = 5f;

    [SerializeField, Range(1f, 24f)]
    private float framesPerSecond = 8f;

    [SerializeField, Range(1, 8)]
    private int variationCount = 3;

    [SerializeField]
    private float seed = 17f;

    private readonly List<UIVertex> sourceVertices = new();
    private readonly List<UIVertex> outputVertices = new();
    private int lastAnimationFrame = int.MinValue;

    public void Configure(
        float outlineThickness,
        Color color,
        float animationFramesPerSecond,
        int animationVariations,
        float animationSeed)
    {
        thickness = Mathf.Max(0f, outlineThickness);
        outlineColor = color;
        framesPerSecond = Mathf.Max(1f, animationFramesPerSecond);
        variationCount = Mathf.Clamp(animationVariations, 1, 8);
        seed = animationSeed;
        graphic?.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vertexHelper)
    {
        if (!IsActive() || thickness <= 0f || vertexHelper.currentVertCount == 0)
            return;

        sourceVertices.Clear();
        outputVertices.Clear();
        vertexHelper.GetUIVertexStream(sourceVertices);

        int animationFrame = GetAnimationFrame();
        int variation = PositiveModulo(animationFrame, variationCount);

        for (int sampleIndex = 0; sampleIndex < OutlineSampleCount; sampleIndex++)
        {
            Vector2 offset = CalculateOutlineOffset(sampleIndex, variation);
            AppendOutlineCopy(offset);
        }

        outputVertices.AddRange(sourceVertices);
        vertexHelper.Clear();
        vertexHelper.AddUIVertexTriangleStream(outputVertices);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        lastAnimationFrame = int.MinValue;
        graphic?.SetVerticesDirty();
    }

    protected override void OnDisable()
    {
        graphic?.SetVerticesDirty();
        base.OnDisable();
    }

    private void Update()
    {
        if (!IsActive())
            return;

        int animationFrame = GetAnimationFrame();
        if (animationFrame == lastAnimationFrame)
            return;

        lastAnimationFrame = animationFrame;
        graphic.SetVerticesDirty();
    }

    /*protected override void OnValidate()
    {
        base.OnValidate();
        thickness = Mathf.Clamp(thickness, 0f, 12f);
        framesPerSecond = Mathf.Clamp(framesPerSecond, 1f, 24f);
        variationCount = Mathf.Clamp(variationCount, 1, 8);
        graphic?.SetVerticesDirty();
    }*/

    private void AppendOutlineCopy(Vector2 offset)
    {
        Color32 tint = outlineColor;

        for (int vertexIndex = 0; vertexIndex < sourceVertices.Count; vertexIndex++)
        {
            UIVertex vertex = sourceVertices[vertexIndex];
            vertex.position += (Vector3)offset;
            vertex.color = new Color32(
                tint.r,
                tint.g,
                tint.b,
                (byte)(vertex.color.a * tint.a / 255));
            outputVertices.Add(vertex);
        }
    }

    private Vector2 CalculateOutlineOffset(int sampleIndex, int variation)
    {
        float baseAngle = Mathf.PI * 2f * sampleIndex / OutlineSampleCount;
        float animatedAngle = baseAngle + Mathf.Sin(seed + variation * 1.73f + sampleIndex) * 0.08f;
        float irregularity = 1f + Mathf.Sin(seed * 0.37f + variation * 2.41f + sampleIndex * 1.91f) * 0.14f;
        return new Vector2(Mathf.Cos(animatedAngle), Mathf.Sin(animatedAngle)) * thickness * irregularity;
    }

    private int GetAnimationFrame()
    {
        float time = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
        return Mathf.FloorToInt(time * framesPerSecond);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }
}
