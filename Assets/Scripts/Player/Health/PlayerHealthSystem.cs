using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// ──
//  player health system
//  handles hp, speed scaling, healing flask charges, and deathh
//  attach to player game object
// ──────────

public class PlayerHealthSystem : MonoBehaviour
{
    // ── config & refs ────

    [Header("config")]
    [SerializeField] private PlayerHealthConfig config;

    [Header("refs")]
    [SerializeField] private MovementController movementController;

    // ── flaska ────

    [Header("flask")]
    [Tooltip("max charges the flask can hold")]
    [SerializeField] private int maxFlaskCharges  = 5;

    [Tooltip("charges gained per enemy hit")]
    [SerializeField] private int chargesPerHit    = 1;

    [Tooltip("how long the drink animation takes before hp restores")]
    [SerializeField] private float healAnimDuration = 0.6f;

    // ── events — wire these to hud, vfx, audio ───

    [Header("events")]
    public UnityEvent<int, int>  onDamaged; // (currentHp, maxHp)
    public UnityEvent<int, int>  onHealed;  // (currentHp, maxHp)
    public UnityEvent<int, int>  onFlaskChargeChanged;// (currentCharges, maxCharges)
    public UnityEvent            onHealAnimStarted; // → trigger drink animation
    public UnityEvent            onDeath; 

    // ── public state ────

    public int  CurrentHp       { get; private set; }
    public int  MaxHp           => config.maxHp;
    public int  FlaskCharges    { get; private set; }
    public bool IsAlive         { get; private set; } = true;
    public bool IsInvincible    { get; private set; }
    public bool IsHealing       { get; private set; }

    // ── input ──

    private InputAction _healAction;

    // ── lifecycle ───

    private void Awake()
    {
        CurrentHp   = config.maxHp;
        _healAction = InputSystem.actions.FindAction("Heal");

        if (_healAction == null)
            Debug.LogWarning("[health] 'Heal' action not found tip - bind input act. R / L2");

        ApplySpeed();
    }

    private void Update()
    {
        if (_healAction != null && _healAction.triggered)
            TryHeal();
    }

    // ── public api ──
    // call from enemy, hazard, trap
    public void TakeDamage(int amount = 1)
    {
        if (!IsAlive || IsInvincible) return;

        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        onDamaged?.Invoke(CurrentHp, MaxHp);
        ApplySpeed();

        if (CurrentHp <= 0)
        {
            IsAlive = false;
            onDeath?.Invoke();
            return;
        }

        StartCoroutine(IFrames());
    }

    // call from enemy when player lands a hit — rewards flask charge
    public void AddFlaskCharge(int amount = 1)
    {
        int prev     = FlaskCharges;
        FlaskCharges = Mathf.Clamp(FlaskCharges + amount, 0, maxFlaskCharges);
        if (FlaskCharges != prev)
            onFlaskChargeChanged?.Invoke(FlaskCharges, maxFlaskCharges);
    }

    // ── internal ──────

    private void TryHeal()
{
    if (!IsAlive)                        return;
    if (IsHealing)                       return;
    if (FlaskCharges < maxFlaskCharges)  return; // only when full
    if (CurrentHp >= MaxHp)             return;

    StartCoroutine(HealRoutine());
}

    private IEnumerator HealRoutine()
    {
        IsHealing = true;

        // signal hud / animator to play drink animation
        onHealAnimStarted?.Invoke();

        yield return new WaitForSeconds(healAnimDuration);

        // restore hp after animation finishes
        CurrentHp = MaxHp;
        FlaskCharges = 0;
        onFlaskChargeChanged?.Invoke(FlaskCharges, maxFlaskCharges);
        onHealed?.Invoke(CurrentHp, MaxHp);
        ApplySpeed();

        IsHealing = false;
    }

    private IEnumerator IFrames()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(config.iFramesDuration);
        IsInvincible = false;
    }

    private void ApplySpeed()
    {
        if (!movementController) return;
        float mult = config.GetSpeedMultiplier(CurrentHp);
        movementController.SetSpeedMultiplier(mult);
    }
}
