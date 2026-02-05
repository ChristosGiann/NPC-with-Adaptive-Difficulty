using UnityEngine;

public class SimpleMouseLook : MonoBehaviour
{
    public float sensitivity = 2f;
    public Transform body;

    float xRot;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        xRot -= my;
        xRot = Mathf.Clamp(xRot, -80f, 80f);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        if (body != null)
            body.Rotate(0f, mx, 0f);
    }
}
