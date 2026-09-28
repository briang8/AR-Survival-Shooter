using UnityEngine;

// Base class for every enemy. Health, hit flash and death are shared here.
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [SerializeField] protected int maxHealth = 3;
    [SerializeField] protected int scoreValue = 10;
    [SerializeField] protected float moveSpeed = 1f;
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashTime = 0.1f;

    protected Transform player;

    private int currentHealth;
    private Color originalColor;
    private bool isDead = false;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        originalColor = bodyRenderer.material.color;
        player = Camera.main.transform;
    }

    void Update()
    {
        if (isDead)
            return;

        Act();
    }

    // each enemy type decides how it moves and attacks
    protected abstract void Act();

    public void TakeDamage(int amount)
    {
        if (isDead)
            return;

        currentHealth -= amount;

        // quick red flash so the player sees the hit
        bodyRenderer.material.color = hitColor;
        Invoke(nameof(ResetColor), hitFlashTime);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void ResetColor()
    {
        bodyRenderer.material.color = originalColor;
    }

    protected virtual void Die()
    {
        isDead = true;
        GameManager.Instance.AddScore(scoreValue);
        Destroy(gameObject);
    }

    // turn to face the player, ignoring height
    protected void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    // distance to the player, ignoring height
    protected float DistanceToPlayer()
    {
        Vector3 flat = player.position - transform.position;
        flat.y = 0f;
        return flat.magnitude;
    }
}