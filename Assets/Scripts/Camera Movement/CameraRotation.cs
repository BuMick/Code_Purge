using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    public float targetAngle = 45f;
    public float currentAngle = 0f;
    public float mouseSensitivity = 2f;
    public float rotationSpeed = 5f;


    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X");

        if (Input.GetMouseButton(2))
        {
            targetAngle += mouseX * mouseSensitivity;
        }
        else
        {
            targetAngle = Mathf.Round(targetAngle / 45);
            targetAngle *= 45;
        }
        if (targetAngle < 0)
        {
            targetAngle += 360;
        }
        if (targetAngle > 360)
        {
            targetAngle -= 360;
        }

        currentAngle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, currentAngle, 0);
    }
}
