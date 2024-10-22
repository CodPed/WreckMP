using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetTVManager : NetManager
	{
		private void Start()
		{
			GameEvent<NetTVManager> e = new GameEvent<NetTVManager>("On", new Action<ulong, GameEventReader>(this.OnTVToggle), GameScene.GAME);
			this.TVswitch = GameObject.Find("YARD").transform.Find("Building/LIVINGROOM/TV/Switch").GetPlayMaker("Use");
			this.isOn = this.TVswitch.FsmVariables.FindFsmBool("Open");
			FsmEvent fsmEvent = this.TVswitch.AddEvent("MP_OPEN");
			Action<ulong, bool> a = delegate(ulong target, bool init)
			{
				using (GameEventWriter gameEventWriter = e.Writer())
				{
					gameEventWriter.Write(init ? this.isOn.Value : (!this.isOn.Value));
					if (target == 0UL)
					{
						GameEvent<NetTVManager>.Send("On", gameEventWriter, 0UL, true);
					}
					else
					{
						GameEvent<NetTVManager>.Send("On", gameEventWriter, target, true);
					}
				}
			};
			this.TVswitch.InsertAction("Switch", delegate
			{
				a(0UL, false);
			}, 0, false);
			this.TVswitch.AddGlobalTransition(fsmEvent, "State 5");
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				a(user, true);
			});
		}

		private void OnTVToggle(ulong sender, GameEventReader packet)
		{
			bool flag = packet.ReadBoolean();
			this.TVswitch.SendEvent(flag ? "MP_OPEN" : "GLOBALEVENT");
		}

		private PlayMakerFSM TVswitch;

		private FsmBool isOn;
	}
}
