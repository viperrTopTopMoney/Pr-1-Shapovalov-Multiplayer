using System.Collections;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GamePlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject _playerPrefab;

    public IEnumerator SpawnConnectedPlayers()
    {
        if (!InstanceFinder.IsServerStarted || _playerPrefab == null)
            yield break;

        yield return null;

        int spawnIndex = 0;
        foreach (NetworkConnection connection in InstanceFinder.ServerManager.Clients.Values)
        {
            if (HasPlayer(connection.ClientId))
                continue;

            Transform spawn = GameObject.Find($"PlayerSpawn_{spawnIndex + 1}")?.transform;
            Vector3 position = spawn != null ? spawn.position : Vector3.zero;
            Quaternion rotation = spawn != null ? spawn.rotation : Quaternion.identity;

            NetworkObject player = Instantiate(_playerPrefab, position, rotation);
            InstanceFinder.ServerManager.Spawn(player, connection, gameObject.scene);
            spawnIndex++;
        }
    }

    private static bool HasPlayer(int ownerId)
    {
        PlayerNetwork[] players = Object.FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None);
        foreach (PlayerNetwork player in players)
        {
            if (player != null && player.OwnerId == ownerId)
                return true;
        }

        return false;
    }
}
