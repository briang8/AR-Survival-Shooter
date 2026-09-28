using UnityEngine;

// Singleton that keeps the score and how many enemies were defeated
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int Score { get; private set; }
    public int EnemiesDefeated { get; private set; }

    void Awake()
    {
        // only one GameManager is allowed
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddScore(int amount)
    {
        Score += amount;
        EnemiesDefeated++;
        Debug.Log("Score: " + Score);
    }
}