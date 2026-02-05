using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class EnemyAgent : Agent
{
    [Header("Mode")]
    public bool trainingMode = true;

    [Header("Refs (both modes)")]
    public Transform player;                 // gameplay: set by spawner or find by tag
    public Transform muzzle;
    public GameObject bulletPrefab;
    public Health enemyHealth;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float turnSpeed = 90f;

    [Header("Shooting")]
    public float bulletSpeed = 22f;
    public float shootCooldown = 0.45f;
    public float aimErrorDeg = 7f;

    [Header("Conditioning")]
    public string difficultyKey = "difficulty";
    [Range(0f, 1f)] public float difficulty01 = 0.5f;

    [Header("Training reset (optional, only if trainingMode)")]
    public ArenaSpawner arenaSpawner; // call ResetEpisode here

    [Header("Rewards (training only)")]
    public float timePenalty = -0.0002f;
    public float idlePenalty = 0.0035f;
    public float idleSpeedThreshold = 0.35f;

    public float towardRewardScale = 0.002f;
    public float targetTowardSpeed = 5f;

    public float strafeRewardScale = 0.003f;
    public float targetStrafeForFull = 4f;
    public float straightLinePenaltyScale = 0.0007f;

    public float facingRewardScale = 0.0008f;

    [Header("Distance shaping (training, mild)")]
    public float targetDistance = 7.5f;
    public float distanceTolerance = 3f;
    public float distanceRewardScale = 0.0007f;
    public float tooCloseDistance = 4.5f;
    public float tooClosePenalty = 0.0042f;
    public float tooFarDistance = 14f;
    public float tooFarPenalty = 0.0005f;

    [Header("Hits / Episode control (training)")]
    public int maxHitsBeforeEnd = 6;
    public float gotHitPenalty = -0.18f;
    public float hitPlayerReward = 1.5f;

    [Header("Gameplay helpers")]
    public bool autoFindPlayerByTag = true;
    public float keepDistanceGameplay = 2.2f; // να μη “κολλάει” πάνω στον παίκτη
    public float wallAvoidRay = 1.2f;

    Rigidbody rb;
    float shootCd;
    int gotHitCount;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        if (enemyHealth == null) enemyHealth = GetComponent<Health>();

        // fallback difficulty if nothing set yet
        if (PlayerPrefs.HasKey(difficultyKey))
            difficulty01 = Mathf.Clamp01(PlayerPrefs.GetFloat(difficultyKey, difficulty01));
    }

    public void SetDifficulty(float d01)
    {
        difficulty01 = Mathf.Clamp01(d01);
        PlayerPrefs.SetFloat(difficultyKey, difficulty01);

        // OPTIONAL: εδώ μπορείς να κάνεις “gameplay stats scaling”
        // ΠΡΟΣΟΧΗ: το trained model ήδη μαθαίνει conditioning.
        // Αν αρχίσεις να αλλάζεις moveSpeed/aimError εδώ, αλλάζεις distribution.
        // Για demo UI, καλύτερα να τα δείχνεις, όχι να τα πειράζεις.
    }

    public override void OnEpisodeBegin()
    {
        shootCd = 0f;
        gotHitCount = 0;

        if (trainingMode)
        {
            if (arenaSpawner != null)
            {
                float d01 = arenaSpawner.GetNextDifficulty();
                SetDifficulty(d01);
                PlayerPrefs.SetFloat(difficultyKey, d01);
            }
            else if (PlayerPrefs.HasKey(difficultyKey))
            {
                SetDifficulty(Mathf.Clamp01(PlayerPrefs.GetFloat(difficultyKey)));
            }
            else
            {
                SetDifficulty(0.5f); // default fallback
            }

            // (προαιρετικό) reset enemy HP στο training
            if (enemyHealth != null) enemyHealth.ResetHP();
        }
        else
        {
            // gameplay: μην κάνεις resets / rewards
            if (autoFindPlayerByTag && player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.transform;
            }
        }
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        // Observation: difficulty conditioning
        sensor.AddObservation(difficulty01);

        if (player == null)
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            return;
        }

        Vector3 toP = player.position - transform.position;
        float dist = toP.magnitude;
        Vector3 toPn = (dist > 0.0001f) ? (toP / dist) : Vector3.zero;

        sensor.AddObservation(toPn);                                  // 3
        sensor.AddObservation(transform.forward);                     // 3
        sensor.AddObservation(Mathf.Clamp(dist / 40f, 0f, 1f));        // 1
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float forward = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float strafe  = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float turn    = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        int shootAct  = actions.DiscreteActions[0]; // 0/1

        // rotate
        transform.Rotate(0f, turn * turnSpeed * Time.fixedDeltaTime, 0f);

        // move (keep distance in gameplay so δεν κολλάει)
        Vector3 move = (transform.forward * forward + transform.right * strafe);
        if (!trainingMode && player != null)
        {
            float d = Vector3.Distance(transform.position, player.position);
            if (d < keepDistanceGameplay)
            {
                // αν είναι πολύ κοντά, κόψε forward προς τα μέσα
                forward = Mathf.Min(forward, 0f);
                move = (transform.forward * forward + transform.right * strafe);
            }

            // wall avoid micro (ray forward)
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, wallAvoidRay, ~0, QueryTriggerInteraction.Ignore))
            {
                // αν πάει να καρφωθεί, σπρώξε λίγο strafe
                move += transform.right * 0.35f;
            }
        }

        if (rb != null)
        {
            Vector3 vel = move * moveSpeed;
            vel.y = rb.velocity.y;
            rb.velocity = vel;
        }
        else
        {
            transform.position += move * (moveSpeed * Time.fixedDeltaTime);
        }

        // shoot
        shootCd -= Time.fixedDeltaTime;
        if (shootAct == 1 && shootCd <= 0f)
        {
            shootCd = shootCooldown;
            Fire();
        }

        // rewards only in training
        if (trainingMode)
        {
            TrainingRewards(forward, strafe);
        }
    }

    private void Fire()
    {
        if (bulletPrefab == null || muzzle == null || player == null) return;

        Vector3 aimDir = (player.position - muzzle.position);
        aimDir.y = 0f;
        if (aimDir.sqrMagnitude < 0.0001f) aimDir = muzzle.forward;

        // add aim error
        float err = aimErrorDeg;
        Quaternion qErr = Quaternion.Euler(0f, Random.Range(-err, err), 0f);
        aimDir = (qErr * aimDir.normalized);

        GameObject b = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(aimDir, Vector3.up));
        if (b.TryGetComponent<Rigidbody>(out var brb))
            brb.velocity = aimDir * bulletSpeed;

        // connect bullet->owner for training callbacks
        if (b.TryGetComponent<Bullet>(out var bullet))
            bullet.owner = trainingMode ? this : null;
    }

    private void TrainingRewards(float forward, float strafe)
    {
        // time penalty
        AddReward(timePenalty);

        if (player == null) return;

        // idle penalty
        float planarSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        if (planarSpeed < idleSpeedThreshold)
            AddReward(-idlePenalty);

        Vector3 toP = (player.position - transform.position);
        toP.y = 0f;
        float dist = toP.magnitude;
        Vector3 toPn = (dist > 0.0001f) ? (toP / dist) : Vector3.zero;

        // toward + strafe shaping
        float forwardSpeed = Vector3.Dot(new Vector3(rb.velocity.x, 0f, rb.velocity.z), transform.forward);
        float toward = Vector3.Dot(transform.forward, toPn);
        AddReward(towardRewardScale * toward * Mathf.Clamp(forwardSpeed / targetTowardSpeed, -1f, 1f));

        // encourage strafe usage
        float strafeAbs = Mathf.Abs(strafe);
        AddReward(strafeRewardScale * Mathf.Clamp(strafeAbs * (targetStrafeForFull > 0f ? (1f / targetStrafeForFull) : 1f), 0f, 1f));

        // penalize too straight line (low strafe)
        AddReward(-straightLinePenaltyScale * Mathf.Clamp01(1f - strafeAbs));

        // facing reward
        AddReward(facingRewardScale * Mathf.Clamp01(toward));

        // distance shaping mild
        float distErr = Mathf.Abs(dist - targetDistance);
        if (distErr <= distanceTolerance)
        {
            AddReward(distanceRewardScale * (1f - (distErr / Mathf.Max(0.0001f, distanceTolerance))));
        }
        if (dist < tooCloseDistance) AddReward(-tooClosePenalty);
        if (dist > tooFarDistance) AddReward(-tooFarPenalty);
    }

    // called by Bullet.cs when it hits player collider (training)
    public void ReportHitPlayer()
    {
        if (!trainingMode) return;
        AddReward(hitPlayerReward);
    }

    // call this from whatever damages enemy in training (πχ player hitscan callback)
    public void ReportGotHit()
    {
        if (!trainingMode) return;

        gotHitCount++;
        AddReward(gotHitPenalty);

        if (gotHitCount >= maxHitsBeforeEnd)
        {
            EndEpisode();
        }
    }
}
