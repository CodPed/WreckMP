using System;
using HutongGames.PlayMaker;
using Steamworks;
using UnityEngine;
using WreckMP.Properties;

namespace WreckMP
{
	internal class LocalPlayer : MonoBehaviour
	{
		public static LocalPlayer Instance { get; private set; }

		public Transform playerRoot
		{
			get
			{
				return this.player.Value.transform.root;
			}
		}

		private void Awake()
		{
			AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(Resources.clothes);
			CharacterCustomization.LoadTextures(assetBundle);
			assetBundle.Unload(false);
			LocalPlayer.Instance = this;
		}

		private void Start()
		{
			this.isGhost = SteamNet.allowedGhostPlayer == SteamUser.GetSteamID();
			if (this.isGhost)
			{
				LocalPlayer.toggleMesh = new GameEvent("ToggleMesh" + SteamUser.GetSteamID().ToString(), delegate(GameEventReader p)
				{
					Console.Log(string.Format("{0} tried to toggle your mesh!", p.sender), true);
				}, GameScene.GAME);
			}
			new GameEvent("PlayerJoined", delegate(GameEventReader p)
			{
				Action<ulong> onMemberJoin = WreckMPGlobals.OnMemberJoin;
				if (onMemberJoin == null)
				{
					return;
				}
				onMemberJoin(p.sender);
			}, GameScene.GAME).SendEmpty(0UL, true);
			LocalPlayer.syncPositionEvent = new GameEvent(string.Format("SyncPosition{0}", WreckMPGlobals.UserID), delegate(GameEventReader r)
			{
			}, GameScene.GAME);
			LocalPlayer.grabItemEvent = new GameEvent(string.Format("GrabItem{0}", WreckMPGlobals.UserID), delegate(GameEventReader r)
			{
			}, GameScene.GAME);
			LocalPlayer.pushEvent = new GameEvent(string.Format("Push{0}", WreckMPGlobals.UserID), delegate(GameEventReader r)
			{
			}, GameScene.GAME);
			this.player = FsmVariables.GlobalVariables.FindFsmGameObject("SavePlayer");
			this.headTrans = FsmVariables.GlobalVariables.FindFsmGameObject("SavePlayerCam");
			PlayerAnimationManager.RegisterEvents();
			base.gameObject.AddComponent<LocalPlayerAnimationManager>();
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				AssetBundle assetBundle = AssetBundle.CreateFromMemoryImmediate(Resources.clothes);
				Console.Log("charcustom init", false);
				this.characterCustomization = CharacterCustomization.Init(assetBundle);
				Console.Log("charcustom init 2", false);
				WreckMPGlobals.OnMemberReady.Add(delegate(ulong userId)
				{
					this.characterCustomization.InitialSkinSync(null, userId);
				});
				assetBundle.Unload(false);
				this.EditDeath();
				this.characterMotor = this.player.Value.GetComponent<CharacterMotor>();
				this.handPush = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera/Hand Push").gameObject;
				return "LocalPlayer";
			});
		}

		private void EditDeath()
		{
			this.death = GameObject.Find("Systems").transform.Find("Death").gameObject;
			FsmState fsmState = this.death.GetPlayMaker("Activate Dead Body").FsmStates[0];
			fsmState.Actions = new FsmStateAction[0];
			fsmState.Transitions = new FsmTransition[0];
			this.death.AddComponent<PlayerDeathManager>().isGhost = this.isGhost;
		}

		private void Update()
		{
			if (this.isGhost && Input.GetKeyDown(287))
			{
				this.meshVisible = !this.meshVisible;
				using (GameEventWriter gameEventWriter = LocalPlayer.toggleMesh.Writer())
				{
					gameEventWriter.Write(this.meshVisible);
					LocalPlayer.toggleMesh.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					Console.Log("Set your mesh visibility to " + (this.meshVisible ? "visible" : "hidden"), true);
				}
			}
			LocalPlayer.random = Random.Range(0, LocalPlayer.randomMax);
		}

		private void FixedUpdate()
		{
			if (this.player.Value == null)
			{
				return;
			}
			if (this.headTrans.Value == null)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = LocalPlayer.syncPositionEvent.Writer())
			{
				Transform transform = this.player.Value.transform;
				gameEventWriter.Write(transform.position);
				gameEventWriter.Write(transform.eulerAngles);
				gameEventWriter.Write(this.headTrans.Value.transform.localEulerAngles.x);
				LocalPlayer.syncPositionEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				if (GameEventRouter.IsRecordingPackets)
				{
					using (GameEventWriter gameEventWriter2 = LocalPlayer.syncPositionRecEvent.Writer())
					{
						gameEventWriter2.Write(transform.position);
						gameEventWriter2.Write(transform.eulerAngles);
						gameEventWriter2.Write(this.headTrans.Value.transform.localEulerAngles.x);
						LocalPlayer.syncPositionRecEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}
			}
		}

		private void OnDestroy()
		{
			LocalPlayer.Instance = null;
		}

		public FsmGameObject player;

		public FsmGameObject headTrans;

		public CharacterMotor characterMotor;

		public bool inCar;

		private Transform fpsCameraDefaultParent;

		private GameObject fpsCamera;

		private GameObject death;

		private GameObject gui;

		private GameObject gameOverScreen;

		private GameObject gameOverRespawningLabel;

		private GameObject handPush;

		private FsmFloat gameVolume;

		private bool isGhost;

		private bool respawning;

		private bool meshVisible;

		private bool pushOn;

		private CharacterCustomization characterCustomization;

		internal static GameEvent toggleMesh;

		internal static GameEvent syncPositionEvent;

		internal static GameEvent grabItemEvent;

		internal static GameEvent syncPositionRecEvent;

		internal static GameEvent grabItemRecEvent;

		internal static GameEvent pushEvent;

		public static int random;

		public static int randomMax = 1;
	}
}
