using System;
using System.Collections;
using System.IO;

namespace WreckMP
{
	internal class NetReplayManager : NetManager
	{
		private void Start()
		{
			NetReplayManager.instance = this;
		}

		internal static void StartPlayback(string path)
		{
			if (!File.Exists(path))
			{
				return;
			}
			NetReplayManager.instance.StartCoroutine(NetReplayManager.Playback(path));
		}

		internal static void StopPlayback()
		{
			NetReplayManager.instance.StopAllCoroutines();
			GameEventRouter.blockGateway = false;
			Console.Log("Destroying local player model", true);
			CoreManager.DeleteUserObjects(1UL);
			Console.Log("Playback halted", true);
			NetReplayManager.playbackOngoing = false;
		}

		private static IEnumerator Playback(string path)
		{
			NetReplayManager.<Playback>d__5 <Playback>d__ = new NetReplayManager.<Playback>d__5(0);
			<Playback>d__.path = path;
			return <Playback>d__;
		}

		private static NetReplayManager instance;

		internal static bool playbackOngoing;
	}
}
