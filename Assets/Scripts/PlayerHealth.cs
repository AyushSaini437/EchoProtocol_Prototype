using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    // Public getter for UI/HUD access
    public int CurrentHealth => currentHealth;

    [Header("Regeneration")]
    public float timeBeforeHeal = 5f; // Seconds to hide before healing starts
    public int healAmountPerSecond = 20;
    
    private float lastDamageTime = -10f;
    private float nextHealTick = 0f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        // Check if we are hurt AND enough time has passed since the last bullet hit us
        if (currentHealth < maxHealth && Time.time >= lastDamageTime + timeBeforeHeal)
        {
            if (Time.time >= nextHealTick)
            {
                currentHealth += healAmountPerSecond;
                if (currentHealth > maxHealth) currentHealth = maxHealth;
                
                nextHealTick = Time.time + 1f; // Wait 1 second before the next heal tick
                Debug.Log("Player recovering... Health: " + currentHealth);
            }
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        lastDamageTime = Time.time; // Reset the regen timer!
        
        Debug.Log("Player hit! Health: " + currentHealth);
        
        if (currentHealth <= 0)
        {
            Debug.Log("Player Died.");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerDied();
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}