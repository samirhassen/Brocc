using NTech.Core;
using System;

namespace NTech.Legacy.Module.Shared.Infrastructure
{
    // Exists just to simplify migration to .net core since we cant take a dependancy on NTech.Services.Infrastructure in the netstandard shared libraries.
    // This is intended to work exactly the same as IClock in every way.
    public class CoreClock : ICombinedClock
    {
        public DateTimeOffset Now => ClockFactory.SharedInstance.Now;

        public DateTime Today => ClockFactory.SharedInstance.Today;

        public static ICombinedClock SharedInstance => new CoreClock();
    }

    public interface ICombinedClock : ICoreClock, IClock
    {

    }
}