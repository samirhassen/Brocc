namespace StagingDatabaseTransformer
{
    public enum StagingStateCode
    {
        RestoredFromBackup,
        Aquiring,
        AquireDone,
        Transforming,
        TransformDone,
        BackingUp,
        BackingUpDone
    }
}
