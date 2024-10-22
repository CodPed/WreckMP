using System;
using HutongGames.PlayMaker;

namespace WreckMP
{
	internal class StoreProduct
	{
		public StoreProduct(PlayMakerFSM fsm)
		{
			this.fsm = fsm;
			fsm.Initialize();
			this.Quantity = fsm.FsmVariables.FindFsmInt("Quantity");
			this.Bought = fsm.FsmVariables.FindFsmInt("Bought");
			this.Name = fsm.name;
			this.Purchase = fsm.AddEvent("MP_PURCHASE");
			if (fsm.HasState("Check inventory"))
			{
				fsm.AddGlobalTransition(this.Purchase, "Check inventory");
				fsm.InsertAction("Check inventory", delegate
				{
					if (this.doSync)
					{
						this.purchase.SendEmpty(0UL, true);
					}
					this.doSync = true;
				}, 2, false);
			}
			else
			{
				fsm.AddGlobalTransition(this.Purchase, "Play anim");
				fsm.InsertAction("Play anim", delegate
				{
					if (this.doSync)
					{
						this.purchase.SendEmpty(0UL, true);
					}
					this.doSync = true;
				}, 2, false);
			}
			if (fsm.HasState("Check if 0"))
			{
				this.Depurchase = fsm.AddEvent("MP_DEPURCHASE");
				fsm.AddGlobalTransition(this.Depurchase, "Check if 0");
				fsm.InsertAction("Check if 0", delegate
				{
					if (this.doSync)
					{
						this.depurchase.SendEmpty(0UL, true);
					}
					this.doSync = true;
				}, 2, false);
			}
			this.purchase = new GameEvent<StoreProduct>("Purchase" + this.Name, new Action<ulong, GameEventReader>(this.OnPurchase), GameScene.GAME);
			this.depurchase = new GameEvent<StoreProduct>("Depurchase" + this.Name, new Action<ulong, GameEventReader>(this.OnDepurchase), GameScene.GAME);
			this.syncInitial = new GameEvent<StoreProduct>("SyncInitial" + this.Name, new Action<ulong, GameEventReader>(this.OnSyncInitial), GameScene.GAME);
		}

		private void OnSyncInitial(ulong sender, GameEventReader packet)
		{
			this.Quantity.Value = packet.ReadInt32();
			this.Bought.Value = packet.ReadInt32();
		}

		private void OnPurchase(ulong sender, GameEventReader packet)
		{
			if (sender == WreckMPGlobals.UserID)
			{
				return;
			}
			this.doSync = false;
			this.fsm.SendEvent(this.Purchase.Name);
		}

		private void OnDepurchase(ulong sender, GameEventReader packet)
		{
			if (sender == WreckMPGlobals.UserID)
			{
				return;
			}
			this.doSync = false;
			this.fsm.SendEvent(this.Depurchase.Name);
		}

		public void SyncInitial(ulong target)
		{
			using (GameEventWriter gameEventWriter = this.syncInitial.Writer())
			{
				if (this.Quantity != null && this.Bought != null)
				{
					gameEventWriter.Write(this.Quantity.Value);
					gameEventWriter.Write(this.Bought.Value);
					this.syncInitial.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		public PlayMakerFSM fsm;

		public FsmInt Quantity;

		public FsmInt Bought;

		public string Name;

		public FsmEvent Purchase;

		public FsmEvent Depurchase;

		public GameEvent<StoreProduct> purchase;

		public GameEvent<StoreProduct> depurchase;

		public GameEvent<StoreProduct> syncInitial;

		public bool doSync = true;
	}
}
