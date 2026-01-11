using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    [Header("Jump Buffer")]
    public float jumpBuffer = 0.2f; // seconds
    float jumpTimer;

    CharacterController cc;
    Vector3 velocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // --- Κίνηση WASD ---
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        
        // --- Jump Buffer logic ---
        if (Input.GetButtonDown("Jump"))
            jumpTimer = jumpBuffer;

        if (cc.isGrounded && velocity.y < 0)
            velocity.y = -2f; // κρατάει στο έδαφος

        if (cc.isGrounded && jumpTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpTimer = 0f;
        }

        jumpTimer -= Time.deltaTime;

        // --- Gravity ---
        velocity.y += gravity * Time.deltaTime;

        // --- Ενιαίο vector κίνησης ---
        Vector3 totalMove = move * speed + Vector3.up * velocity.y;
        cc.Move(totalMove * Time.deltaTime);
    }
}
