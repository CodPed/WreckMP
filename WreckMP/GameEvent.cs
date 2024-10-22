using System;

namespace WreckMP
{
	public class GameEvent
	{
		public string Name
		{
			get
			{
				return this.name;
			}
		}

		public int Hash
		{
			get
			{
				return this.hash;
			}
		}

		public GameScene TargetScene
		{
			get
			{
				return this.targetScene;
			}
		}

		public GameEvent(string name, Action<GameEventReader> callback, GameScene targetScene = GameScene.GAME)
		{
			this.name = name;
			this.hash = name.GetHashCode();
			this.targetScene = targetScene;
			this.callback = callback;
			GameEventRouter.RegisterEvent(this);
		}

		[Obsolete("The <ulong, GameEventReader> callback is for compatibility with old BeerMP code. Please use the <GameEventReader> callback instead.", false)]
		public GameEvent(string name, Action<ulong, GameEventReader> callback, GameScene targetScene = GameScene.GAME)
		{
			this.name = name;
			this.hash = name.GetHashCode();
			this.targetScene = targetScene;
			this.oldCallback = callback;
			GameEventRouter.RegisterEvent(this);
		}

		public void Unregister()
		{
			GameEventRouter.UnregisterEvent(this.hash);
		}

		public GameEventWriter Writer()
		{
			GameEventWriter gameEventWriter = new GameEventWriter();
			gameEventWriter.Write(this.hash);
			gameEventWriter.Write((byte)this.targetScene);
			return gameEventWriter;
		}

		[Obsolete("The static Writer is for compatibility with old BeerMP code. Please use the instance Writer call instead.")]
		public static GameEventWriter Writer(string name, GameScene targetScene = GameScene.GAME)
		{
			GameEventWriter gameEventWriter = new GameEventWriter();
			gameEventWriter.Write(name.GetHashCode());
			gameEventWriter.Write((byte)targetScene);
			return gameEventWriter;
		}

		[Obsolete("The static Writer is for compatibility with old BeerMP code. Please use the instance Writer call instead.")]
		public static GameEventWriter EmptyWriter(string unusedparam = "")
		{
			return new GameEventWriter
			{
				isEmpty = true
			};
		}

		public void Send(GameEventWriter data, ulong target = 0UL, bool safe = true, GameEvent.RecordingProperties recordingProperties = default(GameEvent.RecordingProperties))
		{
			if (data.isEmpty)
			{
				byte[] packet = data.GetPacket();
				byte[] array = new byte[packet.Length + 4 + 1];
				BitConverter.GetBytes(this.hash).CopyTo(array, 0);
				array[4] = (byte)this.targetScene;
				packet.CopyTo(array, 5);
				GameEventRouter.SendPacket(array, target, safe, recordingProperties, this.targetScene);
				return;
			}
			GameEventRouter.SendPacket(data.GetPacket(), target, safe, recordingProperties, this.targetScene);
		}

		public void SendEmpty(ulong target = 0UL, bool safe = true)
		{
			using (GameEventWriter gameEventWriter = this.Writer())
			{
				this.Send(gameEventWriter, target, safe, default(GameEvent.RecordingProperties));
			}
		}

		internal void OnReceive(GameEventReader reader)
		{
			if (CoreManager.currentScene == this.targetScene)
			{
				if (this.oldCallback == null)
				{
					Action<GameEventReader> action = this.callback;
					if (action == null)
					{
						return;
					}
					action(reader);
					return;
				}
				else
				{
					Action<ulong, GameEventReader> action2 = this.oldCallback;
					if (action2 == null)
					{
						return;
					}
					action2(reader.sender, reader);
				}
			}
		}

		private string name;

		private int hash;

		private GameScene targetScene;

		private Action<GameEventReader> callback;

		private Action<ulong, GameEventReader> oldCallback;

		public struct RecordingProperties
		{
			public int[] playerIdIndexes;
		}
	}
}
