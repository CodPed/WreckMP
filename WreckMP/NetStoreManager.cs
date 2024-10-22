using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WreckMP
{
	internal class NetStoreManager : NetManager
	{
		private void Start()
		{
			this.store = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault((GameObject x) => x.name == "STORE");
			this.store.GetPlayMaker("LOD").enabled = false;
			this.syncInventory = new GameEvent("SyncStoreInventory", new Action<GameEventReader>(this.OnSyncInventory), GameScene.GAME);
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				if (!WreckMPGlobals.IsHost)
				{
					return;
				}
				this.SyncInventory(user);
				for (int j = 0; j < this.products.Count; j++)
				{
					this.products[j].SyncInitial(user);
				}
				this.register.SyncInitial(user);
			});
			Transform transform = this.store.transform.Find("LOD");
			transform.gameObject.SetActive(true);
			PlayMakerFSM[] array = (from x in transform.Find("ActivateStore").GetComponentsInChildren<PlayMakerFSM>(true)
				where x.FsmName == "Buy"
				select x).ToArray<PlayMakerFSM>();
			for (int i = 0; i < array.Length; i++)
			{
				this.products.Add(new StoreProduct(array[i]));
			}
			Transform transform2 = this.store.transform.Find("Inventory");
			this.boughtInventory = transform2.GetComponents<PlayMakerHashTableProxy>().FirstOrDefault((PlayMakerHashTableProxy x) => x.referenceName == "Bought");
			PlayMakerFSM playMaker = this.store.transform.Find("StoreCashRegister/Register").GetPlayMaker("Data");
			this.register = new CashRegister(playMaker, transform.Find("ShopFunctions/BagCreator").gameObject);
		}

		private void SyncInventory(ulong target)
		{
			using (GameEventWriter gameEventWriter = this.syncInventory.Writer())
			{
				object[] array = new object[this.boughtInventory._hashTable.Count];
				this.boughtInventory._hashTable.Keys.CopyTo(array, 0);
				gameEventWriter.Write(this.boughtInventory._hashTable.Count);
				for (int i = 0; i < this.boughtInventory._hashTable.Count; i++)
				{
					gameEventWriter.Write(array[i].ToString());
					gameEventWriter.Write((int)this.boughtInventory._hashTable[array[i]]);
				}
				this.syncInventory.Send(gameEventWriter, target, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnSyncInventory(GameEventReader packet)
		{
			int num = packet.ReadInt32();
			for (int i = 0; i < num; i++)
			{
				string text = packet.ReadString();
				int num2 = packet.ReadInt32();
				this.boughtInventory._hashTable[text] = num2;
			}
		}

		public GameObject store;

		public List<StoreProduct> products = new List<StoreProduct>();

		public CashRegister register;

		public PlayMakerHashTableProxy boughtInventory;

		public GameEvent syncInventory;
	}
}
