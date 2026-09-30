using UnityEngine;

// Melee enemy: walks at the player and hits them when close, with a cooldown
public class MeleeEnemy : Enemy
{
    [SerializeField] private float attackRange = 0.7f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;

    private PlayerHealth playerHealth;
    private float nextAttackTime = 0f;

    protected override void Start()
    {
        base.Start();
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    protected override void Act()
    {
        FacePlayer();

        if (DistanceToPlayer() > attackRange)
        {
            float speedMultiplier = GameManager.Instance.CurrentDifficulty == Difficulty.Hard ? 1.4f : 1f;
            transform.position += transform.forward * moveSpeed * speedMultiplier * Time.deltaTime;
        }
        else if (Time.time >= nextAttackTime)
        {
            playerHealth.TakeDamage(attackDamage);
            SoundManager.Instance.PlayEnemyAttack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }
}