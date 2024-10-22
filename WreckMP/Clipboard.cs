using System;
using System.Reflection;
using UnityEngine;

namespace WreckMP
{
	internal class Clipboard
	{
		public static string text
		{
			get
			{
				return Clipboard.cp.GetValue(null, null).ToString();
			}
			set
			{
				Clipboard.cp.SetValue(null, value, null);
			}
		}

		private static PropertyInfo cp = typeof(GUIUtility).GetProperty("systemCopyBuffer", BindingFlags.Static | BindingFlags.NonPublic);
	}
}
