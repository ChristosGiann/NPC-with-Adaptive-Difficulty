using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage = 10f;
    public float life = 3f;

    [HideInInspector] public EnemyAgent owner; // training: για reward callbacks

    void Start()
    {
        Destroy(gameObject, life);
    }

    void OnCollisionEnter(Collision col)
    {
        var d = col.collider.GetComponentInParent<IDamageable>();
        if (d != null)
        {
            d.TakeDamage(damage);

            // Αν είναι training, και ο owner υπάρχει, ενημέρωσε για hit player (μόνο αν χτύπησε Player target)
            if (owner != null && col.collider.CompareTag("Player"))
            {
                owner.ReportHitPlayer();
            }
        }

        Destroy(gameObject);
    }
}
