using System;
using System.Collections;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Steamworks;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace WreckMP
{
	internal class PlayerDeathManager : MonoBehaviour
	{
		public PlayerDeathManager()
		{
			this.player = GameObject.Find("PLAYER");
			this.charMotor = this.player.GetComponent<CharacterMotor>();
			PlayMakerFSM playMaker = this.player.GetPlayMaker("Crouch");
			this.playerInCar = playMaker.FsmVariables.FindFsmBool("PlayerInCar");
			this.playerInWater = playMaker.FsmVariables.FindFsmBool("PlayerInWater");
			this.playerThirst = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerThirst");
			this.playerHunger = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerHunger");
			this.playerStress = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerStress");
			this.playerFatigue = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerFatigue");
			this.playerDirtiness = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerDirtiness");
			this.playerUrine = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerUrine");
			this.playerMoney = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerMoney");
			GameObject gameObject = GameObject.Find("Systems");
			this.newsPhotos = gameObject.transform.Find("Photomode Cam/NewsPhotos").gameObject;
			this.optionsToggle = gameObject.transform.Find("Options").gameObject;
			this.fpsCameraParent = this.player.transform.Find("Pivot/AnimPivot/Camera/FPSCamera");
			this.fpsCamera = this.fpsCameraParent.Find("FPSCamera").gameObject;
			this.blood = this.fpsCamera.GetComponent<ScreenOverlay>();
			this.fpsCameraClone = Object.Instantiate<GameObject>(this.fpsCamera);
			this.fpsCameraClone.transform.parent = null;
			this.fpsCameraClone.SetActive(false);
			this.gameVolume = PlayMakerGlobals.Instance.Variables.FindFsmFloat("GameVolume");
			this.gui = GameObject.Find("GUI");
			this.gameOverScreen = base.transform.Find("GameOverScreen").gameObject;
			this.gameOverRespawningLabel = this.gameOverScreen.transform.Find("Saving").gameObject;
			this.gameOverRespawningLabel.transform.GetComponent<TextMesh>().text = "RESPAWNING...";
			this.deadBody = base.GetComponent<PlayMakerFSM>().FsmVariables.FindFsmGameObject("DeadBody");
			this.playerCtrlCache = new PlayerDeathManager.PlayerCtrlCache(this.player);
			this.DeathEvent = new GameEvent<PlayerDeathManager>("Death", new Action<ulong, GameEventReader>(this.OnSomeoneDieEvent), GameScene.GAME);
			PlayMakerFSM[] array = Resources.FindObjectsOfTypeAll<PlayMakerFSM>();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].Initialize();
				for (int j = 0; j < array[i].FsmStates.Length; j++)
				{
					for (int k = 0; k < array[i].FsmStates[j].Actions.Length; k++)
					{
						DestroyComponent destroyComponent = array[i].FsmStates[j].Actions[k] as DestroyComponent;
						if (destroyComponent != null && (!(destroyComponent.component.Value != "CharacterController") || !(destroyComponent.component.Value != "CharacterMotor") || !(destroyComponent.component.Value != "FPSInputController")))
						{
							destroyComponent.Enabled = false;
							if (destroyComponent.component.Value == "CharacterMotor")
							{
								array[i].InsertAction(array[i].FsmStates[j].Name, delegate
								{
									this.charMotor.canControl = false;
								}, k + 1, false);
							}
						}
					}
				}
			}
		}

		private void OnSomeoneDieEvent(ulong sender, GameEventReader packet)
		{
			CSteamID csteamID = (CSteamID)sender;
			bool flag = packet.ReadBoolean();
			Console.Log(CoreManager.playerNames[csteamID] + " " + (flag ? "has respawned" : "has died") + ". You have been charged 300 MK!", true);
			if (!flag)
			{
				this.playerMoney.Value = Mathf.Clamp(this.playerMoney.Value - 300f, 0f, float.MaxValue);
			}
			CoreManager.Players[sender].player.SetActive(flag);
		}

		private void SendIDied(bool respawned)
		{
			if (this.isGhost)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				gameEventWriter.Write(respawned);
				this.DeathEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnEnable()
		{
			base.StartCoroutine(this.OnPlayerDie());
		}

		private IEnumerator OnPlayerDie()
		{
			this.SendIDied(false);
			PlayerGrabbingManager.ForceDropItem();
			this.newsPhotos.SetActive(true);
			float volume = 1f;
			while (volume > 0f)
			{
				volume = Mathf.Clamp01(volume - Time.deltaTime / 2f);
				this.gameVolume.Value = volume;
				yield return new WaitForEndOfFrame();
			}
			this.gameVolume.Value = 0f;
			yield return new WaitForSeconds(0.4f);
			this.fpsCamera.SetActive(false);
			this.gameVolume.Value = 1f;
			this.player.SetActive(false);
			this.deadBody.Value.SetActive(false);
			this.gui.SetActive(false);
			this.gameOverScreen.SetActive(true);
			while (!Input.GetKeyDown(27) && !Input.GetKeyDown(13))
			{
				yield return new WaitForEndOfFrame();
			}
			this.gameOverRespawningLabel.SetActive(true);
			yield return new WaitForSeconds(2f);
			this.fpsCamera.transform.parent = this.fpsCameraParent;
			this.fpsCamera.transform.localPosition = Vector3.forward * -0.05f;
			this.fpsCamera.transform.localEulerAngles = Vector3.zero;
			this.fpsCamera.SetActive(true);
			this.fpsCamera.gameObject.tag = "MainCamera";
			Camera component = this.fpsCamera.GetComponent<Camera>();
			if (component == null)
			{
				Console.LogError("Player camera null after death", false);
			}
			else
			{
				component.enabled = true;
			}
			this.player.SetActive(true);
			this.player.transform.parent = null;
			this.player.transform.position = new Vector3(-1434.642f, 4.682786f, 1151.625f);
			this.player.transform.eulerAngles = new Vector3(0f, 252.6235f, 0f);
			this.charMotor.canControl = true;
			this.blood.enabled = false;
			this.playerThirst.Value = (this.playerHunger.Value = (this.playerStress.Value = (this.playerUrine.Value = (this.playerDirtiness.Value = (this.playerFatigue.Value = 0f)))));
			this.playerInCar.Value = (this.playerInWater.Value = false);
			this.gui.SetActive(true);
			this.optionsToggle.SetActive(true);
			this.newsPhotos.SetActive(false);
			this.gameOverRespawningLabel.SetActive(false);
			this.gameOverScreen.SetActive(false);
			base.gameObject.SetActive(false);
			this.playerMoney.Value = Mathf.Clamp(this.playerMoney.Value - 300f, 0f, float.MaxValue);
			Console.Log("You died! You have been charged 300 MK for respawn.", true);
			this.SendIDied(true);
			yield break;
		}

		private Transform fpsCameraParent;

		private GameObject newsPhotos;

		private GameObject fpsCamera;

		private GameObject fpsCameraClone;

		private GameObject player;

		private GameObject gui;

		private GameObject gameOverScreen;

		private GameObject gameOverRespawningLabel;

		private GameObject optionsToggle;

		private FsmGameObject deadBody;

		private FsmFloat gameVolume;

		private FsmFloat playerThirst;

		private FsmFloat playerHunger;

		private FsmFloat playerStress;

		private FsmFloat playerFatigue;

		private FsmFloat playerDirtiness;

		private FsmFloat playerUrine;

		private FsmFloat playerMoney;

		private FsmBool playerInCar;

		private FsmBool playerInWater;

		private CharacterMotor charMotor;

		private PlayerDeathManager.PlayerCtrlCache playerCtrlCache;

		private ScreenOverlay blood;

		internal bool isGhost;

		private GameEvent<PlayerDeathManager> DeathEvent;

		private class PlayerCtrlCache
		{
			public PlayerCtrlCache(GameObject player)
			{
				CharacterController component = player.GetComponent<CharacterController>();
				this.radius = component.radius;
				this.height = component.height;
				this.center = component.center;
				this.slopeLimit = component.slopeLimit;
				this.stepOffset = component.stepOffset;
				this.detectCollisions = component.detectCollisions;
			}

			public void Apply(GameObject player)
			{
				CharacterController characterController = player.AddComponent<CharacterController>();
				player.AddComponent<CharacterMotor>();
				player.AddComponent<FPSInputController>();
				characterController.radius = this.radius;
				characterController.height = this.height;
				characterController.center = this.center;
				characterController.slopeLimit = this.slopeLimit;
				characterController.stepOffset = this.stepOffset;
				characterController.detectCollisions = this.detectCollisions;
			}

			public float radius;

			public float height;

			public Vector3 center;

			public float slopeLimit;

			public float stepOffset;

			public bool detectCollisions;
		}
	}
}
