using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// ─────
//  enemy base class
//  extend this for specific enemies — override OnHit() and OnDeath()
//  call TakeHit() from your player attack / hitbox
// ─────────────
public class Enemy : MonoBehaviour
{
    [Header("stats")]
    [SerializeField] protected int   maxHp          = 3;
    [SerializeField] protected int   damageToPlayer = 1;
    [SerializeField] private   int   chargesPerHit  = 1;

    [Header("patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float       moveSpeed      = 2.5f;
    [SerializeField] private float       waypointRadius = 0.3f;

    [Header("chase")]
    [SerializeField] private float chaseRange    = 8f;
    [SerializeField] private float attackRange   = 1.4f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("knockback")]
    [SerializeField] private float knockbackForce    = 8f;
    [SerializeField] private float knockbackDuration = 0.15f;

    [Header("iframes")]
    [SerializeField] private float iFramesDuration = 0.3f;

    [Header("events")]
    public UnityEvent<int, int> onHit;
    public UnityEvent           onDeath;

    public int  CurrentHp    { get; private set; }
    public bool IsAlive      { get; private set; } = true;
    public bool IsInvincible { get; private set; }

    protected Rigidbody        rb;
    protected Collider         col;
    private   Transform        player;
    private   PlayerHealthSystem playerHealth;

    private enum State { Patrol, Chase, Attack }
    private State  _state         = State.Patrol;
    private int    _waypointIndex = 0;
    private float  _attackTimer   = 0f;

    // ── lifecycle ─────

    protected virtual void Awake()
    {
        CurrentHp = maxHp;
        rb        = GetComponent<Rigidbody>();
        col       = GetComponent<Collider>();
    }

    private void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go)
        {
            player       = go.transform;
            playerHealth = go.GetComponent<PlayerHealthSystem>();
        }
    }

    private void Update()
    {
        if (!IsAlive) return;

        _attackTimer -= Time.deltaTime;

        float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        // выбор состояния
        if (distToPlayer <= attackRange)
            _state = State.Attack;
        else if (distToPlayer <= chaseRange)
            _state = State.Chase;
        else
            _state = State.Patrol;

        switch (_state)
        {
            case State.Patrol: DoPatrol(); break;
            case State.Chase:  DoChase();  break;
            case State.Attack: DoAttack(); break;
        }
    }

    // ── doings ────

    private void DoPatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[_waypointIndex];
        MoveTowards(target.position);

        if (Vector3.Distance(transform.position, target.position) < waypointRadius)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
    }

    private void DoChase()
    {
        if (player) MoveTowards(player.position);
    }

    private void DoAttack()
    {
        if (_attackTimer > 0f) return;
        DealDamage(playerHealth);
        _attackTimer = attackCooldown;
    }

    private void MoveTowards(Vector3 target)
    {
        Vector3 dir = (target - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
        transform.rotation  = Quaternion.LookRotation(dir.normalized);
    }

    // ── public api ──

    public void TakeHit(int damage, PlayerHealthSystem p, Vector3 hitDirection)
    {
        if (!IsAlive || IsInvincible) return;

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        onHit?.Invoke(CurrentHp, maxHp);
        p?.AddFlaskCharge(chargesPerHit);

        if (rb) StartCoroutine(Knockback(hitDirection));
        OnHit();

        if (CurrentHp <= 0) HandleDeath();
        else                StartCoroutine(IFrames());
    }

    public void DealDamage(PlayerHealthSystem p)
    {
        if (!IsAlive) return;
        p?.TakeDamage(damageToPlayer);
    }

    // ── override in subclasses ──────

    protected virtual void OnHit()   { }
    protected virtual void OnDeath() { }

    // ── internal ───
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
            rb.linearVelocity = direction * (knockbackForce * (1f - elapsed / knockbackDuration));
            elapsed          += Time.deltaTime;
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
    }
}