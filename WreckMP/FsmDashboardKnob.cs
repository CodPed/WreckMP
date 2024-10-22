using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;

namespace WreckMP
{
	internal class FsmDashboardKnob
	{
		public FsmDashboardKnob(PlayMakerFSM fsm)
		{
			this.fsm = fsm;
			this.hash = fsm.transform.GetGameobjectHashString().GetHashCode();
			this.SetupFSM();
			if (FsmDashboardKnob.updateEvent == null)
			{
				FsmDashboardKnob.updateEvent = new GameEvent("Twist", new Action<GameEventReader>(FsmDashboardKnob.OnKnobUpdate), GameScene.GAME);
			}
			if (!FsmDashboardKnob.initSyncLoaded)
			{
				FsmDashboardKnob.initSyncLoaded = true;
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
				{
					if (!WreckMPGlobals.IsHost)
					{
						return;
					}
					for (int i = 0; i < FsmDashboardKnob.knobs.Count; i++)
					{
						FsmDashboardKnob.knobs[i].SendKnobUpdate();
					}
				});
			}
			FsmDashboardKnob.knobs.Add(this);
			CoreManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(CoreManager.sceneLoaded, new Action<GameScene>(delegate(GameScene a)
			{
				if (FsmDashboardKnob.knobs.Contains(this))
				{
					FsmDashboardKnob.knobs.Remove(this);
				}
			}));
		}

		private void SetupFSM()
		{
			this.fsm.Initialize();
			try
			{
				FsmState state = this.fsm.GetState("Increase");
				string toState = state.Transitions[0].ToState;
				for (int i = 0; i < state.Actions.Length; i++)
				{
					FloatAdd floatAdd = state.Actions[i] as FloatAdd;
					if (floatAdd != null)
					{
						this.targetValue = floatAdd.floatVariable;
						this.add = floatAdd.add.Value;
						break;
					}
				}
				this.update = this.fsm.AddEvent("MP_UPDATE");
				this.fsm.AddGlobalTransition(this.update, "Increase");
				this.fsm.InsertAction(toState, new Action(this.SendKnobUpdate), 0, false);
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Failed to setup dashboard knob {0} ({1}): {2}, {3}, {4}", new object[]
				{
					this.hash,
					this.fsm.transform.GetGameobjectHashString(),
					ex.GetType(),
					ex.Message,
					ex.StackTrace
				}), false);
			}
		}

		private static void OnKnobUpdate(GameEventReader packet)
		{
			if (!NetRadioManager.radioLoaded)
			{
				return;
			}
			int hash = packet.ReadInt32();
			float num = packet.ReadSingle();
			FsmDashboardKnob fsmDashboardKnob = FsmDashboardKnob.knobs.FirstOrDefault((FsmDashboardKnob b) => b.hash == hash);
			if (fsmDashboardKnob == null)
			{
				Console.LogError(string.Format("Received dashboard knob triggered action from {0} but the hash {1} cannot be found", CoreManager.playerNames[(CSteamID)packet.sender], hash), false);
				return;
			}
			fsmDashboardKnob.updating = true;
			fsmDashboardKnob.targetValue.Value = num - fsmDashboardKnob.add;
			fsmDashboardKnob.fsm.Fsm.Event(fsmDashboardKnob.update);
		}

		private void SendKnobUpdate()
		{
			if (!NetRadioManager.radioLoaded)
			{
				return;
			}
			if (this.updating)
			{
				this.updating = false;
				return;
			}
			using (GameEventWriter gameEventWriter = FsmDashboardKnob.updateEvent.Writer())
			{
				gameEventWriter.Write(this.hash);
				gameEventWriter.Write(this.targetValue.Value);
				FsmDashboardKnob.updateEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private PlayMakerFSM fsm;

		private FsmFloat targetValue;

		private FsmEvent update;

		private float add;

		private int hash;

		private bool updating;

		private static GameEvent updateEvent;

		private static List<FsmDashboardKnob> knobs = new List<FsmDashboardKnob>();

		private static bool initSyncLoaded = false;
	}
}
