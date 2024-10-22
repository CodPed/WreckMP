using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class FsmDashboardButton
	{
		public FsmDashboardButton(PlayMakerFSM fsm)
		{
			this.fsm = fsm;
			this.isFrontloader = fsm.transform.name.StartsWith("FrontHyd");
			this.hash = fsm.transform.GetGameobjectHashString().GetHashCode();
			this.SetupFSM();
			this.knobToggleEvent = new GameEvent("DashboardKnobToggle" + this.hash.ToString(), new Action<GameEventReader>(this.OnTriggeredAction), GameScene.GAME);
			if (!FsmDashboardButton.initSyncLoaded)
			{
				FsmDashboardButton.initSyncLoaded = true;
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
				{
					if (!WreckMPGlobals.IsHost)
					{
						return;
					}
					for (int i = 0; i < FsmDashboardButton.dashboardButtons.Count; i++)
					{
						int num = FsmDashboardButton.dashboardButtons[i].currentAction;
						FsmDashboardButton.dashboardButtons[i].TriggeredAction(num, u);
					}
				});
			}
			FsmDashboardButton.dashboardButtons.Add(this);
			CoreManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(CoreManager.sceneLoaded, new Action<GameScene>(delegate(GameScene a)
			{
				if (FsmDashboardButton.dashboardButtons.Contains(this))
				{
					FsmDashboardButton.dashboardButtons.Remove(this);
				}
			}));
		}

		protected void SetupFSM()
		{
			if (this.isFrontloader)
			{
				this.actionEvents = new string[] { "MP_INC", "MP_DEC", "MP_STOP" };
				FsmEvent fsmEvent = this.fsm.AddEvent(this.actionEvents[0]);
				this.fsm.AddGlobalTransition(fsmEvent, "INCREASE 2");
				this.fsm.InsertAction("INCREASE 2", delegate
				{
					this.TriggeredAction(0, 0UL);
				}, 0, false);
				this.EditLMBUp(this.fsm.GetState("INCREASE 2"));
				fsmEvent = this.fsm.AddEvent(this.actionEvents[1]);
				this.fsm.AddGlobalTransition(fsmEvent, "DECREASE 2");
				this.fsm.InsertAction("DECREASE 2", delegate
				{
					this.TriggeredAction(1, 0UL);
				}, 0, false);
				this.EditLMBUp(this.fsm.GetState("DECREASE 2"));
				fsmEvent = this.fsm.AddEvent(this.actionEvents[2]);
				this.fsm.AddGlobalTransition(fsmEvent, "State 1");
				this.fsm.InsertAction("State 1", delegate
				{
					this.TriggeredAction(2, 0UL);
				}, 0, false);
				return;
			}
			FsmState fsmState = this.fsm.FsmStates.FirstOrDefault((FsmState s) => s.Name.Contains("Test"));
			this.actionEvents = new string[fsmState.Transitions.Length];
			for (int i = 0; i < this.actionEvents.Length; i++)
			{
				string text = "MP_" + fsmState.Transitions[i].EventName;
				this.actionEvents[i] = text;
				FsmEvent fsmEvent2 = this.fsm.AddEvent(text);
				this.fsm.AddGlobalTransition(fsmEvent2, fsmState.Transitions[i].ToState);
				int index = i;
				this.fsm.InsertAction(fsmState.Transitions[i].ToState, delegate
				{
					this.TriggeredAction(index, 0UL);
				}, 0, false);
			}
		}

		private void EditLMBUp(FsmState state)
		{
			FsmStateAction[] actions = state.Actions;
			for (int i = 0; i < actions.Length; i++)
			{
				GetMouseButtonUp lmbup = actions[i] as GetMouseButtonUp;
				if (lmbup != null)
				{
					actions[i] = new PM_Hook(delegate
					{
						if ((this.owner == 0UL || this.owner == WreckMPGlobals.UserID) && Input.GetMouseButtonUp(lmbup.button))
						{
							state.Fsm.Event(lmbup.sendEvent);
						}
					}, true);
				}
			}
			state.Actions = actions;
		}

		protected void TriggeredAction(int index, ulong target = 0UL)
		{
			if (this.updatingAction == index)
			{
				this.updatingAction = -1;
				return;
			}
			using (GameEventWriter gameEventWriter = this.knobToggleEvent.Writer())
			{
				gameEventWriter.Write((byte)index);
				this.currentAction = index;
				if (this.isFrontloader)
				{
					this.owner = ((index != 2) ? WreckMPGlobals.UserID : 0UL);
				}
				if (target == 0UL)
				{
					this.knobToggleEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
				else
				{
					this.knobToggleEvent.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		protected void OnTriggeredAction(GameEventReader p)
		{
			int num = (int)p.ReadByte();
			if (num == 255)
			{
				return;
			}
			this.updatingAction = (this.currentAction = num);
			if (this.isFrontloader)
			{
				this.owner = ((num != 2) ? p.sender : 0UL);
			}
			this.fsm.SendEvent(this.actionEvents[num]);
		}

		protected PlayMakerFSM fsm;

		protected string[] actionEvents;

		protected int hash;

		protected int updatingAction = -1;

		protected int currentAction = 255;

		private bool isFrontloader;

		private ulong owner;

		protected GameEvent knobToggleEvent;

		protected static List<FsmDashboardButton> dashboardButtons = new List<FsmDashboardButton>();

		protected static bool initSyncLoaded = false;
	}
}
