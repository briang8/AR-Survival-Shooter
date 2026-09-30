using UnityEngine;

// Singleton that plays one-off sound effects through a shared AudioSource
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip playerShootClip;
    [SerializeField] private AudioClip playerDeathClip;
    [SerializeField] private AudioClip enemySpawnClip;
    [SerializeField] private AudioClip enemyShootClip;
    [SerializeField] private AudioClip enemyAttackClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayPlayerShoot()
    {
        audioSource.PlayOneShot(playerShootClip);
    }

    public void PlayPlayerDeath()
    {
        audioSource.PlayOneShot(playerDeathClip);
    }

    public void PlayEnemySpawn()
    {
        audioSource.PlayOneShot(enemySpawnClip);
    }

    public void PlayEnemyShoot()
    {
        audioSource.PlayOneShot(enemyShootClip);
    }

    public void PlayEnemyAttack()
    {
        audioSource.PlayOneShot(enemyAttackClip);
    }
}