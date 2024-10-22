using System;
using System.Collections.Generic;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace WreckMP
{
	internal class FsmNetVehicle
	{
		public FsmNetVehicle(Transform transform)
		{
			this.transform = transform;
			this.netVehicle = new NetVehicle(transform);
			PlayMakerFSM[] componentsInChildren = transform.GetComponentsInChildren<PlayMakerFSM>(true);
			this.DoItemCollider();
			this.DoFsms();
			this.DoDrivingMode(componentsInChildren);
			this.DoPassengerSeats();
			this.DoDriverPivots();
			this.DoFluidsAndFields(componentsInChildren);
			this.DoDoors(componentsInChildren);
			this.DoDashboard(componentsInChildren);
			this.DoIgnition(componentsInChildren);
			this.DoHorn();
		}

		private void DoIgnition(PlayMakerFSM[] fsms)
		{
			if (this.transform.name == "JONNEZ ES(Clone)")
			{
				return;
			}
			PlayMakerFSM playMakerFSM = null;
			PlayMakerFSM playMakerFSM2 = null;
			for (int i = 0; i < fsms.Length; i++)
			{
				if (fsms[i].FsmName == "Starter")
				{
					playMakerFSM2 = fsms[i];
				}
				else if ((fsms[i].FsmName == "Use" && fsms[i].transform.name == "Ignition") || (fsms[i].FsmName == "UseNew" && fsms[i].transform.name == "IgnitionSatsuma"))
				{
					playMakerFSM = fsms[i];
				}
				if (playMakerFSM2 != null && playMakerFSM != null)
				{
					break;
				}
			}
			this.ignition = new FsmIgnition(playMakerFSM, playMakerFSM2);
		}

		public void DoItemCollider()
		{
		}

		private void DoDrivingMode(PlayMakerFSM[] fsms)
		{
			for (int i = 0; i < fsms.Length; i++)
			{
				if (fsms[i].FsmName == "Death" && fsms[i].gameObject.name == "DriverHeadPivot")
				{
					Transform transform = fsms[i].transform;
					transform.GetComponent<Rigidbody>().isKinematic = true;
					Object.Destroy(fsms[i]);
					ConfigurableJoint configurableJoint = transform.GetComponent<ConfigurableJoint>();
					if (configurableJoint == null)
					{
						configurableJoint = transform.parent.GetComponentInChildren<ConfigurableJoint>();
					}
					configurableJoint.transform.localPosition = configurableJoint.connectedAnchor;
					configurableJoint.transform.localEulerAngles = Vector3.zero;
					configurableJoint.GetComponent<Rigidbody>().isKinematic = true;
					Object.Destroy(configurableJoint);
					Console.Log("Successfully removed death fsm from driving mode of " + this.transform.name, false);
					return;
				}
			}
		}

		private void DoDashboard(PlayMakerFSM[] fsms)
		{
			foreach (PlayMakerFSM playMakerFSM in fsms)
			{
				playMakerFSM.Initialize();
				if (playMakerFSM.FsmName == "Change" && playMakerFSM.gameObject.name == "Transmission")
				{
					this.InitGifuRange(playMakerFSM);
					Console.Log("Added gifu range for car " + this.transform.name + ": " + playMakerFSM.transform.name, false);
				}
				else if (playMakerFSM.FsmName == "Pos" && playMakerFSM.gameObject.name == "Pivot" && playMakerFSM.transform.parent.gameObject.name == "GearShifter")
				{
					this.InitFerdalShifter(playMakerFSM);
					Console.Log("Added ferndale shifter for car " + this.transform.name + ": " + playMakerFSM.transform.name, false);
				}
				else if (playMakerFSM.FsmName == "Knob" && playMakerFSM.HasState("Increase"))
				{
					this.dashboardKnobs.Add(new FsmDashboardKnob(playMakerFSM));
					Console.Log("Added dashboard knob for car " + this.transform.name + ": " + playMakerFSM.transform.name, false);
				}
				else if (playMakerFSM.transform.name == "TurnSignals" && playMakerFSM.FsmName == "Usage")
				{
					this.turnSignals.Add(new FsmTurnSignals(playMakerFSM));
					Console.Log("Added turn signals for car " + this.transform.name + ": " + playMakerFSM.transform.name, false);
				}
				else if (!(playMakerFSM.FsmName != "Use"))
				{
					if (playMakerFSM.transform.name == "Range" && this.transform.name == "KEKMET(350-400psi)")
					{
						this.InitKekmetRange(playMakerFSM);
					}
					else if (playMakerFSM.transform.name == "InteriorLight" || playMakerFSM.transform.parent.name == "InteriorLight")
					{
						this.interiorLights.Add(new FsmInteriorLight(playMakerFSM));
						Console.Log("Added interior light for car " + this.transform.name + ": " + playMakerFSM.transform.name, false);
					}
					else if (!playMakerFSM.HasState("Test") && !playMakerFSM.HasState("Test 2"))
					{
						if ((playMakerFSM.HasState("INCREASE") && playMakerFSM.HasState("DECREASE")) || (playMakerFSM.HasState("INCREASE 2") && playMakerFSM.HasState("DECREASE 2")))
						{
							if (playMakerFSM.transform.name.StartsWith("FrontHyd") && playMakerFSM.transform.parent.name == "NewHydraulics")
							{
								this.InitKekmetFrontLoader(playMakerFSM);
							}
							else
							{
								this.dashboardLevers.Add(new FsmDashboardLever(playMakerFSM));
								Console.Log("Added dashboard lever for car " + this.transform.name + ": " + playMakerFSM.transform.name, false);
							}
						}
					}
					else if (!(playMakerFSM.transform.name == "Ignition"))
					{
						this.dashboardButtons.Add(new FsmDashboardButton(playMakerFSM));
						Console.Log("Added dashboard button for car " + this.transform.name + ": " + playMakerFSM.transform.name, false);
					}
				}
			}
		}

		private void DoHorn()
		{
			Transform[] componentsInChildren = this.transform.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i].name == "CarHorn")
				{
					componentsInChildren[i].gameObject.AddComponent<FsmHorn>();
					Console.Log("Added horn for car " + this.transform.name, false);
				}
			}
		}

		private void InitKekmetFrontLoader(PlayMakerFSM fsm)
		{
			FsmFloat armRot_var = fsm.FsmVariables.FindFsmFloat("ArmRot");
			string text = (fsm.HasState("INCREASE") ? "INCREASE" : "INCREASE 2");
			string text2 = (fsm.HasState("DECREASE") ? "DECREASE" : "DECREASE 2");
			FsmState state = fsm.GetState(text);
			FsmState state2 = fsm.GetState(text2);
			FsmStateAction mbu1 = state.Actions[6];
			FsmStateAction mbu2 = state2.Actions[6];
			bool stopMoving = false;
			Action action = delegate
			{
				if (stopMoving)
				{
					stopMoving = false;
					fsm.SendEvent("OFF");
				}
			};
			fsm.InsertAction(text, action, -1, true);
			fsm.InsertAction(text2, action, -1, true);
			bool isStartingMoving = false;
			bool isStoppingMoving = false;
			GameEvent startMovingEvent = new GameEvent("KekmetFrontloaderStartMove" + fsm.transform.name, delegate(GameEventReader p)
			{
				bool flag = p.ReadBoolean();
				isStartingMoving = true;
				mbu1.Enabled = false;
				mbu2.Enabled = false;
				fsm.SendEvent(flag ? "INCREASE" : "DECREASE");
			}, GameScene.GAME);
			GameEvent stopMovingEvent = new GameEvent("KekmetFrontloaderStopMove" + fsm.transform.name, delegate(GameEventReader p)
			{
				armRot_var.Value = p.ReadSingle();
				mbu1.Enabled = true;
				mbu2.Enabled = true;
				isStoppingMoving = true;
				stopMoving = true;
			}, GameScene.GAME);
			Action<bool> startMovingCallback = delegate(bool direction)
			{
				if (isStartingMoving)
				{
					isStartingMoving = false;
					return;
				}
				using (GameEventWriter gameEventWriter = startMovingEvent.Writer())
				{
					gameEventWriter.Write(direction);
					startMovingEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			};
			fsm.InsertAction(text, delegate
			{
				startMovingCallback(true);
			}, -1, false);
			fsm.InsertAction(text2, delegate
			{
				startMovingCallback(false);
			}, -1, false);
			fsm.InsertAction("State 1", delegate
			{
				if (isStoppingMoving)
				{
					isStoppingMoving = false;
					return;
				}
				using (GameEventWriter gameEventWriter2 = stopMovingEvent.Writer())
				{
					gameEventWriter2.Write(armRot_var.Value);
					stopMovingEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}, -1, false);
		}

		private void InitKekmetRange(PlayMakerFSM fsm)
		{
			FsmBool range_var = fsm.FsmVariables.FindFsmBool("Range");
			FsmEvent backdoorEvent = fsm.AddEvent("MP_TOGGLE");
			fsm.AddGlobalTransition(backdoorEvent, "Flip");
			bool isUpdating = false;
			GameEvent gameEvent = new GameEvent("KemetRangeKnob", delegate(GameEventReader p)
			{
				isUpdating = true;
				range_var.Value = p.ReadBoolean();
				fsm.Fsm.Event(backdoorEvent);
			}, GameScene.GAME);
			fsm.InsertAction("Flip", delegate
			{
				if (isUpdating)
				{
					isUpdating = false;
					return;
				}
				using (GameEventWriter gameEventWriter = gameEvent.Writer())
				{
					gameEventWriter.Write(!range_var.Value);
					gameEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}, -1, false);
		}

		private void InitGifuRange(PlayMakerFSM fsm)
		{
			FsmBool lowGears_var = fsm.FsmVariables.FindFsmBool("LowGears");
			FsmEvent fsmEvent = fsm.AddEvent("MP_TOGGLE");
			fsm.AddGlobalTransition(fsmEvent, "State 2");
			bool isUpdating = false;
			GameEvent gameEvent = new GameEvent("GifuGearRange", delegate(GameEventReader p)
			{
				isUpdating = true;
				lowGears_var.Value = p.ReadBoolean();
				fsm.Fsm.Event(fsmEvent);
			}, GameScene.GAME);
			fsm.InsertAction("State 2", delegate
			{
				if (isUpdating)
				{
					isUpdating = false;
					return;
				}
				using (GameEventWriter gameEventWriter = gameEvent.Writer())
				{
					gameEventWriter.Write(lowGears_var.Value);
					gameEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}, 0, false);
		}

		private void InitFerdalShifter(PlayMakerFSM fsm)
		{
			FsmString letter_var = fsm.FsmVariables.FindFsmString("Letter");
			FsmEvent shiftUpEvent = fsm.AddEvent("MP_SHIFTUP");
			fsm.AddGlobalTransition(shiftUpEvent, "State 4");
			FsmEvent shiftDownEvent = fsm.AddEvent("MP_SHIFTDOWN");
			fsm.AddGlobalTransition(shiftDownEvent, "State 3");
			bool isUpdating = false;
			GameEvent gameEvent = new GameEvent("FerndaleGearstickUpdate", delegate(GameEventReader p)
			{
				bool flag = p.ReadBoolean();
				char c = p.ReadChar();
				isUpdating = true;
				letter_var.Value = c.ToString();
				fsm.Fsm.Event(flag ? shiftUpEvent : shiftDownEvent);
			}, GameScene.GAME);
			Action<bool> fsmCallback = delegate(bool shiftUp)
			{
				if (isUpdating)
				{
					isUpdating = false;
					return;
				}
				using (GameEventWriter gameEventWriter = gameEvent.Writer())
				{
					gameEventWriter.Write(shiftUp);
					gameEventWriter.Write(letter_var.Value[0]);
					gameEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			};
			fsm.InsertAction("State 4", delegate
			{
				fsmCallback(true);
			}, 0, false);
			fsm.InsertAction("State 3", delegate
			{
				fsmCallback(false);
			}, 0, false);
		}

		private void DoDoors(PlayMakerFSM[] fsms)
		{
			if (this.transform.name == "SATSUMA(557kg, 248)")
			{
				this.vehicleDoors.Add(new FsmVehicleDoor(NetItemsManager.GetDatabaseObject("Database/DatabaseBody/Door_Left").GetPlayMaker("Use"), false));
				this.vehicleDoors.Add(new FsmVehicleDoor(NetItemsManager.GetDatabaseObject("Database/DatabaseBody/Door_Right").GetPlayMaker("Use"), false));
				this.vehicleDoors.Add(new FsmVehicleDoor(NetItemsManager.GetDatabaseObject("Database/DatabaseBody/Bootlid").transform.Find("Handles").GetPlayMaker("Use"), false));
				return;
			}
			foreach (PlayMakerFSM playMakerFSM in fsms)
			{
				if (!(playMakerFSM.FsmName != "Use") && (!(playMakerFSM.transform.name != "Handle") || !(playMakerFSM.transform.name != "Handles") || playMakerFSM.transform.name.ToLower().Contains("door") || playMakerFSM.transform.name.ToLower().Contains("bootlid")))
				{
					this.vehicleDoors.Add(new FsmVehicleDoor(playMakerFSM, false));
				}
			}
			if (this.transform.name == "HAYOSIKO(1500kg, 250)")
			{
				this.vehicleDoors.Add(new FsmVehicleDoor(this.transform.Find("SideDoor/door/Collider").GetComponent<PlayMakerFSM>(), true));
			}
		}

		private void DoFluidsAndFields(PlayMakerFSM[] fsms)
		{
			if (this.transform.name.ToUpper().Contains("SATSUMA"))
			{
				this.fuelLevel = this.GetDatabaseFsmFloat("Database/DatabaseMechanics/FuelTank", "FuelLevel");
				this.engineTemp = PlayMakerGlobals.Instance.Variables.FindFsmFloat("EngineTemp");
				this.oilLevel = this.GetDatabaseFsmFloat("Database/DatabaseMotor/Oilpan", "Oil");
				this.oilContamination = this.GetDatabaseFsmFloat("Database/DatabaseMotor/Oilpan", "OilContamination");
				this.oilGrade = this.GetDatabaseFsmFloat("Database/DatabaseMotor/Oilpan", "OilGrade");
				this.coolant1Level = this.GetDatabaseFsmFloat("Database/DatabaseMechanics/Radiator", "Water");
				this.coolant2Level = this.GetDatabaseFsmFloat("Database/DatabaseOrders/Racing Radiator", "Water");
				this.brake1Level = this.GetDatabaseFsmFloat("Database/DatabaseMechanics/BrakeMasterCylinder", "BrakeFluidF");
				this.brake2Level = this.GetDatabaseFsmFloat("Database/DatabaseMechanics/BrakeMasterCylinder", "BrakeFluidR");
				this.clutchLevel = this.GetDatabaseFsmFloat("Database/DatabaseMechanics/ClutchMasterCylinder", "ClutchFluid");
				Console.Log(string.Format("Init fluids and fields for Satsuma, {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", new object[] { this.fuelLevel, this.engineTemp, this.oilLevel, this.oilContamination, this.oilGrade, this.coolant1Level, this.coolant2Level, this.brake1Level, this.brake2Level, this.clutchLevel }), false);
				return;
			}
			for (int i = 0; i < fsms.Length; i++)
			{
				if (fsms[i].transform.name == "FuelTank")
				{
					this.fuelLevel = fsms[i].FsmVariables.FindFsmFloat("FuelLevel");
					this.oilLevel = fsms[i].FsmVariables.FindFsmFloat("FuelOil");
				}
				else if (fsms[i].FsmName == "Cooling")
				{
					this.engineTemp = fsms[i].FsmVariables.FloatVariables.FirstOrDefault((FsmFloat f) => f.Name.Contains("EngineTemp"));
				}
			}
			Console.Log(string.Format("Init fluids and fields for {0}, {1}, {2}, {3}", new object[]
			{
				this.transform.name,
				this.fuelLevel,
				this.engineTemp,
				this.oilLevel
			}), false);
		}

		internal static void SendCarFluidsAndFields()
		{
			using (GameEventWriter gameEventWriter = NetItemsManager.carFluidsSync.Writer())
			{
				for (int i = 0; i < NetVehicleManager.vanillaVehicles.Count; i++)
				{
					FsmNetVehicle fsmNetVehicle = NetVehicleManager.vanillaVehicles[i];
					gameEventWriter.Write(fsmNetVehicle.netVehicle.Hash);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.fuelLevel, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.oilLevel, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.oilContamination, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.oilGrade, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.coolant1Level, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.coolant2Level, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.brake1Level, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.brake2Level, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.clutchLevel, gameEventWriter);
					FsmNetVehicle.WriteNullableFloat(fsmNetVehicle.engineTemp, gameEventWriter);
				}
				NetItemsManager.carFluidsSync.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		internal static void OnCarFluidsAndFields(GameEventReader p)
		{
			while (p.UnreadLength() > 0)
			{
				int hash = p.ReadInt32();
				FsmNetVehicle fsmNetVehicle = NetVehicleManager.vanillaVehicles.FirstOrDefault((FsmNetVehicle v) => v.netVehicle.Hash == hash);
				if (fsmNetVehicle == null)
				{
					Console.LogError(string.Format("OnCarFluidsAndFields vehicle of hash {0} cannot be found", hash), false);
					for (int i = 0; i < 10; i++)
					{
						p.ReadSingle();
					}
				}
				else
				{
					float num;
					if (FsmNetVehicle.ReadNullableFloat(p, out num))
					{
						fsmNetVehicle.fuelLevel.Value = num;
					}
					float num2;
					if (FsmNetVehicle.ReadNullableFloat(p, out num2))
					{
						fsmNetVehicle.oilLevel.Value = num2;
					}
					float num3;
					if (FsmNetVehicle.ReadNullableFloat(p, out num3))
					{
						fsmNetVehicle.oilContamination.Value = num3;
					}
					float num4;
					if (FsmNetVehicle.ReadNullableFloat(p, out num4))
					{
						fsmNetVehicle.oilGrade.Value = num4;
					}
					float num5;
					if (FsmNetVehicle.ReadNullableFloat(p, out num5))
					{
						fsmNetVehicle.coolant1Level.Value = num5;
					}
					float num6;
					if (FsmNetVehicle.ReadNullableFloat(p, out num6))
					{
						fsmNetVehicle.coolant2Level.Value = num6;
					}
					float num7;
					if (FsmNetVehicle.ReadNullableFloat(p, out num7))
					{
						fsmNetVehicle.brake1Level.Value = num7;
					}
					float num8;
					if (FsmNetVehicle.ReadNullableFloat(p, out num8))
					{
						fsmNetVehicle.brake2Level.Value = num8;
					}
					float num9;
					if (FsmNetVehicle.ReadNullableFloat(p, out num9))
					{
						fsmNetVehicle.clutchLevel.Value = num9;
					}
					float num10;
					if (FsmNetVehicle.ReadNullableFloat(p, out num10))
					{
						fsmNetVehicle.engineTemp.Value = num10;
					}
				}
			}
		}

		private static bool ReadNullableFloat(GameEventReader p, out float f)
		{
			f = p.ReadSingle();
			return !float.IsNaN(f);
		}

		private static void WriteNullableFloat(FsmFloat f, GameEventWriter p)
		{
			p.Write((f == null) ? float.NaN : f.Value);
		}

		private FsmFloat GetDatabaseFsmFloat(string databasePath, string variableName)
		{
			GameObject gameObject = GameObject.Find(databasePath);
			if (gameObject == null)
			{
				Console.Log("NV: Database '" + databasePath + "' could not be found", true);
				return null;
			}
			PlayMakerFSM component = gameObject.GetComponent<PlayMakerFSM>();
			if (component == null)
			{
				Console.Log("NV: Database '" + databasePath + "' doesn't have an fsm", true);
				return null;
			}
			FsmFloat fsmFloat = component.FsmVariables.FindFsmFloat(variableName);
			if (fsmFloat == null)
			{
				Console.Log(string.Concat(new string[] { "NV: Database '", databasePath, "' doesn't have a ", variableName, " variable" }), true);
				return null;
			}
			return fsmFloat;
		}

		private void DoDriverPivots()
		{
			int num = Array.IndexOf<string>(FsmNetVehicle.carNames, this.transform.name);
			if (num == -1)
			{
				return;
			}
			NetVehicle netVehicle = this.netVehicle;
			NetVehicleDriverPivots netVehicleDriverPivots = new NetVehicleDriverPivots();
			netVehicleDriverPivots.throttlePedal = this.MakeDriverPivot(FsmNetVehicle.throttlePedals[num]);
			netVehicleDriverPivots.brakePedal = this.MakeDriverPivot(FsmNetVehicle.brakePedals[num]);
			netVehicleDriverPivots.clutchPedal = this.MakeDriverPivot(FsmNetVehicle.clutchPedals[num]);
			netVehicleDriverPivots.steeringWheel = this.MakeDriverPivot(FsmNetVehicle.steeringWheels[num]);
			NetVehicleDriverPivots netVehicleDriverPivots2 = netVehicleDriverPivots;
			Transform[] array;
			if (num != 4)
			{
				(array = new Transform[1])[0] = this.MakeDriverPivot(FsmNetVehicle.gearSticks[num]);
			}
			else
			{
				Transform[] array2 = new Transform[2];
				array2[0] = this.MakeDriverPivot(FsmNetVehicle.gearSticks[num]);
				array = array2;
				array2[1] = this.MakeDriverPivot(FsmNetVehicle.gearSticks[num].GetAltPivot());
			}
			netVehicleDriverPivots2.gearSticks = array;
			netVehicleDriverPivots.driverParent = this.MakeDriverPivot(FsmNetVehicle.drivingModes[num]);
			netVehicle.driverPivots = netVehicleDriverPivots;
		}

		private Transform MakeDriverPivot(FsmNetVehicle.Pivot pivot)
		{
			if (pivot.path == "")
			{
				return null;
			}
			Transform transform = ((pivot.path == null) ? this.transform : this.transform.Find(pivot.path));
			if (transform == null)
			{
				return null;
			}
			Transform transform2 = new GameObject("WreckMP_DriverPivot").transform;
			transform2.parent = transform;
			transform2.localPosition = pivot.position;
			transform2.localEulerAngles = pivot.eulerAngles;
			return transform2;
		}

		private void DoFsms()
		{
			PlayMakerFSM[] componentsInChildren = this.transform.GetComponentsInChildren<PlayMakerFSM>(true);
			PlayMakerFSM drivingMode = componentsInChildren.FirstOrDefault(delegate(PlayMakerFSM fsm)
			{
				if (fsm.FsmName != "PlayerTrigger" || fsm.gameObject.name != "DriveTrigger")
				{
					return false;
				}
				fsm.Initialize();
				FsmState state = fsm.GetState("Press return");
				if (state == null)
				{
					return false;
				}
				SetStringValue setStringValue = state.Actions.FirstOrDefault((FsmStateAction a) => a is SetStringValue) as SetStringValue;
				return setStringValue != null && setStringValue.stringValue.Value.Contains("DRIVING");
			});
			if (drivingMode != null)
			{
				drivingMode.Initialize();
				drivingMode.InsertAction("Press return", delegate
				{
					if (this.netVehicle.DriverSeatTaken)
					{
						drivingMode.SendEvent("FINISHED");
					}
				}, 0, false);
				drivingMode.InsertAction("Reset view", new Action(this.netVehicle.SendEnterDrivingMode), -1, false);
				drivingMode.InsertAction("Create player", new Action(this.netVehicle.SendExitDrivingMode), -1, false);
				Console.Log("Init driving mode for " + this.transform.name, false);
			}
		}

		private void DoPassengerSeats()
		{
			int num = Array.IndexOf<string>(FsmNetVehicle.carNames, this.transform.name);
			if (num == -1)
			{
				Console.LogWarning(string.Format("no passenger seats for car with hash {0} ({1})", this.netVehicle.Hash, this.transform.name), false);
				return;
			}
			Vector3[][] array = FsmNetVehicle.carPassengerSeats[num];
			if (array.Length == 0)
			{
				Console.LogWarning(string.Format("no passenger seats for car with hash {0} ({1})", this.netVehicle.Hash, this.transform.name), false);
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				this.netVehicle.AddPassengerSeat(array[i][0], array[i][1]);
			}
		}

		public static void DoFlatbedPassengerSeats(out Transform _flatbed, out int _hash, Action<bool> enterPassenger)
		{
			Transform transform = GameObject.Find("FLATBED").transform;
			_flatbed = transform;
			int hashCode = transform.gameObject.name.GetHashCode();
			_hash = hashCode;
			Transform transform2 = transform.Find("Bed");
			Transform transform3 = NetVehicle.AddPassengerSeat(null, transform.GetComponent<Rigidbody>(), transform2, new Vector3(0f, 0.5f, 2f), default(Vector3)).Find("PlayerOffset/PassengerTrigger");
			Object.Destroy(transform3.GetComponent<CapsuleCollider>());
			BoxCollider boxCollider = transform3.gameObject.AddComponent<BoxCollider>();
			boxCollider.isTrigger = true;
			boxCollider.size = new Vector3(2f, 1f, 4.8f);
			Transform transform4 = Object.Instantiate<Transform>(GameObject.Find("RCO_RUSCKO12(270)").transform.Find("LOD/PlayerTrigger"));
			for (int i = 0; i < transform4.childCount; i++)
			{
				Object.Destroy(transform4.GetChild(i));
			}
			transform4.transform.parent = transform3;
			transform4.transform.localPosition = (transform4.transform.localEulerAngles = Vector3.zero);
			transform4.GetComponent<BoxCollider>().size = new Vector3(2f, 1f, 4.8f);
			PlayMakerFSM component = transform3.GetComponent<PlayMakerFSM>();
			component.Initialize();
			component.InsertAction("Reset view", delegate
			{
				enterPassenger(true);
			}, -1, false);
			component.InsertAction("Create player", delegate
			{
				enterPassenger(false);
			}, -1, false);
			Transform transform5 = transform.Find("KekmetHydraulicArm(Clone)");
			if (transform5 == null)
			{
				return;
			}
			transform3 = NetVehicle.AddPassengerSeat(null, transform.GetComponent<Rigidbody>(), transform5.Find("Colliders"), new Vector3(-7.6f, 4.2f, 4.2f), default(Vector3)).Find("PlayerOffset/PassengerTrigger");
			Object.Destroy(transform3.GetComponent<CapsuleCollider>());
			BoxCollider boxCollider2 = transform3.gameObject.AddComponent<BoxCollider>();
			boxCollider2.isTrigger = true;
			boxCollider2.size = new Vector3(1f, 1f, 1f);
			PlayMakerFSM component2 = transform3.GetComponent<PlayMakerFSM>();
			component2.Initialize();
			component2.InsertAction("Reset view", delegate
			{
				enterPassenger(true);
			}, -1, false);
			component2.InsertAction("Create player", delegate
			{
				enterPassenger(false);
			}, -1, false);
			component2.GetState("Create player").Actions[8].Enabled = false;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static FsmNetVehicle()
		{
			FsmNetVehicle.Pivot[] array = new FsmNetVehicle.Pivot[7];
			int num = 0;
			FsmNetVehicle.Pivot pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals 1/throttle",
				position = new Vector3(0f, -0.2f, -0.26f),
				eulerAngles = new Vector3(310f, 0f, 180f)
			};
			array[num] = pivot;
			int num2 = 1;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals/Throttle",
				position = default(Vector3),
				eulerAngles = new Vector3(320f, 0f, 180f)
			};
			array[num2] = pivot;
			int num3 = 2;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals 2/throttle",
				position = new Vector3(0f, -0.2f, 0f),
				eulerAngles = new Vector3(294f, 0f, 180f)
			};
			array[num3] = pivot;
			int num4 = 3;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals/throttle",
				position = new Vector3(0f, 0.02f, 0.1f),
				eulerAngles = new Vector3(340f, 0f, 180f)
			};
			array[num4] = pivot;
			int num5 = 4;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "Dashboard/Pedals/pedal_throttle",
				position = new Vector3(0f, -0.24f, -0.15f),
				eulerAngles = new Vector3(330f, 0f, 180f)
			};
			array[num5] = pivot;
			int num6 = 5;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/ThrottleFoot/Pivot/tractor_pedal_speed",
				position = new Vector3(-0.21f, -0.03f, 0.04f),
				eulerAngles = new Vector3(-45f, 90f, 90f)
			};
			array[num6] = pivot;
			int num7 = 6;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "MESH",
				position = new Vector3(-0.05999999f, 0.13f, -0.18f),
				eulerAngles = new Vector3(350.0001f, 90.00019f, 100.0002f)
			};
			array[num7] = pivot;
			FsmNetVehicle.throttlePedals = array;
			FsmNetVehicle.Pivot[] array2 = new FsmNetVehicle.Pivot[7];
			int num8 = 0;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals 1/brake",
				position = new Vector3(0f, -0.1f, -0.26f),
				eulerAngles = new Vector3(350f, 0f, 180f)
			};
			array2[num8] = pivot;
			int num9 = 1;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals/Brake",
				position = default(Vector3),
				eulerAngles = new Vector3(330f, 0f, 180f)
			};
			array2[num9] = pivot;
			int num10 = 2;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals 2/brake",
				position = new Vector3(0f, -0.38f, -0.3f),
				eulerAngles = new Vector3(312f, 0f, 180f)
			};
			array2[num10] = pivot;
			int num11 = 3;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals/brake",
				position = new Vector3(0f, -0.05f, 0.26f),
				eulerAngles = new Vector3(340f, 0f, 180f)
			};
			array2[num11] = pivot;
			int num12 = 4;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "Dashboard/Pedals/pedal_brake",
				position = new Vector3(0f, -0.25f, -0.41f),
				eulerAngles = new Vector3(330f, 0f, 180f)
			};
			array2[num12] = pivot;
			int num13 = 5;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Brake/Pivot/tractor_pedal_brake",
				position = new Vector3(-0.14f, -0.1f, -0.04f),
				eulerAngles = new Vector3(-10f, 30f, 90f)
			};
			array2[num13] = pivot;
			int num14 = 6;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "MESH",
				position = new Vector3(-0.05999999f, -0.13f, -0.18f),
				eulerAngles = new Vector3(350.0001f, 80.00032f, 100.0003f)
			};
			array2[num14] = pivot;
			FsmNetVehicle.brakePedals = array2;
			FsmNetVehicle.Pivot[] array3 = new FsmNetVehicle.Pivot[7];
			int num15 = 0;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals 1/clutch",
				position = new Vector3(0f, -0.1f, -0.26f),
				eulerAngles = new Vector3(350f, 0f, 180f)
			};
			array3[num15] = pivot;
			int num16 = 1;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals/Clutch",
				position = new Vector3(0f, -0.21f, -0.36f),
				eulerAngles = new Vector3(330f, 0f, 180f)
			};
			array3[num16] = pivot;
			int num17 = 2;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "",
				position = default(Vector3),
				eulerAngles = default(Vector3)
			};
			array3[num17] = pivot;
			int num18 = 3;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Pedals/clutch",
				position = new Vector3(0f, -0.05f, 0.15f),
				eulerAngles = new Vector3(340f, 0f, 180f)
			};
			array3[num18] = pivot;
			int num19 = 4;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "Dashboard/Pedals/pedal_clutch",
				position = new Vector3(0f, -0.25f, -0.41f),
				eulerAngles = new Vector3(330f, 0f, 180f)
			};
			array3[num19] = pivot;
			int num20 = 5;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Clutch/Pivot/tractor_pedal_clutch",
				position = new Vector3(-0.17f, 0f, -0.11f),
				eulerAngles = new Vector3(0f, 30f, 90f)
			};
			array3[num20] = pivot;
			int num21 = 6;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "",
				position = default(Vector3),
				eulerAngles = default(Vector3)
			};
			array3[num21] = pivot;
			FsmNetVehicle.clutchPedals = array3;
			FsmNetVehicle.Pivot[] array4 = new FsmNetVehicle.Pivot[7];
			int num22 = 0;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Steering/VanSteeringPivot",
				position = new Vector3(0f, -0.2f, -0.09f),
				eulerAngles = default(Vector3)
			};
			array4[num22] = pivot;
			int num23 = 1;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Steering/TruckSteeringPivot",
				position = new Vector3(0f, 0.05f, 0.22f),
				eulerAngles = new Vector3(0f, 90f, 0f)
			};
			array4[num23] = pivot;
			int num24 = 2;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Steering/MuscleSteeringPivot",
				position = new Vector3(0.2f, 0f, 0.05f),
				eulerAngles = new Vector3(0f, 90f, 90f)
			};
			array4[num24] = pivot;
			int num25 = 3;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Steering/RusckoSteeringPivot",
				position = new Vector3(0f, -0.22f, -0.1f),
				eulerAngles = new Vector3(0f, 350f, 70f)
			};
			array4[num25] = pivot;
			int num26 = 4;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "Dashboard/Steering/CarSteeringPivot",
				position = new Vector3(0f, 0.22f, 0.82f),
				eulerAngles = new Vector3(10f, 190f, 0f)
			};
			array4[num26] = pivot;
			int num27 = 5;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Steering/TractorSteeringPivot/valmet_steering",
				position = new Vector3(0.2f, 0f, 0f),
				eulerAngles = new Vector3(0f, 0f, 90f)
			};
			array4[num27] = pivot;
			int num28 = 6;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Suspension/Steering/SteeringPivot/Column",
				position = new Vector3(0.03f, -0.3f, 0.41f),
				eulerAngles = new Vector3(3.585849E-05f, 320.0001f, 80.00005f)
			};
			array4[num28] = pivot;
			FsmNetVehicle.steeringWheels = array4;
			FsmNetVehicle.Pivot[] array5 = new FsmNetVehicle.Pivot[7];
			int num29 = 0;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/GearShifter/lever",
				position = new Vector3(-0.02f, 0.21f, -0.02f),
				eulerAngles = default(Vector3)
			};
			array5[num29] = pivot;
			int num30 = 1;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/GearLever/Pivot/Lever",
				position = new Vector3(0f, -0.05f, 0.34f),
				eulerAngles = new Vector3(310f, 0f, 190f)
			};
			array5[num30] = pivot;
			int num31 = 2;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/GearShifter/Pivot/muscle_gear_lever",
				position = new Vector3(-0.07f, 0.08f, 0.06f),
				eulerAngles = new Vector3(30f, 80f, 110f)
			};
			array5[num31] = pivot;
			int num32 = 3;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/GearLever/Vibration/Pivot/lever",
				position = new Vector3(-0.01f, -0.14f, 0.39f),
				eulerAngles = new Vector3(280f, 0f, 180f)
			};
			array5[num32] = pivot;
			int num33 = 4;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "Dashboard/gear stick(xxxxx)/GearLever/Pivot/Lever/gear_stick",
				position = new Vector3(-0.05f, 0.2f, 0.2f),
				eulerAngles = new Vector3(340f, 120f, 70f),
				path_alt = "Dashboard/center console gt(xxxxx)/GearLever/Pivot/Lever/gear_stick",
				position_alt = new Vector3(-0.05f, 0.2f, 0.2f),
				eulerAngles_alt = new Vector3(340f, 120f, 70f)
			};
			array5[num33] = pivot;
			int num34 = 5;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Dashboard/Gear/Lever/tractor_lever_gear",
				position = new Vector3(0.12f, 0.14f, 0.21f),
				eulerAngles = new Vector3(0f, 90f, 90f)
			};
			array5[num34] = pivot;
			int num35 = 6;
			pivot = new FsmNetVehicle.Pivot
			{
				path = "LOD/Suspension/Steering/SteeringPivot/Throttle",
				position = new Vector3(0.06999999f, 0.01999998f, 0f),
				eulerAngles = new Vector3(0.0001466356f, 10.00063f, 340.0001f)
			};
			array5[num35] = pivot;
			FsmNetVehicle.gearSticks = array5;
			FsmNetVehicle.Pivot[] array6 = new FsmNetVehicle.Pivot[7];
			int num36 = 0;
			pivot = new FsmNetVehicle.Pivot
			{
				position = new Vector3(-0.4f, 0.93f, 0.99f),
				eulerAngles = default(Vector3)
			};
			array6[num36] = pivot;
			int num37 = 1;
			pivot = new FsmNetVehicle.Pivot
			{
				position = new Vector3(-0.75f, 1.84f, 2.74f),
				eulerAngles = default(Vector3)
			};
			array6[num37] = pivot;
			int num38 = 2;
			pivot = new FsmNetVehicle.Pivot
			{
				position = new Vector3(-0.4f, 0.53f, -0.05f),
				eulerAngles = default(Vector3)
			};
			array6[num38] = pivot;
			int num39 = 3;
			pivot = new FsmNetVehicle.Pivot
			{
				position = new Vector3(-0.29f, 0.37f, -0.08f),
				eulerAngles = default(Vector3)
			};
			array6[num39] = pivot;
			int num40 = 4;
			pivot = new FsmNetVehicle.Pivot
			{
				position = new Vector3(-0.25f, 0.2f, 0f),
				eulerAngles = default(Vector3)
			};
			array6[num40] = pivot;
			int num41 = 5;
			pivot = new FsmNetVehicle.Pivot
			{
				position = new Vector3(0f, 1.31f, -0.6f),
				eulerAngles = default(Vector3)
			};
			array6[num41] = pivot;
			int num42 = 6;
			pivot = new FsmNetVehicle.Pivot
			{
				position = new Vector3(0.02f, 0.66f, -0.44f),
				eulerAngles = default(Vector3)
			};
			array6[num42] = pivot;
			FsmNetVehicle.drivingModes = array6;
		}

		public Transform transform;

		public NetVehicle netVehicle;

		public List<FsmVehicleDoor> vehicleDoors = new List<FsmVehicleDoor>();

		public List<FsmDashboardButton> dashboardButtons = new List<FsmDashboardButton>();

		public List<FsmDashboardLever> dashboardLevers = new List<FsmDashboardLever>();

		public List<FsmDashboardKnob> dashboardKnobs = new List<FsmDashboardKnob>();

		public List<FsmTurnSignals> turnSignals = new List<FsmTurnSignals>();

		public List<FsmInteriorLight> interiorLights = new List<FsmInteriorLight>();

		internal static string[] carNames = new string[] { "HAYOSIKO(1500kg, 250)", "GIFU(750/450psi)", "FERNDALE(1630kg)", "RCO_RUSCKO12(270)", "SATSUMA(557kg, 248)", "KEKMET(350-400psi)", "JONNEZ ES(Clone)" };

		internal static Vector3[] itemColliderCenter = new Vector3[]
		{
			default(Vector3),
			new Vector3(0f, 2f, 3f),
			new Vector3(0f, 0f, -1f),
			new Vector3(0f, 0f, -0.3f),
			default(Vector3),
			new Vector3(0f, 1f, -2.2f),
			new Vector3(0f, 0.4f, -0.3f)
		};

		internal static float[] itemColliderRadius = new float[] { 2.4f, 1.7f, 2f, 2f, 2f, 5.5f, 0.5f };

		internal static Vector3[][][] carPassengerSeats = new Vector3[][][]
		{
			new Vector3[][]
			{
				new Vector3[]
				{
					new Vector3(0.4364214f, 0.8763284f, 0.7790703f),
					new Vector3(0.3336831f, 1.290549f, 0.7909793f)
				},
				new Vector3[]
				{
					new Vector3(0.0364214f, 0.8763284f, 0.7790703f),
					new Vector3(0.3336831f, 1.290549f, 0.7909793f)
				}
			},
			new Vector3[][]
			{
				new Vector3[]
				{
					new Vector3(0.696f, 1.759f, 2.831f),
					new Vector3(0.6999686f, 1.290549f, 0.7909793f)
				},
				new Vector3[]
				{
					new Vector3(0.696f, 1.759f, 2.331f),
					new Vector3(0.6999686f, 1.290549f, 0.7909793f)
				},
				new Vector3[]
				{
					new Vector3(0f, 1.759f, 2.331f),
					new Vector3(0.6999686f, 1.290549f, 0.7909793f)
				},
				new Vector3[]
				{
					new Vector3(-0.704f, 1.759f, 2.331f),
					new Vector3(0.6999686f, 1.290549f, 0.7909793f)
				}
			},
			new Vector3[][]
			{
				new Vector3[]
				{
					new Vector3(0.513f, 0.669f, -0.259f),
					new Vector3(0.3940828f, 0.8053533f, -0.2172538f)
				},
				new Vector3[]
				{
					new Vector3(-0.513f, 0.669f, -0.9589999f),
					new Vector3(-0.3940828f, 0.8053533f, -1.1f)
				},
				new Vector3[]
				{
					new Vector3(0f, 0.669f, -0.9589999f),
					new Vector3(0f, 0.8053533f, -1.1f)
				},
				new Vector3[]
				{
					new Vector3(0.513f, 0.669f, -0.9589999f),
					new Vector3(0.3940828f, 0.8053533f, -1.1f)
				}
			},
			new Vector3[][] { new Vector3[]
			{
				new Vector3(0.26f, 0.325f, -0.087f),
				new Vector3(0.2239742f, 0.8545685f, -0.3627806f)
			} },
			new Vector3[][]
			{
				new Vector3[]
				{
					new Vector3(0.282f, 0.30727938f, -0.0671216f),
					new Vector3(0.3000017f, 0.5315849f, 0.01975246f)
				},
				new Vector3[]
				{
					new Vector3(-0.282f, 0.30727938f, -0.5671216f),
					new Vector3(-0.3000017f, 0.5315849f, -0.48024753f)
				},
				new Vector3[]
				{
					new Vector3(0.282f, 0.30727938f, -0.5671216f),
					new Vector3(0.3000017f, 0.5315849f, -0.48024753f)
				}
			},
			new Vector3[0][],
			new Vector3[0][]
		};

		internal static readonly FsmNetVehicle.Pivot[] throttlePedals;

		internal static readonly FsmNetVehicle.Pivot[] brakePedals;

		internal static readonly FsmNetVehicle.Pivot[] clutchPedals;

		internal static readonly FsmNetVehicle.Pivot[] steeringWheels;

		internal static readonly FsmNetVehicle.Pivot[] gearSticks;

		internal static readonly FsmNetVehicle.Pivot[] drivingModes;

		public FsmFloat fuelLevel;

		public FsmFloat engineTemp;

		public FsmFloat oilLevel;

		public FsmFloat oilContamination;

		public FsmFloat oilGrade;

		public FsmFloat coolant1Level;

		public FsmFloat coolant2Level;

		public FsmFloat brake1Level;

		public FsmFloat brake2Level;

		public FsmFloat clutchLevel;

		public FsmIgnition ignition;

		internal struct Pivot
		{
			public FsmNetVehicle.Pivot GetAltPivot()
			{
				return new FsmNetVehicle.Pivot
				{
					path = this.path_alt,
					position = this.position_alt,
					eulerAngles = this.eulerAngles_alt
				};
			}

			public string path;

			public Vector3 position;

			public Vector3 eulerAngles;

			public string path_alt;

			public Vector3 position_alt;

			public Vector3 eulerAngles_alt;
		}
	}
}
