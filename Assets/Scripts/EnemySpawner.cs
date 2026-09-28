using System.Collections.Generic;
using UnityEngine;

// Spawns melee and shooter enemies on the plane and clears them when the game ends
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Enemy meleePrefab;
    [SerializeField] private Enemy shooterPrefab;
    [SerializeField] private BulletPool enemyBulletPool;
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxEnemies = 5;
    [Range(0f, 1f)]
    [SerializeField] private float shooterChance = 0.4f;

    private List<Enemy> activeEnemies = new List<Enemy>();
    private Vector3 spawnCenter;
    private bool isSpawning = false;
    private float nextSpawnTime = 0f;

    // called once the world is placed on the plane
    public void BeginSpawning(Vector3 center)
    {
        spawnCenter = center;
        isSpawning = true;
        nextSpawnTime = Time.time + spawnInterval;
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    // destroys every enemy that is still alive
    public void ClearEnemies()
    {
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();
    }

    void Update()
    {
        if (!isSpawning)
            return;

        // drop enemies that were already killed
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
            }
        }

        if (Time.time >= nextSpawnTime && activeEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        // random point on a circle around the placed world, at plane height
        Vector2 circle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 position = new Vector3(spawnCenter.x + circle.x, spawnCenter.y, spawnCenter.z + circle.y);

        activeEnemies.Add(CreateEnemy(position));
    }

    // factory method: decides which enemy type to build
    private Enemy CreateEnemy(Vector3 position)
    {
        if (Random.value < shooterChance)
        {
            Enemy shooter = Instantiate(shooterPrefab, position, Quaternion.identity);
            shooter.GetComponent<ShooterEnemy>().SetBulletPool(enemyBulletPool);
            return shooter;
        }

        return Instantiate(meleePrefab, position, Quaternion.identity);
    }
}