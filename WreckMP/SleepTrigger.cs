using System;
using System.Collections;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class SleepTrigger : MonoBehaviour
	{
		private void Start()
		{
			this.col = base.GetComponent<Collider>();
			this.player = GameObject.Find("PLAYER").transform;
			this.sleepEyes = this.player.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera/SleepEyes").GetComponent<Animation>();
			this.sleepPivot = base.transform.Find("Pivot");
			this.updateCursor = this.player.GetPlayMaker("Update Cursor");
			this.weather = GameObject.Find("MAP").transform.Find("CloudSystem/Clouds").GetPlayMaker("Weather");
			this.time = GameObject.Find("MAP").transform.Find("SUN/Pivot/SUN").GetPlayMaker("Color").FsmVariables.FindFsmInt("Time");
			Transform transform = GameObject.Find("YARD").transform.Find("Building/LIVINGROOM/Telephone/Cord");
			if (transform != null)
			{
				this.phoneCordFsm = transform.GetPlayMaker("Use");
				this.phoneCord = this.phoneCordFsm.FsmVariables.FindFsmBool("CordPhone");
				this.drunkMoved = GameObject.Find("YARD").transform.Find("Building/BEDROOM1/LOD_bedroom1/Sleep/SleepTrigger").GetPlayMaker("Activate").FsmVariables.FindFsmBool("DrunkMoved");
			}
			this.drunkAngry = GameObject.Find("JOBS").transform.Find("HouseDrunk/BeerCampOld/BeerCamp/KiljuBuyer/CanTrigger").GetPlayMaker("Logic").FsmVariables.FindFsmBool("Angry");
		}

		private void Update()
		{
			bool flag = Raycaster.Raycast(this.col, 3f, -1) && this.canLaydown;
			if (flag != this._guiuse)
			{
				if (!flag)
				{
					this.guiuse.Value = false;
					this.guiinteraction.Value = "";
				}
				this._guiuse = flag;
			}
			if (flag)
			{
				bool flag2 = this.fatigue.Value > 10f && !this.layingDown;
				this.guiuse.Value = flag2;
				this.guiinteraction.Value = (flag2 ? "SLEEP" : "");
				if (cInput.GetKeyDown("Use") && flag2)
				{
					this.layingDown = true;
					this.playerPos = this.player.position;
					this.playerRot = this.player.rotation;
					base.StartCoroutine(this.LayDown(true));
				}
			}
			if (this.layingDown && !this.sleeping)
			{
				this.guiinteraction.Value = string.Format("WAITING FOR {0} PLAYER{1} TO SLEEP", NetSleepingManager.EmptyBags, (NetSleepingManager.EmptyBags > 1) ? "S" : "");
				if (cInput.GetKeyDown("PlayerLeft") || cInput.GetKeyDown("PlayerRight") || cInput.GetKeyDown("PlayerUp") || cInput.GetKeyDown("PlayerDown"))
				{
					base.StartCoroutine(this.LayDown(false));
					this.guiinteraction.Value = "";
				}
			}
		}

		private IEnumerator LayDown(bool down)
		{
			Action<bool> action = this.laydown;
			if (action != null)
			{
				action(down);
			}
			this.layingDown = down;
			if (down)
			{
				this.freezePlayer.Value = true;
			}
			Vector3 playerpos = (down ? this.playerPos : this.player.position);
			Quaternion playerrot = (down ? this.playerRot : this.player.rotation);
			Vector3 targetpos = ((!down) ? this.playerPos : this.sleepPivot.position);
			Quaternion targetrot = ((!down) ? this.playerRot : this.sleepPivot.rotation);
			float t = 0f;
			while (t < 1f)
			{
				t += Time.deltaTime;
				this.player.position = Vector3.Lerp(playerpos, targetpos, t);
				this.player.rotation = Quaternion.Lerp(playerrot, targetrot, t);
				yield return new WaitForEndOfFrame();
			}
			if (!down)
			{
				this.freezePlayer.Value = false;
				NetSleepingManager.sleeping = false;
			}
			yield break;
		}

		public void TriggerSleep()
		{
			if (!this.layingDown)
			{
				return;
			}
			base.StartCoroutine(this.Sleep());
		}

		private IEnumerator Sleep()
		{
			this.guiinteraction.Value = "SLEEPING...";
			this.sleeping = true;
			this.updateCursor.SendEvent("SLEEP");
			this.sleeps.Value = true;
			MasterAudio.PlaySound3DAndForget("PlayerMisc", this.sleepPivot, false, 1f, null, 0f, "tosleep1");
			this.sleepEyes.gameObject.SetActive(true);
			this.sleepEyes.Play("sleep_on", 4);
			float vol = 1f;
			while (vol > 0f)
			{
				vol -= Time.deltaTime / 4f;
				this.gamevolume.Value = vol;
				yield return new WaitForEndOfFrame();
			}
			int num = Mathf.FloorToInt(this.fatigue.Value / 20f) * 2;
			float num2 = this.fatigue.Value / 5f;
			this.dirtness.Value += 10f;
			this.drunk.Value -= num2;
			this.hunger.Value += num2;
			this.thirst.Value += num2;
			this.urine.Value += num2;
			this.fatigue.Value = 0f;
			if (WreckMPGlobals.IsHost)
			{
				this.weather.enabled = false;
				if (this.time.Value == 24)
				{
					this.time.Value = num;
				}
				this.time.Value += num;
				if (this.time.Value >= 24)
				{
					this.time.Value -= 24;
					this.time.Value = Mathf.Clamp(this.time.Value, 2, 24);
					FsmInt fsmInt = this.day;
					int value = fsmInt.Value;
					fsmInt.Value = value + 1;
					if (this.day.Value > 7)
					{
						this.day.Value = 1;
					}
					if (this.phoneCord != null && this.phoneCordFsm != null && this.drunkMoved != null && (this.player.position - this.phoneCordFsm.transform.position).sqrMagnitude < 100f && WreckMPGlobals.IsHost && this.phoneCord.Value && Random.Range(0, 10) < 6)
					{
						this.time.Value = 2;
						if (this.drunkAngry.Value)
						{
							this.drunkAngry.Value = false;
							NetTelephoneManager.TriggerCall("DRUNKANGRY");
						}
						else if (!this.drunkMoved.Value)
						{
							NetTelephoneManager.TriggerCall("DRUNK");
						}
					}
				}
			}
			this.sleeps.Value = false;
			this.sleepEyes.Play("sleep_off", 4);
			if (WreckMPGlobals.IsHost)
			{
				PlayMakerFSM.BroadcastEvent("WAKEUP");
			}
			vol = 0f;
			while (vol < 1f)
			{
				vol += Time.deltaTime / 4f;
				this.gamevolume.Value = vol;
				yield return new WaitForEndOfFrame();
			}
			this.sleepEyes.gameObject.SetActive(false);
			MasterAudio.PlaySound3DAndForget("PlayerMisc", this.sleepPivot, false, 1f, null, 0f, "yawn01");
			this.updateCursor.SendEvent("WAKE");
			yield return base.StartCoroutine(this.LayDown(false));
			this.sleeping = false;
			this.guiinteraction.Value = "";
			yield break;
		}

		private FsmInt day = PlayMakerGlobals.Instance.Variables.FindFsmInt("GlobalDay");

		private FsmInt time;

		private FsmString guiinteraction = PlayMakerGlobals.Instance.Variables.FindFsmString("GUIinteraction");

		private FsmFloat fatigue = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerFatigue");

		private FsmFloat gamevolume = PlayMakerGlobals.Instance.Variables.FindFsmFloat("GameVolume");

		private FsmFloat dirtness = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerDirtiness");

		private FsmFloat drunk = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerDrunkAdjusted");

		private FsmFloat hunger = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerHunger");

		private FsmFloat thirst = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerThirst");

		private FsmFloat urine = PlayMakerGlobals.Instance.Variables.FindFsmFloat("PlayerUrine");

		private FsmBool guiuse = PlayMakerGlobals.Instance.Variables.FindFsmBool("GUIuse");

		private FsmBool sleeps = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerSleeps");

		private FsmBool phoneCord;

		private FsmBool drunkAngry;

		private FsmBool drunkMoved;

		private FsmBool freezePlayer = PlayMakerGlobals.Instance.Variables.FindFsmBool("PlayerStop");

		private bool _guiuse;

		private Collider col;

		private Animation sleepEyes;

		internal Transform player;

		internal Transform sleepPivot;

		private PlayMakerFSM updateCursor;

		private PlayMakerFSM weather;

		private PlayMakerFSM phoneCordFsm;

		public Action<bool> laydown;

		public bool canLaydown = true;

		public bool layingDown;

		public bool sleeping;

		public ulong owner;

		private Vector3 playerPos;

		private Quaternion playerRot;
	}
}
