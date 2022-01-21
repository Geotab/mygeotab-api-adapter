using NLog;

namespace MyGeotabAPIAdapter.Logging
{
    /// <summary>
    /// Log levels corresponding with <see cref="NLog.LogLevel"/>s. 
    /// </summary>
    public enum LogLevel { Debug, Error, Fatal, Info, Off, Trace, Warn }
}
