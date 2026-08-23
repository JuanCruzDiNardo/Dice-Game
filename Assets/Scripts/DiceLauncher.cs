using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class DiceLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    [Header("Selection")]
    [SerializeField] private float selectedHeightOffset = 0.15f;
    [SerializeField] private float selectionTilt = 8f;

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

    [SerializeField] private int diceValue;
    [SerializeField] private List<GameObject> diceFaces;

    private Rigidbody rb;
    private Collider diceCollider;

    private Vector3 dragStartWorld;
    private Vector3 dragCurrentWorld;

    private float normalHeight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        diceCollider = GetComponent<Collider>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        normalHeight = transform.position.y;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame && isStill)
        {
            TrySelectDice();
        }

        if (isSelected &&
            Mouse.current.leftButton.isPressed)
        {
            UpdateDrag();
        }

        if (isSelected &&
            Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ReleaseDice();
            newValue = true;
            onFloor = false;
        }

        if (rb.angularVelocity == Vector3.zero && rb.linearVelocity == Vector3.zero && onFloor)
        {
            isStill = true;            
            if (!newValue) return;            
            diceValue = GetDiceValue();  
            newValue = false;
            //Debug.Log(diceValue);
        }
        else
        {
            isStill = false;
        }
    }

    private int GetDiceValue()
    {
        return int.Parse(diceFaces.OrderByDescending(x => x.gameObject.transform.position.y).FirstOrDefault().name);
    }

    private void TrySelectDice()
    {
        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

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

        // Stop physics while aiming.
        rb.isKinematic = true;

        normalHeight = transform.position.y;

        // Lift the die slightly.
        transform.position +=
            Vector3.up * selectedHeightOffset;

        // Give the die a small random tilt.
        float tiltX = Random.Range(-selectionTilt, selectionTilt);
        float tiltZ = Random.Range(-selectionTilt, selectionTilt);

        transform.rotation =
            Quaternion.Euler(tiltX, 0f, tiltZ) *
            transform.rotation;

        if (TryGetMousePositionOnTray(out Vector3 mouseWorld))
        {
            dragStartWorld = mouseWorld;
            dragCurrentWorld = mouseWorld;
        }
    }

    private void UpdateDrag()
    {
        if (!TryGetMousePositionOnTray(out Vector3 mouseWorld))
            return;

        dragCurrentWorld = mouseWorld;

        Vector3 dragVector =
            dragStartWorld - dragCurrentWorld;

        dragVector.y = 0f;

        currentDragDistance = Mathf.Min(
            dragVector.magnitude,
            maxDragDistance
        );
    }

    private void ReleaseDice()
    {
        isSelected = false;        

        Vector3 dragVector =
            dragStartWorld - dragCurrentWorld;

        dragVector.y = 0f;

        float dragDistance = Mathf.Min(
            dragVector.magnitude,
            maxDragDistance
        );

        currentDragDistance = 0f;

        // Give physics control back to the die.
        rb.isKinematic = false;

        if (dragDistance < minimumDragDistance)
        {
            return;
        }

        Vector3 direction =
            dragVector.normalized;

        float power =
            dragDistance / maxDragDistance;

        Vector3 horizontalImpulse =
            new Vector3(
                direction.x,
                0f,
                direction.z
            );

        horizontalImpulse *=
            launchForce * power;

        rb.AddForce(
            horizontalImpulse,
            ForceMode.Impulse
        );
    }

    private bool TryGetMousePositionOnTray(
        out Vector3 worldPosition)
    {
        Ray ray = mainCamera.ScreenPointToRay(
            Mouse.current.position.ReadValue()
        );

        Plane trayPlane = new Plane(
            Vector3.up,
            new Vector3(
                0f,
                normalHeight,
                0f
            )
        );

        if (trayPlane.Raycast(
            ray,
            out float distance))
        {
            worldPosition =
                ray.GetPoint(distance);

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