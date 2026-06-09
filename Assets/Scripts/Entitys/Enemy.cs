using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHp = 10;

    [Header("Patrol")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float       moveSpeed      = 2.5f;
    [SerializeField] private float       waypointRadius = 0.3f;

    [Header("Detection")]
    [SerializeField] private float frontViewRange  = 8f;   // how far sees in front
    [SerializeField] private float frontViewAngle  = 90f;  // half-angle, 90 = 180 deg total cone
    [SerializeField] private float backViewRange   = 1.5f; // how close from behind to notice
    [SerializeField] private float loseAggroRange  = 12f;  // how far player must go to lose aggro

    [Header("Attack")]
    [SerializeField] private float attackRange    = 1.4f;
    [SerializeField] private float attackCooldown = 1.2f;

    private int   _currentHp;
    private bool  _isAlive    = true;
    private bool  _isAggroed  = false;
    private float _attackTimer = 0f;
    private int   _waypointIndex = 0;

    private Transform _player; // im not a good developer and this is not giving boolean (true or false) this is giving cords OR null!!!!!;

    void Start()
    {
        _currentHp = maxHp;

        var playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            _player = playerObject.transform;
    }

    void Update()
    {
        if (!_isAlive) return;

        _attackTimer -= Time.deltaTime;

        UpdateAggro();

        if (_isAggroed)
            ChaseAndAttack();
        else
            Patrol();
    }

    // check if should aggro or deaggro
    private void UpdateAggro()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (!_isAggroed && CanSeePlayer(dist))
        {
            _isAggroed = true;
        }

        // only deaggro if player runs far enough away
        if (_isAggroed && dist > loseAggroRange)
        {
            _isAggroed = false;
        }
    }

    private bool CanSeePlayer(float dist)
    {
        // behind — only if very close
        if (dist <= backViewRange)
            return true;

        // front cone check
        Vector3 toPlayer = (_player.position - transform.position); // looking angle
        toPlayer.y = 0f;
        float angle = Vector3.Angle(transform.forward, toPlayer);

        if (angle <= frontViewAngle && dist <= frontViewRange)
            return true;

        return false;
    }

    private void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[_waypointIndex];
        MoveTowards(target.position);

        if (Vector3.Distance(transform.position, target.position) < waypointRadius)
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length; // 3 % 3 = 0 when 0 he coming back
    }

    private void ChaseAndAttack()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        if (dist <= attackRange)
            TryAttack();
        else
            MoveTowards(_player.position);
    }

    private void TryAttack()
    {
        if (_attackTimer > 0f) return;

        _attackTimer = attackCooldown;

        // TODO: deal damage to player here when hp system is ready
        Debug.Log("Enemy attacked player");
    }

    private void MoveTowards(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        transform.position += dir.normalized * (moveSpeed * Time.deltaTime);
        transform.rotation  = Quaternion.LookRotation(dir.normalized);
    }

    // call this from your attack hitbox when player hits enemy
    public void TakeHit(int damage)
    {
        if (!_isAlive) return;

        _currentHp -= damage;

        // TODO: play hit animation / sound here

        if (_currentHp <= 0)
            Killed();
    }

    private void Killed()
    {
        _isAlive = false;

        // TODO: play death animation, drop loot, etc
        Debug.Log("Enemy died");

        Destroy(gameObject, 1f);
    }
}