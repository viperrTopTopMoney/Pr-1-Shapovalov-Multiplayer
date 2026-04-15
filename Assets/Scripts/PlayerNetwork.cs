using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components; 
using UnityEngine;
using System.Collections;

public class PlayerNetwork : NetworkBehaviour
{
    [Header("Network Variables")]
    public NetworkVariable<int> HP = new(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsAlive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<FixedString32Bytes> Nickname = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Settings")]
    [SerializeField] public Transform[] _spawnPoints;
    [SerializeField] private float _respawnTime = 3f;

    public override void OnNetworkSpawn()
    {
        HP.OnValueChanged += OnHpChanged;
        IsAlive.OnValueChanged += OnIsAliveChanged;
        
        if (IsOwner)
        {
            SubmitNicknameServerRpc(ConnectionUI.PlayerNickname);
        }

        ToggleVisuals(IsAlive.Value);
    }

    public override void OnNetworkDespawn()
    {
        HP.OnValueChanged -= OnHpChanged;
        IsAlive.OnValueChanged -= OnIsAliveChanged;
    }

    public void TakeDamage(int amount)
    {
        if (!IsServer || !IsAlive.Value) return;
        HP.Value = Mathf.Max(0, HP.Value - amount);
    }

    public void Heal(int amount)
    {
        if (!IsServer || !IsAlive.Value) return;
        HP.Value = Mathf.Min(100, HP.Value + amount);
    }

    [ServerRpc]
    private void SubmitNicknameServerRpc(string nickname)
    {
        string safeValue = string.IsNullOrWhiteSpace(nickname) ? $"Player_{OwnerClientId}" : nickname.Trim();
        Nickname.Value = safeValue;
    }

    private void OnHpChanged(int prev, int next)
    {
        if (!IsServer) return;

        if (next <= 0 && IsAlive.Value)
        {
            IsAlive.Value = false;
            Debug.Log("Сервер: Игрок погиб, запускаю респавн...");
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
{
    Debug.Log("[SERVER] Игрок погиб. Ожидание респавна...");
    yield return new WaitForSeconds(_respawnTime);

    // 1. Прямая проверка: если массив пуст, ищем точки заново ПРЯМО СЕЙЧАС
    if (_spawnPoints == null || _spawnPoints.Length == 0)
    {
        GameObject[] respawnObjs = GameObject.FindGameObjectsWithTag("Respawn");
        
        if (respawnObjs.Length > 0)
        {
            _spawnPoints = new Transform[respawnObjs.Length];
            for (int i = 0; i < respawnObjs.Length; i++)
            {
                _spawnPoints[i] = respawnObjs[i].transform;
            }
            Debug.Log($"[SERVER] Найдено точек респавна: {respawnObjs.Length}");
        }
    }
    

    // 2. Выбор позиции с защитой от "пустого списка"
    Vector3 targetPosition = Vector3.zero;
    
    if (_spawnPoints != null && _spawnPoints.Length > 0)
    {
        int idx = Random.Range(0, _spawnPoints.Length);
        
        // Проверка: не удалилась ли точка со сцены?
        if (_spawnPoints[idx] != null)
        {
            targetPosition = _spawnPoints[idx].position;
        }
        else
        {
            // Если точка была, но пропала (например, сцена сменилась)
            Debug.LogWarning("[SERVER] Выбранная точка респавна null! Сбрасываю массив.");
            _spawnPoints = null; 
        }
    }
    else
    {
        Debug.LogError("[SERVER] Точки с тегом 'Respawn' не найдены! Респавн в (0,0,0)");
    }

    // 3. Перемещение (Сначала позиция, потом включение визуала)
    if (TryGetComponent(out NetworkTransform nt))
    {
        nt.Teleport(targetPosition, transform.rotation, transform.localScale);
    }
    else
    {
        transform.position = targetPosition;
    }

    // Ждем один кадр, чтобы позиция точно применилась в физике
    yield return null;

    HP.Value = 100;
    IsAlive.Value = true;
    
    Debug.Log($"[SERVER] Респавн завершен в {targetPosition}");


}
    // --- НЕДОСТАЮЩИЕ МЕТОДЫ ДЛЯ ИСПРАВЛЕНИЯ ОШИБОК ---

    private void OnIsAliveChanged(bool previousValue, bool newValue)
    {
        ToggleVisuals(newValue);
    }

    private void ToggleVisuals(bool isVisible)
{
    // Рендереры и Канвас у тебя уже есть
    foreach (var renderer in GetComponentsInChildren<Renderer>())
    {
        renderer.enabled = isVisible;
    }

    var canvas = GetComponentInChildren<Canvas>();
    if (canvas != null) canvas.enabled = isVisible;

    // НОВОЕ: Если есть CharacterController, его нужно выключать на время смерти,
    // иначе он может "сопротивляться" телепортации или мешать другим игрокам.
    if (TryGetComponent(out CharacterController cc))
    {
        cc.enabled = isVisible;
    }
}
}