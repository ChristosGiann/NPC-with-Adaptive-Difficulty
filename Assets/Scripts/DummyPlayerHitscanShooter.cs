using UnityEngine;

public class DummyPlayerHitscanShooter : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                 // θα το κάνει assign ο ArenaSpawner
    public float targetHeight = 1.2f;        // πού στοχεύει πάνω στον enemy

    [Header("Fire")]
    public float fireRate = 4f;              // bullets/sec
    public float damage = 10f;
    public float range = 50f;

    [Header("Aim (human-like)")]
    public float aimErrorDeg = 6f;           // μεγαλώνει = χειρότερη στόχευση
    public float reactionJitter = 0.06f;     // μικρή καθυστέρηση/θόρυβος

    [Header("Raycast")]
    public LayerMask hitMask = ~0;           // βάλε να πιάνει Obstacles + Enemy layers
    public bool requireLineOfSight = true;   // αν true, σταματάει σε τοίχο

    float _nextShotTime;

    void Update()
    {
        if (target == null) return;
        if (Time.time < _nextShotTime) return;

        float interval = 1f / Mathf.Max(0.01f, fireRate);
        _nextShotTime = Time.time + interval + Random.Range(0f, reactionJitter);

        Shoot();
    }

    void Shoot()
    {
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 aimPoint = target.position + Vector3.up * targetHeight;

        Vector3 dir = (aimPoint - origin).normalized;

        // aim error
        dir = Quaternion.Euler(
            Random.Range(-aimErrorDeg, aimErrorDeg),
            Random.Range(-aimErrorDeg, aimErrorDeg),
            0f
        ) * dir;

        Debug.DrawRay(origin, dir * range, Color.red, 0.05f);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Αν χτυπήσει πρώτα τοίχο/cover, είναι miss (LoS break)
            // Αυτό συμβαίνει φυσικά επειδή raycast παίρνει το πρώτο collider.
            var dmgable = hit.collider.GetComponentInParent<IDamageable>();
            if (dmgable != null)
            {
                dmgable.TakeDamage(damage);

                // penalty στον enemy αν υπάρχει EnemyAgent πάνω του
                var agent = hit.collider.GetComponentInParent<EnemyAgent>();
                if (agent != null)
                    agent.OnHitByPlayer(damage);
            }
        }
    }
}
