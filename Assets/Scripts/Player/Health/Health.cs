using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private int currentHP;
    [SerializeField] private Animator healthUIAnimator;



    void Start()
    {
        currentHP = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
