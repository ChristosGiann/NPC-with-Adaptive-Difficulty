using UnityEngine;

public class DummyPlayerHitscanShooter : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // assign από ArenaSpawner
    public float targetHeight = 1.2f;

    [Header("Fire")]
    public float fireRate = 4f;              // shots/sec
    public float damage = 10f;
    public float range = 50f;

    [Header("Aim (human-like)")]
    public float aimErrorDeg = 6f;
    public float reactionJitter = 0.06f;

    [Header("Training")]
    public float warmupSeconds = 1.5f;       // χρόνο να κινηθεί ο enemy πριν αρχίσουν hits
    public bool callEnemyHitCallback = true; // να καλεί EnemyAgent.OnHitByPlayer

    private float _nextShotTime;

    private void Start()
    {
        ResetWarmup();
    }

        private void OnEnable()
    {
        ResetWarmup();
    }


    // ΚΑΛΕΣΕ ΤΟ ΣΕ ΚΑΘΕ EPISODE (το κάνει ήδη το EnemyAgent.OnEpisodeBegin)
    public void ResetWarmup()
    {
        _nextShotTime = Time.time + Mathf.Max(0f, warmupSeconds) + Random.Range(0f, reactionJitter);
    }

    private void Update()
    {
        if (target == null) return;
        if (Time.time < _nextShotTime) return;

        ShootOnce();
    }

    private void ShootOnce()
    {
        float interval = 1f / Mathf.Max(0.01f, fireRate);
        _nextShotTime = Time.time + interval + Random.Range(0f, reactionJitter);

        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 targetPos = target.position + Vector3.up * targetHeight;

        Vector3 dir = targetPos - origin;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        // aim error
        dir = Quaternion.Euler(
            Random.Range(-aimErrorDeg, aimErrorDeg),
            Random.Range(-aimErrorDeg, aimErrorDeg),
            0f
        ) * dir;

        Debug.DrawRay(origin, dir * range, Color.red, 0.05f);

        if (!Physics.Raycast(origin, dir, out RaycastHit hit, range))
            return;

        var dmgable = hit.collider.GetComponentInParent<IDamageable>();
        if (dmgable == null) return;

        dmgable.TakeDamage(damage);

        if (!callEnemyHitCallback) return;

        // Αν χτύπησε Enemy, ενημέρωσε τον agent για shaping (recentlyHitTimer, hit penalty, hitsTaken++)
        var enemyAgent = hit.collider.GetComponentInParent<EnemyAgent>();
        if (enemyAgent != null)
            enemyAgent.ReportGotHit();
    }
    
}
