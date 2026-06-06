using UnityEngine;
using UnityEngine.Events;

// ─── main hp logic ───────────────────────────────────────────
// max hp can be reduced permanently (boss progression mechanic)
// speed multiplier fires on every hp change so movement can react

public class PlayerHealth : MonoBehaviour
{
    [Header("HP Settings")]
    [SerializeField] private int _maxHp = 5;
    // hard floor — can't reduce max below this
    [SerializeField] private int _absoluteMinHp = 1;

    [Header("Speed Tiers — index 0 = full hp, last = 1 hp left")]
    // fill these in inspector: e.g. [1.0, 1.2, 1.5, 2.0, 3.0] for 5 max hp
    [SerializeField] private float[] _speedMultiplierPerTier;

    // ── events ──────────────────────────────────────────────────
    // float = current speed multiplier
    public UnityEvent<float> OnSpeedChanged;
    // int currentHp, int maxHp
    public UnityEvent<int, int> OnHpChanged;
    public UnityEvent OnDeath;
    public UnityEvent OnHeal;
    // int newMax
    public UnityEvent<int> OnMaxHpReduced;

    // ── state ───────────────────────────────────────────────────
    public int CurrentHp  { get; private set; }
    public int MaxHp      { get; private set; }
    public bool IsDead    { get; private set; }

    void Start()
    {
        MaxHp     = _maxHp;
        CurrentHp = _maxHp;
        FireSpeedEvent();
    }

    // ─── take hit ────────────────────────────────────────────────
    public void TakeDamage(int amount = 1)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        FireSpeedEvent();

        if (CurrentHp <= 0)
        {
            IsDead = true;
            OnDeath?.Invoke();
        }
    }

    // ─── heal ────────────────────────────────────────────────────
    public void Heal(int amount = 1)
    {
        if (IsDead) return;

        int before = CurrentHp;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);

        if (CurrentHp != before)
        {
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            OnHeal?.Invoke();
            FireSpeedEvent();
        }
    }

    // ─── progression: boss killed = lose one max hp slot ─────────
    public void ReduceMaxHp()
    {
        if (MaxHp <= _absoluteMinHp) return;

        MaxHp--;
        // clamp current hp if it was at the old max
        CurrentHp = Mathf.Min(CurrentHp, MaxHp);
        OnMaxHpReduced?.Invoke(MaxHp);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        FireSpeedEvent();
    }

    // ─── speed tier calc ─────────────────────────────────────────
    // tier 0 = full hp (slow), last tier = 1 hp left (fastest)
    private void FireSpeedEvent()
    {
        if (_speedMultiplierPerTier == null || _speedMultiplierPerTier.Length == 0)
        {
            OnSpeedChanged?.Invoke(1f);
            return;
        }

        // map current hp loss to tier index
        int hpLost = MaxHp - CurrentHp;
        int tier   = Mathf.Clamp(hpLost, 0, _speedMultiplierPerTier.Length - 1);
        OnSpeedChanged?.Invoke(_speedMultiplierPerTier[tier]);
    }

    // ─── debug helpers ───────────────────────────────────────────
    [ContextMenu("Debug: Take Damage")]
    private void DebugDamage() => TakeDamage();

    [ContextMenu("Debug: Heal")]
    private void DebugHeal() => Heal();

    [ContextMenu("Debug: Reduce Max HP")]
    private void DebugReduceMax() => ReduceMaxHp();
}