using UnityEngine;

public static class PlayerAnimationHooks
{
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
    public static readonly int IsRunning = Animator.StringToHash("IsRunning");
    public static readonly int IsDowned = Animator.StringToHash("IsDowned");
    public static readonly int IsDead = Animator.StringToHash("IsDead");
    public static readonly int Hit = Animator.StringToHash("Hit");
    public static readonly int Revive = Animator.StringToHash("Revive");
    public static readonly int Death = Animator.StringToHash("Death");
}
