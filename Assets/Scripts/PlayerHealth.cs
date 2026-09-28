using System;
using UnityEngine;
using UnityEngine.UI;

// Player health, red damage flash, and the game over trigger
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private Image damageFlash;
    [SerializeField] private float flashFadeSpeed = 2f;

    private int currentHealth;
    private bool isDead = false;

    // other scripts (like the UI later) can listen to these two events
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Update()
    {
        // fade the red flash away
        if (damageFlash.color.a > 0f)
        {
            Color c = damageFlash.color;
            c.a = Mathf.Max(0f, c.a - flashFadeSpeed * Time.deltaTime);
            damageFlash.color = c;
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;
        if (currentHealth < 0)
            currentHealth = 0;

        // show the red flash
        Color c = damageFlash.color;
        c.a = 0.5f;
        damageFlash.color = c;

        Debug.Log("Player health: " + currentHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth == 0)
        {
            isDead = true;
            Debug.Log("Game over");
            OnPlayerDied?.Invoke();
        }
    }
}