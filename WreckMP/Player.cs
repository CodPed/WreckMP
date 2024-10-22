using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using WreckMP.Properties;

namespace WreckMP
{
	public class Player : MonoBehaviour
	{
		public ulong SteamID
		{
			get
			{
				return this._steamID;
			}
			internal set
			{
				this._steamID = value;
				if (this.useName)
				{
					this.pname = SteamFriends.GetFriendPersonaName((CSteamID)value);
				}
			}
		}

		public string PlayerName
		{
			get
			{
				return this.pname;
			}
		}

		public Vector3 HeadPos
		{
			get
			{
				return this.head.position;
			}
		}

		internal void SetInCar(bool inCar, NetVehicle car)
		{
			this.SetPlayerParent(inCar ? car.driverPivots.driverParent : null, false);
			this.playerAnimationManager.SetPlayerInCar(inCar, car);
		}

		internal void SetPassengerMode(bool enter, Transform car, bool applySitAnim = true)
		{
			this.SetPlayerParent(enter ? car : null, true);
			this.playerAnimationManager.SetPassengerMode(enter && applySitAnim);
		}

		internal void SetPlayerParent(Transform parent, bool worldPositionStays)
		{
			this.playerAnimationManager.allowMoveAnims = parent == null;
			this.player.transform.SetParent((parent == null) ? base.transform : parent, parent == null || worldPositionStays);
			if (!worldPositionStays && parent != null)
			{
				this.player.transform.localPosition = (this.player.transform.localEulerAngles = Vector3.zero);
			}
		}

		private void Start()
		{
			this.isGhost = SteamNet.allowedGhostPlayer == (CSteamID)this.SteamID;
			if (this.isGhost)
			{
				new GameEvent("ToggleMesh" + this.SteamID.ToString(), new Action<GameEventReader>(this.OnToggleMesh), GameScene.GAME);
			}
			base.transform.parent = WreckMP.instance.transform;
			this.SyncPosition = new GameEvent(string.Format("SyncPosition{0}", this.SteamID), new Action<GameEventReader>(this.OnSyncPosition), GameScene.GAME);
			this.grabItem = new GameEvent("GrabItem" + this.SteamID.ToString(), new Action<GameEventReader>(this.OnGrabItem), GameScene.GAME);
			AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(Resources.clothes);
			this.player = Object.Instantiate<GameObject>(assetBundle.LoadAsset<GameObject>("char.prefab"));
			if (this.disableModel)
			{
				this.player.SetActive(false);
			}
			this.player.name = string.Format("{0} ({1})", this.PlayerName, this.SteamID);
			this.player.transform.parent = base.transform;
			this.head = this.player.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot");
			this.player.SetActive(!this.isGhost);
			this.jonnez = GameObject.Find("JONNEZ ES(Clone)");
			this.jonnezColl = this.jonnez.transform.Find("Colliders/Coll").gameObject;
			SkinnedMeshRenderer component = this.player.transform.Find("char/bodymesh").GetComponent<SkinnedMeshRenderer>();
			component.materials[0] = new Material(component.materials[0]);
			component.materials[1] = new Material(component.materials[1]);
			component.materials[2] = new Material(component.materials[2]);
			GameObject gameObject = this.player.transform.Find("char/PUSH").gameObject;
			this.pushEvent = new GameEvent(string.Format("Push{0}", this.SteamID), delegate(GameEventReader r)
			{
			}, GameScene.GAME);
			gameObject.SetActive(false);
			this.playerAnimationManager = this.player.transform.Find("char/skeleton").gameObject.AddComponent<PlayerAnimationManager>();
			this.playerAnimationManager.charTf = this.player.transform.Find("char");
			MeshRenderer component2 = this.player.transform.Find("char/skeleton/thig_left/knee_left/ankle_left/shoeLeft").GetComponent<MeshRenderer>();
			MeshRenderer component3 = this.player.transform.Find("char/skeleton/thig_right/knee_right/ankle_right/shoeRight").GetComponent<MeshRenderer>();
			Material material = new Material(component2.material);
			component2.material = material;
			component3.material = material;
			CharacterCustomizationItem.parentTo = base.transform;
			this.characterCustomizationItems = new CharacterCustomizationItem[]
			{
				CharacterCustomizationItem.Init(0, null, null, null, null, null, null, this.player.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/glasses"), null),
				CharacterCustomizationItem.Init(1, null, null, null, null, null, null, this.player.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/head_end"), null),
				CharacterCustomizationItem.Init(2, null, null, null, null, CharacterCustomization.faces, component.materials[2], null, null),
				CharacterCustomizationItem.Init(3, null, null, null, null, CharacterCustomization.shirts, component.materials[0], null, null),
				CharacterCustomizationItem.Init(4, null, null, null, null, CharacterCustomization.pants, component.materials[1], null, null),
				CharacterCustomizationItem.Init(5, null, null, null, null, CharacterCustomization.shoes, material, component2.transform, component3.transform)
			};
			CharacterCustomizationItem.parentTo = null;
			assetBundle.Unload(false);
			WreckMPGlobals.OnMemberExit = (Action<ulong>)Delegate.Combine(WreckMPGlobals.OnMemberExit, new Action<ulong>(this.OnLeave));
		}

		private void OnLeave(ulong player)
		{
			if (player != this.SteamID)
			{
				return;
			}
			WreckMPGlobals.OnMemberExit = (Action<ulong>)Delegate.Remove(WreckMPGlobals.OnMemberExit, new Action<ulong>(this.OnLeave));
			int num = Player.grabbedItemsHashes.Count;
			while (Player.grabbedItemsHashes.Count > 0)
			{
				this.OnGrabItem(false, Player.grabbedItemsHashes[0], Vector3.zero);
				if (Player.grabbedItemsHashes.Count == num)
				{
					Console.LogError("An error occured trying to drop items old player was holding. ERR_NO_REMOVE", true);
					return;
				}
				num = Player.grabbedItemsHashes.Count;
			}
		}

		private void OnToggleMesh(GameEventReader packet)
		{
			if (packet.sender != this.SteamID)
			{
				return;
			}
			bool flag = packet.ReadBoolean();
			this.player.SetActive(flag);
		}

		private void OnGrabItem(GameEventReader packet)
		{
			if (packet.sender != this.SteamID)
			{
				return;
			}
			bool flag = packet.ReadBoolean();
			int num = packet.ReadInt32();
			Vector3 vector = Vector3.zero;
			if (packet.UnreadLength() > 0)
			{
				vector = packet.ReadVector3();
			}
			this.OnGrabItem(flag, num, vector);
		}

		private void OnGrabItem(bool grab, int hash, Vector3 throwVelocity)
		{
			if (grab)
			{
				this.grabbedItem = NetRigidbodyManager.GetRigidbody(hash);
				if (this.grabbedItem == null)
				{
					Console.LogError(string.Format("Player {0} grabbed unknown rigidbody of hash {1}", this.PlayerName, hash), false);
				}
				else
				{
					this.grabbedItemCollsToggle = 0;
					this.grabbedItemColls = this.grabbedItem.transform.GetComponents<Collider>();
					for (int i = 0; i < this.grabbedItemColls.Length; i++)
					{
						if (!this.grabbedItemColls[i].isTrigger)
						{
							this.grabbedItemColls[i].isTrigger = true;
							this.grabbedItemCollsToggle |= 1 << i;
						}
					}
					if (this.grabbedItem.gameObject == this.jonnez)
					{
						this.jonnezColl.SetActive(false);
					}
					this.grabbedItem.isKinematic = true;
					this.grabbedItem.gameObject.layer = 16;
					Player.grabbedItems.Add(this.grabbedItem);
					Player.grabbedItemsHashes.Add(hash);
				}
			}
			else if (this.grabbedItem != null)
			{
				for (int j = 0; j < this.grabbedItemColls.Length; j++)
				{
					if ((this.grabbedItemCollsToggle >> j) % 2 == 1)
					{
						this.grabbedItemColls[j].isTrigger = false;
					}
				}
				if (this.grabbedItem.gameObject == this.jonnez)
				{
					this.jonnezColl.SetActive(true);
				}
				this.grabbedItem.isKinematic = false;
				this.grabbedItem.gameObject.layer = 19;
				Player.grabbedItems.Remove(this.grabbedItem);
				Player.grabbedItemsHashes.Remove(hash);
				if (WreckMPGlobals.IsHost)
				{
					NetRigidbodyManager.RequestOwnership(this.grabbedItem);
					this.grabbedItem.AddForce(throwVelocity);
				}
				this.grabbedItem = null;
				this.grabbedItemColls = null;
				this.grabbedItemCollsToggle = 0;
			}
			this.playerAnimationManager.GrabItem(this.grabbedItem);
		}

		internal void OnSyncPosition(GameEventReader packet)
		{
			this.pos = packet.ReadVector3();
			this.pos += this.offest;
			this.rot = packet.ReadVector3();
			float num = packet.ReadSingle();
			if (this.player.transform.parent == base.transform)
			{
				this.head.localEulerAngles = Vector3.forward * -num;
				return;
			}
			this.head.eulerAngles = new Vector3(0f, this.rot.y - 90f, -num);
		}

		internal void OnInitialSkinSync(int[] skinPreset)
		{
			for (int i = 0; i < skinPreset.Length; i++)
			{
				this.OnSkinChange(i, skinPreset[i]);
			}
		}

		internal void OnSkinChange(int clothesIndex, int selectedIndex)
		{
			this.characterCustomizationItems[clothesIndex].SetOption(selectedIndex, false);
		}

		private void FixedUpdate()
		{
			if (this.player.transform.parent == base.transform)
			{
				this.player.transform.position = Vector3.Lerp(this.player.transform.position, this.pos, Time.deltaTime * 15f);
				this.player.transform.eulerAngles = Vector3.Lerp(this.player.transform.eulerAngles, this.rot, Time.deltaTime * 30f);
			}
		}

		internal void Leave()
		{
			this.player.SetActive(false);
		}

		private ulong _steamID;

		internal bool useName = true;

		private string pname;

		public GameObject player;

		internal PlayerAnimationManager playerAnimationManager;

		private GameEvent SyncPosition;

		private GameEvent grabItem;

		private GameEvent pushEvent;

		private Vector3 offest = new Vector3(0f, 0.17f, 0f);

		private Vector3 pos;

		private Vector3 rot;

		private GameObject jonnez;

		private GameObject jonnezColl;

		private Transform head;

		private CharacterCustomizationItem[] characterCustomizationItems;

		private Rigidbody grabbedItem;

		private Collider[] grabbedItemColls;

		private int grabbedItemCollsToggle;

		private const string grabItemEvent = "GrabItem";

		internal static List<Rigidbody> grabbedItems = new List<Rigidbody>();

		internal static List<int> grabbedItemsHashes = new List<int>();

		internal bool disableModel = true;

		internal bool isGhost;
	}
}
