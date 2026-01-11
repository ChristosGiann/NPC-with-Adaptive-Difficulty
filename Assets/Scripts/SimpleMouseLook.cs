using UnityEngine;

public class SimpleMouseLook : MonoBehaviour
{
    public Transform playerBody;   // ο Player capsule
    public float sens = 200f;      // ευαισθησία ποντικιού
    float pitch;                   // γωνία πάνω-κάτω

    void Start()
    {
        // Κλειδώνουμε τον κέρσορα στο κέντρο
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Διάβασε κίνηση ποντικιού
        float mx = Input.GetAxis("Mouse X") * sens * Time.deltaTime;
        float my = Input.GetAxis("Mouse Y") * sens * Time.deltaTime;

        // Pitch (πάνω-κάτω) στην κάμερα
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -85f, 85f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // Yaw (δεξιά-αριστερά) στο σώμα του Player
        playerBody.Rotate(Vector3.up * mx);
    }
}
