using UnityEngine;

// Lets the player shoot bullets from the camera using the bullet pool
public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private float fireCooldown = 0.25f;

    private float nextFireTime = 0f;

    // called by the fire button
    public void Shoot()
    {
        // shooting only works while the round is running
        if (!GameManager.Instance.IsPlaying())
            return;
            
        if (Time.time < nextFireTime)
            return;

        // start a little in front of the camera so the bullet is visible
        Vector3 spawnPosition = transform.position + transform.forward * 0.2f;
        SoundManager.Instance.PlayPlayerShoot();

        bulletPool.GetBullet(spawnPosition, transform.rotation);
        nextFireTime = Time.time + fireCooldown;
    }
}