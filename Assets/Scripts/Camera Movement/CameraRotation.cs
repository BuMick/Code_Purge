using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotation : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float mouseSensitivity = 2.0f;
    [Tooltip("Approximate time in seconds to reach the target angle.")]
    [SerializeField] private float rotationSmoothTime = 0.1f; // SmoothDamp uses time, not speed
    [Tooltip("Angle increment to snap to when not rotating freely.")]
    [SerializeField] private float snapAngleIncrement = 45.0f;

    [Header("Input Actions (Setup Required)")]
    [Tooltip("Input Action for mouse delta (Vector2). Ensure 'Mouse Delta' binding.")]
    [SerializeField] private InputActionReference lookActionReference;
    [Tooltip("Input Action for the rotation modifier key (Button). Ensure 'Middle Mouse Button' binding.")]
    [SerializeField] private InputActionReference rotateModifierActionReference;

    // --- State Variables ---
    private float targetAngleY;         // The desired angle
    private float currentAngleY;        // The smoothed, current angle
    private float currentAngularVelocity; // Used internally by SmoothDampAngle
    private bool isRotatingFreely = false; // Is the modifier button held?
    private bool needsToSnap = false;      // Flag to trigger snapping once on release
    private Transform cachedTransform;     // Cache the transform component

    public bool IsRotatingFreely => isRotatingFreely;

    private void Awake()
    {
        cachedTransform = transform; // Cache the transform

        // Initialize angles based on starting rotation to prevent initial jump
        currentAngleY = cachedTransform.eulerAngles.y;
        targetAngleY = currentAngleY;

        // Perform initial snap if not starting exactly on an increment
        SnapTargetAngle(); // Snap immediately on start
        currentAngleY = targetAngleY; // Set current angle to the snapped target
        cachedTransform.rotation = Quaternion.Euler(0, currentAngleY, 0); // Apply rotation immediately

        // --- Input System Setup Validation ---
        if (lookActionReference == null || rotateModifierActionReference == null)
        {
            Debug.LogError("CameraRotation: Input Action References not set in the inspector!", this);
            enabled = false; // Disable script if actions aren't set
            return;
        }
        if (lookActionReference.action == null || rotateModifierActionReference.action == null)
        {
            Debug.LogError("CameraRotation: Input Actions referenced are null or invalid!", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // Enable actions and subscribe to events
        if (rotateModifierActionReference != null && rotateModifierActionReference.action != null)
        {
            rotateModifierActionReference.action.Enable();
            rotateModifierActionReference.action.started += OnRotateModifierStarted;
            rotateModifierActionReference.action.canceled += OnRotateModifierCanceled;
        }
        if (lookActionReference != null && lookActionReference.action != null)
        {
            lookActionReference.action.Enable();
        }
    }

    private void OnDisable()
    {
        // Unsubscribe and disable actions
        if (rotateModifierActionReference != null && rotateModifierActionReference.action != null)
        {
            rotateModifierActionReference.action.started -= OnRotateModifierStarted;
            rotateModifierActionReference.action.canceled -= OnRotateModifierCanceled;
            // Consider disabling the action if nothing else uses it:
            rotateModifierActionReference.action.Disable();
        }
        if (lookActionReference != null && lookActionReference.action != null)
        {
            // Consider disabling the action if nothing else uses it:
            lookActionReference.action.Disable();
        }
    }

    // --- Input Callbacks ---

    private void OnRotateModifierStarted(InputAction.CallbackContext context)
    {
        isRotatingFreely = true;
        needsToSnap = false; // Don't snap while holding
    }

    private void OnRotateModifierCanceled(InputAction.CallbackContext context)
    {
        isRotatingFreely = false;
        needsToSnap = true; // Set flag to snap on the next Update
    }

    // --- Main Logic ---

    // Use LateUpdate for camera movements to ensure they happen after player updates
    private void LateUpdate()
    {
        // --- Handle Input and Target Angle ---
        if (isRotatingFreely)
        {
            // Read mouse delta X while modifier is held
            Vector2 lookInput = Vector2.zero;
            if (lookActionReference != null && lookActionReference.action != null)
            {
                lookInput = lookActionReference.action.ReadValue<Vector2>();
            }

            float mouseX = lookInput.x;
            targetAngleY += mouseX * mouseSensitivity * Time.deltaTime; // Apply sensitivity and delta time here for smooth input feel

            // Optional: Keep target angle within 0-360 while rotating freely (less critical with SmoothDampAngle)
            // targetAngleY = Mathf.Repeat(targetAngleY, 360f);
        }
        else if (needsToSnap)
        {
            // Snap the target angle ONCE when the button is released
            SnapTargetAngle();
            needsToSnap = false; // Reset the flag
        }

        // --- Apply Smoothing and Rotation ---
        // Smoothly interpolate the current angle towards the target angle
        currentAngleY = Mathf.SmoothDampAngle(currentAngleY, targetAngleY, ref currentAngularVelocity, rotationSmoothTime);

        // Apply the smoothed rotation to the transform
        cachedTransform.rotation = Quaternion.Euler(0, currentAngleY, 0);

        // --- Debugging (Optional) ---
        // Debug.Log($"Target: {targetAngleY:F1}, Current: {currentAngleY:F1}, FreeRotate: {isRotatingFreely}");
    }

    /// <summary>
    /// Calculates the nearest angle based on the snapAngleIncrement and sets it as the target.
    /// </summary>
    private void SnapTargetAngle()
    {
        if (snapAngleIncrement <= 0) return; // Avoid division by zero

        // Calculate the closest snap point
        targetAngleY = Mathf.Round(targetAngleY / snapAngleIncrement) * snapAngleIncrement;

        // Ensure the angle stays within standard ranges (optional but clean)
        targetAngleY = Mathf.Repeat(targetAngleY, 360f);
    }
}