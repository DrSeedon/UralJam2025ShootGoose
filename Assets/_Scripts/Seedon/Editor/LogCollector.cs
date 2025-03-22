using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

[InitializeOnLoad]
public static class LogCollector
{
    private static readonly List<LogEntry> logs = new List<LogEntry>();
    
    static LogCollector()
    {
        ClearLogs();
        Application.logMessageReceived += OnLogMessageReceived;
        Debug.Log("LogCollector запущен");
    }

    private static void OnLogMessageReceived(string message, string stackTrace, LogType type)
    {
        logs.Add(new LogEntry 
        { 
            Message = message,
            StackTrace = stackTrace,
            Type = type,
            Time = System.DateTime.Now
        });
    }

    [MenuItem("Tools/Copy All Logs")]
    public static void CopyAllLogs()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"=== Логи Unity ({System.DateTime.Now:yyyy-MM-dd HH:mm:ss}) ===\n");
        
        foreach (var log in logs)
        {
            sb.AppendLine($"[{log.Time:HH:mm:ss}] [{log.Type}] {log.Message}");
            if (!string.IsNullOrEmpty(log.StackTrace))
            {
                sb.AppendLine(log.StackTrace);
                sb.AppendLine();
            }
        }

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        Debug.Log("Все логи скопированы в буфер обмена");
    }

    [MenuItem("Tools/Clear Logs")]
    public static void ClearLogs()
    {
        logs.Clear();
        Debug.Log("Все логи очищены");
    }

    private class LogEntry
    {
        public string Message;
        public string StackTrace;
        public LogType Type;
        public System.DateTime Time;
    }
} 