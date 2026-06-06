using UnityEngine;

// ─── manages all orbs, listens to PlayerHealth events ────────
// put this on a Canvas/HUD GameObject
// assign OrbPrefab and OrbContainer in inspector

public class HealthHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("Orb Setup")]
    // prefab with HealthOrb component + Animator
    [SerializeField] private HealthOrb _orbPrefab;
    // horizontal layout group works great here
    [SerializeField] private Transform _orbContainer;

    // max possible hp across entire game — preallocate all slots
    [SerializeField] private int _absoluteMaxHp = 5;

    private HealthOrb[] _orbs;

    void Awake()
    {
        SpawnOrbs();
        SubscribeEvents();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ─── spawn all orb slots upfront ────────────────────────────
    private void SpawnOrbs()
    {
        _orbs = new HealthOrb[_absoluteMaxHp];

        for (int i = 0; i < _absoluteMaxHp; i++)
        {
            HealthOrb orb = Instantiate(_orbPrefab, _orbContainer);
            orb.Init(i, _playerHealth.MaxHp, i < _playerHealth.CurrentHp);
            _orbs[i] = orb;
        }

        // hide slots above current max hp
        RefreshOrbVisibility(_playerHealth.CurrentHp, _playerHealth.MaxHp);
    }

    // ─── events ──────────────────────────────────────────────────
    private void SubscribeEvents()
    {
        _playerHealth.OnHpChanged.AddListener(OnHpChanged);
        _playerHealth.OnDeath.AddListener(OnDeath);
        _playerHealth.OnMaxHpReduced.AddListener(OnMaxHpReduced);
    }

    private void UnsubscribeEvents()
    {
        _playerHealth.OnHpChanged.RemoveListener(OnHpChanged);
        _playerHealth.OnDeath.RemoveListener(OnDeath);
        _playerHealth.OnMaxHpReduced.RemoveListener(OnMaxHpReduced);
    }

    // ─── hp changed — update each orb ───────────────────────────
    private void OnHpChanged(int currentHp, int maxHp)
    {
        RefreshOrbVisibility(currentHp, maxHp);
    }

    // ─── max hp permanently reduced ─────────────────────────────
    private void OnMaxHpReduced(int newMax)
    {
        // slot at newMax index is now gone forever — play damaged on it
        if (newMax < _orbs.Length)
            _orbs[newMax].SetAlive(newMax, newMax); // triggers damaged anim
    }

    // ─── death ───────────────────────────────────────────────────
    private void OnDeath()
    {
        foreach (var orb in _orbs)
            orb.PlayDeath();
    }

    // ─── sync all orbs to current state ─────────────────────────
    private void RefreshOrbVisibility(int currentHp, int maxHp)
    {
        for (int i = 0; i < _orbs.Length; i++)
        {
            // slots above max hp are permanently hidden
            if (i >= maxHp)
            {
                _orbs[i].gameObject.SetActive(false);
                continue;
            }

            _orbs[i].SetAlive(currentHp, maxHp);
        }
    }
}