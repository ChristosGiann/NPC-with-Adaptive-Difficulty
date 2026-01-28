using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DummyPlayerMover : MonoBehaviour
{
    [Header("Motion")]
    public float forwardSpeed = 2.2f;
    public float strafeSpeed = 1.6f;
    public float turnSpeed = 120f;

    [Header("Behaviour")]
    public float changeEvery = 1.2f;      // κάθε πότε αλλάζει "στυλ"
    public float strafeChance = 0.6f;     // πόσο συχνά κάνει strafe
    public float strafeSwitchEvery = 0.7f;

    [Header("Arena")]
    public float arenaRadius = 14f;       // αν φύγει πολύ έξω, γυρνάει μέσα
    public Transform arenaCenter;

    private Rigidbody rb;

    private float tChange;
    private float tStrafe;

    private float forward;
    private float turn;
    private float strafeDir;
    private bool strafeMode;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        PickNewStyle();
        PickStrafe();
    }

    private void FixedUpdate()
    {
        if (arenaCenter != null)
        {
            Vector3 flat = transform.position - arenaCenter.position;
            flat.y = 0f;
            if (flat.magnitude > arenaRadius)
            {
                // γύρνα προς το κέντρο
                Vector3 dir = (-flat).normalized;
                float signed = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
                turn = Mathf.Clamp(signed / 45f, -1f, 1f);
                forward = 1f;
                strafeMode = false;
            }
        }

        tChange += Time.fixedDeltaTime;
        tStrafe += Time.fixedDeltaTime;

        if (tChange >= changeEvery)
        {
            PickNewStyle();
        }

        if (strafeMode && tStrafe >= strafeSwitchEvery)
        {
            PickStrafe();
        }

        // κίνηση
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;

        Vector3 vel = fwd * (forward * forwardSpeed);

        if (strafeMode)
            vel += right * (strafeDir * strafeSpeed);

        // ομαλοποίησε λίγο
        Vector3 origVel = rb.velocity;
        Vector3 desired = new Vector3(vel.x, origVel.y, vel.z);
        rb.velocity = Vector3.Lerp(origVel, desired, 0.35f);

        // στροφή
        transform.Rotate(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f);
    }

    void PickNewStyle()
    {
        tChange = 0f;

        // συνήθως προχωράει, καμιά φορά σταματάει λίγο
        forward = Random.value < 0.85f ? 1f : 0.2f;

        // γύρνα λίγο τυχαία
        turn = Random.Range(-1f, 1f);

        // μερικές φορές κάνει strafe
        strafeMode = Random.value < strafeChance;
    }

    void PickStrafe()
    {
        tStrafe = 0f;
        strafeDir = Random.value < 0.5f ? -1f : 1f;
    }
}
