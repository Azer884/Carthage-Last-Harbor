using System;
using UnityEngine;

/// <summary>A ship's health is its living crew count. Higher ranks make its attacks stronger.</summary>
public class CarthaginianShipCrew : MonoBehaviour, ICombatTarget
{
    [SerializeField] private bool returnSurvivingCrewToRoster = true;
    private CrewCount[] _crew = new CrewCount[0];
    private float _health;
    public Transform TargetTransform => transform;
    public CarthaginianTargetType TargetType => CarthaginianTargetType.Ship;
    public bool IsDestroyed => _health <= 0f;
    public int CrewNumber { get { int total = 0; foreach (CrewCount count in _crew) total += count.amount; return total; } }
    public float DamageMultiplier
    {
        get
        {
            if (_crew == null || _crew.Length == 0 || CrewNumber == 0) return 1f;
            float quality = 0f;
            foreach (CrewCount count in _crew) quality += count.amount * (1f + (int)count.rank * 0.15f);
            return quality / CrewNumber;
        }
    }

    public void AssignCrew(CrewCount[] crew)
    {
        _crew = crew ?? new CrewCount[0];
        _health = 0f;
        foreach (CrewCount count in _crew) _health += count.amount;
    }

    public void TakeDamage(float damage)
    {
        if (IsDestroyed || damage <= 0f) return;
        _health = Mathf.Max(0f, _health - damage);
        if (IsDestroyed) { Destroy(gameObject); return; }
    }

    private void OnDestroy()
    {
        if (!returnSurvivingCrewToRoster || IsDestroyed || CrewRoster.Instance == null) return;
        foreach (CrewCount count in _crew) CrewRoster.Instance.AddCrew(count.rank, count.amount);
    }
}
