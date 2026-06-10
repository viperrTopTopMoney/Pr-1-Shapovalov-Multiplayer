using FishNet;
using TMPro;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;

    public static string PlayerNickname { get; private set; } = "Player";

    public void StartAsHost()
    {
        SaveNickname();
        if (SessionManager.Instance != null)
            SessionManager.Instance.StartHost();
        else
        {
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();
        }
        HideMenu();
    }

    public void StartAsClient()
    {
        SaveNickname();
        if (SessionManager.Instance != null)
            SessionManager.Instance.StartClient();
        else
            InstanceFinder.ClientManager.StartConnection();
        HideMenu();
    }

    public void OpenSettings()
    {
        Debug.Log("Settings panel is not wired yet.");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private void SaveNickname()
    {
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }

    private void HideMenu()
    {
        gameObject.SetActive(false);
    }
}
