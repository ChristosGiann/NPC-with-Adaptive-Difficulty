using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float MaxHP = 100f;
    public bool TrainingImmortal = false;
    public bool LogDamage = false;

    [SerializeField] private float hp = 100f;
    public float Hp => hp;

    public event Action<Health> Died;
    public event Action<Health, float> Damaged; // (who, dmg)

    void Awake()
    {
        ResetHP();
    }

    public void ResetHP()
    {
        hp = Mathf.Max(1f, MaxHP);
    }

    public void SetHP(float value)
    {
        hp = Mathf.Clamp(value, 0f, Mathf.Max(1f, MaxHP));
        if (hp <= 0f) Die();
    }

    public void TakeDamage(float dmg)
    {
        if (TrainingImmortal) return;
        if (dmg <= 0f) return;

        hp -= dmg;
        if (LogDamage) Debug.Log($"[Health] {gameObject.name} took {dmg} dmg => {hp}", this);
        Damaged?.Invoke(this, dmg);

        if (hp <= 0f)
            Die();
    }

    private void Die()
    {
        hp = 0f;
        if (LogDamage) Debug.Log($"[Health] {gameObject.name} died", this);
        Died?.Invoke(this);
    }
}
