using UnityEngine;

public class SimpleMouseLook : MonoBehaviour
{
    public float sensitivity = 2f;
    public Transform body;

    float xRot;

    void Update()
    {
        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        xRot -= my;
        xRot = Mathf.Clamp(xRot, -80f, 80f);

        transform.localRotation = Quaternion.Euler(xRot, 0f, 0f);
        if (body != null)
            body.Rotate(0f, mx, 0f);
    }
}
