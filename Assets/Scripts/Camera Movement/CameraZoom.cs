using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraZoom : MonoBehaviour
{
    public float zoomSpeed = 2f;
    public float zoomSmothness = 5f;

    public float minZoom = 5f;
    public float maxZoom = 20f;

    private float currentZoom = 10f;
    private float zoomInput;

    [SerializeField] private Camera _camera;

    private InputSystemActions inputActions;
    private float currentDistance;

    private void Awake()
    {
        inputActions = new InputSystemActions();
        currentDistance = -_camera.transform.localPosition.z;
        currentDistance = Mathf.Clamp(currentDistance, minZoom, maxZoom);
    }
    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.Zoom.performed += OnZoom;
        inputActions.Player.Zoom.canceled += OnZoom;
    }

    private void OnDisable()
    {
        inputActions.Player.Zoom.performed -= OnZoom;
        inputActions.Player.Zoom.canceled -= OnZoom;
        inputActions.Disable();
    }
    private void OnZoom(InputAction.CallbackContext context)
    {
        zoomInput = context.ReadValue<float>();
        //currentZoom = Mathf.Clamp(currentZoom - (zoomInput * zoomSpeed * Time.fixedDeltaTime), minZoom, maxZoom);
        //_camera.transform.position = new Vector3(_camera.transform.localPosition.x, _camera.transform.localPosition.y, Mathf.Lerp(currentDistance, currentZoom, zoomSmothness * Time.fixedDeltaTime));
    }
}
