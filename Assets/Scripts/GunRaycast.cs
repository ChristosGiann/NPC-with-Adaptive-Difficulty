using UnityEngine;

public class GunRaycast : MonoBehaviour
{
    public float damage = 20f;
    public float range = 50f;
    public float fireRate = 6f;

    public Transform firePoint;

    float cd;

    void Update()
    {
        cd -= Time.deltaTime;
        if (Input.GetMouseButton(0) && cd <= 0f)
        {
            cd = 1f / Mathf.Max(0.01f, fireRate);
            Shoot();
        }
    }

    void Shoot()
    {
        if (firePoint == null) firePoint = transform;

        Vector3 origin = firePoint.position;
        Vector3 dir = firePoint.forward;

        Debug.DrawRay(origin, dir * range, Color.yellow, 0.2f);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, range))
        {
            var d = hit.collider.GetComponentInParent<IDamageable>();
            if (d != null)
                d.TakeDamage(damage);
        }
    }
}
