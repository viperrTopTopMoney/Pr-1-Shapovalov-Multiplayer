using FishNet;
using FishNet.Connection;
using FishNet.Transporting;
using UnityEngine;

public class ServerAutoStart : MonoBehaviour
{
    private void Start()
    {
        // Проверяем, запущена ли игра без графического интерфейса
        if (Application.isBatchMode)
        {
            Debug.Log("[Server] Обнаружен Headless-режим. Попытка автоматического запуска...");

            if (InstanceFinder.NetworkManager == null)
            {
                Debug.LogError("[Server] ОШИБКА: NetworkManager еще не инициализирован!");
                return;
            }

            // Подписываемся на события сервера (кто зашел / кто вышел)
            InstanceFinder.ServerManager.OnRemoteConnectionState += OnClientConnectionChanged;

            // Запускаем сервер
            bool isServerStarted = InstanceFinder.ServerManager.StartConnection();

            if (isServerStarted)
            {
                Debug.Log("[Server] FishNet успешно принял команду на старт сервера и слушает игроков!");
            }
            else
            {
                Debug.LogError("[Server] FishNet ОТКЛОНИЛ запуск сервера.");
            }
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении объекта
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnRemoteConnectionState -= OnClientConnectionChanged;
        }
    }

    // Метод срабатывает автоматически при изменении статуса клиента
    private void OnClientConnectionChanged(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
        // В FishNet вместо Connected используется Started, а вместо Disconnected — Stopped
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // Игрок успешно зашел на сервер
            Debug.Log($"[Server Host] ИГРОК ПОДКЛЮЧИЛСЯ! ID Клиента: {connection.ClientId}");
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // Игрок вышел
            Debug.Log($"[Server Host] ИГРОК ОТКЛЮЧИЛСЯ! ID Клиента: {connection.ClientId}");
        }
    }
}