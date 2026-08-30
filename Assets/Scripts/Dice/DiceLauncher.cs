using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(DiceFaceManager))]
public class DiceLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private DiceFaceManager faceManager;

    [Header("Selection")]
    [SerializeField] private float selectedHeightOffset = 0.15f;
    [SerializeField] private float selectionTilt = 8f;
    [SerializeField] private float selectionMoveDuration = 0.1f;

    [Header("Launch")]
    [SerializeField] private float launchForce = 5f;
    [SerializeField] private float maxDragDistance = 3f;
    [SerializeField] private float minimumDragDistance = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool isSelected;
    [SerializeField] private bool isStill;
    [SerializeField] private bool newValue = true;
    [SerializeField] private bool onFloor = true;
    [SerializeField] private float currentDragDistance;

    [Header("Face Value")]
    [SerializeField] private int diceValue;

    private Rigidbody rb;
    private Collider diceCollider;

    private Vector3 dragStartWorld;
    private Vector3 dragCurrentWorld;

    private float normalHeight;
    private Coroutine selectionMovementCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        diceCollider = GetComponent<Collider>();

        if (faceManager == null)
            faceManager = GetComponent<DiceFaceManager>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        normalHeight = transform.position.y;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame && isStill)
            TrySelectDice();

        if (isSelected && Mouse.current.leftButton.isPressed)
            UpdateDrag();

        if (isSelected && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ReleaseDice();
            newValue = true;
            onFloor = false;
        }

        if (rb.angularVelocity == Vector3.zero && rb.linearVelocity == Vector3.zero && onFloor)
        {
            isStill = true;

            if (!newValue)
                return;

            diceValue = GetDiceValue();
            newValue = false;

            if (DiceDamageManager.Instance != null)
                DiceDamageManager.Instance.ResolveThrow(diceValue);
        }
        else
        {
            isStill = false;
        }
    }

    private int GetDiceValue()
    {
        if (faceManager == null)
            return 0;

        return faceManager.GetTopFaceValue();
    }

    private void TrySelectDice()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.collider != diceCollider)
            return;

        SelectDice();
    }

    private void SelectDice()
    {
        isSelected = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        normalHeight = transform.position.y;

        if (selectionMovementCoroutine != null)
            StopCoroutine(selectionMovementCoroutine);

        float tiltX = Random.Range(-selectionTilt, selectionTilt);
        float tiltZ = Random.Range(-selectionTilt, selectionTilt);

        Quaternion targetRotation = Quaternion.Euler(tiltX, 0f, tiltZ) * transform.rotation;

        selectionMovementCoroutine = StartCoroutine(MoveToSelectedState(targetRotation));

        if (TryGetMousePositionOnTray(out Vector3 mouseWorld))
        {
            dragStartWorld = mouseWorld;
            dragCurrentWorld = mouseWorld;
        }
    }


    private IEnumerator MoveToSelectedState(Quaternion targetRotation)
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * selectedHeightOffset;

        Quaternion startRotation = transform.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < selectionMoveDuration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / selectionMoveDuration);

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        selectionMovementCoroutine = null;
    }

    private void UpdateDrag()
    {
        if (!TryGetMousePositionOnTray(out Vector3 mouseWorld))
            return;

        dragCurrentWorld = mouseWorld;

        Vector3 dragVector = dragStartWorld - dragCurrentWorld;
        dragVector.y = 0f;

        currentDragDistance = Mathf.Min(dragVector.magnitude, maxDragDistance);
    }

    private void ReleaseDice()
    {
        isSelected = false;

        if (selectionMovementCoroutine != null)
        {
            StopCoroutine(selectionMovementCoroutine);
            selectionMovementCoroutine = null;
        }

        Vector3 dragVector = dragStartWorld - dragCurrentWorld;
        dragVector.y = 0f;

        float dragDistance = Mathf.Min(dragVector.magnitude, maxDragDistance);

        currentDragDistance = 0f;

        rb.isKinematic = false;

        if (dragDistance < minimumDragDistance)
            return;

        if (DiceDamageManager.Instance != null)
            DiceDamageManager.Instance.BeginThrow();

        Vector3 direction = dragVector.normalized;
        float power = dragDistance / maxDragDistance;
        Vector3 horizontalImpulse = new Vector3(direction.x, 0f, direction.z);

        horizontalImpulse *= launchForce * power;

        rb.AddForce(horizontalImpulse, ForceMode.Impulse);
    }

    private bool TryGetMousePositionOnTray(out Vector3 worldPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane trayPlane = new Plane(Vector3.up, new Vector3(0f, normalHeight, 0f));

        if (trayPlane.Raycast(ray, out float distance))
        {
            worldPosition = ray.GetPoint(distance);
            return true;
        }

        worldPosition = Vector3.zero;
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "TrayFloor")
            onFloor = true;
    }
}
