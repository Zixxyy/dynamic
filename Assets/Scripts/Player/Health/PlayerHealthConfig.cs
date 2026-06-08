using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthConfig", menuName = "Player/Health Config")]
public class PlayerHealthConfig : ScriptableObject
{
    [Header("hp")]
    public int maxHp = 4;

    [Header("speed multipliers per hp — index 0 = full hp, last = 1 hp")]
    public float[] speedMultipliers = { 1f, 1.4f, 2f, 3f };

    [Header("iframes after taking damage (seconds)")]
    public float iFramesDuration = 0.8f;

    [Header("base speeds — keep in sync with MovementController")]
    public float baseWalkSpeed = 6f;
    public float baseRunSpeed  = 10f;

    public float GetSpeedMultiplier(int currentHp)
    {
        if (speedMultipliers == null || speedMultipliers.Length == 0) return 1f;
        int i = Mathf.Clamp(maxHp - currentHp, 0, speedMultipliers.Length - 1);
        return speedMultipliers[i];
    }
}