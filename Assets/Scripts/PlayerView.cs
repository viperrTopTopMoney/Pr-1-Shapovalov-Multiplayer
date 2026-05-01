using TMPro;
using UnityEngine;
// УДАЛИЛИ FishNet.Object

public class PlayerView : MonoBehaviour // ТЕПЕРЬ MonoBehaviour
{
    [SerializeField] private TMP_Text _nicknameText;
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private GameObject _uiRoot;
    [SerializeField] private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (_canvasGroup == null && _uiRoot != null)
            _canvasGroup = _uiRoot.GetComponent<CanvasGroup>();

        if (_canvasGroup == null && _uiRoot != null)
            _canvasGroup = _uiRoot.AddComponent<CanvasGroup>();
    }

    // УДАЛИЛИ OnStartNetwork полностью! 
    // Теперь PlayerNetwork сам будет вызывать эти методы, когда данные будут готовы.

    public void UpdateNickname(string name)
    {
        if (_nicknameText != null)
        {
            // Не даем записать пустую строку, если там уже что-то есть
            if (string.IsNullOrEmpty(name)) return; 
            _nicknameText.text = name;
        }
    }

    public void UpdateHP(int hp)
    {
        if (_hpText != null)
            _hpText.text = $"HP: {hp}";
    }

    public void UpdateVisibility(bool isAlive)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = isAlive ? 1 : 0;
            _canvasGroup.interactable = isAlive;
            _canvasGroup.blocksRaycasts = isAlive;
        }
        else if (_uiRoot != null)
        {
            _uiRoot.SetActive(isAlive);
        }
    }
}