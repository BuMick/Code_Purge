using System;
using UnityEngine;
using UnityEngine.InputSystem;

//[RequireComponent(typeof(Rigidbody))] // Good practice to ensure Rigidbody exists
//[RequireComponent(typeof(Animator))]  // Good practice to ensure Animator exists
public class CharacterInputController : MonoBehaviour
{
    // --- Components ---
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform targetTransform; // For LookAt

    // --- Input & Movement ---
    [Header("Movement Settings")]
    [SerializeField] private float maxWalkVelocity = 3.0f; // Adjusted typical walk speed
    [SerializeField] private float maxRunVelocity = 6.0f;  // Adjusted typical run speed
    [SerializeField] private float acceleration = 10f;     // Renamed 'rate' for clarity
    [SerializeField] private float deceleration = 15f;   // Add separate deceleration

    private InputSystemActions inputSystemActions;
    private Vector2 inputDirection; // Renamed from inputVelocity for clarity
    private bool runPressed;

    // --- Jumping ---
    [Header("Jumping")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private float groundCheckOffsetY = 0.25f;
    [SerializeField] private LayerMask groundLayer; // Optional: Specify what is ground

    // --- Animation ---
    [Header("Animation")]
    [SerializeField] private float animationSmoothTime = 0.1f; // Smoothing for animator parameters

    private int velocityXHash; // Corrected hash variable names
    private int velocityZHash; // Corrected hash variable names

    // --- Internal State ---
    private Vector3 currentMovementInput; // Store the desired world movement direction * magnitude
    private Vector3 currentVelocityRef;   // Used for SmoothDamp

    private void Awake()
    {
        inputSystemActions = new InputSystemActions();

        // --- Get Components if not assigned in Inspector ---
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (playerCamera == null) playerCamera = Camera.main; // Assign main camera if none set

        // --- Correctly Hash Animator Parameters ---
        // Use the ACTUAL names of your parameters in the Animator Controller
        velocityXHash = Animator.StringToHash("VelocityXHashCode");
        velocityZHash = Animator.StringToHash("VelocityZHashCode");
    }

    private void OnEnable()
    {
        inputSystemActions.Player.Enable();
        inputSystemActions.Player.Jump.performed += DoJump;
        inputSystemActions.Player.Move.performed += OnMove;
        inputSystemActions.Player.Move.canceled += OnMove;
        inputSystemActions.Player.Sprint.performed += OnRun;
        inputSystemActions.Player.Sprint.canceled += OnRun;
    }

    private void OnDisable()
    {
        inputSystemActions.Player.Disable();
        inputSystemActions.Player.Jump.performed -= DoJump;
        inputSystemActions.Player.Move.performed -= OnMove;
        inputSystemActions.Player.Move.canceled -= OnMove;
        inputSystemActions.Player.Sprint.performed -= OnRun;
        inputSystemActions.Player.Sprint.canceled -= OnRun;
    }

    private void Update() // Use Update for input and animation logic
    {
        HandleLookAt(); // Handle rotation separate from physics
        HandleAnimation(); // Update animator parameters based on current velocity
    }

    private void FixedUpdate() // Use FixedUpdate for physics calculations
    {
        HandleMovement();
    }

    // --- Input Handlers ---
    public void OnMove(InputAction.CallbackContext context)
    {
        inputDirection = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        runPressed = context.ReadValueAsButton(); // Simpler way to check if pressed
    }

    private void DoJump(InputAction.CallbackContext context)
    {
        if (IsOnGround())
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    // --- Movement Logic (Physics Based) ---
    private void HandleMovement()
    {
        float targetSpeed = runPressed ? maxRunVelocity : maxWalkVelocity;

        // Get camera-relative directions
        Vector3 cameraForward = GetCameraForwardDirection(playerCamera);
        Vector3 cameraRight = GetCameraRightDirection(playerCamera);

        // Calculate desired move direction based on input and camera orientation
        Vector3 desiredMoveDirection = (cameraForward * inputDirection.y + cameraRight * inputDirection.x).normalized;

        // Calculate target velocity vector
        Vector3 targetVelocity = desiredMoveDirection * targetSpeed;

        // --- Acceleration/Deceleration ---
        // Choose rate based on whether we are accelerating or decelerating
        float currentAccel = (inputDirection.magnitude > 0.1f) ? acceleration : deceleration;

        // Smoothly change the current velocity towards the target velocity
        // Note: This replaces direct position manipulation with velocity control
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 newVelocity = Vector3.SmoothDamp(currentHorizontalVelocity, targetVelocity, ref currentVelocityRef, 1.0f / currentAccel); // Adjust smoothing based on accel/decel

        // Apply the calculated velocity (keeping vertical velocity)
        rb.linearVelocity = new Vector3(newVelocity.x, rb.linearVelocity.y, newVelocity.z);
    }

    // --- Animation Logic ---
    private void HandleAnimation()
    {
        // 1. Get Global Velocity from the Rigidbody
        Vector3 globalVelocity = rb.linearVelocity;

        // Optional: You might want to ignore vertical velocity for locomotion animations
        // Vector3 horizontalVelocity = new Vector3(globalVelocity.x, 0, globalVelocity.z);

        // 2. Transform Global Velocity to Local Velocity
        // InverseTransformDirection converts world direction to local direction
        Vector3 localVelocity = transform.InverseTransformDirection(globalVelocity);
        // If you ignored vertical velocity above, use this instead:
        // Vector3 localVelocity = transform.InverseTransformDirection(horizontalVelocity);


        // 3. Feed Local Velocity Components to Animator Parameters (with smoothing)
        // localVelocity.z corresponds to forward/backward movement relative to the character
        // localVelocity.x corresponds to sideways movement relative to the character
        animator.SetFloat(velocityZHash, localVelocity.z, animationSmoothTime, Time.deltaTime);
        animator.SetFloat(velocityXHash, localVelocity.x, animationSmoothTime, Time.deltaTime);

        // --- Debugging (Optional) ---
        // Debug.Log($"Global Vel: {globalVelocity:F2} | Local Vel: {localVelocity:F2}");
    }


    // --- Rotation ---
    private void HandleLookAt()
    {
        if (targetTransform != null) // Simple LookAt Target
        {
            Vector3 lookPos = targetTransform.position;
            lookPos.y = transform.position.y; // Keep the character upright
            transform.LookAt(lookPos);
        }
        else if (rb.linearVelocity.sqrMagnitude > 0.1f && inputDirection.magnitude > 0.1f) // Look in movement direction (if no target)
        {
            Vector3 moveDirection = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // Smooth rotation
            }
        }
    }


    // --- Utility Functions ---
    private bool IsOnGround()
    {
        // Cast slightly below the pivot point
        Vector3 rayStart = transform.position + Vector3.up * groundCheckOffsetY;
        // Use groundLayer if specified, otherwise check everything
        if (groundLayer == 0) // LayerMask 0 means nothing is selected
        {
            return Physics.Raycast(rayStart, Vector3.down, groundCheckDistance + 0.01f); // Add small buffer
        }
        else
        {
            return Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundCheckDistance + 0.01f, groundLayer);
        }
    }

    private Vector3 GetCameraRightDirection(Camera cam)
    {
        if (cam == null) return Vector3.right; // Fallback
        Vector3 right = cam.transform.right;
        right.y = 0; // Project onto the horizontal plane
        return right.normalized;
    }

    private Vector3 GetCameraForwardDirection(Camera cam)
    {
        if (cam == null) return Vector3.forward; // Fallback
        Vector3 forward = cam.transform.forward;
        forward.y = 0; // Project onto the horizontal plane
        return forward.normalized;
    }
}