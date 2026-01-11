using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAgent : Agent
{
    public Transform player;
    public Transform muzzle;
    public GameObject bulletPrefab;

    [Header("Move")]
    public float moveSpeed = 6f;
    public float turnSpeed = 220f;

    [Header("Shoot")]
    public float bulletSpeed = 18f;
    public float shootCooldown = 0.35f;

    Rigidbody rb;
    float cd;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        cd = 0f;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // defaults (7 total)
        Vector3 dirToPlayer = Vector3.zero; // 3
        float dist01 = 1f;                  // 1
        float velX = rb.velocity.x;         // 1
        float velZ = rb.velocity.z;         // 1
        float facing = 0f;                  // 1

        if (player != null)
        {
            Vector3 toP = player.position - transform.position;
            dirToPlayer = toP.normalized;
            dist01 = Mathf.Clamp01(toP.magnitude / 20f);
            facing = Vector3.Dot(transform.forward, dirToPlayer);
        }

        sensor.AddObservation(dirToPlayer); // 3
        sensor.AddObservation(dist01);      // 1
        sensor.AddObservation(velX);        // 1
        sensor.AddObservation(velZ);        // 1
        sensor.AddObservation(facing);      // 1
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        cd -= Time.fixedDeltaTime;

        var c = actions.ContinuousActions;
        float mx = Mathf.Clamp(c[0], -1f, 1f);
        float mz = Mathf.Clamp(c[1], -1f, 1f);
        float tr = Mathf.Clamp(c[2], -1f, 1f);

        Vector3 move = (transform.right * mx + transform.forward * mz) * moveSpeed;
        rb.AddForce(new Vector3(move.x, 0f, move.z), ForceMode.Acceleration);

        transform.Rotate(0f, tr * turnSpeed * Time.fixedDeltaTime, 0f);

        int shoot = actions.DiscreteActions[0];
        if (shoot == 1) TryShoot();

        // shaping: μικρό reward όταν κοιτάει τον player
        if (player != null)
        {
            Vector3 toP = (player.position - transform.position).normalized;
            AddReward(0.001f * Mathf.Clamp(Vector3.Dot(transform.forward, toP), -1f, 1f));
        }

        AddReward(-0.0005f); // time penalty
    }

    void TryShoot()
    {
        if (cd > 0f || player == null || muzzle == null) return;
        cd = shootCooldown;

        Vector3 dir = (player.position - muzzle.position).normalized;

        // AIM ERROR (σε μοίρες)
        float errDeg = 7f; // δοκίμασε 5–12
        dir = Quaternion.Euler(
            Random.Range(-errDeg, errDeg),
            Random.Range(-errDeg, errDeg),
            0f
        ) * dir;

        var go = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(dir, Vector3.up));

        if (go.TryGetComponent<Rigidbody>(out var brb))
            brb.velocity = dir * bulletSpeed;

        var b = go.GetComponent<Bullet>();
        if (b != null) b.owner = this;
    }


    public void OnHitPlayer()
    {
        AddReward(+1f);
    }

    public void OnHitByPlayer(float damage)
{
    // penalty όταν τρώει hit (tune later)
    AddReward(-0.2f);
}


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        var d = actionsOut.DiscreteActions;

        c[0] = 0f; c[1] = 0f; c[2] = 0f;
        d[0] = 0;

        if (player == null) return;

        Vector3 toP = player.position - transform.position;
        toP.y = 0f;

        float dist = toP.magnitude;
        Vector3 dir = toP.normalized;

        // στρίψε προς τον player
        float signed = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        c[2] = Mathf.Clamp(signed / 45f, -1f, 1f);

        // κίνηση: αν είναι μακριά, πήγαινε μπροστά. αν είναι κοντά, λίγο strafe.
        if (dist > 6f) c[1] = 1f;
        else
        {
            c[1] = 0.2f;
            c[0] = Mathf.Sign(Mathf.Sin(Time.time * 1.5f)); // strafe L/R
        }

        // shoot μόνο αν “βλέπει” αρκετά
        float facing = Vector3.Dot(transform.forward, dir);
        if (facing > 0.97f) d[0] = 1;
    }


}
