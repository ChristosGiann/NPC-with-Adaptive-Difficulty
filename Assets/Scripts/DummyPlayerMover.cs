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
    public LayerMask obstacles;           // βάλε μόνο Obstacles
    public float wallCheckDist = 1.2f;

    Rigidbody rb;
    float tChange, tStrafe;
    float targetTurn;
    float strafeDir; // -1 ή +1
    float forward;   // 0..1
    bool strafeMode;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // να μην πέφτει
    }

    void OnEnable()
    {
        PickNewIntent();
        PickStrafe();
    }

    void FixedUpdate()
    {
        // timers
        tChange += Time.fixedDeltaTime;
        tStrafe += Time.fixedDeltaTime;

        if (tChange >= changeEvery) PickNewIntent();
        if (tStrafe >= strafeSwitchEvery) PickStrafe();

        // wall avoidance: αν μπροστά έχει τοίχο, γύρνα
        Vector3 fwd = transform.forward;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, fwd, wallCheckDist, obstacles))
        {
            targetTurn = Random.Range(-160f, 160f);
            forward = 0.0f; // σταμάτα λίγο για να γυρίσει
        }

        // turn
        transform.Rotate(0f, targetTurn * turnSpeed * Time.fixedDeltaTime * 0.01f, 0f);

        // move (Rigidbody velocity)
        float vx = strafeMode ? strafeDir * strafeSpeed : 0f;
        float vz = forward * forwardSpeed;

        Vector3 v = (transform.right * vx) + (transform.forward * vz);
        v.y = rb.velocity.y;
        rb.velocity = v;
    }

    void PickNewIntent()
    {
        tChange = 0f;

        // μικρή τυχαιότητα στο turning (σαν άνθρωπος)
        targetTurn = Random.Range(-100f, 100f);

        // συνήθως προχωράει, καμιά φορά σταματάει λίγο
        forward = Random.value < 0.85f ? 1f : 0.2f;

        // μερικές φορές κάνει strafe
        strafeMode = Random.value < strafeChance;
    }

    void PickStrafe()
    {
        tStrafe = 0f;
        strafeDir = Random.value < 0.5f ? -1f : 1f;
    }
}
