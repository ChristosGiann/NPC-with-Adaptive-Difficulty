using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleFPSController : MonoBehaviour
{
    public float speed = 5f;
    public float gravity = -15f;

    private CharacterController cc;
    private Vector3 vel;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = (transform.right * x + transform.forward * z) * speed;
        cc.Move(move * Time.deltaTime);

        if (cc.isGrounded && vel.y < 0f) vel.y = -2f;

        vel.y += gravity * Time.deltaTime;
        cc.Move(vel * Time.deltaTime);
    }
}
