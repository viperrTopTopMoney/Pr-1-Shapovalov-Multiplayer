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
    private Coroutine _respawnCoroutine; // Ссылка для отслеживания корутины респауна

    [Header("Settings")]
    [SerializeField] public Transform[] _spawnPoints;
    [SerializeField] private float _respawnTime = 3f;

    private void Awake()
    {
        _playerView = GetComponentInChildren<PlayerView>();
        
        HP.OnChange += OnHpChanged;
        IsAlive.OnChange += OnIsAliveChanged;
        Nickname.OnChange += OnNicknameChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        RefreshUI();

        if (base.IsOwner)
        {
            StartCoroutine(DelayedNicknameSubmit());
        }
    }

    private IEnumerator DelayedNicknameSubmit()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
    }

    public void TakeDamage(int amount)
    {
        if (!base.IsServerInitialized || !IsAlive.Value) return;
        HP.Value = Mathf.Max(0, HP.Value - amount);
    }

    public void Heal(int amount)
    {
        if (!base.IsServerInitialized || !IsAlive.Value) return;
        HP.Value = Mathf.Min(100, HP.Value + amount);
    }

    [ServerRpc]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{base.OwnerId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    // --- МЕТОД ПОЛНОГО СБРОСА ДЛЯ НОВОГО РАУНДА ---
    public void ResetRoundState()
    {
        if (!base.IsServerInitialized) return;

        // Если игрок в этот момент лежал мертвым и ждал респауна — отменяем старый таймер респауна
        if (_respawnCoroutine != null)
        {
            StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = null;
        }

        // Восстанавливаем сетевые переменные здоровья
        HP.Value = 100;
        IsAlive.Value = true;

        // Телепортируем на точку спауна
        if (TryGetComponent(out CharacterController cc)) cc.enabled = false;
        
        Vector3 targetPosition = new Vector3(16.87f, 16.87f, -1.38f);
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, _spawnPoints.Length);
            if (_spawnPoints[idx] != null) targetPosition = _spawnPoints[idx].position;
        }
        transform.position = targetPosition;
        
        if (cc != null) cc.enabled = true;

        // Обновляем интерфейс
        RefreshUI();

        // --- ТУТ ТВОЙ СБРОС ПАТРОНОВ ---
        // Так как патроны хранятся в твоем скрипте оружия/стрельбы, обратись к нему здесь. Примеры:
        // 1. Если скрипт оружия висит тут же: 
        //    if(TryGetComponent(out YourWeaponScript weapon)) { weapon.RestoreAmmo(); }
        // 2. Если патроны прямо в этом скрипте (просто раскомментируй и впиши свои переменные):
        //    YourAmmoSyncVar.Value = MaxAmmoConst;
    }

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

    private void OnHpChanged(int prev, int next, bool asServer)
    {
        UpdateHPLocal(next);

        if (asServer)
        {
            if (next <= 0 && IsAlive.Value)
            {
                IsAlive.Value = false;
                // Запоминаем запущенную корутину респауна
                _respawnCoroutine = StartCoroutine(RespawnRoutine());
            }
        }
    }

    private void OnIsAliveChanged(bool prev, bool next, bool asServer)
    {
        UpdateVisibilityLocal(next);
    }

    private void OnNicknameChanged(string prev, string next, bool asServer)
    {
        UpdateNicknameLocal(next);
    }

    public void RefreshUI()
    {
        UpdateHPLocal(HP.Value);
        UpdateNicknameLocal(Nickname.Value);
        UpdateVisibilityLocal(IsAlive.Value);
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnTime);

        Vector3 targetPosition = new Vector3(16.87f, 16.87f, -1.38f);
        if (_spawnPoints != null && _spawnPoints.Length > 0)
        {
            int idx = Random.Range(0, _spawnPoints.Length);
            if (_spawnPoints[idx] != null) targetPosition = _spawnPoints[idx].position;
        }

        if (TryGetComponent(out CharacterController cc)) cc.enabled = false;
        transform.position = targetPosition;
        yield return new WaitForEndOfFrame();
        if (cc != null) cc.enabled = true;

        HP.Value = 100;
        IsAlive.Value = true;
        
        RefreshUI();
        _respawnCoroutine = null; // Очищаем ссылку по завершению
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