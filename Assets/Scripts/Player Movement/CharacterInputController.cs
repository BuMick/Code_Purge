using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInputController : MonoBehaviour
{
    private InputSystemActions inputSystemActions;

    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform targetTransform;


    [SerializeField] private float jumpForce = 5f;
    private Vector3 forceDirection = Vector3.zero;

    Vector2 inputVelocity;
    Vector2 currentVelocity;

    float maxVelocityX;
    float maxVelocityY;

    [SerializeField] private float maxWalkVelocity = 0.5f;
    [SerializeField] private float maxRunVelocity = 1f;
    [SerializeField] private float rate = 10f;

    bool runPressed;

    private void Awake()
    {
        inputSystemActions = new InputSystemActions();
    }
    private void FixedUpdate()
    {
        LateralMovement();
        ForwardMovement();

        animator.SetFloat("VelocityXHashCode", currentVelocity.x);
        animator.SetFloat("VelocityZHashCode", currentVelocity.y);

        LookAt();
    }

    private void LateralMovement()
    {
        maxVelocityX = runPressed ? maxRunVelocity : maxWalkVelocity;
        
        maxVelocityX *= MathF.Sign(inputVelocity.x);

        currentVelocity.x = Mathf.MoveTowards(currentVelocity.x, maxVelocityX, Time.deltaTime * rate);
        rb.transform.position += GetCameraRightDirection(playerCamera) * currentVelocity.x * Time.fixedDeltaTime;
    }

    private void ForwardMovement()
    {
        maxVelocityY = runPressed ? maxRunVelocity : maxWalkVelocity;

        maxVelocityY *= MathF.Sign(inputVelocity.y);

        currentVelocity.y = Mathf.MoveTowards(currentVelocity.y, maxVelocityY, Time.deltaTime * rate);
        rb.transform.position += GetCameraForwardDirection(playerCamera) * currentVelocity.y * Time.fixedDeltaTime;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        inputVelocity = context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            runPressed = true;
        }
        else if (context.canceled)
        {
            runPressed = false;
        }   
    }

    private void LookAt()
    {
        if (targetTransform != null)
            transform.LookAt(targetTransform); 
    }

    private void OnEnable()
    {
        inputSystemActions.Enable();
        inputSystemActions.Player.Jump.performed += DoJump;
        inputSystemActions.Player.Move.performed += OnMove;
        inputSystemActions.Player.Move.canceled += OnMove;
        inputSystemActions.Player.Sprint.performed += OnRun;
        inputSystemActions.Player.Sprint.canceled += OnRun;
    }

    private void OnDisable()
    {
        inputSystemActions.Disable();
    }

    private void DoJump(InputAction.CallbackContext context)
    {
        if (IsOnGround())
        {
            forceDirection += Vector3.up * jumpForce;
        }
    }

    private bool IsOnGround()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.25f, Vector3.down);
        return Physics.Raycast(ray, out RaycastHit hit, 0.3f);
    }

    private Vector3 GetCameraRightDirection(Camera playerCamera)
    {
        Vector3 right = playerCamera.transform.right;
        right.y = 0;
        return right.normalized;
    }

    private Vector3 GetCameraForwardDirection(Camera playerCamera)
    {
        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }
}
