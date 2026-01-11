using UnityEngine;

public class ProjectileShooter : MonoBehaviour
{
    public Transform muzzle;          // σημείο εκτόξευσης (στην Camera για τον player)
    public GameObject bulletPrefab;
    public float muzzleSpeed = 40f;   // 30–60 για να «φαίνεται»
    public float fireRate = 8f;
    [Range(0,30f)] public float spreadDeg = 2.5f;  // 0=laser

    float nextFire;

    void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= nextFire)
        {
            nextFire = Time.time + 1f / fireRate;
            Fire();
        }
    }

    void Fire()
    {
        var b = Instantiate(bulletPrefab, muzzle.position, muzzle.rotation);
        var rb = b.GetComponent<Rigidbody>();
        Vector3 dir = ApplyCone(muzzle.forward, spreadDeg * Mathf.Deg2Rad);
        rb.velocity = dir * muzzleSpeed;
        // προαιρετικά: πρόσθεσε και την ταχύτητα του shooter για “inherit”
        // rb.velocity += shooterRigidbodyVelocity;
    }

    static Vector3 ApplyCone(Vector3 fwd, float coneRad)
    {
        if (coneRad <= 0f) return fwd;
        // δειγματοληψία μικρής γωνίας γύρω από το fwd
        var rand = Random.onUnitSphere;
        var axis = Vector3.Cross(fwd, rand).normalized;
        float angle = coneRad * Mathf.Sqrt(Random.value);
        return Quaternion.AngleAxis(angle * Mathf.Rad2Deg, axis) * fwd;
    }
}
