using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPanner : MonoBehaviour
{
    public float panSpeed = 1f;
    public float panThreshold = 1f;
    private float followSpeed = 10f;

    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _player;

    private InputSystemActions inputActions;
    private Vector2 lookInput;
    private Vector3 lastOffset;
    private bool isPanning;

    private void Awake()
    {
        inputActions = new InputSystemActions();
    }
    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.canceled += OnLook;
    }


    private void OnDisable()
    {
        inputActions.Player.Look.performed -= OnLook;
        inputActions.Player.Look.canceled -= OnLook;
        inputActions.Disable();
    }
    private void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
        isPanning = lookInput.magnitude > 0.01f;
    }
    void FixedUpdate()
    {
        if (isPanning)
        {
            // Calculate the desired position based on player and pan input
            Vector3 desiredPosition = transform.position + Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0) * new Vector3(lookInput.x, 0, lookInput.y) * panSpeed * Time.fixedDeltaTime;

            // Calculate the offset from the player
            Vector3 offset = desiredPosition - _player.position;

            // Clamp the offset to the panThreshold
            offset = Vector3.ClampMagnitude(offset, panThreshold);

            // Apply the offset to the player's position to get the final position
            Vector3 finalPosition = _player.position + offset;

            // Enforce the same Y position as the player
            finalPosition.y = _player.position.y;

            transform.position = finalPosition;

            lastOffset = offset; //store last offset
        }
        else
        {
            Vector3 finalPosition = _player.position + lastOffset; //Use last Offset
            finalPosition.y = _player.position.y;  // Match player's Y position

            // Calculate the follow amount
            transform.position = Vector3.MoveTowards(transform.position, finalPosition, followSpeed * Time.fixedDeltaTime);
        }
    }
}