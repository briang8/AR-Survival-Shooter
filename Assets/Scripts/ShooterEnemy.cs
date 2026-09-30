using UnityEngine;

// Shooter enemy: walks toward the player, stops at shooting distance, and fires bullets
public class ShooterEnemy : Enemy
{
    [SerializeField] private float shootingDistance = 3f;
    [SerializeField] private float fireCooldown = 2f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletPool bulletPool;

    private float nextFireTime = 0f;

    // the spawner will call this later to give each enemy its bullet pool
    public void SetBulletPool(BulletPool pool)
    {
        bulletPool = pool;
    }

    protected override void Act()
    {
        FacePlayer();

        if (DistanceToPlayer() > shootingDistance)
        {
            float speedMultiplier = GameManager.Instance.CurrentDifficulty == Difficulty.Hard ? 1.4f : 1f;
            transform.position += transform.forward * moveSpeed * speedMultiplier * Time.deltaTime;
        }
        else if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireCooldown;
        }
    }

    private void Shoot()
    {
        // aim the bullet straight at the player
        Vector3 direction = (player.position - firePoint.position).normalized;
        SoundManager.Instance.PlayEnemyShoot();
        bulletPool.GetBullet(firePoint.position, Quaternion.LookRotation(direction));
    }
}