using NTech.Core.Module.Shared.Infrastructure;
using System;

namespace NTech.Core.Module.Shared.Database
{
    public abstract class InfrastructureBaseItem
    {
        public byte[] Timestamp { get; set; } //To support replication
        public int ChangedById { get; set; }
        public DateTimeOffset ChangedDate { get; set; }
        public string InformationMetaData { get; set; }
    }
    public static class InfrastructureBaseItemExtensions
    {
        public static T PopulateInfraFields<T>(this T source, INTechCurrentUserMetadata user, ICoreClock clock) where T : InfrastructureBaseItem
        {
            source.ChangedById = user.UserId;
            source.ChangedDate = clock.Now;
            source.InformationMetaData = user.InformationMetadata;
            return source;
        }
    }
}