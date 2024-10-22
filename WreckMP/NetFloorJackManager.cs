using System;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetFloorJackManager : NetManager
	{
		private void Start()
		{
			GameEvent<NetFloorJackManager> e = new GameEvent<NetFloorJackManager>("Move", new Action<ulong, GameEventReader>(this.OnMove), GameScene.GAME);
			Transform transform = GameObject.Find("ITEMS").transform.Find("floor jack(itemx)");
			this.usageFsm = transform.Find("Trigger").GetPlayMaker("Use");
			this.y = this.usageFsm.FsmVariables.FindFsmFloat("Y");
			Action<bool> move = delegate(bool isUp)
			{
				if (this.receivedJackEvent)
				{
					this.receivedJackEvent = false;
					return;
				}
				using (GameEventWriter gameEventWriter = e.Writer())
				{
					gameEventWriter.Write(isUp);
					if (isUp)
					{
						gameEventWriter.Write(this.y.Value);
					}
					GameEvent<NetFloorJackManager>.Send("Move", gameEventWriter, 0UL, true);
				}
			};
			this.usageFsm.InsertAction("Up", delegate
			{
				move(true);
			}, 0, false);
			this.usageFsm.InsertAction("Down", delegate
			{
				move(false);
			}, 0, false);
			this.usageFsm.AddGlobalTransition(this.usageFsm.FsmEvents.First((FsmEvent e) => e.Name == "LIFT UP"), "Up");
			this.usageFsm.AddGlobalTransition(this.usageFsm.FsmEvents.First((FsmEvent e) => e.Name == "LIFT DOWN"), "Down");
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				if (this.y.Value != 0f)
				{
					using (GameEventWriter gameEventWriter2 = e.Writer())
					{
						gameEventWriter2.Write(true);
						gameEventWriter2.Write(this.y.Value);
						GameEvent<NetFloorJackManager>.Send("Move", gameEventWriter2, user, true);
					}
				}
			});
		}

		private void OnMove(ulong sender, GameEventReader packet)
		{
			this.receivedJackEvent = true;
			bool flag = packet.ReadBoolean();
			if (flag)
			{
				this.y.Value = packet.ReadSingle();
			}
			this.usageFsm.SendEvent("LIFT " + (flag ? "UP" : "DOWN"));
		}

		private FsmFloat y;

		private PlayMakerFSM usageFsm;

		private bool receivedJackEvent;
	}
}
