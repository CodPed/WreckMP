using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class NetTelephoneManager : NetManager
	{
		private void Start()
		{
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				Transform transform = GameObject.Find("YARD").transform.Find("Building/LIVINGROOM/Telephone/Logic");
				if (transform == null)
				{
					Console.LogWarning("Telephone manager failed to find phone, perhaps house burnt down? Skipping phone sync...", true);
					return "Telephone - SKIPPED";
				}
				NetTelephoneManager.ring = transform.Find("Ring").GetPlayMaker("Ring");
				NetTelephoneManager.ring.Initialize();
				NetTelephoneManager.ring.gameObject.AddComponent<NetTelephoneManager.TelephoneEventHandler>().mgr = this;
				NetTelephoneManager.ringEventName = NetTelephoneManager.ring.FsmVariables.FindFsmString("Topic");
				this.maxRingCount = NetTelephoneManager.ring.FsmVariables.FindFsmInt("RandomTimes");
				NetTelephoneManager.ring.InsertAction("State 2", new Action(this.PhonePickedUp), 0, false);
				NetTelephoneManager.handle = transform.Find("UseHandle").GetPlayMaker("Use");
				NetTelephoneManager.handle.Initialize();
				this.closePhoneFSMEvent = NetTelephoneManager.handle.AddEvent("MP_CLOSE");
				NetTelephoneManager.handle.AddGlobalTransition(this.closePhoneFSMEvent, "Close phone");
				if (!WreckMPGlobals.IsHost)
				{
					transform.Find("PhoneLogic").gameObject.SetActive(false);
					NetTelephoneManager.handle.GetState("Close phone").Actions[9].Enabled = false;
				}
				NetTelephoneManager.rst_customSubtitles = new FsmString("CustomSubtitle");
				List<FsmString> list = NetTelephoneManager.ring.FsmVariables.StringVariables.ToList<FsmString>();
				list.Add(NetTelephoneManager.rst_customSubtitles);
				NetTelephoneManager.ring.FsmVariables.StringVariables = list.ToArray();
				for (int i = 0; i < NetTelephoneManager.ring.FsmStates.Length; i++)
				{
					for (int j = 0; j < NetTelephoneManager.ring.FsmStates[i].Actions.Length; j++)
					{
						SetStringValue a = NetTelephoneManager.ring.FsmStates[i].Actions[j] as SetStringValue;
						if (a != null && !(a.stringVariable.Name != "GUIsubtitle"))
						{
							string val = a.stringValue.Value;
							NetTelephoneManager.ring.FsmStates[i].Actions[j] = new PM_Hook(delegate
							{
								a.stringVariable.Value = ((NetTelephoneManager.rst_customSubtitles.Value.Length == 0) ? val : NetTelephoneManager.rst_customSubtitles.Value);
								NetTelephoneManager.rst_customSubtitles.Value = "";
							}, false);
						}
					}
				}
				this.ringEvent = new GameEvent<NetTelephoneManager>("Ring", new Action<ulong, GameEventReader>(this.OnTelephoneRing), GameScene.GAME);
				this.pickupEvent = new GameEvent<NetTelephoneManager>("Pickup", new Action<ulong, GameEventReader>(this.OnTelephonePickup), GameScene.GAME);
				return "Telephone";
			});
		}

		private void Update()
		{
			this.rstcallCheckTime += Time.deltaTime;
			if (this.rstcallCheckTime > 10f)
			{
				this.rstcallCheckTime = 0f;
				if (File.Exists(NetTelephoneManager.rstcallFile))
				{
					string[] array = File.ReadAllText(NetTelephoneManager.rstcallFile).Split(new char[] { '|' });
					File.Delete(NetTelephoneManager.rstcallFile);
					NetTelephoneManager.ringEventName.Value = array[0];
					NetTelephoneManager.rst_customSubtitles.Value = "\"" + array[1] + "\"";
					NetTelephoneManager.ring.gameObject.SetActive(true);
				}
			}
		}

		public static void TriggerCall(string type)
		{
			NetTelephoneManager.ringEventName.Value = type;
			NetTelephoneManager.ring.gameObject.SetActive(true);
		}

		private void Enabled()
		{
			if (!WreckMPGlobals.IsHost)
			{
				return;
			}
			using (GameEventWriter gameEventWriter = this.ringEvent.Writer())
			{
				gameEventWriter.Write(NetTelephoneManager.ringEventName.Value);
				gameEventWriter.Write(this.maxRingCount.Value);
				if (NetTelephoneManager.rst_customSubtitles != null)
				{
					gameEventWriter.Write(NetTelephoneManager.rst_customSubtitles.Value);
				}
				this.ringEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void OnTelephoneRing(ulong u, GameEventReader p)
		{
			string text = p.ReadString();
			int num = p.ReadInt32();
			string text2 = "";
			if (p.UnreadLength() > 0)
			{
				text2 = p.ReadString();
			}
			NetTelephoneManager.ringEventName.Value = text;
			if (NetTelephoneManager.rst_customSubtitles != null)
			{
				NetTelephoneManager.rst_customSubtitles.Value = text2;
			}
			this.maxRingCount.Value = num;
			NetTelephoneManager.ring.gameObject.SetActive(true);
		}

		private void PhonePickedUp()
		{
			if (this.receivedPhonePickedUp)
			{
				this.receivedPhonePickedUp = false;
				return;
			}
			this.pickupEvent.SendEmpty(0UL, true);
		}

		private void OnTelephonePickup(ulong u, GameEventReader p)
		{
			if (!NetTelephoneManager.ring.gameObject.activeSelf)
			{
				return;
			}
			this.receivedPhonePickedUp = true;
			base.StartCoroutine(this.C_OnTelephonePickedUp());
		}

		private IEnumerator C_OnTelephonePickedUp()
		{
			NetTelephoneManager.ring.SendEvent("ANSWER");
			while (NetTelephoneManager.ring.ActiveStateName == "State 2")
			{
				yield return new WaitForEndOfFrame();
			}
			NetTelephoneManager.ring.SendEvent("ENDCALL");
			NetTelephoneManager.handle.Fsm.Event(this.closePhoneFSMEvent);
			yield break;
		}

		private static PlayMakerFSM ring;

		private static PlayMakerFSM handle;

		private FsmEvent closePhoneFSMEvent;

		private static FsmString ringEventName;

		private static FsmString rst_customSubtitles;

		private FsmInt maxRingCount;

		private GameEvent<NetTelephoneManager> ringEvent;

		private GameEvent<NetTelephoneManager> pickupEvent;

		private bool receivedPhonePickedUp;

		private static readonly string rstcallFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\..\\LocalLow\\Amistech\\rstcall";

		private float rstcallCheckTime;

		internal class TelephoneEventHandler : MonoBehaviour
		{
			private void OnEnable()
			{
				this.mgr.Enabled();
			}

			public NetTelephoneManager mgr;
		}
	}
}
