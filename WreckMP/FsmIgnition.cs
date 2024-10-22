using System;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class FsmIgnition
	{
		public FsmIgnition(PlayMakerFSM ignition, PlayMakerFSM starter)
		{
			this.starter = new FsmStarter(this, starter);
			this.fsm = ignition;
			this.acc = ignition.FsmVariables.FindFsmBool("ACC");
			this.motorOn = ignition.FsmVariables.FindFsmBool("MotorOn");
			this.updateIgnition = this.fsm.AddEvent("MP_UPDATE");
			this.fsm.AddGlobalTransition(this.updateIgnition, this.fsm.HasState("Sound") ? "Sound" : "Test");
			this.fsm.InsertAction("Motor starting", delegate
			{
				if (!this.starting)
				{
					this.stage = FsmIgnition.ActionType.MotorRunning;
					this.SendUpdate();
				}
			}, 0, false);
			this.fsm.InsertAction("Motor OFF", delegate
			{
				if (!this.updatingMP)
				{
					this.stage = FsmIgnition.ActionType.Off;
					this.SendUpdate();
				}
				this.updatingMP = false;
			}, 0, false);
			FsmState fsmState = this.fsm.GetState("Motor starting");
			FsmStateAction[] array = fsmState.Actions;
			for (int i = 0; i < array.Length; i++)
			{
				GetMouseButtonUp lmbup2 = array[i] as GetMouseButtonUp;
				if (lmbup2 != null)
				{
					array[i] = new PM_Hook(delegate
					{
						if ((!this.starting && Input.GetMouseButtonUp(0)) || (!this.starting && this.updatingMP))
						{
							this.fsm.Fsm.Event(lmbup2.sendEvent);
							this.updatingMP = false;
						}
					}, true);
					break;
				}
			}
			fsmState.Actions = array;
			fsmState = this.fsm.GetState("ACC on");
			array = fsmState.Actions;
			for (int j = 0; j < array.Length; j++)
			{
				GetMouseButtonUp lmbup3 = array[j] as GetMouseButtonUp;
				if (lmbup3 != null)
				{
					array[j] = new PM_Hook(delegate
					{
						if (Input.GetMouseButtonUp(0) || this.updatingMP)
						{
							this.fsm.Fsm.Event(lmbup3.sendEvent);
							if (!this.updatingMP)
							{
								this.stage = FsmIgnition.ActionType.ACC;
								this.SendUpdate();
							}
							this.updatingMP = false;
						}
					}, true);
					break;
				}
			}
			fsmState.Actions = array;
			FsmEvent start = this.fsm.FsmEvents.FirstOrDefault((FsmEvent e) => e.Name == "START");
			FsmState state = this.fsm.GetState("ACC on 2");
			array = state.Actions;
			for (int k = 0; k < array.Length; k++)
			{
				GetMouseButtonUp lmbup = array[k] as GetMouseButtonUp;
				if (lmbup != null)
				{
					array[k] = new PM_Hook(delegate
					{
						if (this.starting)
						{
							this.fsm.Fsm.Event(start);
							this.updatingMP = false;
							return;
						}
						if (Input.GetMouseButtonUp(0) || this.updatingMP)
						{
							this.fsm.Fsm.Event(lmbup.sendEvent);
						}
					}, true);
					break;
				}
			}
			state.Actions = array;
			this.update = new GameEvent("IgnitionUpdate" + ignition.transform.root.name, new Action<GameEventReader>(this.Update), GameScene.GAME);
			this.initSync = new GameEvent("IgnitionInitSync" + ignition.transform.root.name, new Action<GameEventReader>(this.InitSync), GameScene.GAME);
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
			{
				if (!WreckMPGlobals.IsHost)
				{
					return;
				}
				using (GameEventWriter gameEventWriter = this.initSync.Writer())
				{
					gameEventWriter.Write(this.starter.plugHeat.Value);
					gameEventWriter.Write(this.starter.engineTemp.Value);
					gameEventWriter.Write((byte)this.stage);
					this.initSync.Send(gameEventWriter, u, true, default(GameEvent.RecordingProperties));
				}
			});
			Console.Log("Init ignition for " + this.fsm.transform.root.name, false);
		}

		private void SendUpdate()
		{
			using (GameEventWriter gameEventWriter = this.update.Writer())
			{
				gameEventWriter.Write((byte)this.stage);
				this.update.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void InitSync(GameEventReader obj)
		{
			this.starter.plugHeat.Value = obj.ReadSingle();
			this.starter.engineTemp.Value = obj.ReadSingle();
			FsmIgnition.ActionType actionType = (FsmIgnition.ActionType)obj.ReadByte();
			this.stage = actionType;
			this.ToggleKey(actionType);
		}

		private void Update(GameEventReader obj)
		{
			FsmIgnition.ActionType actionType = (FsmIgnition.ActionType)obj.ReadByte();
			this.stage = actionType;
			this.ToggleKey(actionType);
		}

		public void ToggleKey(FsmIgnition.ActionType action)
		{
			switch (action)
			{
			case FsmIgnition.ActionType.Off:
				this.acc.Value = true;
				this.starter.Start(false, false);
				break;
			case FsmIgnition.ActionType.ACC:
				this.acc.Value = false;
				this.starter.Start(false, true);
				break;
			case FsmIgnition.ActionType.MotorRunning:
				this.acc.Value = true;
				this.starter.Start(true, true);
				break;
			}
			this.updatingMP = true;
			this.starting = action == FsmIgnition.ActionType.MotorRunning;
			this.motorOn.Value = false;
			this.fsm.Fsm.Event(this.updateIgnition);
		}

		private FsmStarter starter;

		private GameEvent update;

		private GameEvent initSync;

		private PlayMakerFSM fsm;

		private FsmBool acc;

		private FsmBool motorOn;

		private FsmEvent updateIgnition;

		internal bool updatingMP;

		internal bool starting;

		private FsmIgnition.ActionType stage;

		public enum ActionType
		{
			Off,
			ACC,
			MotorRunning
		}
	}
}
