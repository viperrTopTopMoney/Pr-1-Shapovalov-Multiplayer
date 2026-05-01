using TMPro;
using UnityEngine;
using FishNet; // Добавлено пространство имен FishNet

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;

    public static string PlayerNickname { get; private set; } = "Player";

    public void StartAsHost()
    {
        SaveNickname();
        // В FishNet Хост = запуск сервера + запуск клиента
        InstanceFinder.ServerManager.StartConnection();
        InstanceFinder.ClientManager.StartConnection();
        HideMenu();
    }

    public void StartAsClient()
    {
        SaveNickname();
        // Запуск только клиента
        InstanceFinder.ClientManager.StartConnection();
        HideMenu();
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