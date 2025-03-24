using Eevee.Debug;
using System;
using UnityEngine;

/// <summary>
/// Log示例代码
/// </summary>
internal sealed class LogSample : MonoBehaviour
{
    private sealed class SampleLog : ILog
    {
        public void Trace(string message) => Debug.LogWarning(message);
        public void Log(string message) => Debug.LogWarning(message);
        public void Info(string message) => Debug.LogWarning(message);
        public void Warn(string message) => Debug.LogWarning(message);
        public void Error(string message) => Debug.LogWarning(message);
        public void Error(Exception exception) => Debug.LogWarning(exception);
        public void Fail(string message) => Debug.LogWarning(message);
        public void Fail(Exception exception) => Debug.LogWarning(exception);
    }

    private void OnEnable()
    {
        LogRelay.Log("[Sample] SampleLog.Log");
        LogRelay.Error("[Sample] SampleLog.Error");

        LogProxy.Inject(new SampleLog());
        LogRelay.Log("[Sample] Default.Log");
        LogRelay.Error("[Sample] Default.Error");

        LogProxy.UnInject();
    }
}