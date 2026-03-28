using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ConnectionUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _nicknameInput;

    // Сохраняем ник локально до появления сетевого объекта игрока.
    public static string PlayerNickname { get; private set; } = "Player";

    public void StartAsHost()
    {
        SaveNickname();
        // Хост одновременно является сервером и клиентом.
        NetworkManager.Singleton.StartHost();
        HideMenu();
    }

    public void StartAsClient()
    {
        SaveNickname();
        // Клиент только подключается к уже запущенному хосту/серверу.
        NetworkManager.Singleton.StartClient();
        HideMenu();
    }

    private void SaveNickname()
    {
        // Нормализуем ввод, чтобы сервер не получил пустую строку.
        string rawValue = _nicknameInput != null ? _nicknameInput.text : string.Empty;
        PlayerNickname = string.IsNullOrWhiteSpace(rawValue) ? "Player" : rawValue.Trim();
    }


    private void HideMenu()
    {
        gameObject.SetActive(false); // Выключаем весь Canvas, на котором висит скрипт
    }
}