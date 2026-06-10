using FishNet;
using FishNet.Managing.Scened;
using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Scenes")]
    [SerializeField] private string _lobbySceneName = "Main";
    [SerializeField] private string _gameSceneName = "SampleScene";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartHost()
    {
        if (InstanceFinder.ServerManager != null && !InstanceFinder.IsServerStarted)
        {
            InstanceFinder.ServerManager.StartConnection();
        }

        if (InstanceFinder.ClientManager != null && !InstanceFinder.IsClientStarted)
        {
            InstanceFinder.ClientManager.StartConnection();
        }
    }

    public void StartClient()
    {
        if (InstanceFinder.ClientManager != null && !InstanceFinder.IsClientStarted)
        {
            InstanceFinder.ClientManager.StartConnection();
        }
    }

    public void LoadLobbyScene()
    {
        LoadFishNetScene(_lobbySceneName);
    }

    public void LoadGameScene()
    {
        LoadFishNetScene(_gameSceneName);
    }

    public void ReturnToMainMenu()
    {
        StopNetwork();

        if (!string.IsNullOrWhiteSpace(_lobbySceneName))
        {
            UnitySceneManager.LoadScene(_lobbySceneName);
        }
    }

    public void StopNetwork()
    {
        if (InstanceFinder.ClientManager != null && InstanceFinder.IsClientStarted)
        {
            InstanceFinder.ClientManager.StopConnection();
        }

        if (InstanceFinder.ServerManager != null && InstanceFinder.IsServerStarted)
        {
            InstanceFinder.ServerManager.StopConnection(true);
        }
    }

    private void LoadFishNetScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[SessionManager] Scene name is empty.");
            return;
        }

        if (InstanceFinder.SceneManager == null || !InstanceFinder.IsServerStarted)
        {
            UnitySceneManager.LoadScene(sceneName);
            return;
        }

        SceneLoadData loadData = new SceneLoadData(sceneName)
        {
            ReplaceScenes = ReplaceOption.All
        };

        InstanceFinder.SceneManager.LoadGlobalScenes(loadData);
    }
}