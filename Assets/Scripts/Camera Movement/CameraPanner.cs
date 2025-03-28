using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CameraRotation))] // Ensure the rotation script is present
public class CameraPanner : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Camera _camera; // The actual camera object
    [SerializeField] private Transform _player; // The target to pan relative to
    [Tooltip("Reference to the CameraRotation script to check its state.")]
    [SerializeField] private CameraRotation cameraRotationScript; // Reference to check rotation state

    [Header("Panning Settings")]
    [Tooltip("Sensitivity of panning relative to mouse movement.")]
    [SerializeField] private float panSensitivity = 0.5f; // Changed name from panSpeed for clarity
    [Tooltip("Maximum distance the camera rig can be offset from the player.")]
    [SerializeField] private float maxPanOffsetDistance = 2.0f; // Renamed from panThreshold
    [Tooltip("Approximate time it takes to smoothly return to the default offset.")]
    [SerializeField] private float returnSmoothTime = 0.2f;

    [Header("Input Actions (Setup Required)")]
    [Tooltip("Input Action for Look/Pan input (Vector2).")]
    [SerializeField] private InputActionReference lookActionReference;

    // --- State Variables ---
    private Vector2 panInputDelta;       // Raw input delta for panning
    private bool isPanInputActive = false; // Is there active panning input?
    private Vector3 currentPanOffset;   // The current offset from the player (smoothed)
    private Vector3 targetPanOffset;    // The desired offset based on input
    private Vector3 panReturnVelocity;  // Used by SmoothDamp for returning
    private Transform cachedTransform;    // Cache this GameObject's transform (the camera rig)

    private void Awake()
    {
        cachedTransform = transform; // Cache the transform this script is on

        // --- Component Validation ---
        if (_camera == null)
        {
            Debug.LogError("CameraPanner: Camera reference (_camera) is not set!", this);
            enabled = false; return;
        }
        if (_player == null)
        {
            Debug.LogError("CameraPanner: Player reference (_player) is not set!", this);
            enabled = false; return;
        }
        // Get CameraRotation script if not assigned
        if (cameraRotationScript == null)
        {
            cameraRotationScript = GetComponent<CameraRotation>();
            if (cameraRotationScript == null)
            {
                Debug.LogError("CameraPanner: CameraRotation script reference not set and not found on this GameObject!", this);
                enabled = false; return;
            }
        }
        if (lookActionReference == null || lookActionReference.action == null)
        {
            Debug.LogError("CameraPanner: Look Action Reference not set or invalid!", this);
            enabled = false; return;
        }

        // --- Initialize Offset ---
        // Calculate the initial offset based on starting positions
        targetPanOffset = cachedTransform.position - _player.position;
        targetPanOffset.y = 0; // Ignore initial vertical difference for panning offset
        // Clamp the initial offset immediately
        targetPanOffset = Vector3.ClampMagnitude(targetPanOffset, maxPanOffsetDistance);
        currentPanOffset = targetPanOffset; // Start smoothed value at the target

        // Apply the initial position correctly based on clamped offset
        Vector3 initialPosition = _player.position + targetPanOffset;
        initialPosition.y = cachedTransform.position.y; // Keep the original Y position of the rig
        cachedTransform.position = initialPosition;
    }

    private void OnEnable()
    {
        if (lookActionReference != null && lookActionReference.action != null)
        {
            lookActionReference.action.Enable();
            lookActionReference.action.performed += OnLookInput;
            lookActionReference.action.canceled += OnLookInput;
        }
    }

    private void OnDisable()
    {
        if (lookActionReference != null && lookActionReference.action != null)
        {
            lookActionReference.action.performed -= OnLookInput;
            lookActionReference.action.canceled -= OnLookInput;
            // Consider disabling action if nothing else uses it
            lookActionReference.action.Disable();
        }
    }

    // --- Input Handling ---
    private void OnLookInput(InputAction.CallbackContext context)
    {
        panInputDelta = context.ReadValue<Vector2>();
        // Check if input magnitude is significant enough to be considered active
        isPanInputActive = panInputDelta.sqrMagnitude > 0.01f * 0.01f; // Use sqrMagnitude for efficiency
    }

    // --- Camera Movement Logic ---
    // Use LateUpdate for camera movements to follow player correctly
    private void LateUpdate()
    {
        // --- CRITICAL CHECK: Stop panning if camera is rotating freely ---
        if (cameraRotationScript != null && cameraRotationScript.IsRotatingFreely)
        {
            // If rotation is active, force the camera to smoothly return to the default (zero offset)
            // or maintain the last valid offset before rotation started. Let's return to center.
            targetPanOffset = Vector3.zero; // Or could use the offset captured before rotation started
            isPanInputActive = false; // Ensure panning input is ignored
            panInputDelta = Vector2.zero; // Reset delta
        }

        // --- Calculate Target Offset ---
        if (isPanInputActive)
        {
            // Calculate panning direction based on camera's Y rotation
            Quaternion cameraYRotation = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0);
            // Convert 2D input into a 3D world-space direction relative to camera facing
            Vector3 inputDirection = cameraYRotation * new Vector3(panInputDelta.x, 0, panInputDelta.y);

            // Adjust the target offset based on input, sensitivity, and time
            // We directly modify the target offset vector
            targetPanOffset += inputDirection * panSensitivity * Time.deltaTime;

            // Clamp the magnitude of the target offset vector
            targetPanOffset = Vector3.ClampMagnitude(targetPanOffset, maxPanOffsetDistance);

            // Reset the return velocity while actively panning so SmoothDamp restarts correctly when input stops
            panReturnVelocity = Vector3.zero;
        }

        // --- Apply Smoothing and Position ---
        // Smoothly interpolate the current offset towards the target offset
        currentPanOffset = Vector3.SmoothDamp(currentPanOffset, targetPanOffset, ref panReturnVelocity, cameraRotationScript.IsRotatingFreely? returnSmoothTime : 0);

        // Calculate the final world position for the camera rig
        Vector3 finalPosition = _player.position + currentPanOffset;
        finalPosition.y = cachedTransform.position.y; // Maintain the rig's original height (or player's Y if preferred: _player.position.y)

        // Apply the calculated position
        cachedTransform.position = finalPosition;

        // --- Debugging (Optional) ---
        // Debug.DrawLine(_player.position, cachedTransform.position, Color.cyan);
        // Debug.Log($"PanInputActive: {isPanInputActive}, TargetOffset: {targetPanOffset:F2}, CurrentOffset: {currentPanOffset:F2}");
    }
}