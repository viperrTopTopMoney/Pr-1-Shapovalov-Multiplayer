using System.Text;
using FishNet;
using TMPro;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private TMP_Text _playersText;
    [SerializeField] private TMP_Text _statusText;
    [SerializeField] private GameObject _startGameButton;

    private void Update()
    {
        StringBuilder sb = new StringBuilder();
        PlayerNetwork[] players = UnityEngine.Object.FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);

        foreach (PlayerNetwork player in players)
        {
            if (player == null)
                continue;

            sb.AppendLine($"{player.Nickname.Value} [{player.LifeState.Value}] HP:{player.HP.Value}");
        }

        if (_playersText != null)
            _playersText.text = sb.ToString();

        int connectedPlayers = InstanceFinder.ServerManager != null && InstanceFinder.IsServerStarted
            ? InstanceFinder.ServerManager.Clients.Count
            : InstanceFinder.IsClientStarted ? 1 : 0;

        if (_statusText != null)
            _statusText.text = $"Players: {connectedPlayers}/2";

        if (_startGameButton != null)
            _startGameButton.SetActive(InstanceFinder.IsServerStarted && connectedPlayers >= 2);
    }

    public void StartGame()
    {
        if (InstanceFinder.IsServerStarted && InstanceFinder.ServerManager.Clients.Count >= 2)
            SessionManager.Instance?.LoadGameScene();
    }
}
