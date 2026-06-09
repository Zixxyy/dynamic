using UnityEngine.SceneManagement;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int currentHP;
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
        // тут должна быть система хилки я хотел сделать 5 пипов на экране что когда игрок бьет врага 5 раз то может захилится у меня еще нет ударов так что пока что забудем об этом.
    }
    void Die()
    {
        
        if (currentHP <= 0)
        {
            AudioListener.volume = 0f; 
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