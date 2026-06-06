using UnityEngine;

// ─── single hp orb — sits on a UI GameObject with Animator ───
// animator must have these bool/trigger params:
//   bool   "IsFullHp"   — true when player is at full hp
//   int    "MaxHpTier"  — which set of idles to use (5,4,3,2,1)
//   trigger "Damaged"
//   trigger "Heal"
//   trigger "Death"

public class HealthOrb : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    // animator param names — match these in your Animator Controller
    private static readonly int P_IsFullHp  = Animator.StringToHash("IsFullHp");
    private static readonly int P_MaxHpTier = Animator.StringToHash("MaxHpTier");
    private static readonly int P_Damaged   = Animator.StringToHash("Damaged");
    private static readonly int P_Heal      = Animator.StringToHash("Heal");
    private static readonly int P_Death     = Animator.StringToHash("Death");

    // ── state this orb knows about ──────────────────────────────
    // slot index: 0 = leftmost orb, maxHp-1 = rightmost
    public int SlotIndex { get; private set; }

    // ─── init ────────────────────────────────────────────────────
    public void Init(int slotIndex, int currentMaxHp, bool isAlive)
    {
        SlotIndex = slotIndex;
        _animator.SetInteger(P_MaxHpTier, currentMaxHp);
        SetIdle(isAlive, currentMaxHp);
    }

    // ─── called by HealthHUD when hp changes ────────────────────
    public void SetAlive(int currentHp, int maxHp)
    {
        bool orbIsActive = SlotIndex < currentHp;
        bool wasActive   = gameObject.activeSelf;

        _animator.SetInteger(P_MaxHpTier, maxHp);
        _animator.SetBool(P_IsFullHp, currentHp == maxHp);

        if (orbIsActive && !wasActive)
        {
            // orb coming back — heal anim
            gameObject.SetActive(true);
            _animator.SetTrigger(P_Heal);
        }
        else if (!orbIsActive && wasActive)
        {
            // orb being lost — damaged anim then deactivate via animation event
            _animator.SetTrigger(P_Damaged);
        }
    }

    // ─── death — all orbs play death anim ───────────────────────
    public void PlayDeath()
    {
        if (gameObject.activeSelf)
            _animator.SetTrigger(P_Death);
    }

    // ─── called from Animation Event at end of Damaged clip ─────
    // add this as anim event on the last frame of Damaged animation
    public void OnDamagedAnimFinished()
    {
        gameObject.SetActive(false);
    }

    // ─── idle setup ─────────────────────────────────────────────
    private void SetIdle(bool isActive, int maxHp)
    {
        gameObject.SetActive(isActive);
        if (isActive)
            _animator.SetBool(P_IsFullHp, true);
    }
}