using TMPro;
using UnityEngine;
using FishNet;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _resultsPanel;
    [SerializeField] private GameObject _pausePanel;

    [Header("Texts")]
    [SerializeField] private TMP_Text _lobbyText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _resultsText;
    [SerializeField] private TMP_Text _warningText;
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private TMP_Text _zombieCountText;
    [SerializeField] private TMP_Text _doorText;
    [SerializeField] private TMP_Text _windowText;
    [SerializeField] private TMP_Text _playersText;
    [SerializeField] private TMP_Text _intruderTimerText;

    private void Update()
    {
        if (GameManager.Instance == null || !InstanceFinder.IsClientStarted)
        {
            HideAll();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) && _pausePanel != null)
            _pausePanel.SetActive(!_pausePanel.activeSelf);

        MatchFlowState state = GameManager.Instance.CurrentState.Value;

        SetVisible(_lobbyPanel, state == MatchFlowState.Lobby);
        SetVisible(_gamePanel, state == MatchFlowState.InWave || state == MatchFlowState.Preparation || state == MatchFlowState.IntruderWarning);
        SetVisible(_resultsPanel, state == MatchFlowState.Victory || state == MatchFlowState.Defeat);

        if (_lobbyText != null)
            _lobbyText.text = $"Waiting for players: {GameManager.Instance.ConnectedPlayers.Value}/{GameManager.Instance.RequiredPlayers}";

        if (_timerText != null)
            _timerText.text = GameManager.Instance.FormatStateTimer();

        if (_resultsText != null)
            _resultsText.text = GameManager.Instance.FormatResultsText();

        if (_warningText != null)
            _warningText.text = GameManager.Instance.IntruderWarningActive ? "INTRUDER INSIDE THE HOUSE" : string.Empty;

        if (_waveText != null)
            _waveText.text = $"Wave: {GameManager.Instance.CurrentWave.Value}";

        if (_zombieCountText != null)
            _zombieCountText.text = $"Zombies left: {GameManager.Instance.RemainingZombies.Value}";

        if (_doorText != null)
            _doorText.text = GameManager.Instance.FormatDoorText();

        if (_windowText != null)
            _windowText.text = GameManager.Instance.FormatWindowText();

        if (_playersText != null)
            _playersText.text = GameManager.Instance.FormatPlayersSummary();

        if (_intruderTimerText != null)
            _intruderTimerText.text = GameManager.Instance.FormatIntruderTimer();
    }

    public void ContinueGame()
    {
        if (_pausePanel != null)
            _pausePanel.SetActive(false);
    }

    public void ExitToMenu()
    {
        SessionManager.Instance?.ReturnToMainMenu();
    }

    private void HideAll()
    {
        SetVisible(_lobbyPanel, false);
        SetVisible(_gamePanel, false);
        SetVisible(_resultsPanel, false);

        if (_pausePanel != null)
            _pausePanel.SetActive(false);
    }

    private static void SetVisible(GameObject go, bool value)
    {
        if (go != null)
            go.SetActive(value);
    }
}
