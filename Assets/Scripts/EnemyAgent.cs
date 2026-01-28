// EnemyAgent.cs (FULL SCRIPT) - Go-to-player + Strafe/Dodge shaping + Difficulty-aware hit penalty
// Notes for your setup:
// - Set Behavior Parameters -> Vector Observation Space Size = 8 (we add difficulty as 1 extra obs)
// - Actions: Continuous=3 (strafe, forward, turn), Discrete Branches=1 (shoot 0/1)
// - Set maxHitsBeforeEnd = 6 in Inspector (as you said)
// - For quick Play test WITHOUT trainer: set Behavior Type = Heuristic Only (optional), or Inference Only with a model
// - For TRAINING: Behavior Type = Default and run mlagents-learn first.

using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class EnemyAgent : Agent
{
    [Header("References")]
    public Transform player;
    public Transform muzzle;
    public GameObject bulletPrefab;

    [Header("Move")]
    public float moveSpeed = 12f;
    public float turnSpeed = 320f;

    [Header("Shoot")]
    public float bulletSpeed = 18f;
    public float shootCooldown = 0.35f;
    public float aimErrorDeg = 7f; // bigger = worse aim (we’ll later drive this by difficulty)

    [Header("Episode Reset (Training)")]
    public Transform playerSpawn;
    public Transform enemySpawn;
    public Rigidbody playerRb;
    public Health playerHealth;
    public Health enemyHealth;

    public bool resetToSpawnsOnEpisodeBegin = true;
    public bool faceEachOtherOnReset = true;
    public float randomSpawnRadius = 0f; // 0 = fixed spawns

    [Header("Difficulty (0..1)")]
    public string difficultyKey = "difficulty";
    [Range(0f, 1f)] public float difficulty01 = 0f; // debug fallback if you don't set env params yet

    [Header("Reward: Time / Idle")]
    public float timePenalty = -0.0005f;
    public float idlePenalty = 0.0015f;
    public float idleSpeedThreshold = 0.25f;

    [Header("Reward: Go-to + Strafe")]
    public float towardRewardScale = 0.0012f;           // reward to move toward player
    public float targetTowardSpeedForFullReward = 5f;

    public float strafeRewardScale = 0.0022f;           // reward to move perpendicular (strafe/dodge)
    public float targetStrafeForFullReward = 4f;

    public float straightLinePenaltyScale = 0.0015f;    // penalty if moving "straight at" player
    public float facingRewardScale = 0.0008f;           // small secondary reward for facing player

    [Header("Reward: Distance shaping (optional, mild)")]
    public float targetDistance = 7.5f;
    public float distanceTolerance = 4f;
    public float distanceRewardScale = 0.0004f;
    public float tooCloseDistance = 3.2f;
    public float tooClosePenalty = 0.0012f;
    public float tooFarDistance = 15f;
    public float tooFarPenalty = 0.0007f;

    [Header("Recently Hit Boost")]
    public float recentlyHitWindow = 0.8f;  // seconds after getting hit
    public float strafeBoostWhenHit = 2.5f; // stronger push to dodge
    private float recentlyHitTimer = 0f;

    [Header("Hits / Episode Control")]
    public int maxHitsBeforeEnd = 6;         // you said you'll set this to 6
    public float gotHitPenalty = -0.35f;     // base penalty per hit (scaled by difficulty)
    public float hitPlayerReward = +1.0f;

    private Rigidbody rb;
    private float cd;
    private int hitsTaken;
    private bool episodeEnded;

    private DummyPlayerHitscanShooter cachedDummyShooter;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        episodeEnded = false;
        hitsTaken = 0;
        cd = 0f;
        recentlyHitTimer = 0f;

        // fetch difficulty for this episode (if set externally)
        difficulty01 = Mathf.Clamp01(Academy.Instance.EnvironmentParameters.GetWithDefault(difficultyKey, difficulty01));

        // reset enemy physics
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // reset player physics
        if (playerRb != null)
        {
            playerRb.velocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        if (resetToSpawnsOnEpisodeBegin)
        {
            if (enemySpawn != null)
            {
                Vector3 p = enemySpawn.position + RandomXZ(randomSpawnRadius);
                transform.SetPositionAndRotation(p, enemySpawn.rotation);
            }

            if (player != null && playerSpawn != null)
            {
                Vector3 p = playerSpawn.position + RandomXZ(randomSpawnRadius);
                player.SetPositionAndRotation(p, playerSpawn.rotation);
            }

            if (faceEachOtherOnReset && player != null)
            {
                FaceFlat(transform, player.position);
                FaceFlat(player, transform.position);
            }
        }

        // reset HP if you use it (still safe even with maxHitsBeforeEnd)
        enemyHealth?.ResetHP();
        playerHealth?.ResetHP();

        // warmup the dummy each episode (so enemy has time to move)
        if (player != null)
        {
            if (cachedDummyShooter == null)
                cachedDummyShooter = player.GetComponent<DummyPlayerHitscanShooter>();

            cachedDummyShooter?.ResetWarmup();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Observations:
        // 0-2 dirToPlayer (3)
        // 3   dist01 (1)
        // 4   velX (1)
        // 5   velZ (1)
        // 6   facing (1)
        // 7   difficulty01 (1)  -> Total = 8

        Vector3 dirToPlayer = Vector3.zero;
        float dist01 = 1f;
        float velX = rb.velocity.x;
        float velZ = rb.velocity.z;
        float facing = 0f;

        if (player != null)
        {
            Vector3 toP = player.position - transform.position;
            toP.y = 0f;
            float dist = toP.magnitude;

            if (dist > 0.001f)
            {
                dirToPlayer = toP / dist;
                dist01 = Mathf.Clamp01(dist / 20f);
                facing = Vector3.Dot(transform.forward, dirToPlayer);
            }
        }

        sensor.AddObservation(dirToPlayer);
        sensor.AddObservation(dist01);
        sensor.AddObservation(velX);
        sensor.AddObservation(velZ);
        sensor.AddObservation(facing);

        // difficulty as an extra observation
        float d = Academy.Instance.EnvironmentParameters.GetWithDefault(difficultyKey, difficulty01);
        difficulty01 = Mathf.Clamp01(d);
        sensor.AddObservation(difficulty01);
    }

    private void OnCollisionStay(Collision col)
    {
        if (col.collider.CompareTag("Wall"))
            AddReward(-0.0015f);
    }


    public override void OnActionReceived(ActionBuffers actions)
    {
        if (cd > 0f) cd -= Time.fixedDeltaTime;

        // Update difficulty and hit timer each physics step
        difficulty01 = Mathf.Clamp01(Academy.Instance.EnvironmentParameters.GetWithDefault(difficultyKey, difficulty01));

        if (recentlyHitTimer > 0f)
            recentlyHitTimer = Mathf.Max(0f, recentlyHitTimer - Time.fixedDeltaTime);

        // Actions: Continuous[0]=strafe, Continuous[1]=forward, Continuous[2]=turn
        var c = actions.ContinuousActions;
        float mx = Mathf.Clamp(c[0], -1f, 1f);
        float mz = Mathf.Clamp(c[1], -1f, 1f);
        float tr = Mathf.Clamp(c[2], -1f, 1f);

        Vector3 move = (transform.right * mx + transform.forward * mz) * moveSpeed;
        rb.AddForce(new Vector3(move.x, 0f, move.z), ForceMode.Acceleration);

        transform.Rotate(0f, tr * turnSpeed * Time.fixedDeltaTime, 0f);

        int shoot = actions.DiscreteActions[0];
        if (shoot == 1) TryShoot();

        // ---------------------------
        // Reward shaping
        // ---------------------------
        AddReward(timePenalty);

        Vector3 vel = rb.velocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);
        float speed = velXZ.magnitude;

        // Anti-idle
        if (speed < idleSpeedThreshold)
        {
            // όσο πιο ακίνητος, τόσο πιο penalty (σπρώχνει να ξεκολλήσει)
            float idle01 = 1f - Mathf.Clamp01(speed / Mathf.Max(0.01f, idleSpeedThreshold));
            AddReward(-idlePenalty * idle01);
        }


        if (player == null) return;

        Vector3 toP = player.position - transform.position;
        toP.y = 0f;
        float dist = toP.magnitude;
        Vector3 dirToP = (dist > 0.001f) ? (toP / dist) : transform.forward;

        // 1) Reward: move TOWARD the player (aggression slightly higher at high difficulty)
        float towardSpeed = Mathf.Max(0f, Vector3.Dot(velXZ, dirToP));
        float toward01 = Mathf.Clamp01(towardSpeed / Mathf.Max(0.01f, targetTowardSpeedForFullReward));
        float towardMult = Mathf.Lerp(0.8f, 1.2f, difficulty01);
        AddReward(towardRewardScale * toward01 * towardMult);

        // 2) Reward: STRAFE / perpendicular velocity (more at high difficulty, and boosted after hit)
        Vector3 velAlong = Vector3.Dot(velXZ, dirToP) * dirToP;
        Vector3 velPerp = velXZ - velAlong;
        float strafeSpeed = velPerp.magnitude;
        float strafe01 = Mathf.Clamp01(strafeSpeed / Mathf.Max(0.01f, targetStrafeForFullReward));

        float strafeMult = Mathf.Lerp(1.0f, 1.8f, difficulty01);
        if (recentlyHitTimer > 0f) strafeMult *= strafeBoostWhenHit;
        AddReward(strafeRewardScale * strafe01 * strafeMult);

        // 3) Penalty: going "straight at" player (pushes agent to add strafe)
        if (speed > 0.3f && dist < 12f)
        {
            float alignment = Mathf.Abs(Vector3.Dot(velXZ.normalized, dirToP)); // 1 = straight at
            float straightPenalty = straightLinePenaltyScale * alignment * Mathf.Lerp(0.8f, 1.3f, difficulty01);
            AddReward(-straightPenalty);
        }

        // 4) Mild distance shaping (optional)
        float dErr = Mathf.Abs(dist - targetDistance);
        float dist01 = 1f - Mathf.Clamp01(dErr / Mathf.Max(0.01f, distanceTolerance));
        AddReward(distanceRewardScale * dist01);

        if (dist < tooCloseDistance) AddReward(-tooClosePenalty);
        if (dist > tooFarDistance)   AddReward(-tooFarPenalty);

        // 5) Small facing reward (secondary)
        float facing = Vector3.Dot(transform.forward, dirToP);
        AddReward(facingRewardScale * Mathf.Clamp(facing, -1f, 1f));
    }

    private void TryShoot()
    {
        if (cd > 0f || player == null || muzzle == null || bulletPrefab == null) return;
        cd = shootCooldown;

        Vector3 dir = (player.position - muzzle.position);
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.001f ? dir.normalized : muzzle.forward;

        // (Optional) make aim error slightly better on high difficulty
        float err = Mathf.Lerp(aimErrorDeg, aimErrorDeg * 0.55f, difficulty01);

        dir = Quaternion.Euler(
            Random.Range(-err, err),
            Random.Range(-err, err),
            0f
        ) * dir;

        var go = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(dir, Vector3.up));
        if (go.TryGetComponent<Rigidbody>(out var brb))
            brb.velocity = dir * bulletSpeed;

        var b = go.GetComponent<Bullet>();
        if (b != null) b.owner = this;
    }

    // Called by Bullet when Enemy hits Player
    public void OnHitPlayer()
    {
        AddReward(hitPlayerReward);
        EndEpisodeSafe();
    }

    // Called by Dummy hitscan when Enemy is hit by Player
    public void OnHitByPlayer(float damage)
    {
        recentlyHitTimer = recentlyHitWindow;

        // hits matter more at high difficulty (so it learns to avoid)
        float hitPenalty = Mathf.Lerp(-0.12f, gotHitPenalty, difficulty01);
        AddReward(hitPenalty);

        hitsTaken++;
        if (hitsTaken >= maxHitsBeforeEnd)
            EndEpisodeSafe();
    }

    private void EndEpisodeSafe()
    {
        if (episodeEnded) return;
        episodeEnded = true;
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Simple heuristic: go toward player but add alternating strafe (for quick Play test)
        var c = actionsOut.ContinuousActions;
        var d = actionsOut.DiscreteActions;

        c[0] = 0f; // strafe
        c[1] = 0f; // forward
        c[2] = 0f; // turn
        d[0] = 0;  // shoot

        if (player == null) return;

        Vector3 toP = player.position - transform.position;
        toP.y = 0f;
        float dist = toP.magnitude;
        Vector3 dir = dist > 0.001f ? (toP / dist) : transform.forward;

        float signed = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        c[2] = Mathf.Clamp(signed / 45f, -1f, 1f);

        // forward toward player
        c[1] = dist > 4f ? 1f : 0.2f;

        // strafe oscillation (more when closer)
        float strafeAmt = dist < 10f ? 0.8f : 0.35f;
        c[0] = Mathf.Sign(Mathf.Sin(Time.time * 1.5f)) * strafeAmt;

        // shoot if mostly facing
        float facing = Vector3.Dot(transform.forward, dir);
        if (facing > 0.97f) d[0] = 1;
    }

    private static Vector3 RandomXZ(float r)
    {
        if (r <= 0f) return Vector3.zero;
        Vector2 v = Random.insideUnitCircle * r;
        return new Vector3(v.x, 0f, v.y);
    }

    private static void FaceFlat(Transform t, Vector3 worldTarget)
    {
        Vector3 look = worldTarget - t.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.0001f) return;
        t.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }
}
