using System;
using UnityEngine;

public class GunRaycast : MonoBehaviour
{
    public float damage = 100f;
    public float range = 80f;
    public float fireRate = 8f;

    [Header("Ray Origin / Direction")]
    public Camera cam;                  // αν είναι null -> Camera.main
    public LayerMask hitMask = ~0;      // default όλα
    public bool ignorePlayerLayer = true;

    [Header("Debug / Visual")]
    public bool logShots = false;
    public bool showTracer = true;
    public float tracerSeconds = 0.05f;
    public float tracerWidth = 0.02f;
    public bool showHitPoint = false;
    public float hitPointSize = 0.06f;

    /// <summary>
    /// Fires once per shot. Argument: hitSomething (true/false).
    /// Used by WaveMetricsCollector to compute accuracy.
    /// </summary>
    public event Action<bool> ShotResolved;

    float cd;

    void Update()
    {
        cd -= Time.deltaTime;

        // ΠΡΟΣΟΧΗ: στο Editor θέλει click στο Game window για focus
        if (Input.GetMouseButton(0) && cd <= 0f)
        {
            cd = 1f / Mathf.Max(0.01f, fireRate);
            Shoot();
        }
    }

    void Shoot()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Ray από το κέντρο της οθόνης (κλασικό FPS)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        int mask = hitMask;
        if (ignorePlayerLayer)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) mask &= ~(1 << playerLayer);
        }

        if (logShots) Debug.Log($"[GunRaycast] Shot from '{gameObject.name}'", this);

        // (Scene view only)
        Debug.DrawRay(ray.origin, ray.direction * range, Color.yellow, 0.15f);

        bool hitSomething = false;

        Vector3 end = ray.origin + ray.direction * range;
        if (Physics.Raycast(ray, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            hitSomething = true;
            end = hit.point;

            var d = hit.collider.GetComponentInParent<IDamageable>();
            if (d != null) d.TakeDamage(damage);

            if (logShots)
            {
                string hitName = hit.collider != null ? hit.collider.name : "(none)";
                Debug.Log($"[GunRaycast] Hit: {hitName} @ {hit.point}", this);
            }

            if (showHitPoint) SpawnHitPoint(end);
        }

        if (showTracer) SpawnTracer(ray.origin, end);

        ShotResolved?.Invoke(hitSomething);
    }

    void SpawnTracer(Vector3 a, Vector3 b)
    {
        var go = new GameObject("_Tracer");
        go.hideFlags = HideFlags.DontSave;

        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startWidth = tracerWidth;
        lr.endWidth = tracerWidth;

        Destroy(go, tracerSeconds);
    }

    void SpawnHitPoint(Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.hideFlags = HideFlags.DontSave;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * hitPointSize;
        Destroy(go.GetComponent<Collider>());
        Destroy(go, 0.35f);
    }
}
