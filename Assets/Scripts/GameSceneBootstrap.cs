using System.Collections;
using FishNet;
using UnityEngine;

public sealed class GameSceneBootstrap : MonoBehaviour
{
    [SerializeField] private GamePlayerSpawner _playerSpawner;

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        if (!InstanceFinder.IsServerStarted)
            yield break;

        yield return new WaitForSecondsRealtime(0.75f);

        if (_playerSpawner != null)
            yield return _playerSpawner.SpawnConnectedPlayers();

        yield return null;
        GameManager.Instance?.StartMatchFromLobby();
    }
}
