using Eevee.Log;
using System;
using UnityEngine;
using UDebug = UnityEngine.Debug;

internal sealed class LogSample : MonoBehaviour
{
    private sealed class Logger : IELogger
    {
        public void Trace(string message) => UDebug.LogWarning(message);
        public void Debug(string message) => UDebug.LogWarning(message);
        public void Info(string message) => UDebug.LogWarning(message);
        public void Warn(string message) => UDebug.LogWarning(message);
        public void Error(string message) => UDebug.LogWarning(message);
        public void Error(Exception exception) => UDebug.LogWarning(exception);
        public void Fail(string message) => UDebug.LogWarning(message);
        public void Fail(Exception exception) => UDebug.LogWarning(exception);
    }

    private void OnEnable()
    {
        ELogProxy.Inject(new Logger());
        ELog.Debug("Logger.Debug");
        ELog.Error("Logger.Error");

        ELogProxy.UnInject();
        ELog.Debug("Default.Debug");
        ELog.Error("Default.Error");
    }
}