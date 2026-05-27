using TMPro;
using UnityEngine;
using FishNet;

public class GameUIManager : MonoBehaviour
{
    [Header("Панели")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _gamePanel;
    [SerializeField] private GameObject _resultsPanel;

    [Header("Тексты")]
    [SerializeField] private TMP_Text _lobbyText;
    [SerializeField] private TMP_Text _timerText;
    [SerializeField] private TMP_Text _resultsText;

    private void Update()
    {
        // Если игра еще не началась по сети или GameManager не создался,
        // держим панели выключенными (чтобы не мешать окну ввода никнейма)
        if (GameManager.Instance == null || !InstanceFinder.IsClientStarted)
        {
            HideAll();
            return;
        }

        // Получаем текущее состояние напрямую из сетевой переменной GameManager
        GameManager.GameState state = GameManager.Instance.CurrentState.Value;

        // Включаем только ту панель, которая соответствует состоянию игры
        _lobbyPanel.SetActive(state == GameManager.GameState.WaitingForPlayers);
        _gamePanel.SetActive(state == GameManager.GameState.InProgress);
        _resultsPanel.SetActive(state == GameManager.GameState.ShowingResults);

        // Обновляем тексты на активных панелях
        if (state == GameManager.GameState.WaitingForPlayers && _lobbyText != null)
        {
            _lobbyText.text = $"Ожидание игроков: {GameManager.Instance.ConnectedPlayers.Value} / {GameManager.Instance.RequiredPlayers}";
        }
        else if (state == GameManager.GameState.InProgress && _timerText != null)
        {
            _timerText.text = $"Времени осталось: {Mathf.CeilToInt(GameManager.Instance.MatchTimer.Value)}";
        }
        else if (state == GameManager.GameState.ShowingResults && _resultsText != null)
        {
            _resultsText.text = "Матч окончен!";
        }
    }

    private void HideAll()
    {
        if (_lobbyPanel.activeSelf) _lobbyPanel.SetActive(false);
        if (_gamePanel.activeSelf) _gamePanel.SetActive(false);
        if (_resultsPanel.activeSelf) _resultsPanel.SetActive(false);
    }
}