using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace WreckMP
{
	internal class SatsumaProfiler
	{
		internal SatsumaProfiler(Rigidbody satuma)
		{
			this.satsuma = satuma;
			SatsumaProfiler.Instance = this;
			Console.Log("Satsuma profiler initialized", true);
		}

		internal void Update(bool receivedRBupdate, ulong owner)
		{
			this.logs[this.currentPosition] = string.Format("[{0}] Velocity: {1} ({2}), received update: {3}, owner: {4}", new object[]
			{
				Time.timeSinceLevelLoad,
				this.satsuma.velocity,
				this.satsuma.velocity.magnitude,
				receivedRBupdate,
				CoreManager.Players[owner].PlayerName
			});
			if (this.attached.Count > 0)
			{
				string text = "\n Attached: ";
				for (int i = 0; i < this.attached.Count; i++)
				{
					if (i > 0)
					{
						text += ", ";
					}
					text += this.attached[i];
				}
				string[] array = this.logs;
				int num = this.currentPosition;
				array[num] += text;
			}
			if (this.detached.Count > 0)
			{
				string text2 = "\n Detached: ";
				for (int j = 0; j < this.detached.Count; j++)
				{
					if (j > 0)
					{
						text2 += ", ";
					}
					text2 += this.detached[j];
				}
				string[] array2 = this.logs;
				int num2 = this.currentPosition;
				array2[num2] += text2;
			}
			this.attached.Clear();
			this.detached.Clear();
			this.currentPosition++;
			if (this.currentPosition >= this.logs.Length)
			{
				this.currentPosition = 0;
			}
		}

		internal void PrintToFile()
		{
			string text = "";
			int num = this.currentPosition;
			int num2 = this.currentPosition;
			do
			{
				text = text + this.logs[num2] + "\n";
				num2++;
				if (num2 == this.logs.Length)
				{
					num2 = 0;
				}
			}
			while (num2 != num);
			File.WriteAllText("satsuma_profiler.txt", text);
		}

		internal static SatsumaProfiler Instance;

		internal List<string> attached = new List<string>();

		internal List<string> detached = new List<string>();

		private Rigidbody satsuma;

		private string[] logs = new string[Mathf.RoundToInt(10f * (1f / Time.fixedDeltaTime))];

		private int currentPosition;
	}
}
