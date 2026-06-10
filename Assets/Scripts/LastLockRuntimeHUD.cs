using FishNet;
using UnityEngine;

public sealed class LastLockRuntimeHUD : MonoBehaviour
{
    private GUIStyle _titleStyle;
    private GUIStyle _bodyStyle;
    private GUIStyle _warningStyle;

    private void OnGUI()
    {
        EnsureStyles();

        GUILayout.BeginArea(new Rect(18f, 18f, 390f, Screen.height - 36f), GUI.skin.box);
        GUILayout.Label("THE LAST LOCK", _titleStyle);

        if (!InstanceFinder.IsClientStarted)
        {
            GUILayout.Label("Start Host or Client in the main menu.", _bodyStyle);
            GUILayout.EndArea();
            return;
        }

        GameManager game = GameManager.Instance;
        if (game == null)
        {
            GUILayout.Label("Waiting for GameManager...", _bodyStyle);
            GUILayout.EndArea();
            return;
        }

        GUILayout.Label($"State: {game.CurrentState.Value}", _bodyStyle);
        GUILayout.Label($"Players: {game.ConnectedPlayers.Value}/{game.RequiredPlayers}", _bodyStyle);
        GUILayout.Label($"Wave: {game.CurrentWave.Value}   Zombies: {game.RemainingZombies.Value}", _bodyStyle);
        GUILayout.Label(game.FormatDoorText(), _bodyStyle);
        GUILayout.Label(game.FormatWindowText(), _bodyStyle);

        string players = game.FormatPlayersSummary();
        if (!string.IsNullOrWhiteSpace(players))
            GUILayout.Label(players, _bodyStyle);

        if (game.CurrentState.Value == MatchFlowState.Preparation)
            GUILayout.Label($"Next wave in {Mathf.CeilToInt(game.StateTimer.Value)}", _bodyStyle);

        if (game.IntruderWarningActive)
        {
            GUILayout.Space(8f);
            GUILayout.Label("INTRUDER INSIDE THE HOUSE", _warningStyle);
            GUILayout.Label($"Defeat in {Mathf.CeilToInt(game.StateTimer.Value)}", _warningStyle);
        }

        if (game.CanHostStartGame && GUILayout.Button("START GAME", GUILayout.Height(42f)))
            game.StartMatchFromLobby();

        GUILayout.FlexibleSpace();
        GUILayout.Label("WASD move | Shift run | Space shoot | Hold E repair/revive | ESC pause", _bodyStyle);
        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (_titleStyle != null)
            return;

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        _bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
        _warningStyle = new GUIStyle(_bodyStyle)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.2f, 0.15f) }
        };
    }
}
