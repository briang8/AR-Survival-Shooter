using UnityEngine;

// Singleton that owns the game state, the round timer, and the score
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float roundDuration = 60f;

    public int Score { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public float TimeRemaining { get; private set; }
    public float TimeSurvived { get; private set; }

    private IGameState currentState;
    private Vector3 worldPosition;

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

    void Start()
    {
        playerHealth.OnPlayerDied += HandlePlayerDied;
        ChangeState(new PlacementState(this));
    }

    void OnDestroy()
    {
        playerHealth.OnPlayerDied -= HandlePlayerDied;
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.Tick();
        }
    }

    public void ChangeState(IGameState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;
        currentState.Enter();
    }

    public bool IsPlacing()
    {
        return currentState is PlacementState;
    }

    public bool IsPlaying()
    {
        return currentState is PlayState;
    }

    // called by TapToPlace once the world is placed
    public void OnWorldPlaced(Vector3 position)
    {
        worldPosition = position;
        ChangeState(new PlayState(this));
    }

    public void StartRound()
    {
        Score = 0;
        EnemiesDefeated = 0;
        TimeRemaining = roundDuration;
        TimeSurvived = 0f;
        spawner.BeginSpawning(worldPosition);
    }

    public void UpdateTimer()
    {
        TimeRemaining -= Time.deltaTime;
        TimeSurvived += Time.deltaTime;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            ChangeState(new EndState(this));
        }
    }

    public void StopRound()
    {
        spawner.StopSpawning();
        spawner.ClearEnemies();
    }

    private void HandlePlayerDied()
    {
        if (IsPlaying())
        {
            ChangeState(new EndState(this));
        }
    }

    public void AddScore(int amount)
    {
        Score += amount;
        EnemiesDefeated++;
        Debug.Log("Score: " + Score);
    }
}