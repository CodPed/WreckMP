using System;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class LocalPlayerAnimationManager : MonoBehaviour
	{
		private void TriggerGesture(int index, int fuckType = 0)
		{
			using (GameEventWriter gameEventWriter = PlayerAnimationManager.triggerGestureEvent.Writer())
			{
				gameEventWriter.Write((byte)index);
				if (index == 4)
				{
					gameEventWriter.Write((byte)fuckType);
				}
				PlayerAnimationManager.triggerGestureEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
			}
		}

		private void StopGesture()
		{
			PlayerAnimationManager.stopGestureEvent.SendEmpty(0UL, true);
		}

		private void Start()
		{
			this.player = FsmVariables.GlobalVariables.FindFsmGameObject("SavePlayer");
			_ObjectsLoader.gameLoaded.Add(delegate
			{
				Transform transform = GameObject.Find("PLAYER").transform.Find("Pivot/AnimPivot/Camera/FPSCamera/FPSCamera");
				PlayMakerFSM playMaker = transform.parent.GetPlayMaker("PlayerFunctions");
				playMaker.Initialize();
				FsmInt middleFingerSwearType = playMaker.FsmVariables.FindFsmInt("RandomInt");
				GameobjectToggleWatcher gameobjectToggleWatcher = transform.Find("Lift").gameObject.AddComponent<GameobjectToggleWatcher>();
				gameobjectToggleWatcher.toggled = (Action<bool>)Delegate.Combine(gameobjectToggleWatcher.toggled, new Action<bool>(delegate(bool b)
				{
					if (b)
					{
						this.TriggerGesture(0, 0);
						return;
					}
					this.StopGesture();
				}));
				GameobjectToggleWatcher gameobjectToggleWatcher2 = transform.Find("Hello").gameObject.AddComponent<GameobjectToggleWatcher>();
				gameobjectToggleWatcher2.toggled = (Action<bool>)Delegate.Combine(gameobjectToggleWatcher2.toggled, new Action<bool>(delegate(bool b)
				{
					if (b)
					{
						this.TriggerGesture(1, 0);
					}
				}));
				GameobjectToggleWatcher gameobjectToggleWatcher3 = transform.Find("Fist").gameObject.AddComponent<GameobjectToggleWatcher>();
				gameobjectToggleWatcher3.toggled = (Action<bool>)Delegate.Combine(gameobjectToggleWatcher3.toggled, new Action<bool>(delegate(bool b)
				{
					if (b)
					{
						this.TriggerGesture(2, 0);
					}
				}));
				GameobjectToggleWatcher gameobjectToggleWatcher4 = transform.Find("Hand Push").gameObject.AddComponent<GameobjectToggleWatcher>();
				gameobjectToggleWatcher4.toggled = (Action<bool>)Delegate.Combine(gameobjectToggleWatcher4.toggled, new Action<bool>(delegate(bool b)
				{
					if (b)
					{
						this.TriggerGesture(3, 0);
						return;
					}
					this.StopGesture();
				}));
				GameobjectToggleWatcher gameobjectToggleWatcher5 = transform.Find("MiddleFinger").gameObject.AddComponent<GameobjectToggleWatcher>();
				gameobjectToggleWatcher5.toggled = (Action<bool>)Delegate.Combine(gameobjectToggleWatcher5.toggled, new Action<bool>(delegate(bool b)
				{
					if (b)
					{
						this.TriggerGesture(4, middleFingerSwearType.Value);
					}
				}));
				GameobjectToggleWatcher gameobjectToggleWatcher6 = transform.Find("Smoking").gameObject.AddComponent<GameobjectToggleWatcher>();
				gameobjectToggleWatcher6.toggled = (Action<bool>)Delegate.Combine(gameobjectToggleWatcher6.toggled, new Action<bool>(delegate(bool b)
				{
					using (GameEventWriter gameEventWriter = PlayerAnimationManager.toggleCigaretteEvent.Writer())
					{
						gameEventWriter.Write(b);
						PlayerAnimationManager.toggleCigaretteEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}));
				PlayMakerFSM playMaker2 = transform.Find("Smoking").GetPlayMaker("Start");
				playMaker2.InsertAction("Double", delegate
				{
					this.TriggerGesture(5, 0);
				}, -1, false);
				playMaker2.InsertAction("Anim 3", new Action(this.StopGesture), -1, false);
				playMaker2.InsertAction("Put out 2", new Action(this.StopGesture), -1, false);
				PlayMakerFSM playMaker3 = transform.parent.Find("SpeakDatabase").GetPlayMaker("Speech");
				FsmInt swearType = playMaker3.FsmVariables.FindFsmInt("RandomInt");
				playMaker3.InsertAction("Swear", delegate
				{
					using (GameEventWriter gameEventWriter2 = PlayerAnimationManager.swearEvent.Writer())
					{
						gameEventWriter2.Write((byte)swearType.Value);
						PlayerAnimationManager.swearEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
					}
				}, -1, false);
				return "local anim mgr";
			});
		}

		private void Update()
		{
			if (this.player.Value == null)
			{
				return;
			}
			if (this.playerHeight == null)
			{
				this.InitPlayerVariables();
			}
			if (Input.GetMouseButtonDown(0))
			{
				using (GameEventWriter gameEventWriter = PlayerAnimationManager.clickEvent.Writer())
				{
					PlayerAnimationManager.clickEvent.Send(gameEventWriter, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}
			byte b = (this.playerInCar.Value ? 2 : ((this.playerHeight.Value >= 1.3f) ? 0 : ((this.playerHeight.Value >= 0.5f) ? 1 : 2)));
			if (b != this.lastCrouch)
			{
				this.lastCrouch = b;
				using (GameEventWriter gameEventWriter2 = PlayerAnimationManager.crouchEvent.Writer())
				{
					gameEventWriter2.Write(b);
					PlayerAnimationManager.crouchEvent.Send(gameEventWriter2, 0UL, true, default(GameEvent.RecordingProperties));
				}
			}
		}

		private void InitPlayerVariables()
		{
			PlayMakerFSM playMaker = this.player.Value.GetPlayMaker("Crouch");
			this.playerHeight = playMaker.FsmVariables.FindFsmFloat("Position");
			this.playerInCar = playMaker.FsmVariables.FindFsmBool("PlayerInCar");
		}

		private FsmGameObject player;

		private FsmFloat playerHeight;

		private FsmBool playerInCar;

		private bool lastSprint;

		private byte lastCrouch;
	}
}
