using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour
{
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private TMP_Text _stateText;
    [SerializeField] private GameObject _uiRoot;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Animator _animator;

    private void Awake()
    {
        if (_canvasGroup == null && _uiRoot != null)
            _canvasGroup = _uiRoot.GetComponent<CanvasGroup>();

        if (_canvasGroup == null && _uiRoot != null)
            _canvasGroup = _uiRoot.AddComponent<CanvasGroup>();
    }

    public void UpdateNickname(string name)
    {
        if (_nicknameText != null && !string.IsNullOrWhiteSpace(name))
            _nicknameText.text = name;
    }

    public void UpdateHP(int hp)
    {
        if (_hpText != null)
            _hpText.text = $"HP: {hp}";
    }

    public void UpdateLifeState(PlayerLifeState state)
    {
        if (_stateText != null)
            _stateText.text = state.ToString().ToUpperInvariant();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = state == PlayerLifeState.Dead ? 0.7f : 1f;
            _canvasGroup.interactable = state == PlayerLifeState.Alive;
            _canvasGroup.blocksRaycasts = state == PlayerLifeState.Alive;
        }
        else if (_uiRoot != null)
        {
            _uiRoot.SetActive(state != PlayerLifeState.Dead);
        }

        if (_animator != null)
        {
            _animator.SetBool(PlayerAnimationHooks.IsDowned, state == PlayerLifeState.Downed);
            _animator.SetBool(PlayerAnimationHooks.IsDead, state == PlayerLifeState.Dead);

            if (state == PlayerLifeState.Downed)
                _animator.SetTrigger(PlayerAnimationHooks.Hit);
            else if (state == PlayerLifeState.Dead)
                _animator.SetTrigger(PlayerAnimationHooks.Death);
        }
    }

    public void SetMoveState(float speed, bool running)
    {
        if (_animator == null)
            return;

        _animator.SetFloat(PlayerAnimationHooks.Speed, speed);
        _animator.SetFloat(PlayerAnimationHooks.MoveSpeed, speed);
        _animator.SetBool(PlayerAnimationHooks.IsRunning, running);
    }

    public void PlayHit()
    {
        if (_animator != null)
            _animator.SetTrigger(PlayerAnimationHooks.Hit);
    }

    public void PlayRevive()
    {
        if (_animator != null)
            _animator.SetTrigger(PlayerAnimationHooks.Revive);
    }
}
