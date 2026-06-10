using UnityEngine;

public static class ReviveSystem
{
    public const float ReviveHoldSeconds = 3f;
    public const float DownedTimeoutSeconds = 20f;
    public const float RepairPerSecond = 5f;

    public static bool CanRevive(PlayerNetwork reviver, PlayerNetwork target)
    {
        return reviver != null && target != null && reviver != target && reviver.LifeState.Value == PlayerLifeState.Alive && target.LifeState.Value == PlayerLifeState.Downed;
    }

    public static bool CanRepair(PlayerNetwork player, BarricadeController target)
    {
        return player != null && target != null && player.LifeState.Value == PlayerLifeState.Alive && !target.IsDestroyed && target.CurrentHP.Value < target.MaxHP;
    }

    public static void ApplyRevive(PlayerNetwork target)
    {
        if (target == null)
            return;

        target.ReviveFromDowned();
    }

    public static void ApplyRepair(BarricadeController target, float deltaTime)
    {
        if (target == null || deltaTime <= 0f)
            return;

        target.RepairServer(RepairPerSecond * deltaTime);
    }
}
