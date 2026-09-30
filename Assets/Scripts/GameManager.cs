using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton that owns the game state, the round timer, and the score
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private UnityEngine.XR.ARFoundation.ARSession arSession;

    public int Score { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public float TimeRemaining { get; private set; }
    public float TimeSurvived { get; private set; }
    public IGameState CurrentState { get { return currentState; } }

    // the UI listens to these, so GameManager does not need to know about the UI
    public event Action<IGameState> OnStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<float> OnTimeChanged;

    private IGameState currentState;
    private Vector3 worldPosition;
    private float stateStartTime;

    // survives a scene reload so Restart can skip the menu
    private static bool skipMenu = false;

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

        if (skipMenu)
        {
            skipMenu = false;
            ChangeState(new PlacementState(this));
        }
        else
        {
            ChangeState(new MenuState(this));
        }
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
        stateStartTime = Time.time;
        currentState.Enter();
        OnStateChanged?.Invoke(currentState);
    }

    public bool IsPlacing()
    {
        // short delay so the tap that pressed Start does not also place the world
        return currentState is PlacementState && Time.time - stateStartTime > 0.4f;
    }

    public bool IsPlaying()
    {
        return currentState is PlayState;
    }

    // called by the Start button
    public void StartGame()
    {
        ChangeState(new PlacementState(this));
    }

        // called by the Restart button
    public void RestartGame()
    {
        skipMenu = true;
        arSession.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // called by the Main Menu button
    public void ReturnToMenu()
    {
        skipMenu = false;
        arSession.Reset();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
        OnScoreChanged?.Invoke(Score);
        OnTimeChanged?.Invoke(TimeRemaining);
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
            return;
        }

        OnTimeChanged?.Invoke(TimeRemaining);
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
        OnScoreChanged?.Invoke(Score);
    }
}