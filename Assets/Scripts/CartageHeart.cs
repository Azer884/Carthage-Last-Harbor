using System;
using UnityEngine;

public class CartageHeart : MonoBehaviour, ICombatTarget
{
    [SerializeField] private int maxHealth = 35;
    private int currentHealth;

    public Transform TargetTransform => transform;
    public CarthaginianTargetType TargetType => CarthaginianTargetType.Tower;
    public bool IsDestroyed => currentHealth <= 0;

    public void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
            currentHealth = 0;
            //GamerManager.EndGame();
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(Mathf.CeilToInt(damage));
    }
}
