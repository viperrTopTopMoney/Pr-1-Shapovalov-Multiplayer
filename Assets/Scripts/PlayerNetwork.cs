using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Network Variables")]
    public readonly SyncVar<int> HP = new SyncVar<int>(100);
    public readonly SyncVar<bool> IsAlive = new SyncVar<bool>(true);
    public readonly SyncVar<string> Nickname = new SyncVar<string>("");

    private PlayerView _playerView;

    [Header("Settings")]
    [SerializeField] public Transform[] _spawnPoints;
    [SerializeField] private float _respawnTime = 3f;

    private void Awake()
    {
        _playerView = GetComponentInChildren<PlayerView>();
        
        // Подписки на будущее (когда значения будут меняться в процессе игры)
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;
        Nickname.OnChange += OnNicknameChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // СТРОГО: Принудительно обновляем UI текущими значениями из сети сразу при старте
        // Это закроет проблему, когда OnChange не срабатывает для первого игрока
        RefreshUI();

        // Если это наш локальный персонаж — отправляем свой ник серверу
        if (base.IsOwner)
        {
            StartCoroutine(DelayedNicknameSubmit());
        }
    }

    private IEnumerator DelayedNicknameSubmit()
    {
        // Ждем пару кадров, чтобы объект полностью "устаканился" в сети
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        string myNick = ConnectionUI.PlayerNickname;
        SubmitNicknameServerRpc(myNick);
    }

    // Удаляем Update() полностью — он нам больше не нужен

    public void TakeDamage(int amount)
    {
        if (!base.IsServerInitialized || !IsAlive.Value) return;
        HP.Value = Mathf.Max(0, HP.Value - amount);
    }

    public void Heal(int amount)
        {
            // Логика лечения работает только на сервере
            if (!base.IsServerInitialized || !IsAlive.Value) return;
            
            // Ограничиваем хп максимумом в 100 единиц
            HP.Value = Mathf.Min(100, HP.Value + amount);
            
            Debug.Log($"[Server] Игрок вылечен на {amount}. Текущее HP: {HP.Value}");
        }
        [ServerRpc]
        private void SubmitNicknameServerRpc(string nickname)
        {
            string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{base.OwnerId}" : nickname.Trim();
            Nickname.Value = safeValue;
        }

    // --- МЕТОДЫ ОБНОВЛЕНИЯ (БЕЗ ИЗМЕНЕНИЙ, они рабочие) ---

    private void UpdateHPLocal(int value)
    {
        if (_playerView != null) _playerView.UpdateHP(value);
        else 
        {
            var texts = GetComponentsInChildren<TMP_Text>();
            foreach (var txt in texts) if (txt.gameObject.name == "HpText") { txt.text = $"HP: {value}"; break; }
        }
    }

    private void UpdateNicknameLocal(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        if (_playerView != null) _playerView.UpdateNickname(name);
        else 
        {
            var texts = GetComponentsInChildren<TMP_Text>();
            foreach (var txt in texts) if (txt.gameObject.name == "NicknameText") { txt.text = name; break; }
        }
    }

    private void UpdateVisibilityLocal(bool isAlive)
    {
        if (_playerView != null) _playerView.UpdateVisibility(isAlive);
        else ToggleVisuals(isAlive);
    }

    private void OnHpChanged(int prev, int next, bool asServer) => UpdateHPLocal(next);
    private void OnIsAliveChanged(bool prev, bool next, bool asServer) => UpdateVisibilityLocal(next);
    private void OnNicknameChanged(string prev, string next, bool asServer) => UpdateNicknameLocal(next);

    public void RefreshUI()
    {
        UpdateHPLocal(HP.Value);
        UpdateNicknameLocal(Nickname.Value);
        UpdateVisibilityLocal(IsAlive.Value);
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnTime);
        // ... (твой код респавна без изменений)
        HP.Value = 100;
        IsAlive.Value = true;
        RefreshUI();
    }

    private void ToggleVisuals(bool isVisible)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
                renderer.enabled = isVisible;
        }
        var canvas = GetComponentInChildren<Canvas>();
        if (canvas != null) foreach (var ui in canvas.GetComponentsInChildren<Graphic>()) ui.enabled = isVisible;
        if (TryGetComponent(out CharacterController cc)) cc.enabled = isVisible;
    }
}