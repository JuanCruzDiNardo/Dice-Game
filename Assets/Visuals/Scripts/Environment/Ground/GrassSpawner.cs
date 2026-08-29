using UnityEngine;

public sealed class GrassSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer grassPrefab;
    [SerializeField] private Sprite[] grassSprites;

    [Header("Generation")]
    [SerializeField, Min(0)] private int amount = 100;
    [SerializeField] private int seed = 12345;

    [Header("Placement")]
    [SerializeField, Min(0f)] private float edgePadding = 0.5f;
    [SerializeField] private float heightOffset = 0.01f;

    [Header("Variation")]
    [SerializeField] private Vector2 scaleRange = new(0.7f, 1.2f);

    [Range(0f, 1f)]
    [SerializeField] private float flipChance = 0.5f;

    [Header("Runtime")]
    [SerializeField] private bool generateOnStart = true;

    private Transform grassContainer;

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    [ContextMenu("Generate Grass")]
    public void Generate()
    {
        Clear();

        if (grassPrefab == null ||
            grassSprites == null ||
            grassSprites.Length == 0)
        {
            Debug.LogWarning("GrassSpawner: faltan referencias.");
            return;
        }

        Renderer groundRenderer = GetComponent<Renderer>();

        if (groundRenderer == null)
        {
            Debug.LogWarning(
                "GrassSpawner necesita estar en el objeto del suelo."
            );

            return;
        }

        Bounds bounds = groundRenderer.bounds;

        if (edgePadding * 2f >= bounds.size.x ||
            edgePadding * 2f >= bounds.size.z)
        {
            Debug.LogWarning(
                "GrassSpawner: Edge Padding es demasiado grande."
            );

            return;
        }

        System.Random random = new(seed);

        GameObject container = new("Generated Grass");
        container.transform.SetParent(transform);

        grassContainer = container.transform;

        for (int i = 0; i < amount; i++)
        {
            SpawnGrass(bounds, random);
        }
    }

    private void SpawnGrass(
        Bounds bounds,
        System.Random random)
    {
        float x = RandomRange(
            random,
            bounds.min.x + edgePadding,
            bounds.max.x - edgePadding
        );

        float z = RandomRange(
            random,
            bounds.min.z + edgePadding,
            bounds.max.z - edgePadding
        );

        float scale = RandomRange(
            random,
            scaleRange.x,
            scaleRange.y
        );

        bool flipX = random.NextDouble() < flipChance;

        int spriteIndex =
            random.Next(0, grassSprites.Length);

        Vector3 position = new(
            x,
            bounds.max.y + heightOffset,
            z
        );

        SpriteRenderer grass = Instantiate(
            grassPrefab,
            position,
            Quaternion.Euler(90f, 0f, 0f),
            grassContainer
        );

        grass.sprite = grassSprites[spriteIndex];

        grass.transform.localScale = new Vector3(
            flipX ? -scale : scale,
            scale,
            scale
        );
    }

    [ContextMenu("Clear Grass")]
    public void Clear()
    {
        if (grassContainer == null)
            return;

        if (Application.isPlaying)
            Destroy(grassContainer.gameObject);
        else
            DestroyImmediate(grassContainer.gameObject);

        grassContainer = null;
    }

    private static float RandomRange(
        System.Random random,
        float min,
        float max)
    {
        return Mathf.Lerp(
            min,
            max,
            (float)random.NextDouble()
        );
    }
}