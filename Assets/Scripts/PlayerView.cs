using Unity.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField] private PlayerNetwork _playerNetwork;
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private GameObject _uiRoot; // Весь объект с UI (над головой)

    public override void OnNetworkSpawn()
    {
        _playerNetwork.Nickname.OnValueChanged += OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged += OnHpChanged;
        
        // Подписываемся на состояние жизни
        _playerNetwork.IsAlive.OnValueChanged += OnAliveChanged;

        OnNicknameChanged(default, _playerNetwork.Nickname.Value);
        OnHpChanged(0, _playerNetwork.HP.Value);
        OnAliveChanged(true, _playerNetwork.IsAlive.Value);
    }

    public override void OnNetworkDespawn()
    {
        _playerNetwork.Nickname.OnValueChanged -= OnNicknameChanged;
        _playerNetwork.HP.OnValueChanged -= OnHpChanged;
        _playerNetwork.IsAlive.OnValueChanged -= OnAliveChanged;
    }

    private void OnAliveChanged(bool oldVal, bool newVal)
    {
        // Если игрок мертв — прячем весь UI полностью
        if (_uiRoot != null) _uiRoot.SetActive(newVal);
    }

    private void OnNicknameChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        _nicknameText.text = newValue.ToString();
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        _hpText.text = $"HP: {newValue}";
    }
}

