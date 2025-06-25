namespace Console
{
    using System;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential)]
    internal struct LogEntry
    {
        public MessageType Type;
        public LogLevel Level;
        public DateTime Time;
        public string Message;
        public string LoweredMessage;
    }
}

