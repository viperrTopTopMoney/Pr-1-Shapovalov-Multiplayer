using FishNet;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class LobbyRuntimeHUD : MonoBehaviour
{
    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != "Main" || !InstanceFinder.IsClientStarted)
            return;

        int connected = InstanceFinder.IsServerStarted && InstanceFinder.ServerManager != null
            ? InstanceFinder.ServerManager.Clients.Count
            : 1;

        GUILayout.BeginArea(new Rect(20f, 20f, 330f, 180f), GUI.skin.box);
        GUILayout.Label("THE LAST LOCK - LOBBY");
        GUILayout.Label($"Players connected: {connected}/2");

        if (InstanceFinder.IsServerStarted)
        {
            if (connected >= 2)
            {
                if (GUILayout.Button("START GAME", GUILayout.Height(42f)))
                    SessionManager.Instance?.LoadGameScene();
            }
            else
            {
                GUILayout.Label("Waiting for second player...");
            }
        }
        else
        {
            GUILayout.Label("Connected to host. Waiting for game start...");
        }

        GUILayout.EndArea();
    }
}
