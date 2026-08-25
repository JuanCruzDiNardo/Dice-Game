using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyVisualController : MonoBehaviour
{
    public enum VisualState
    {
        Walking,
        Dead
    }

    [Header("References")]
    [SerializeField]
    private SpriteRenderer bodyRenderer;

    [Header("Walking Animation")]
    [SerializeField]
    private Sprite[] walkSprites;

    [Tooltip("Duración completa del ciclo de caminar a velocidad normal.")]
    [SerializeField, Min(0.01f)]
    private float walkCycleDuration = 40f / 60f;

    [SerializeField]
    private bool randomizeInitialFrame = true;

    [Header("Death")]
    [SerializeField]
    private Sprite deadSprite;

    [SerializeField]
    private Sprite weaponSprite;

    [Header("Blood")]
    [SerializeField]
    private Sprite[] bloodSprites;

    [SerializeField, Min(1)]
    private int bloodSpriteCount = 1;

    [SerializeField]
    private Vector2 bloodRadiusRange = new Vector2(0.5f, 0.5f);

    [SerializeField]
    private Vector2 bloodScaleRange = new Vector2(1.5f, 1.5f);

    [Header("Dropped Weapon")]
    [SerializeField]
    private Vector2 weaponRadiusRange = new Vector2(1.8f, 1.8f);

    [Header("Sorting")]
    [SerializeField]
    private int weaponSortingOffset = -1;

    [SerializeField]
    private int bloodSortingOffset = -2;

    public VisualState CurrentState { get; private set; }

    public bool IsDead => CurrentState == VisualState.Dead;

    public float AnimationSpeed { get; private set; } = 1f;

    private float animationTimer;
    private int currentFrame;

    private readonly List<GameObject> spawnedDeathObjects = new();

    private void Awake()
    {
        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        PlayWalk();
    }

    private void Update()
    {
        UpdateWalkingAnimation();
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public void PlayWalk()
    {
        ClearDeathObjects();

        CurrentState = VisualState.Walking;

        animationTimer = 0f;

        if (walkSprites == null || walkSprites.Length == 0)
            return;

        currentFrame = randomizeInitialFrame
            ? Random.Range(0, walkSprites.Length)
            : 0;

        bodyRenderer.sprite = walkSprites[currentFrame];

        if (randomizeInitialFrame)
        {
            float normalizedFrame =
                currentFrame / (float)walkSprites.Length;

            animationTimer =
                normalizedFrame * walkCycleDuration;
        }

        enabled = true;
    }

    public void PlayDeath()
    {
        if (CurrentState == VisualState.Dead)
            return;

        CurrentState = VisualState.Dead;

        if (deadSprite != null)
            bodyRenderer.sprite = deadSprite;

        SpawnBlood();
        SpawnWeapon();

        // El cadáver deja de ejecutar Update.
        enabled = false;
    }

    /// <summary>
    /// Cambia el multiplicador de velocidad de la animación.
    ///
    /// 1   = velocidad normal.
    /// 0.5 = mitad de velocidad.
    /// 2   = doble velocidad.
    /// </summary>
    public void SetAnimationSpeed(float speedMultiplier)
    {
        AnimationSpeed = Mathf.Max(0f, speedMultiplier);
    }

    // =========================================================
    // WALKING
    // =========================================================

    private void UpdateWalkingAnimation()
    {
        if (walkSprites == null || walkSprites.Length == 0)
            return;

        if (walkCycleDuration <= 0f)
            return;

        if (AnimationSpeed <= 0f)
            return;

        animationTimer += Time.deltaTime * AnimationSpeed;

        if (animationTimer >= walkCycleDuration)
            animationTimer %= walkCycleDuration;

        float normalizedTime =
            animationTimer / walkCycleDuration;

        int frameIndex = Mathf.FloorToInt(
            normalizedTime * walkSprites.Length
        );

        frameIndex = Mathf.Clamp(
            frameIndex,
            0,
            walkSprites.Length - 1
        );

        if (frameIndex == currentFrame)
            return;

        currentFrame = frameIndex;

        bodyRenderer.sprite =
            walkSprites[currentFrame];
    }

    // =========================================================
    // DEATH
    // =========================================================

    private void SpawnBlood()
    {
        if (bloodSprites == null || bloodSprites.Length == 0)
            return;

        for (int i = 0; i < bloodSpriteCount; i++)
        {
            Sprite selectedSprite =
                bloodSprites[
                    Random.Range(0, bloodSprites.Length)
                ];

            Vector2 offset = GetRandomOffset(
                bloodRadiusRange.x,
                bloodRadiusRange.y
            );

            float rotation =
                Random.Range(0f, 360f);

            float scale =
                Random.Range(
                    bloodScaleRange.x,
                    bloodScaleRange.y
                );

            CreateDecoration(
                "Blood",
                selectedSprite,
                offset,
                rotation,
                scale,
                bloodSortingOffset
            );
        }
    }

    private void SpawnWeapon()
    {
        if (weaponSprite == null)
            return;

        Vector2 offset = GetRandomOffset(
            weaponRadiusRange.x,
            weaponRadiusRange.y
        );

        float rotation =
            Random.Range(0f, 360f);

        // El arma conserva siempre su tamaño original.
        CreateDecoration(
            "DroppedWeapon",
            weaponSprite,
            offset,
            rotation,
            1f,
            weaponSortingOffset
        );
    }

    private void CreateDecoration(
        string objectName,
        Sprite sprite,
        Vector2 localOffset,
        float rotation,
        float scale,
        int sortingOffset)
    {
        GameObject decoration =
            new GameObject(objectName);

        Transform decorationTransform =
            decoration.transform;

        decorationTransform.SetParent(transform);

        decorationTransform.localPosition =
            new Vector3(
                localOffset.x,
                localOffset.y,
                0f
            );

        decorationTransform.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                rotation
            );

        decorationTransform.localScale =
            Vector3.one * scale;

        SpriteRenderer renderer =
            decoration.AddComponent<SpriteRenderer>();

        renderer.sprite = sprite;

        renderer.sortingLayerID =
            bodyRenderer.sortingLayerID;

        renderer.sortingOrder =
            bodyRenderer.sortingOrder + sortingOffset;

        spawnedDeathObjects.Add(decoration);
    }

    // =========================================================
    // UTILITIES
    // =========================================================

    private Vector2 GetRandomOffset(
        float minRadius,
        float maxRadius)
    {
        float angle =
            Random.Range(0f, Mathf.PI * 2f);

        float radius =
            Random.Range(minRadius, maxRadius);

        return new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)
        ) * radius;
    }

    private void ClearDeathObjects()
    {
        for (int i = 0; i < spawnedDeathObjects.Count; i++)
        {
            GameObject spawnedObject =
                spawnedDeathObjects[i];

            if (spawnedObject != null)
                Destroy(spawnedObject);
        }

        spawnedDeathObjects.Clear();
    }
}