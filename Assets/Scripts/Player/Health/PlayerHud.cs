using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
//  player hud
//  drives the hp bar fill + cell animation + flask pips
// ─────────────────────────────────────────────────────────────────────────────

public class PlayerHud : MonoBehaviour
{
    // ── variables ───

    [Header("health system")]
    [SerializeField] private PlayerHealthSystem healthSystem;

    [Header("hp bar")]
    [SerializeField] private Image     hpBarFill;     // the fill image inside the frame
    [SerializeField] private Animator  hpBarAnimator; // animator on the frame root

    [Header("hp bar fill colors — shifts as hp drops")]
    [SerializeField] private Color colorFull   = new Color(0.2f, 0.9f, 0.3f); // green color
    [SerializeField] private Color colorMedium = new Color(0.9f, 0.7f, 0.1f); // yellow colour
    [SerializeField] private Color colorLow    = new Color(0.9f, 0.2f, 0.1f); // red colourawr

    [Header("fill animation speed")]
    [Tooltip("time in seconds to reach target fill — lower = faster")]
    [SerializeField] private float fillLerpTime = 0.1f;

    [Header("flask pips — one Image per charge slot")]
    [SerializeField] private Image[] chargePips;   // assign in inspector
    [SerializeField] private Color   pipFilled = Color.white;
    [SerializeField] private Color   pipEmpty  = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Sprite pipFilledSprite; // enable
    [SerializeField] private Sprite pipEmptySprite;  // isnt enable

    

    [Header("flask animations")]
    [SerializeField] private Animator flaskAnimator;                // animator on FlaskRoot
    [SerializeField] private string   flaskReadyTrigger = "Ready"; // plays when flask is full
    [SerializeField] private string   flaskUsedTrigger  = "Used";  // plays when flask is consumed

    [Header("flask sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   flaskReadySound;

    // ── animator parameter names ── 

    [Header("animator parameters")]
    [Tooltip("int parameter: current hp value (0–4). drive your animation states from this.")]
    [SerializeField] private string hpIntParam    = "Hp";      // int  — current hp
    [SerializeField] private string damageTrigger = "Damage";  // trigger
    [SerializeField] private string healTrigger   = "Heal";    // trigger

    // ── state ───

    private float targetFill;
    private float currentFill;

    // ── lifecycle ────

    private void Awake()
    {
        // snap bar to full on start
        targetFill  = 1f;
        currentFill = 1f;
        if (hpBarFill) hpBarFill.fillAmount = 1f;

        RefreshAll();
    }

    private void OnEnable()
    {
        healthSystem.onDamaged.AddListener(OnDamaged);
        healthSystem.onHealed.AddListener(OnHealed);
        healthSystem.onFlaskChargeChanged.AddListener(OnFlaskChanged);
    }

    private void OnDisable()
    {
        healthSystem.onDamaged.RemoveListener(OnDamaged);
        healthSystem.onHealed.RemoveListener(OnHealed);
        healthSystem.onFlaskChargeChanged.RemoveListener(OnFlaskChanged);
    }

    private void Update()
    {
        AnimateFill();
    }

    // ── event handlers ───

    private void OnDamaged(int currentHp, int maxHp)
    {
        targetFill = (float)currentHp / maxHp;

        if (hpBarAnimator && hpBarAnimator.runtimeAnimatorController)
        {
            hpBarAnimator.SetInteger(hpIntParam, currentHp);
            hpBarAnimator.SetTrigger(damageTrigger);
        }
    }

    private void OnHealed(int currentHp, int maxHp)
    {
        targetFill = (float)currentHp / maxHp;

        if (hpBarAnimator && hpBarAnimator.runtimeAnimatorController)
        {
            hpBarAnimator.SetInteger(hpIntParam, currentHp);
            hpBarAnimator.SetTrigger(healTrigger);
        }
    }

    private void OnFlaskChanged(int current, int max)
    {
        if (chargePips == null) return;

        for (int i = 0; i < chargePips.Length; i++)
        {
            if (chargePips[i] == null) continue;
            chargePips[i].sprite = i < current ? pipFilledSprite : pipEmptySprite;
        }

        if (current >= max)
        {
            if (flaskAnimator && flaskAnimator.runtimeAnimatorController)
                flaskAnimator.SetTrigger(flaskReadyTrigger);

            if (audioSource && flaskReadySound)
                audioSource.PlayOneShot(flaskReadySound);
        }

        if (current == 0)
        {
            if (flaskAnimator && flaskAnimator.runtimeAnimatorController)
                flaskAnimator.SetTrigger(flaskUsedTrigger);
        }
    }

    // ── internal ────

    // smooth fill lerp every frame — frame-rate independent exp
    private void AnimateFill()
    {
        if (!hpBarFill) return;
        if (Mathf.Approximately(currentFill, targetFill)) return;

        float t = 1f - Mathf.Exp(-Time.deltaTime / fillLerpTime);
        currentFill = Mathf.Lerp(currentFill, targetFill, t);
        hpBarFill.fillAmount = currentFill;

        // shift color green → yellow → red as hp drops
        hpBarFill.color = currentFill > 0.5f
            ? Color.Lerp(colorMedium, colorFull,  (currentFill - 0.5f) * 2f)
            : Color.Lerp(colorLow,    colorMedium, currentFill * 2f);
    }

    // snaps everything to current health state on scene load or respawn
    private void RefreshAll()
    {
        int hp    = healthSystem.CurrentHp;
        int maxHp = healthSystem.MaxHp;

        targetFill  = (float)hp / maxHp;
        currentFill = targetFill;
        if (hpBarFill) hpBarFill.fillAmount = currentFill;

        if (hpBarAnimator && hpBarAnimator.runtimeAnimatorController)
            hpBarAnimator.SetInteger(hpIntParam, hp);

        OnFlaskChanged(healthSystem.FlaskCharges, healthSystem.MaxHp);
    }
}