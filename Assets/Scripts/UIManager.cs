using UnityEngine;
using TMPro;

// Shows and hides the UI panels and keeps the texts up to date
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject placementPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject endPanel;

    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;

    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text defeatedText;
    [SerializeField] private TMP_Text survivedText;

    [SerializeField] private PlayerHealth playerHealth;

    private int lastShownSecond = -1;

    void Start()
    {
        GameManager manager = GameManager.Instance;

        manager.OnStateChanged += HandleStateChanged;
        manager.OnScoreChanged += UpdateScore;
        manager.OnTimeChanged += UpdateTime;
        playerHealth.OnHealthChanged += UpdateHealth;

        // covers the case where the state was set before this script started
        HandleStateChanged(manager.CurrentState);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnTimeChanged -= UpdateTime;
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealth;
        }
    }

    private void HandleStateChanged(IGameState state)
    {
        menuPanel.SetActive(state is MenuState);
        placementPanel.SetActive(state is PlacementState);
        hudPanel.SetActive(state is PlayState);
        endPanel.SetActive(state is EndState);

        if (state is EndState)
        {
            ShowEndStats();
        }
    }

    private void ShowEndStats()
    {
        GameManager manager = GameManager.Instance;
        finalScoreText.text = "Final Score: " + manager.Score;
        defeatedText.text = "Enemies Defeated: " + manager.EnemiesDefeated;
        survivedText.text = "Time Survived: " + manager.TimeSurvived.ToString("F1") + "s";
    }

    private void UpdateHealth(int current, int max)
    {
        healthText.text = "Health: " + current;
    }

    private void UpdateScore(int score)
    {
        scoreText.text = "Score: " + score;
    }

    private void UpdateTime(float timeRemaining)
    {
        // only redraw when the whole second changes
        int seconds = Mathf.CeilToInt(timeRemaining);

        if (seconds != lastShownSecond)
        {
            lastShownSecond = seconds;
            timeText.text = "Time: " + seconds;
        }
    }
}