using System;

namespace NTech.Core
{
    public interface ICoreClock
    {
        DateTimeOffset Now { get; }

        DateTime Today { get; }
    }
}
