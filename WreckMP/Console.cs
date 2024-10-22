using System;
using System.Diagnostics;

namespace WreckMP
{
	internal static class Console
	{
		internal static void Init()
		{
			Console.ts.Switch.Level = SourceLevels.All;
			Console.ts.Listeners.Add(Console.tw);
		}

		private static void _Log(string msg, string logMessage, bool show)
		{
			string text = "[" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.ff") + "]: " + logMessage;
			Console.tw.WriteLine(text);
			Console.tw.Flush();
			if (CoreManager.uiManager != null && show)
			{
				CoreManager.uiManager.LogConsoleSystemMessage(msg);
			}
		}

		public static void Log(object message, bool show = true)
		{
			Console._Log(message.ToString(), message.ToString(), show);
		}

		public static void LogWarning(object message, bool show = true)
		{
			Console._Log(string.Format("<color=orange>WARNING!</color> {0}", message), string.Format("WARNING! {0}", message), show);
		}

		public static void LogError(object message, bool show = false)
		{
			Console._Log(string.Format("<color=red>ERROR!</color> {0}", message), string.Format("ERROR! {0}", message), show);
		}

		private static TraceSource ts = new TraceSource("WreckMP-Console");

		private static TextWriterTraceListener tw = new TextWriterTraceListener("_WreckMP_console_log.txt");
	}
}
