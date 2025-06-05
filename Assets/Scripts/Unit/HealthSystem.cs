using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public event EventHandler OnDie;
    public event EventHandler OnDamage;

    [SerializeField] private int health = 100;
    private int healthMax;

    private void Awake()
    {
        healthMax = health;
    }

    public void Damage(int damageAmount)
    {
        health -= damageAmount;
        OnDamage?.Invoke(this, EventArgs.Empty);

        if (health < 0) health = 0;

        if (health == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDie?.Invoke(this, EventArgs.Empty);
    }

    public float GetHealthNormalized()
    {
        return (float)health / healthMax;
    }
}
