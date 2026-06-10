using FishNet.Object;
using UnityEngine;

public class ZombieView : NetworkBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _visualRoot;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int IsDead = Animator.StringToHash("IsDead");
    private static readonly int Death = Animator.StringToHash("Death");

    public void SetMotion(float speed, bool attacking)
    {
        if (_animator == null)
            return;

        _animator.SetFloat(Speed, speed);
        _animator.SetBool(IsAttacking, attacking);
    }

    public void PlayAttack()
    {
        if (_animator == null)
            return;

        _animator.SetTrigger(Attack);
    }

    public void PlayDeath()
    {
        if (_animator == null)
            return;

        _animator.SetBool(IsDead, true);
        _animator.SetTrigger(Death);
    }

    public void SetScale(float scale)
    {
        if (_visualRoot != null)
            _visualRoot.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }
}
