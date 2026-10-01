using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    private Image healthBarFill;
    private RectTransform healthBarFillRect;
    private float healthBarFillWidth;

    public void OpenExclusivePopup(GameObject popup)
    {
        SetStatePanelsActive(false);
        if (popup != null)
        {
            popup.SetActive(true);
        }
    }

    public void ClosePopup(GameObject popup)
    {
        if (popup != null)
        {
            popup.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            HandleStateChanged(GameManager.Instance.CurrentState);
        }
    }

    void Start()
    {
        GameManager manager = GameManager.Instance;

        if (manager == null)
        {
            return;
        }

        manager.OnStateChanged += HandleStateChanged;
        manager.OnScoreChanged += UpdateScore;
        manager.OnTimeChanged += UpdateTime;
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealth;
        }

        // covers the case where the state was set before this script started
        HandleStateChanged(manager.CurrentState);

        ApplyTheme();
    }

    private void ApplyTheme()
    {
        Sprite panelSprite = LoadRuntimeSprite("RpgUI/panel_brown");
        Sprite buttonSprite = LoadRuntimeSprite("RpgUI/buttonLong_beige");
        Sprite pressedSprite = LoadRuntimeSprite("RpgUI/buttonLong_beige_pressed");
        Sprite closeSprite = LoadRuntimeSprite("RpgUI/buttonSquare_grey");
        Sprite closePressedSprite = LoadRuntimeSprite("RpgUI/buttonSquare_grey_pressed");

        ApplyPanelStyle(menuPanel, panelSprite);
        ApplyPanelStyle(placementPanel, panelSprite);
        ApplyPanelStyle(endPanel, panelSprite);
        ApplyHudStyle(panelSprite, buttonSprite, pressedSprite);

        StyleButtons(menuPanel, buttonSprite, pressedSprite);
        StyleButtons(placementPanel, buttonSprite, pressedSprite);
        StyleButtons(endPanel, buttonSprite, pressedSprite);
        AddQuitButton(buttonSprite, pressedSprite);

        LeaderboardUI leaderboardUI = FindAnyObjectByType<LeaderboardUI>(FindObjectsInactive.Include);
        GameObject leaderboardPanel = leaderboardUI != null ? leaderboardUI.Panel : null;
        if (leaderboardPanel != null)
        {
            ApplyPanelStyle(leaderboardPanel, panelSprite);
            foreach (Button button in leaderboardPanel.GetComponentsInChildren<Button>(true))
            {
                bool isCloseButton = button.name == "CloseButton";
                ApplyButtonStyle(button, isCloseButton ? closeSprite : buttonSprite,
                    isCloseButton ? closePressedSprite : pressedSprite);
            }
        }
    }

    private static void StyleButtons(GameObject panel, Sprite buttonSprite, Sprite pressedSprite)
    {
        if (panel == null)
        {
            return;
        }

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            ApplyButtonStyle(button, buttonSprite, pressedSprite);
        }
    }

    private void ApplyHudStyle(Sprite panelSprite, Sprite buttonSprite, Sprite pressedSprite)
    {
        if (hudPanel == null)
        {
            return;
        }

        StyleButtons(hudPanel, buttonSprite, pressedSprite);
        ApplyMetricPlate(healthText, panelSprite, true);
        ApplyMetricPlate(scoreText, panelSprite, false);
        ApplyMetricPlate(timeText, panelSprite, false);
        CreateHealthBar(panelSprite);
    }

    private void ApplyMetricPlate(TMP_Text text, Sprite panelSprite, bool includeBarSpace)
    {
        if (text == null || panelSprite == null || text.transform.parent == null)
        {
            return;
        }

        text.fontSize = 32f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.96f, 0.9f, 0.72f);

        string plateName = text.gameObject.name + "Plate";
        if (text.transform.parent.Find(plateName) != null)
        {
            return;
        }

        GameObject plate = new GameObject(plateName, typeof(RectTransform), typeof(Image));
        plate.transform.SetParent(text.transform.parent, false);
        RectTransform plateRect = plate.GetComponent<RectTransform>();
        RectTransform textRect = text.rectTransform;
        plateRect.anchorMin = textRect.anchorMin;
        plateRect.anchorMax = textRect.anchorMax;
        plateRect.pivot = textRect.pivot;
        plateRect.anchoredPosition = textRect.anchoredPosition;
        plateRect.sizeDelta = textRect.sizeDelta + new Vector2(24f, includeBarSpace ? 46f : 14f);

        Image plateImage = plate.GetComponent<Image>();
        plateImage.sprite = panelSprite;
        plateImage.type = Image.Type.Sliced;
        plateImage.color = new Color(1f, 1f, 1f, 0.92f);
        plate.transform.SetAsFirstSibling();
    }

    private void CreateHealthBar(Sprite fallbackSprite)
    {
        if (hudPanel == null || healthText == null || healthBarFill != null || fallbackSprite == null)
        {
            return;
        }

        Sprite backgroundLeft = LoadRuntimeSprite("RpgUI/barBack_horizontalLeft") ?? fallbackSprite;
        Sprite backgroundMid = LoadRuntimeSprite("RpgUI/barBack_horizontalMid") ?? fallbackSprite;
        Sprite backgroundRight = LoadRuntimeSprite("RpgUI/barBack_horizontalRight") ?? fallbackSprite;
        Sprite fillSprite = LoadRuntimeSprite("RpgUI/barRed_horizontalLeft") ?? fallbackSprite;
        GameObject barObject = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
        barObject.transform.SetParent(hudPanel.transform, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        RectTransform textRect = healthText.rectTransform;
        barRect.anchorMin = textRect.anchorMin;
        barRect.anchorMax = textRect.anchorMax;
        barRect.pivot = textRect.pivot;
        barRect.anchoredPosition = textRect.anchoredPosition + new Vector2(0f, -88f);
        barRect.sizeDelta = new Vector2(500f, 34f);

        Image background = barObject.GetComponent<Image>();
        background.sprite = backgroundMid;
        background.type = Image.Type.Sliced;
        background.color = new Color(0.18f, 0.12f, 0.08f, 0.9f);
        CreateBarCap(barObject.transform, "Left", backgroundLeft, true);
        CreateBarCap(barObject.transform, "Right", backgroundRight, false);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(barObject.transform, false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(4f, 0f);
        fillRect.sizeDelta = new Vector2(barRect.sizeDelta.x - 8f, 26f);
        healthBarFillRect = fillRect;
        healthBarFillWidth = fillRect.sizeDelta.x;

        healthBarFill = fillObject.GetComponent<Image>();
        healthBarFill.sprite = fillSprite;
        healthBarFill.type = Image.Type.Sliced;
        healthBarFill.color = new Color(0.9f, 0.16f, 0.08f);
        barObject.transform.SetAsLastSibling();
    }

    private static void CreateBarCap(Transform parent, string name, Sprite sprite, bool left)
    {
        GameObject capObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        capObject.transform.SetParent(parent, false);
        RectTransform capRect = capObject.GetComponent<RectTransform>();
        capRect.anchorMin = new Vector2(left ? 0f : 1f, 0.5f);
        capRect.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
        capRect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        capRect.anchoredPosition = Vector2.zero;
        capRect.sizeDelta = new Vector2(18f, 34f);

        Image capImage = capObject.GetComponent<Image>();
        capImage.sprite = sprite;
        capImage.type = Image.Type.Simple;
        capImage.preserveAspect = true;
    }

    private void AddQuitButton(Sprite buttonSprite, Sprite pressedSprite)
    {
        if (endPanel == null || buttonSprite == null || endPanel.transform.Find("QuitButton") != null)
        {
            return;
        }

        GameObject quitObject = new GameObject("QuitButton", typeof(RectTransform), typeof(Image), typeof(Button));
        quitObject.transform.SetParent(endPanel.transform, false);
        RectTransform quitRect = quitObject.GetComponent<RectTransform>();
        quitRect.anchorMin = new Vector2(0.5f, 0.5f);
        quitRect.anchorMax = new Vector2(0.5f, 0.5f);
        quitRect.anchoredPosition = new Vector2(0f, -360f);
        quitRect.sizeDelta = new Vector2(260f, 78f);

        Button quitButton = quitObject.GetComponent<Button>();
        quitButton.onClick.AddListener(QuitGame);
        ApplyButtonStyle(quitButton, buttonSprite, pressedSprite);

        GameObject labelObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(quitObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "QUIT";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 26f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.18f, 0.14f, 0.1f);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static Sprite LoadRuntimeSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect,
            new Vector4(8f, 8f, 8f, 8f));
    }

    private static void ApplyPanelStyle(GameObject panel, Sprite sprite)
    {
        if (panel == null || sprite == null)
        {
            return;
        }

        Image image = panel.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.94f);
        }
    }

    private static void ApplyButtonStyle(Button button, Sprite normalSprite, Sprite pressedSprite)
    {
        if (button == null || normalSprite == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = normalSprite;
            image.type = Image.Type.Sliced;
        }

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.sizeDelta = button.name == "CloseButton"
                ? new Vector2(96f, 72f)
                : new Vector2(260f, 78f);
        }

        foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
        {
            text.fontSize = Mathf.Min(text.fontSize, 26f);
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 1.5f;
        }

        SpriteState spriteState = button.spriteState;
        spriteState.pressedSprite = pressedSprite;
        spriteState.highlightedSprite = normalSprite;
        button.spriteState = spriteState;
        button.transition = Selectable.Transition.SpriteSwap;
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
        SetStatePanelsActive(state is MenuState, state is PlacementState, state is PlayState, state is EndState);

        if (state is EndState)
        {
            ShowEndStats();
        }
    }

    private void SetStatePanelsActive(bool allActive)
    {
        SetStatePanelsActive(allActive, allActive, allActive, allActive);
    }

    private void SetStatePanelsActive(bool menuActive, bool placementActive, bool hudActive, bool endActive)
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(menuActive);
        }

        if (placementPanel != null)
        {
            placementPanel.SetActive(placementActive);
        }

        if (hudPanel != null)
        {
            hudPanel.SetActive(hudActive);
        }

        if (endPanel != null)
        {
            endPanel.SetActive(endActive);
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

        if (healthBarFillRect != null)
        {
            float healthRatio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            healthBarFillRect.sizeDelta = new Vector2(healthBarFillWidth * healthRatio, 26f);
        }
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