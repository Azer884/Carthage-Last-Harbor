using System;
using UnityEngine;

public class CartageHeart : MonoBehaviour, ICombatTarget
{
    // Static so GameManger (and the HUD) can check it without scanning the scene, and so it survives
    // the heart's GameObject being hidden (FindObjectsByType skips inactive objects by default).
    public static event Action Destroyed;
    public static bool HasBeenDestroyed { get; private set; }
    public static CartageHeart Instance { get; private set; }

    [SerializeField] private int maxHealth = 35;
    [Tooltip("Optional. Spawned at the heart's position when it's destroyed.")]
    [SerializeField] private GameObject destroyFxPrefab;
    private int currentHealth;

    public Transform TargetTransform => transform;
    public CarthaginianTargetType TargetType => CarthaginianTargetType.Tower;
    public bool IsDestroyed => currentHealth <= 0;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    public void Awake()
    {
        Instance = this;
        currentHealth = maxHealth;
        HasBeenDestroyed = false;
    }

    public void TakeDamage(int damage)
    {
        if (HasBeenDestroyed || currentHealth <= 0) return;
        currentHealth -= damage;
        FloatingCombatText.Spawn(transform.position, "-" + damage, new Color(1f, .82f, .25f));
        if (currentHealth > 0) return;
        currentHealth = 0;
        HandleDestroyed();
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(Mathf.CeilToInt(damage));
    }

    private void HandleDestroyed()
    {
        HasBeenDestroyed = true;
        CombatFx.PlayExplosion(transform.position, 2.4f);
        CameraShake.Shake(1f);
        if (destroyFxPrefab != null) Instantiate(destroyFxPrefab, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
        Destroyed?.Invoke();
    }
}
