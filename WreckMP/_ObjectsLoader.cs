using System;
using System.Collections.Generic;
using System.Reflection;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class _ObjectsLoader : NetManager
	{
		public static GameObject[] ObjectsInGame
		{
			get
			{
				return _ObjectsLoader.objectsInGame;
			}
		}

		public static bool IsGameLoaded
		{
			get
			{
				return _ObjectsLoader.isGameLoaded;
			}
		}

		public _ObjectsLoader()
		{
			if (WreckMPGlobals.ModLoaderInstalled)
			{
				Type type = WreckMPGlobals.mscloader.GetType("MSCLoader.ModLoader");
				Console.Log((type == null) ? "Modloader is NOT present." : "Modloader is present.", true);
				this.modloaderInstance = type.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
				if (this.modloaderInstance == null)
				{
					Console.LogError("ModLoader instance is null but it is present", false);
					return;
				}
				this.allModsLoaded = type.GetField("allModsLoaded", BindingFlags.Instance | BindingFlags.NonPublic);
			}
		}

		private void Start()
		{
		}

		private void Update()
		{
			if (!_ObjectsLoader.allowInput)
			{
				_ObjectsLoader.blockInputTime = Time.time;
			}
			if (_ObjectsLoader.playerLoadedTime == 0f && GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera") != null)
			{
				_ObjectsLoader.playerLoadedTime = Time.realtimeSinceStartup;
			}
			if (!_ObjectsLoader.isGameLoaded && _ObjectsLoader.playerLoadedTime > 0f && Time.realtimeSinceStartup - _ObjectsLoader.playerLoadedTime > 2f && (!WreckMPGlobals.ModLoaderInstalled || (bool)this.allModsLoaded.GetValue(this.modloaderInstance)))
			{
				try
				{
					_ObjectsLoader.isGameLoaded = true;
					_ObjectsLoader.objectsInGame = Resources.FindObjectsOfTypeAll<GameObject>();
					for (int i = 0; i < _ObjectsLoader.objectsInGame.Length; i++)
					{
						PlayMakerFSM[] components = _ObjectsLoader.objectsInGame[i].GetComponents<PlayMakerFSM>();
						for (int j = 0; j < components.Length; j++)
						{
							components[j].Initialize();
							for (int k = 0; k < components[j].FsmStates.Length; k++)
							{
								bool flag = false;
								for (int l = 0; l < components[j].FsmStates[k].Actions.Length; l++)
								{
									GetButtonDown getButtonDown = components[j].FsmStates[k].Actions[l] as GetButtonDown;
									if (getButtonDown != null)
									{
										FsmString name = getButtonDown.buttonName;
										FsmBool result = getButtonDown.storeResult;
										FsmEvent trigger = getButtonDown.sendEvent;
										PlayMakerFSM f = components[j];
										components[j].FsmStates[k].Actions[l] = new PM_Hook(delegate
										{
											bool flag2 = cInput.GetButtonDown(name.Value) && Time.time - _ObjectsLoader.blockInputTime > 1f;
											if (result != null)
											{
												result.Value = flag2;
											}
											if (trigger != null && flag2)
											{
												f.Fsm.Event(trigger);
											}
										}, true);
									}
								}
								if (flag)
								{
									break;
								}
							}
						}
					}
					_ObjectsLoader.characterMotor = GameObject.Find("PLAYER").GetComponent<CharacterMotor>();
					if (Input.GetKey(291))
					{
						Console.LogWarning("ENTERING STEP-LOAD DEBUG MODE. Use F10 to cycle loading functions", true);
						this.stepLoadI = 0;
						return;
					}
					for (int m = 0; m < _ObjectsLoader.gameLoaded.Count; m++)
					{
						_ObjectsLoader.gameLoaded[m]();
					}
					if (!WreckMPGlobals.IsHost)
					{
						using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
						{
							GameEventRouter.GetEvent("PlayerLoaded").Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
						}
					}
					Console.Log("Game loaded!", true);
				}
				catch (Exception ex)
				{
					Console.LogError("An error occured while loading. More information in the output log. Please report this error alongside with the output_log.txt.", true);
					throw ex;
				}
			}
			if (this.stepLoadI >= 0 && Input.GetKeyDown(291))
			{
				_ObjectsLoader.objectsInGame = Resources.FindObjectsOfTypeAll<GameObject>();
				List<Func<string>> list = _ObjectsLoader.gameLoaded;
				int num = this.stepLoadI;
				this.stepLoadI = num + 1;
				string text = list[num]();
				Console.Log("Loaded " + text, true);
				if (this.stepLoadI >= _ObjectsLoader.gameLoaded.Count)
				{
					if (!WreckMPGlobals.IsHost)
					{
						using (GameEventWriter gameEventWriter2 = GameEvent.EmptyWriter(""))
						{
							GameEventRouter.GetEvent("PlayerLoaded").Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
						}
					}
					Console.Log("Game loaded!", true);
					this.stepLoadI = -1;
				}
			}
		}

		public static void ToggleKeyboardInput(bool enable)
		{
			_ObjectsLoader.allowInput = enable;
			_ObjectsLoader.characterMotor.canControl = enable && _ObjectsLoader.playerCurrentVehicle.Value == "";
			cInput.scanning = true;
		}

		private void OnDestroy()
		{
			_ObjectsLoader.objectsInGame = null;
			_ObjectsLoader.gameLoaded = null;
			_ObjectsLoader.isGameLoaded = false;
		}

		private static GameObject[] objectsInGame;

		public static List<Func<string>> gameLoaded = new List<Func<string>>();

		private static bool isGameLoaded = false;

		private static float playerLoadedTime = 0f;

		private FieldInfo allModsLoaded;

		private object modloaderInstance;

		private static List<PlayMakerFSM> allInputActions = new List<PlayMakerFSM>();

		private static CharacterMotor characterMotor;

		private static FsmString playerCurrentVehicle = PlayMakerGlobals.Instance.Variables.FindFsmString("PlayerCurrentVehicle");

		private static bool allowInput = true;

		private static float blockInputTime = 0f;

		private int stepLoadI = -1;
	}
}
