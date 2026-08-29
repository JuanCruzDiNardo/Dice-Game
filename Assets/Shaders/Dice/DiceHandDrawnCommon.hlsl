#ifndef DICE_HAND_DRAWN_COMMON_INCLUDED
#define DICE_HAND_DRAWN_COMMON_INCLUDED

// Deterministic noise primitives -------------------------------------------

float DiceHash31(float3 value)
{
    value = frac(value * 0.1031);
    value += dot(value, value.yzx + 33.33);
    return frac((value.x + value.y) * value.z);
}

float DiceValueNoise(float3 position)
{
    float3 cell = floor(position);
    float3 localPosition = frac(position);
    float3 blend = localPosition * localPosition * (3.0 - 2.0 * localPosition);

    float n000 = DiceHash31(cell + float3(0.0, 0.0, 0.0));
    float n100 = DiceHash31(cell + float3(1.0, 0.0, 0.0));
    float n010 = DiceHash31(cell + float3(0.0, 1.0, 0.0));
    float n110 = DiceHash31(cell + float3(1.0, 1.0, 0.0));
    float n001 = DiceHash31(cell + float3(0.0, 0.0, 1.0));
    float n101 = DiceHash31(cell + float3(1.0, 0.0, 1.0));
    float n011 = DiceHash31(cell + float3(0.0, 1.0, 1.0));
    float n111 = DiceHash31(cell + float3(1.0, 1.0, 1.0));

    float n00 = lerp(n000, n100, blend.x);
    float n10 = lerp(n010, n110, blend.x);
    float n01 = lerp(n001, n101, blend.x);
    float n11 = lerp(n011, n111, blend.x);

    return lerp(lerp(n00, n10, blend.y), lerp(n01, n11, blend.y), blend.z);
}

// Quantized drawing variants -----------------------------------------------

float DiceQuantizedVariant(float timeSeconds, float framesPerSecond, float variationCount)
{
    float safeFramesPerSecond = max(framesPerSecond, 0.01);
    float safeVariationCount = max(floor(variationCount), 1.0);
    float frame = floor(timeSeconds * safeFramesPerSecond);
    return frame - floor(frame / safeVariationCount) * safeVariationCount;
}

float3 DiceVariantOffset(float seed, float variant)
{
    float combinedSeed = seed + variant * 47.17;

    return float3(
        DiceHash31(float3(combinedSeed, 11.13, 3.71)),
        DiceHash31(float3(5.97, combinedSeed, 19.41)),
        DiceHash31(float3(23.53, 7.31, combinedSeed))) * 2.0 - 1.0;
}

float DiceVariantNoise(
    float3 positionOS,
    float noiseScale,
    float seed,
    float variant)
{
    float3 drawingOffset = DiceVariantOffset(seed, variant) * 29.0;
    return DiceValueNoise(positionOS * max(noiseScale, 0.001) + drawingOffset);
}

// Artificial toon lighting -------------------------------------------------

half DiceToonLighting(
    half3 normalWS,
    float3 lightDirection,
    float2 thresholds,
    half midBrightness,
    half darkBrightness,
    half softness,
    half diffuseJitter)
{
    float3 safeLightDirection = dot(lightDirection, lightDirection) > 0.0001
        ? normalize(lightDirection)
        : float3(0.0, 1.0, 0.0);

    half diffuse = saturate(dot(normalize(normalWS), safeLightDirection) + diffuseJitter);
    half darkThreshold = (half)min(thresholds.x, thresholds.y);
    half lightThreshold = (half)max(thresholds.x, thresholds.y);
    half safeSoftness = max(softness, 0.0001h);

    half darkToMid = smoothstep(
        darkThreshold - safeSoftness,
        darkThreshold + safeSoftness,
        diffuse);

    half midToLight = smoothstep(
        lightThreshold - safeSoftness,
        lightThreshold + safeSoftness,
        diffuse);

    half brightness = lerp(darkBrightness, midBrightness, darkToMid);
    return lerp(brightness, 1.0h, midToLight);
}

#endif
