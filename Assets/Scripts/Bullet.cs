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

    private void OnCollisionEnter(Collision col)
    {
        if (_consumed) return;

        // Πάρε το IDamageable από το collider ή από parent
        var damageable = col.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            // reward μόνο αν χτύπησε Player (root tag)
            if (owner != null)
            {
                var root = col.collider.transform.root;
                if (root != null && root.CompareTag("Player"))
                    owner.OnHitPlayer();
            }
        }

        _consumed = true;
        Destroy(gameObject);
    }
}
