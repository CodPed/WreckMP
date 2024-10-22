using System;
using UnityEngine;

namespace WreckMP
{
	internal class WreckMPEntry
	{
		internal static void Start()
		{
			if (WreckMPEntry.system == null)
			{
				WreckMPEntry.system = new GameObject("WreckMP").AddComponent<WreckMP>();
			}
		}

		internal static WreckMP system;
	}
}
