namespace TextFlow.Infrastructure;

public enum AppThemeMode
{
    System = 0,
    Light = 1,
    Dark = 2
}

public enum SyncStatus
{
    NotConfigured,
    Ready,
    Syncing,
    Synced,
    Error,
    Offline
}

public enum SyncMode
{
    Auto,
    Manual
}

public enum ConflictResolutionStrategy
{
    LocalWins,
    CloudWins,
    CompareChanges,
    MergeManually
}

public enum SyncJobStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}
