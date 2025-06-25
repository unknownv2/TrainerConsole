namespace Console
{
    using System;
    using System.Runtime.CompilerServices;

    internal static class FastDateTime
    {
        public static TimeSpan LocalUtcOffset { get; }

        public static DateTime Now =>
            DateTime.UtcNow + LocalUtcOffset;
    }
}

