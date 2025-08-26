using Eevee.Diagnosis;
using System;
using UnityEngine;
using UDebug = UnityEngine.Debug;

/// <summary>
/// Log示例代码
/// </summary>
internal sealed class LogSample : MonoBehaviour
{
    private sealed class SampleLog : ILog
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
        LogRelay.Debug("[Sample] SampleLog.Log");
        LogRelay.Error("[Sample] SampleLog.Error");

        LogProxy.Inject(new SampleLog());
        LogRelay.Debug("[Sample] Default.Log");
        LogRelay.Error("[Sample] Default.Error");
    }
}