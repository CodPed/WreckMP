using System;

namespace WreckMP
{
	[Obsolete("This class is for compatibility with old BeerMP code. Please use GameEvent instead.", false)]
	internal class GameEvent<T> : GameEvent
	{
		public GameEvent(string name, Action<GameEventReader> callback, GameScene targetScene = GameScene.GAME)
			: base(typeof(T).ToString() + name, callback, targetScene)
		{
		}

		[Obsolete("The <ulong, GameEventReader> callback is for compatibility with old BeerMP code. Please use the <GameEventReader> callback instead.", false)]
		public GameEvent(string name, Action<ulong, GameEventReader> callback, GameScene targetScene = GameScene.GAME)
			: base(typeof(T).ToString() + name, callback, targetScene)
		{
		}

		[Obsolete("The static Send is for compatibility with old BeerMP code. Please use the instance Send call instead.")]
		public static void Send(string name, GameEventWriter data, ulong target = 0UL, bool safe = true)
		{
			GameEvent @event = GameEventRouter.GetEvent(typeof(T).ToString() + name);
			if (@event == null)
			{
				Console.LogError("The event of name '" + name + "' can't be found.", true);
				return;
			}
			@event.Send(data, target, safe, default(GameEvent.RecordingProperties));
		}
	}
}
