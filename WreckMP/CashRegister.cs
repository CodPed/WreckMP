using System;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class CashRegister
	{
		public CashRegister(PlayMakerFSM fsm, GameObject bagCreator)
		{
			this.bagCreator = bagCreator;
			PlayMakerFSM component = bagCreator.GetComponent<PlayMakerFSM>();
			component.Initialize();
			FsmGameObject bag = component.FsmVariables.FindFsmGameObject("Bag");
			component.InsertAction("Activate bag", delegate
			{
				GameObject value = bag.Value;
				if (value != null)
				{
					Rigidbody component2 = value.GetComponent<Rigidbody>();
					if (component2 != null)
					{
						NetRigidbodyManager.AddRigidbody(component2, value.name.GetHashCode());
					}
				}
			}, -1, false);
			this.fsm = fsm;
			fsm.Initialize();
			this.interact = fsm.AddEvent("MP_INTERACT");
			fsm.AddGlobalTransition(this.interact, "Check money");
			fsm.InsertAction("Check money", delegate
			{
				using (GameEventWriter gameEventWriter = this.useRegister.Writer())
				{
					if (this.doSync)
					{
						this.useRegister.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					}
					this.doSync = true;
				}
			}, 0, false);
			this.intVars = fsm.FsmVariables.IntVariables;
			this.useRegister = new GameEvent<CashRegister>("UseStoreRegister", new Action<ulong, GameEventReader>(this.OnUseRegister), GameScene.GAME);
			this.syncInitial = new GameEvent<CashRegister>("SyncStoreRegisterInitial", new Action<ulong, GameEventReader>(this.OnSyncInitial), GameScene.GAME);
		}

		private void OnUseRegister(ulong sender, GameEventReader packet)
		{
			if (sender == WreckMPGlobals.UserID)
			{
				return;
			}
			this.doSync = false;
			for (int i = 0; i < this.intVars.Length; i++)
			{
				if (this.resetVars.Contains(this.intVars[i].Name))
				{
					this.intVars[i].Value = 0;
				}
			}
			this.bagCreator.SetActive(true);
			this.fsm.SendEvent(this.interact.Name);
		}

		public void SyncInitial(ulong target)
		{
			using (GameEventWriter gameEventWriter = this.syncInitial.Writer())
			{
				gameEventWriter.Write(this.intVars.Length);
				for (int i = 0; i < this.intVars.Length; i++)
				{
					gameEventWriter.Write(this.intVars[i].Name);
					gameEventWriter.Write(this.intVars[i].Value);
				}
				this.syncInitial.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnSyncInitial(ulong sender, GameEventReader packet)
		{
			int num = packet.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string name = packet.ReadString();
				int num2 = packet.ReadInt32();
				if (this.intVars.Any((FsmInt x) => x.Name == name))
				{
					this.intVars.FirstOrDefault((FsmInt x) => x.Name == name).Value = num2;
				}
			}
		}

		public PlayMakerFSM fsm;

		public FsmEvent interact;

		private GameObject bagCreator;

		public GameEvent<CashRegister> useRegister;

		public GameEvent<CashRegister> syncInitial;

		public bool doSync = true;

		public FsmInt[] intVars;

		private readonly string[] resetVars = new string[] { "QBeer", "QCarBattery", "QCharcoal", "QCoolant", "QExtinguisher", "QMotorOil", "QTwoStroke" };
	}
}
