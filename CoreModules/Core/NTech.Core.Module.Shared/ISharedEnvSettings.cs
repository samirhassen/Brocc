namespace NTech.Core.Module.Shared
{
    public interface ISharedEnvSettings
    {
        bool IsProduction { get; }
        bool IsTemplateCacheDisabled { get; }
    }
}
