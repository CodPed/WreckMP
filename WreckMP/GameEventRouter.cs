using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace WreckMP
{
	internal class GameEventRouter
	{
		public static GameEventList RegisteredEvents
		{
			get
			{
				return GameEventRouter.registeredEvents;
			}
		}

		internal static void RegisterEvent(GameEvent gameEvent)
		{
			GameEventRouter.registeredEvents.Add(gameEvent);
		}

		internal static GameEvent GetEvent(string name)
		{
			return GameEventRouter.registeredEvents.Find(name.GetHashCode());
		}

		internal static GameEvent GetEvent(int hash)
		{
			return GameEventRouter.registeredEvents.Find(hash);
		}

		internal static bool IsRecordingPackets
		{
			get
			{
				return GameEventRouter.recordingStream != null;
			}
		}

		internal static ulong CurrentRecordingPlayer
		{
			get
			{
				return GameEventRouter.currentRecPlayer;
			}
		}

		internal static void StartRecording(string path)
		{
			if (GameEventRouter.recordingStream != null)
			{
				return;
			}
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			GameEventRouter.recordingStream = File.Create(path);
			GameEventRouter.currentRecTime = -1f;
			GameEventRouter.currentRecFrame = 0;
			GameEventRouter.currentRecPlayer = 1UL;
			LocalPlayer.syncPositionRecEvent = new GameEvent(string.Format("SyncPosition{0}", GameEventRouter.currentRecPlayer), delegate(GameEventReader r)
			{
			}, GameScene.GAME);
			LocalPlayer.grabItemRecEvent = new GameEvent(string.Format("GrabItem{0}", GameEventRouter.currentRecPlayer), delegate(GameEventReader r)
			{
			}, GameScene.GAME);
			for (int i = 0; i < WreckMPGlobals.OnMemberReady.Count; i++)
			{
				try
				{
					WreckMPGlobals.OnMemberReady[i](0UL);
				}
				catch (Exception ex)
				{
					Console.LogError(string.Format("An error of type {0} occured when start recording sent Ready event. Please report this error to the developers", ex.GetType()), false);
					Console.LogError(string.Format("{0} {1} [{2}]", ex.GetType(), ex.Message, ex.StackTrace), false);
				}
			}
		}

		internal static void StopRecording()
		{
			if (GameEventRouter.recordingStream == null)
			{
				return;
			}
			GameEventRouter.recordingStream.Close();
			GameEventRouter.recordingStream.Dispose();
			GameEventRouter.recordingStream = null;
			LocalPlayer.syncPositionRecEvent.Unregister();
			LocalPlayer.grabItemRecEvent.Unregister();
			LocalPlayer.syncPositionRecEvent = null;
			LocalPlayer.grabItemRecEvent = null;
		}

		private static void LogPacket(byte[] data, ulong sender)
		{
			if (GameEventRouter.recordingStream == null || !GameEventRouter.recordingStream.CanWrite)
			{
				return;
			}
			if (Time.frameCount != GameEventRouter.currentRecFrame)
			{
				GameEventRouter.recordingStream.WriteByte(85);
				GameEventRouter.recordingStream.Write(BitConverter.GetBytes(Time.unscaledDeltaTime), 0, 4);
				GameEventRouter.currentRecFrame = Time.frameCount;
			}
			GameEventRouter.recordingStream.WriteByte(170);
			GameEventRouter.recordingStream.Write(BitConverter.GetBytes(sender), 0, 8);
			if (data.Length > 16777215)
			{
				Console.Log(string.Format("Length {0} too big!", data.Length), true);
			}
			GameEventRouter.recordingStream.Write(new byte[]
			{
				(byte)(data.Length & 255),
				(byte)((data.Length >> 8) & 255),
				(byte)((data.Length >> 16) & 255)
			}, 0, 3);
			GameEventRouter.recordingStream.Write(data, 0, data.Length);
		}

		private static void EditPacketRecording(ref byte[] data, bool afterLog, GameEvent.RecordingProperties recordingProperties)
		{
			int num = 5;
			byte[] bytes = BitConverter.GetBytes(afterLog ? WreckMPGlobals.UserID : GameEventRouter.CurrentRecordingPlayer);
			if (recordingProperties.playerIdIndexes != null)
			{
				for (int i = 0; i < recordingProperties.playerIdIndexes.Length; i++)
				{
					if (num + recordingProperties.playerIdIndexes[i] + 8 <= data.Length)
					{
						for (int j = 0; j < 8; j++)
						{
							data[num + recordingProperties.playerIdIndexes[i] + j] = bytes[j];
						}
					}
				}
			}
		}

		internal static void SendPacket(byte[] data, ulong target = 0UL, bool safe = true, GameEvent.RecordingProperties recordingProperties = default(GameEvent.RecordingProperties), GameScene targetScene = GameScene.GAME)
		{
			if (GameEventRouter.blockGateway)
			{
				return;
			}
			if (GameEventRouter.overrideSafeSend != null)
			{
				safe = GameEventRouter.overrideSafeSend.Value;
			}
			GameEventRouter.EditPacketRecording(ref data, false, recordingProperties);
			GameEventRouter.LogPacket(data, GameEventRouter.CurrentRecordingPlayer);
			GameEventRouter.EditPacketRecording(ref data, true, recordingProperties);
			if (target != 0UL)
			{
				CoreManager.SendData(data, target, safe);
				return;
			}
			CoreManager.SendData(data, safe, SteamNet.p2pConnections);
		}

		internal static void ReceivePacket(byte[] data, ulong sender, bool bypassBlock = false)
		{
			if (GameEventRouter.blockGateway && !bypassBlock)
			{
				return;
			}
			if (data.Length == GameEventRouter.handshakeMessage.Length)
			{
				int num = 0;
				while (num < data.Length && data[num] == GameEventRouter.handshakeMessage[num])
				{
					if (num + 1 >= GameEventRouter.handshakeMessage.Length)
					{
						return;
					}
					num++;
				}
			}
			if (sender == WreckMPGlobals.UserID)
			{
				if (!bypassBlock)
				{
					return;
				}
				sender = GameEventRouter.CurrentRecordingPlayer;
			}
			GameEventRouter.LogPacket(data, sender);
			using (GameEventReader gameEventReader = new GameEventReader(sender, data))
			{
				int num2 = gameEventReader.ReadInt32();
				GameScene gameScene = (GameScene)gameEventReader.ReadByte();
				if (gameScene == CoreManager.currentScene)
				{
					if (gameScene != GameScene.GAME || _ObjectsLoader.IsGameLoaded)
					{
						GameEvent gameEvent = GameEventRouter.registeredEvents.Find(num2);
						if (gameEvent == null)
						{
							Console.LogError("Received unknown event of hash " + num2.ToString(), false);
						}
						else
						{
							gameEvent.OnReceive(gameEventReader);
						}
					}
				}
			}
		}

		internal static void UnregisterEvent(int hash)
		{
			GameEventRouter.registeredEvents.Remove(GameEventRouter.registeredEvents.Find(hash));
		}

		private static GameEventList registeredEvents = new GameEventList();

		public static readonly byte[] handshakeMessage = Encoding.ASCII.GetBytes("WreckMP handshake go brr");

		public static bool? overrideSafeSend = null;

		private static int currentRecFrame;

		private static float currentRecTime;

		private static FileStream recordingStream;

		internal static bool blockGateway = false;

		private static ulong currentRecPlayer;
	}
}
