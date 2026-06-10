public enum PlayerLifeState
{
    Alive = 0,
    Downed = 1,
    Dead = 2
}

public enum MatchFlowState
{
    Lobby = 0,
    InWave = 1,
    Preparation = 2,
    IntruderWarning = 3,
    Victory = 4,
    Defeat = 5
}

public enum ZombieKind
{
    Normal = 0,
    Runner = 1,
    Boss = 2
}

public enum InteractionTargetKind
{
    None = 0,
    Repair = 1,
    Revive = 2
}
