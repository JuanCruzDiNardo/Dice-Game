using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float moveSpeed = 1f;
    
    private TextMeshPro textMesh;
    private Camera mainCamera;
    private float timer;

    private void Awake()
    {
        textMesh = GetComponentInChildren<TextMeshPro>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    public void Setup(float damage)
    {
        textMesh.text = damage.ToString();
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        transform.rotation = Quaternion.LookRotation(
            transform.position - mainCamera.transform.position
        );
    }
}