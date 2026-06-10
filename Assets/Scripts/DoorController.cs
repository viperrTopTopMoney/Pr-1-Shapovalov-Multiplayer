using UnityEngine;

public sealed class DoorController : BarricadeController
{
    [SerializeField] private int _doorMaxHp = 100;

    protected override void OnValidate()
    {
        base.OnValidate();
        if (_doorMaxHp > 0)
            _maxHP = _doorMaxHp;
    }
}
