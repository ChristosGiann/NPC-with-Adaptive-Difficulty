using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 20f;
    public float lifeTime = 3f;

    [HideInInspector] public EnemyAgent owner;

    private bool _consumed;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnCollisionEnter(Collision col)
    {
        TryDamage(col.collider);
    }

    private void TryDamage(Collider hit)
    {
        if (_consumed) return;
        if (hit.isTrigger) return;

        // κάνε damage αν υπάρχει IDamageable
        var damageable = hit.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            // reward μόνο αν χτύπησε τον Player
            if (owner != null && hit.GetComponentInParent<Transform>().CompareTag("Player"))
                owner.OnHitPlayer();
        }

        _consumed = true;
        Destroy(gameObject);
    }
}
