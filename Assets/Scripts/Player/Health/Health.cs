using UnityEngine.SceneManagement;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] public int currentHP;
    [SerializeField] private Animator healthUI;


    [Header("FadeIn")]
    [SerializeField] private CanvasGroup blackScreenGroup;
    [SerializeField] private float fadeSpeed = 1f; 



    
        void Start()
    {
        currentHP = maxHealth;

        Time.timeScale = 1f;
        AudioListener.volume = 1f;

        StartCoroutine(FadeInScreen());
    }
    

    
    void Update()
    {
        
    }
    public void Damage()
    {
        currentHP--;

        if(currentHP > 0)
        {   
            healthUI.SetTrigger("Damaged");
        }
        else
        {
            Die();
        }
        
    }
    public void Heal()
    {
        // not done health system. Have to make 5 pips per player punches, when 5 pips is fully loaded you can heal
        currentHP++;
        if(currentHP > 1)
        {   
            healthUI.SetTrigger("Heal");
        } else
        {
            Debug.Log("cantHeal");
        }
    }
    void Die()
    {
        
        if (currentHP <= 0)
        {
            AudioListener.volume = 0f; // not done i have to make a death sound and <---- here its impossible now
            Time.timeScale = 0f;
            blackScreenGroup.alpha = 1f;
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
        }
    }
    System.Collections.IEnumerator FadeInScreen()
    {
        while (blackScreenGroup.alpha > 0)
        {
            blackScreenGroup.alpha -= Time.deltaTime * fadeSpeed; 
            yield return null; 
        }
        blackScreenGroup.alpha = 0f; 
    }
}