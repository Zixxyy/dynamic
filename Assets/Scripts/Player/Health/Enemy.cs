using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// ─────────────────────────────────────────────────────────────────────────────
//  enemy base class
//  extend this for specific enemies — override OnHit() and OnDeath()
//  call TakeHit() from your player attack / hitbox
// ─────────────────────────────────────────────────────────────────────────────

public class Enemy : MonoBehaviour
{
    [Header("stats")]
    [SerializeField] protected int   maxHp          = 3;
    [SerializeField] protected int   damageToPlayer = 1;
    [SerializeField] private   int   chargesPerHit  = 1;   // flask charges given on hit

    [Header("knockback")]
    [SerializeField] private float knockbackForce    = 8f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("iframes")]
    [SerializeField] private float iFramesDuration = 0.3f;

    [Header("events")]
    public UnityEvent<int, int> onHit;    // (currentHp, maxHp)
    public UnityEvent           onDeath;

    public int  CurrentHp    { get; private set; }
    public bool IsAlive      { get; private set; } = true;
    public bool IsInvincible { get; private set; }

    protected Rigidbody rb;
    protected Collider  col;

    protected virtual void Awake()
    {
        CurrentHp = maxHp;
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // ── public api ────────────────────────────────────────────────────────────

    // call this from player attack — pass hitDirection for knockback
    public void TakeHit(int damage, PlayerHealthSystem player, Vector3 hitDirection)
    {
        if (!IsAlive || IsInvincible) return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        onHit?.Invoke(CurrentHp, maxHp);

        // reward flask charge to player
        player?.AddFlaskCharge(chargesPerHit);

        if (rb != null) StartCoroutine(Knockback(hitDirection));

        OnHit();

        if (CurrentHp <= 0)
            HandleDeath();
        else
            StartCoroutine(IFrames());
    }

    // call from trigger / collision when enemy touches player
    public void DealDamage(PlayerHealthSystem player)
    {
        if (!IsAlive) return;
        player?.TakeDamage(damageToPlayer);
    }

    // ── override these in subclasses ──────────────────────────────────────────

    protected virtual void OnHit()   { }   // add hit vfx, sfx, stagger anim here
    protected virtual void OnDeath() { }   // add death anim, loot drop here

    // ── internal ──────────────────────────────────────────────────────────────

    private void HandleDeath()
    {
        IsAlive = false;
        if (col) col.enabled = false;
        onDeath?.Invoke();
        OnDeath();
        Destroy(gameObject, 1f);
    }

    private IEnumerator IFrames()
    {
        IsInvincible = true;
        yield return new WaitForSeconds(iFramesDuration);
        IsInvincible = false;
    }

    private IEnumerator Knockback(Vector3 direction)
    {
        float elapsed = 0f;
        direction.y   = 0f;
        direction     = direction.normalized;

        while (elapsed < knockbackDuration)
        {
            float t = 1f - (elapsed / knockbackDuration);
            rb.linearVelocity = direction * (knockbackForce * t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
    }
}
