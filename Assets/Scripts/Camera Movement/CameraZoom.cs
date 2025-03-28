using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Assign the Camera's Transform here.")]
    [SerializeField] private Transform cameraTransform; // Use Transform for position manipulation

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSensitivity = 1.0f; // How much each scroll unit changes the target distance
    [SerializeField] private float zoomSmoothTime = 0.15f; // Smoothing duration
    [SerializeField] private float minDistance = 2f;   // Min distance from parent origin
    [SerializeField] private float maxDistance = 15f;  // Max distance from parent origin

    // --- State Variables ---
    private float targetDistance;       // The distance we want to reach
    private float currentDistance;      // The current smoothed distance
    private Vector3 currentDirection;   // Direction from parent origin to camera (normalized)
    private float zoomVelocity;         // Required by SmoothDamp

    private InputSystemActions inputActions;
    private bool needsInitialization = true; // Flag to ensure direction is set correctly initially

    private void Awake()
    {
        inputActions = new InputSystemActions();

        // --- Get Camera Transform ---
        if (cameraTransform == null)
        {
            // Try to get Camera component on this GameObject if Transform not assigned
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else
            {
                // Fallback: Use this GameObject's transform if it's not the Camera itself
                // This assumes the script is on the camera object.
                cameraTransform = transform;
                // Add a warning if no Camera component is found, as it might be unexpected
                if (GetComponent<Camera>() == null)
                {
                    Debug.LogWarning("CameraZoomFromParent: No Camera component found on this GameObject. Using transform directly.", this);
                }
            }
        }

        if (cameraTransform == null)
        {
            Debug.LogError("CameraZoomFromParent: Camera Transform could not be found!", this);
            enabled = false; // Disable script if setup fails
            return;
        }

        // --- Initial Distance & Direction Calculation ---
        // We do the main initialization in LateUpdate the first time to ensure
        // the parent's position/rotation is potentially set.
        // Set a reasonable default guess here.
        currentDistance = Mathf.Clamp(cameraTransform.localPosition.magnitude, minDistance, maxDistance);
        targetDistance = currentDistance;
        currentDirection = cameraTransform.localPosition.normalized;
        // Handle case where camera starts exactly at parent origin
        if (currentDirection == Vector3.zero)
        {
            currentDirection = Vector3.back; // Default direction if at origin
            Debug.LogWarning("CameraZoomFromParent: Camera started at parent origin. Using default direction (Vector3.back).", this);
        }
        needsInitialization = true; // Ensure LateUpdate does the proper first setup
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Zoom.performed += OnZoomInput;
    }

    private void OnDisable()
    {
        inputActions.Player.Zoom.performed -= OnZoomInput;
        inputActions.Player.Disable();
    }

    private void OnZoomInput(InputAction.CallbackContext context)
    {
        // Read scroll input (usually Vector2.y or a float if 1D axis)
        // float scrollInput = context.ReadValue<Vector2>().y;
        float scrollInput = context.ReadValue<float>(); // Use if your action is 1D Axis

        // Normalize scroll input for consistent steps (-1, 0, or 1)
        float normalizedScroll = Mathf.Sign(scrollInput);

        // Adjust target distance based on normalized input and sensitivity
        // Negative scroll (towards user) zooms out (increases distance)
        // Positive scroll (away from user) zooms in (decreases distance)
        targetDistance -= normalizedScroll * zoomSensitivity; // Adjust target

        // Clamp the target distance
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
    }

    // Use LateUpdate for camera movements
    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // --- First-Time Initialization ---
        if (needsInitialization)
        {
            Vector3 initialLocalPos = cameraTransform.localPosition;
            currentDistance = Mathf.Clamp(initialLocalPos.magnitude, minDistance, maxDistance);
            targetDistance = currentDistance;
            currentDirection = initialLocalPos.normalized;

            if (currentDirection == Vector3.zero)
            {
                currentDirection = Vector3.back; // Default if at origin
                                                 // Apply a minimum distance if starting at origin
                if (initialLocalPos == Vector3.zero)
                {
                    currentDistance = minDistance;
                    targetDistance = minDistance;
                }
            }

            // Apply clamped initial position immediately
            cameraTransform.localPosition = currentDirection * currentDistance;
            needsInitialization = false; // Don't run this block again
            zoomVelocity = 0f; // Reset smooth damp velocity
        }

        // --- Smoothly Update Distance ---
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref zoomVelocity, zoomSmoothTime);

        // --- Recalculate Direction (Handles potential parent rotation changes) ---
        // Get the current direction vector from the parent's origin to the camera's current position.
        // We recalculate this to ensure that even if the parent rotates, we zoom along the *new* line.
        currentDirection = cameraTransform.localPosition.normalized;
        if (currentDirection == Vector3.zero) // Prevent issues if somehow distance becomes zero
        {
            currentDirection = Vector3.back; // Re-apply default if needed
        }


        // --- Apply New Position ---
        // Set the local position based on the consistent direction and the smoothed distance
        cameraTransform.localPosition = currentDirection * currentDistance;

        // --- Optional Debugging ---
        // Debug.DrawRay(cameraTransform.parent.position, cameraTransform.parent.TransformDirection(currentDirection * currentDistance), Color.blue);
        // Debug.Log($"TargetDist: {targetDistance:F2}, CurrentDist: {currentDistance:F2}, LocalPos: {cameraTransform.localPosition:F2}");
    }
}