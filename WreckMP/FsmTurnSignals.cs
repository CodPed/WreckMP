using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;

namespace WreckMP
{
	internal class FsmTurnSignals
	{
		public FsmTurnSignals(PlayMakerFSM fsm)
		{
			this.hash = fsm.transform.GetGameobjectHashString().GetHashCode();
			this.fsm = fsm;
			this.SetupFSM();
			if (FsmTurnSignals.updateEvent == null)
			{
				FsmTurnSignals.updateEvent = new GameEvent("Update", new Action<GameEventReader>(FsmTurnSignals.OnUpdate), GameScene.GAME);
			}
			if (!FsmTurnSignals.initSyncLoaded)
			{
				FsmTurnSignals.initSyncLoaded = true;
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
				{
					if (!WreckMPGlobals.IsHost)
					{
						return;
					}
					for (int i = 0; i < FsmTurnSignals.turnSignals.Count; i++)
					{
						FsmTurnSignals.turnSignals[i].SendUpdate(FsmTurnSignals.turnSignals[i].current, u);
					}
				});
			}
			FsmTurnSignals.turnSignals.Add(this);
			CoreManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(CoreManager.sceneLoaded, new Action<GameScene>(delegate(GameScene a)
			{
				if (FsmTurnSignals.turnSignals.Contains(this))
				{
					FsmTurnSignals.turnSignals.Remove(this);
				}
			}));
		}

		private void SetupFSM()
		{
			FsmState state = this.fsm.GetState("State 2");
			string toState = state.Transitions.First((FsmTransition t) => t.EventName == "LEFT").ToState;
			string toState2 = state.Transitions.First((FsmTransition t) => t.EventName == "RIGHT").ToState;
			this.fsm.InsertAction("State 3", delegate
			{
				this.SendUpdate(0, 0UL);
			}, 0, false);
			this.fsm.AddGlobalTransition(this.fsm.AddEvent(FsmTurnSignals.eventNames[0]), "State 3");
			this.fsm.InsertAction(toState, delegate
			{
				this.SendUpdate(1, 0UL);
			}, 0, false);
			this.fsm.AddGlobalTransition(this.fsm.AddEvent(FsmTurnSignals.eventNames[1]), toState);
			this.fsm.InsertAction(toState2, delegate
			{
				this.SendUpdate(2, 0UL);
			}, 0, false);
			this.fsm.AddGlobalTransition(this.fsm.AddEvent(FsmTurnSignals.eventNames[2]), toState2);
		}

		private void SendUpdate(int i, ulong target = 0UL)
		{
			if (this.updating == i)
			{
				this.updating = -1;
				return;
			}
			using (GameEventWriter gameEventWriter = FsmTurnSignals.updateEvent.Writer())
			{
				gameEventWriter.Write(this.hash);
				gameEventWriter.Write((byte)i);
				this.current = i;
				if (target == 0UL)
				{
					FsmTurnSignals.updateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
				else
				{
					FsmTurnSignals.updateEvent.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		private static void OnUpdate(GameEventReader p)
		{
			int hash = p.ReadInt32();
			FsmTurnSignals fsmTurnSignals = FsmTurnSignals.turnSignals.FirstOrDefault((FsmTurnSignals ts) => ts.hash == hash);
			if (fsmTurnSignals == null)
			{
				Console.LogError(string.Format("Received turn signal of hash {0} update, but it does not exist", hash), false);
				return;
			}
			int num = (int)p.ReadByte();
			fsmTurnSignals.updating = (fsmTurnSignals.current = num);
			fsmTurnSignals.fsm.SendEvent(FsmTurnSignals.eventNames[num]);
		}

		private PlayMakerFSM fsm;

		private int hash;

		private int updating = -1;

		private int current;

		private static readonly string[] eventNames = new string[] { "MP_OFF", "MP_LEFT", "MP_RIGHT" };

		private static GameEvent updateEvent;

		private static List<FsmTurnSignals> turnSignals = new List<FsmTurnSignals>();

		private static bool initSyncLoaded = false;
	}
}
