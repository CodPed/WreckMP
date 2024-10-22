using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class NetCreateItemsManager : NetManager
	{
		public static PlayMakerFSM[] Beercases
		{
			get
			{
				object[] array = NetCreateItemsManager.beerDB.arrayList.ToArray();
				PlayMakerFSM[] array2 = new PlayMakerFSM[array.Length];
				for (int i = 0; i < array2.Length; i++)
				{
					array2[i] = (array[i] as GameObject).GetPlayMaker("Use");
				}
				return array2;
			}
		}

		private void Start()
		{
			GameObject gameObject = Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault((GameObject x) => x.name == "Spawner" && x.transform.root == x.transform);
			this.createItems = gameObject.transform.Find("CreateItems").gameObject;
			PlayMakerFSM[] array = this.createItems.GetComponents<PlayMakerFSM>();
			for (int i = 0; i < array.Length; i++)
			{
				NetCreateItemsManager.creators.Add(new NetCreateItemsManager.Creator(array[i], false));
			}
			this.createSpraycans = gameObject.transform.Find("CreateSpraycans").gameObject;
			array = this.createSpraycans.GetComponents<PlayMakerFSM>();
			for (int j = 0; j < array.Length; j++)
			{
				NetCreateItemsManager.creators.Add(new NetCreateItemsManager.Creator(array[j], false));
			}
			this.createMooseMeat = gameObject.transform.Find("CreateMooseMeat").gameObject;
			array = this.createMooseMeat.GetComponents<PlayMakerFSM>();
			for (int k = 0; k < array.Length; k++)
			{
				NetCreateItemsManager.creators.Add(new NetCreateItemsManager.Creator(array[k], false));
			}
			this.createShoppingbag = gameObject.transform.Find("CreateShoppingbag").gameObject;
			array = this.createShoppingbag.GetComponents<PlayMakerFSM>();
			for (int l = 0; l < array.Length; l++)
			{
				NetCreateItemsManager.creators.Add(new NetCreateItemsManager.Creator(array[l], true));
			}
			NetCreateItemsManager.beerDB = gameObject.transform.Find("BeerDB").GetComponent<PlayMakerArrayListProxy>();
			WreckMPGlobals.OnMemberReady.Add(delegate(ulong user)
			{
				if (!WreckMPGlobals.IsHost)
				{
					return;
				}
				for (int m = 0; m < NetCreateItemsManager.creators.Count; m++)
				{
					NetCreateItemsManager.creators[m].SyncInitial(user);
				}
			});
		}

		public GameObject createItems;

		public GameObject createSpraycans;

		public GameObject createMooseMeat;

		public GameObject createShoppingbag;

		public static List<NetCreateItemsManager.Creator> creators = new List<NetCreateItemsManager.Creator>();

		private static PlayMakerArrayListProxy beerDB;

		internal class Item
		{
			public Transform transform
			{
				get
				{
					return this.fsm.transform;
				}
			}

			public Item(PlayMakerFSM fsm, NetCreateItemsManager.Creator creator, string id)
			{
				this.fsm = fsm;
				this.ID = id;
				this.hash = this.ID.GetHashCode();
				this.rb = fsm.gameObject.GetComponent<Rigidbody>();
				this.orb = NetRigidbodyManager.AddRigidbody(this.rb, this.hash);
				this.creator = creator;
				this.Owner = fsm.FsmVariables.FindFsmGameObject("Owner");
				this.CheckPart();
				this.CheckShoppingbag();
				this.CheckBeercase();
			}

			private void CheckBeercase()
			{
				string text = this.rb.gameObject.name.ToLower();
				if (text.Contains("beer") && text.Contains("case"))
				{
					NetItemsManager.SetupBeercaseFSM(this.fsm, this);
				}
			}

			private void CheckPart()
			{
				PlayMakerFSM playMaker = this.fsm.gameObject.GetPlayMaker("Removal");
				if (playMaker != null)
				{
					NetPartManager.SetupRemovalPlaymaker(playMaker, this.hash);
					this.orb.Removal_Rigidbody = playMaker.FsmVariables.FindFsmObject("Rigidbody");
					this.orb.remove = playMaker;
				}
				PlayMakerFSM playMaker2 = this.fsm.gameObject.GetPlayMaker("Screw");
				if (playMaker2 != null && !NetPartManager.AddBolt(playMaker2, this.hash))
				{
					Console.LogError(string.Format("Bolt of hash {0} ({1}) doesn't have stage variable", this.hash, this.ID), false);
				}
			}

			private void CheckShoppingbag()
			{
				FsmGameObject fsmGameObject = this.fsm.FsmVariables.FindFsmGameObject("ProductSpawner");
				if (fsmGameObject == null)
				{
					return;
				}
				this.ProductSpawner = fsmGameObject.Value.GetPlayMaker("Logic");
				FsmGameObject currentBag = this.ProductSpawner.FsmVariables.FindFsmGameObject("CurrentBag");
				PlayMakerArrayListProxy[] components = this.fsm.gameObject.GetComponents<PlayMakerArrayListProxy>();
				this.Items = components.FirstOrDefault((PlayMakerArrayListProxy x) => x.referenceName == "Items");
				this.Spraycans = components.FirstOrDefault((PlayMakerArrayListProxy x) => x.referenceName == "Spraycans");
				if (!WreckMPGlobals.IsHost)
				{
					this.fsm.GetState("Spawn one").Actions[0].Enabled = false;
				}
				if (!WreckMPGlobals.IsHost)
				{
					this.fsm.GetState("Spawn all").Actions[0].Enabled = false;
				}
				FsmEvent spawnOne = this.fsm.AddEvent("MP_SPAWNONE");
				this.fsm.AddGlobalTransition(spawnOne, "Spawn one");
				this.fsm.InsertAction("Spawn one", delegate
				{
					if (WreckMPGlobals.IsHost)
					{
						using (GameEventWriter gameEventWriter = this.ShoppingUpdateList.Writer())
						{
							gameEventWriter.Write(this.Items._arrayList.Count);
							gameEventWriter.Write(this.Spraycans._arrayList.Count);
							int num = 0;
							for (int i = 0; i < this.Items._arrayList.Count; i++)
							{
								gameEventWriter.Write((int)this.Items._arrayList[i]);
								num += (int)this.Items._arrayList[i];
							}
							for (int j = 0; j < this.Spraycans._arrayList.Count; j++)
							{
								gameEventWriter.Write((int)this.Spraycans._arrayList[j]);
								num += (int)this.Spraycans._arrayList[j];
							}
							if (num <= 1)
							{
								this.ShoppingDestroy.SendEmpty(0UL, true);
								return;
							}
							this.ShoppingUpdateList.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
							return;
						}
					}
					this.ShoppingSpawnOne.SendEmpty(WreckMPGlobals.HostID, true);
				}, 0, false);
				FsmEvent spawnAll = this.fsm.AddEvent("MP_SPAWNALL");
				this.fsm.AddGlobalTransition(spawnAll, "Spawn all");
				this.fsm.InsertAction("Spawn all", delegate
				{
					currentBag.Value = this.Owner.Value;
					if (WreckMPGlobals.IsHost)
					{
						this.ShoppingDestroy.SendEmpty(0UL, true);
						return;
					}
					this.ShoppingSpawnAll.SendEmpty(WreckMPGlobals.HostID, true);
				}, 0, false);
				this.ShoppingDestroy = new GameEvent(this.ID + "BagDestroy", delegate(GameEventReader p)
				{
					if (p.sender != WreckMPGlobals.HostID)
					{
						return;
					}
					this.fsm.SendEvent("GARBAGE");
				}, GameScene.GAME);
				this.ShoppingSpawnAll = new GameEvent(this.ID + "BagSpawnall", delegate(GameEventReader p)
				{
					if (!WreckMPGlobals.IsHost)
					{
						return;
					}
					currentBag.Value = this.fsm.gameObject;
					this.fsm.SendEvent(spawnAll.Name);
				}, GameScene.GAME);
				this.ShoppingSpawnOne = new GameEvent(this.ID + "Bagspawnone", delegate(GameEventReader p)
				{
					if (!WreckMPGlobals.IsHost)
					{
						return;
					}
					currentBag.Value = this.fsm.gameObject;
					this.fsm.SendEvent(spawnOne.Name);
				}, GameScene.GAME);
				this.ShoppingUpdateList = new GameEvent(this.ID + "Bagupdatelist", delegate(GameEventReader p)
				{
					if (p.sender != WreckMPGlobals.HostID)
					{
						return;
					}
					int num2 = p.ReadInt32();
					int num3 = p.ReadInt32();
					for (int k = 0; k < num2; k++)
					{
						this.Items._arrayList[k] = p.ReadInt32();
					}
					for (int l = 0; l < num3; l++)
					{
						this.Spraycans._arrayList[l] = p.ReadInt32();
					}
				}, GameScene.GAME);
			}

			public OwnedRigidbody orb;

			public PlayMakerFSM fsm;

			public NetCreateItemsManager.Creator creator;

			public int hash;

			public Rigidbody rb;

			public string ID;

			public FsmGameObject Owner;

			public PlayMakerFSM ProductSpawner;

			public PlayMakerArrayListProxy Items;

			public PlayMakerArrayListProxy Spraycans;

			private bool doSync = true;

			private GameEvent ShoppingUpdateList;

			private GameEvent ShoppingDestroy;

			private GameEvent ShoppingSpawnOne;

			private GameEvent ShoppingSpawnAll;
		}

		internal class Creator
		{
			public Creator(PlayMakerFSM fsm, bool isShopbag = false)
			{
				NetCreateItemsManager.Creator <>4__this = this;
				this.name = fsm.FsmName;
				this.isShopbag = isShopbag;
				this.fsm = fsm;
				if (this.name == "Beer")
				{
					Transform transform = GameObject.Find("ITEMS").transform.Find("beercase0");
					if (transform != null)
					{
						this.items.Add(new NetCreateItemsManager.Item(transform.GetPlayMaker("Use"), this, "beercase_default"));
					}
				}
				fsm.Initialize();
				this.Condition = fsm.FsmVariables.FindFsmFloat("Condition");
				this.ObjectNumberInt = fsm.FsmVariables.FindFsmInt("ObjectNumberInt");
				this.New = fsm.FsmVariables.FindFsmGameObject("New");
				this.newItemID = fsm.FsmVariables.FindFsmString("ID");
				this.CreateItem = fsm.AddEvent("MP_CREATEITEM");
				fsm.AddGlobalTransition(this.CreateItem, fsm.HasState("Create") ? "Create" : "Add ID");
				this.SpawnItem = fsm.AddEvent("MP_SPAWNITEM");
				fsm.AddGlobalTransition(this.SpawnItem, fsm.HasState("Spawn") ? "Spawn" : "Create product");
				this.spawnInitItem = new GameEvent<NetCreateItemsManager.Creator>("SpawnInit" + this.name, new Action<ulong, GameEventReader>(this.OnSpawnInitItem), GameScene.GAME);
				this.spawnItem = new GameEvent<NetCreateItemsManager.Creator>("Spawn" + this.name, delegate(ulong sender, GameEventReader GameEventReader)
				{
					if (sender == WreckMPGlobals.UserID)
					{
						return;
					}
					<>4__this.doSync = false;
					fsm.SendEvent(<>4__this.SpawnItem.Name);
				}, GameScene.GAME);
				fsm.Initialize();
				if (fsm.HasState("Spawn"))
				{
					fsm.InsertAction("Spawn", delegate
					{
						<>4__this.OnCreateItem();
					}, -1, false);
				}
				if (fsm.HasState("Create product"))
				{
					fsm.InsertAction("Create product", delegate
					{
						<>4__this.OnCreateItem();
					}, -1, false);
				}
				if (fsm.HasState("Create"))
				{
					fsm.InsertAction("Create", delegate
					{
						<>4__this.OnCreateItem();
					}, 5, false);
				}
				if (fsm.HasState("Add ID"))
				{
					fsm.InsertAction("Add ID", delegate
					{
						<>4__this.OnCreateItem();
					}, 5, false);
				}
			}

			private void OnCreateItem()
			{
				GameObject value = this.New.Value;
				this.items.Add(new NetCreateItemsManager.Item(value.GetPlayMaker("Use"), this, this.newItemID.Value));
				if (this.doSync && _ObjectsLoader.IsGameLoaded && !this.isShopbag)
				{
					using (GameEventWriter gameEventWriter = this.spawnItem.Writer())
					{
						this.spawnItem.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}
				this.doSync = true;
			}

			private void OnSpawnInitItem(ulong sender, GameEventReader packet)
			{
				if (sender == WreckMPGlobals.UserID)
				{
					return;
				}
				int num = packet.ReadInt32();
				if (this.items.Count >= num)
				{
					return;
				}
				for (int i = 0; i < num; i++)
				{
					packet.ReadVector3();
					packet.ReadVector3();
					if (i >= this.items.Count)
					{
						this.fsm.SendEvent(this.SpawnItem.Name);
					}
				}
			}

			public void SyncInitial(ulong target)
			{
				int count = this.items.Count;
			}

			public string name;

			public PlayMakerFSM fsm;

			public FsmString newItemID;

			public FsmFloat Condition;

			public FsmInt ObjectNumberInt;

			public FsmGameObject New;

			public FsmEvent SpawnItem;

			public FsmEvent CreateItem;

			public List<NetCreateItemsManager.Item> items = new List<NetCreateItemsManager.Item>();

			private bool doSync = true;

			private bool isShopbag;

			private GameEvent<NetCreateItemsManager.Creator> spawnInitItem;

			private GameEvent<NetCreateItemsManager.Creator> spawnItem;
		}
	}
}
