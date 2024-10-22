using System;
using System.Collections.Generic;
using HutongGames.PlayMaker;

namespace WreckMP
{
	internal class FsmInteriorLight
	{
		public FsmInteriorLight(PlayMakerFSM fsm)
		{
			this.fsm = fsm;
			this.hash = fsm.transform.GetGameobjectHashString().GetHashCode();
			this.SetupFSM();
			this.toggleEvent = new GameEvent("InteriorLight" + this.hash.ToString(), new Action<GameEventReader>(this.OnTriggeredAction), GameScene.GAME);
			if (!FsmInteriorLight.initSyncLoaded)
			{
				FsmInteriorLight.initSyncLoaded = true;
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong u)
				{
					if (!WreckMPGlobals.IsHost)
					{
						return;
					}
					for (int i = 0; i < FsmInteriorLight.interiorLights.Count; i++)
					{
						bool value = FsmInteriorLight.interiorLights[i].lightOn.Value;
						FsmInteriorLight.interiorLights[i].TriggeredAction(value, u);
					}
				});
			}
			FsmInteriorLight.interiorLights.Add(this);
			CoreManager.sceneLoaded = (Action<GameScene>)Delegate.Combine(CoreManager.sceneLoaded, new Action<GameScene>(delegate(GameScene a)
			{
				if (FsmInteriorLight.interiorLights.Contains(this))
				{
					FsmInteriorLight.interiorLights.Remove(this);
				}
			}));
		}

		protected void SetupFSM()
		{
			this.fsm.Initialize();
			try
			{
				this.lightOn = this.fsm.FsmVariables.FindFsmBool("LightON");
				if (this.fsm.FsmGlobalTransitions.Length == 0)
				{
					FsmEvent fsmEvent = this.fsm.AddEvent("DOOROPEN");
					FsmEvent fsmEvent2 = this.fsm.AddEvent("DOORCLOSE");
					this.fsm.AddGlobalTransition(fsmEvent, "State 2");
					this.fsm.AddGlobalTransition(fsmEvent2, "State 3");
					this.fsm.InsertAction("State 2", delegate
					{
						this.TriggeredAction(true, 0UL);
					}, -1, false);
					this.fsm.InsertAction("State 3", delegate
					{
						this.TriggeredAction(false, 0UL);
					}, -1, false);
					this.fsm.Initialize();
				}
				else
				{
					int num = 0;
					for (int i = 0; i < this.fsm.FsmGlobalTransitions.Length; i++)
					{
						if (this.fsm.FsmGlobalTransitions[i].FsmEvent.Name == "DOOROPEN")
						{
							num++;
							this.fsm.InsertAction(this.fsm.FsmGlobalTransitions[i].ToState, delegate
							{
								this.TriggeredAction(true, 0UL);
							}, -1, false);
						}
						else if (this.fsm.FsmGlobalTransitions[i].FsmEvent.Name == "DOORCLOSE")
						{
							num++;
							this.fsm.InsertAction(this.fsm.FsmGlobalTransitions[i].ToState, delegate
							{
								this.TriggeredAction(false, 0UL);
							}, -1, false);
						}
						if (num == 2)
						{
							break;
						}
					}
					if (num != 2)
					{
						Console.LogError(string.Format("Failed to setup interior light {0} ({1}): Got {2} callbacks, expected 2", this.hash, this.fsm.transform.GetGameobjectHashString(), num), false);
					}
				}
			}
			catch (Exception ex)
			{
				Console.LogError(string.Format("Failed to setup interior light {0} ({1}): {2}, {3}, {4}", new object[]
				{
					this.hash,
					this.fsm.transform.GetGameobjectHashString(),
					ex.GetType(),
					ex.Message,
					ex.StackTrace
				}), false);
			}
		}

		protected void TriggeredAction(bool on, ulong target = 0UL)
		{
			if (this.updating)
			{
				this.updating = false;
				return;
			}
			using (GameEventWriter gameEventWriter = this.toggleEvent.Writer())
			{
				gameEventWriter.Write(on);
				this.toggleEvent.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
			}
		}

		protected void OnTriggeredAction(GameEventReader p)
		{
			bool flag = p.ReadBoolean();
			this.updating = true;
			this.fsm.SendEvent(flag ? "DOOROPEN" : "DOORCLOSE");
		}

		private PlayMakerFSM fsm;

		private FsmBool lightOn;

		private bool updating;

		private int hash;

		private GameEvent toggleEvent;

		private static List<FsmInteriorLight> interiorLights = new List<FsmInteriorLight>();

		private static bool initSyncLoaded = false;
	}
}
