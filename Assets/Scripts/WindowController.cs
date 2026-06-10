using UnityEngine;

public sealed class WindowController : BarricadeController
{
    [SerializeField] private int _windowMaxHp = 50;

    protected override void OnValidate()
    {
        base.OnValidate();
        if (_windowMaxHp > 0)
            _maxHP = _windowMaxHp;
    }
}
