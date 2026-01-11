using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    public float maxHP = 100f;
    public bool trainingImmortal = false; // τσέκαρέ το στον DummyPlayer

    [SerializeField] private float hp;

    private void Awake()
    {
        hp = maxHP;
    }

    public void ResetHP()
    {
        hp = maxHP;
    }

    public void TakeDamage(float dmg)
    {
        hp -= dmg;

        if (hp <= 0f)
        {
            if (trainingImmortal)
            {
                hp = maxHP;   // άμεσο reset για training dummy
                return;
            }

            Destroy(gameObject); // game mode
        }
    }
}
