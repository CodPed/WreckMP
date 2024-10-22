using System;
using System.Collections;
using UnityEngine;

namespace WreckMP
{
	internal class PlayerAnimationManager : MonoBehaviour
	{
		public void TriggerGesture(int index)
		{
			if (index < 0 || index >= PlayerAnimationManager.gesture_keywords.Length)
			{
				return;
			}
			if (this.currentIdleAction != 0)
			{
				this.ToggleIdleAction(false);
			}
			if (this.currentGesture_c != null)
			{
				base.StopCoroutine(this.currentGesture_c);
				this.currentGesture_c = null;
			}
			this.currentGesture_c = base.StartCoroutine(this.C_TriggerGesture(index));
		}

		public void StopGesture()
		{
			int num = this.currentGesture;
			if (num < 0 || num >= PlayerAnimationManager.gesture_keywords.Length)
			{
				return;
			}
			if (PlayerAnimationManager.gesture_onetime[num])
			{
				return;
			}
			if (this.currentGesture_c != null)
			{
				base.StopCoroutine(this.currentGesture_c);
				this.currentGesture_c = null;
			}
			base.StartCoroutine(this.C_StopGesture(num));
		}

		private IEnumerator C_TriggerGesture(int index)
		{
			if (this.currentGesture != -1 && !PlayerAnimationManager.gesture_onetime[this.currentGesture])
			{
				yield return base.StartCoroutine(this.C_StopGesture(this.currentGesture));
			}
			this.currentGesture = index;
			bool flag = PlayerAnimationManager.gesture_onetime[index];
			if (flag)
			{
				this.animatorGestures.SetTrigger(PlayerAnimationManager.gesture_keywords[index]);
			}
			else
			{
				this.animatorGestures.SetBool(PlayerAnimationManager.gesture_keywords[index], true);
			}
			if (PlayerAnimationManager.gesture_lefthand[index])
			{
				this.syncer.leftArm = 2;
				this.leftHand.enabled = false;
			}
			if (PlayerAnimationManager.gesture_righthand[index])
			{
				this.syncer.rightArm = 2;
				this.rightHand.enabled = false;
			}
			if (index == 7)
			{
				this.syncer.head = 2;
			}
			if (flag)
			{
				yield return new WaitForSeconds(0.75f);
				if (index == 7)
				{
					MasterAudio.PlaySound3DAndForget("HouseFoley", base.transform, true, 1f, null, 0f, "bottle_cap");
					this.beer_bottle.SetActive(true);
					yield return new WaitForSeconds(2.4f);
					string text = "Burb";
					Transform transform = base.transform;
					bool flag2 = true;
					float num = 1f;
					string text2 = "burb0" + Random.Range(1, 3).ToString();
					MasterAudio.PlaySound3DAndForget(text, transform, flag2, num, null, 0f, text2);
					yield return new WaitForSeconds(0.93f);
					this.beer_bottle.SetActive(false);
				}
				while (!this.animatorGestures.GetCurrentAnimatorStateInfo(0).IsName("breath"))
				{
					yield return new WaitForEndOfFrame();
				}
				if (PlayerAnimationManager.gesture_lefthand[index])
				{
					this.syncer.leftArm = ((this.currentCar != null) ? 0 : 1);
					this.leftHand.enabled = this.currentCar != null;
				}
				if (PlayerAnimationManager.gesture_righthand[index])
				{
					this.syncer.rightArm = ((this.holdingTowHook || this.grabbedItem != null || this.currentCar != null) ? 0 : 1);
					this.rightHand.enabled = this.holdingTowHook || this.grabbedItem != null || this.currentCar != null;
				}
				if (index == 7)
				{
					this.syncer.head = 0;
				}
				this.currentGesture = -1;
			}
			this.currentGesture_c = null;
			yield break;
		}

		private IEnumerator C_StopGesture(int index)
		{
			this.beer_bottle.SetActive(false);
			this.animatorGestures.SetBool(PlayerAnimationManager.gesture_keywords[index], false);
			while (!this.animatorGestures.GetCurrentAnimatorStateInfo(0).IsName("breath"))
			{
				yield return new WaitForEndOfFrame();
			}
			if (PlayerAnimationManager.gesture_lefthand[index])
			{
				this.syncer.leftArm = ((this.currentCar != null) ? 0 : 1);
			}
			if (PlayerAnimationManager.gesture_righthand[index])
			{
				this.syncer.rightArm = ((this.holdingTowHook || this.grabbedItem != null || this.currentCar != null) ? 0 : 1);
			}
			if (index == 7)
			{
				this.syncer.head = 0;
			}
			this.currentGesture = -1;
			yield break;
		}

		public static void RegisterEvents()
		{
			PlayerAnimationManager.clickEvent = new GameEvent("PlayerClicked", delegate(GameEventReader p)
			{
				PlayerAnimationManager playerAnimationManager = CoreManager.Players[p.sender].playerAnimationManager;
				if (playerAnimationManager.currentCar == null)
				{
					playerAnimationManager.OnPlayerClick();
				}
			}, GameScene.GAME);
			PlayerAnimationManager.crouchEvent = new GameEvent("Crouch", delegate(GameEventReader p)
			{
				CoreManager.Players[p.sender].playerAnimationManager.SetCrouch(p.ReadByte());
			}, GameScene.GAME);
			PlayerAnimationManager.triggerGestureEvent = new GameEvent("TriggerGesture", delegate(GameEventReader p)
			{
				PlayerAnimationManager playerAnimationManager2 = CoreManager.Players[p.sender].playerAnimationManager;
				byte b = p.ReadByte();
				playerAnimationManager2.TriggerGesture((int)b);
				if (b == 4)
				{
					byte b2 = p.ReadByte();
					string text = "Fuck";
					Transform transform = playerAnimationManager2.transform;
					bool flag = true;
					float num = 1f;
					string text2 = b2.ToString();
					MasterAudio.PlaySound3DAndForget(text, transform, flag, num, null, 0f, text2);
				}
			}, GameScene.GAME);
			PlayerAnimationManager.stopGestureEvent = new GameEvent("StopGesture", delegate(GameEventReader p)
			{
				CoreManager.Players[p.sender].playerAnimationManager.StopGesture();
			}, GameScene.GAME);
			PlayerAnimationManager.toggleCigaretteEvent = new GameEvent("ToggleCigarette", delegate(GameEventReader p)
			{
				CoreManager.Players[p.sender].playerAnimationManager.cigarette.SetActive(p.ReadBoolean());
			}, GameScene.GAME);
			PlayerAnimationManager.swearEvent = new GameEvent("Swear", delegate(GameEventReader p)
			{
				PlayerAnimationManager playerAnimationManager3 = CoreManager.Players[p.sender].playerAnimationManager;
				byte b3 = p.ReadByte();
				string text3 = "Swearing";
				Transform transform2 = playerAnimationManager3.transform;
				bool flag2 = true;
				float num2 = 1f;
				string text4 = b3.ToString();
				MasterAudio.PlaySound3DAndForget(text3, transform2, flag2, num2, null, 0f, text4);
			}, GameScene.GAME);
		}

		public void SetPassengerMode(bool set)
		{
			this.animator.SetBool("passenger", set);
			if (set)
			{
				this.animator.Play("PassengerMode");
			}
		}

		public void GrabItem(Rigidbody item)
		{
			bool flag = item != null;
			this.rightHand.enabled = flag || this.currentCar != null;
			this.syncer.rightArm = ((this.currentGesture != -1 && PlayerAnimationManager.gesture_righthand[this.currentGesture]) ? 2 : ((flag || this.currentCar != null) ? 0 : 1));
			this.grabbedItem = (flag ? item.transform : null);
		}

		public void SetTowhook(bool grab)
		{
			this.rightHand.enabled = grab || this.currentCar != null;
			this.syncer.rightArm = ((this.currentGesture != -1 && PlayerAnimationManager.gesture_righthand[this.currentGesture]) ? 2 : ((grab || this.currentCar != null) ? 0 : 1));
			this.holdingTowHook = grab;
		}

		public void OnPlayerClick()
		{
			this.TriggerGesture(6);
		}

		public void SetCrouch(byte val)
		{
			if (this.crouch > 0 != val > 0)
			{
				base.StartCoroutine(this.Crouch(val));
			}
			this.crouch = val;
		}

		public void SetPlayerInCar(bool inCar, NetVehicle car)
		{
			Behaviour behaviour = this.leftHand;
			Behaviour behaviour2 = this.rightHand;
			Behaviour behaviour3 = this.leftLeg;
			this.rightLeg.enabled = inCar;
			behaviour3.enabled = inCar;
			behaviour2.enabled = inCar;
			behaviour.enabled = inCar;
			if (inCar)
			{
				this.syncer.leftArm = 0;
				this.syncer.rightArm = 0;
				this.syncer.leftLeg = 0;
				this.syncer.rightLeg = 0;
			}
			else
			{
				this.syncer.leftArm = 1;
				this.syncer.rightArm = 1;
				this.syncer.leftLeg = 1;
				this.syncer.rightLeg = 1;
			}
			this.animator.enabled = !inCar;
			this.currentCar = (inCar ? car : null);
			this.rotationBendPivot.localEulerAngles = (inCar ? (Vector3.forward * 16f) : Vector3.zero);
		}

		private void Start()
		{
			Transform transform = base.transform.parent.Find("skeleton ANIMATOR");
			Transform transform2 = base.transform.parent.Find("skeleton ANIMATOR gestures");
			this.animator = transform.GetComponent<Animator>();
			this.animatorGestures = transform2.GetComponent<Animator>();
			this.syncer = base.gameObject.AddComponent<AnimatorSyncer>();
			this.syncer.sourceSkeleton = transform;
			this.syncer.sourceSkeleton2 = transform2;
			this.rotationBendPivot = base.transform.Find("pelvis/RotationBendPivot");
			Transform transform3 = base.transform.Find("pelvis/RotationBendPivot/spine_middle/spine_upper/collar_left/shoulder_left/arm_left/hand_left/finger_left");
			this.leftHand = FastIKFabric.CreateInstance(transform3, 3, base.transform.Find("la_hint"));
			this.beer_bottle = transform3.parent.Find("beer_bottle").gameObject;
			Transform transform4 = base.transform.Find("pelvis/RotationBendPivot/spine_middle/spine_upper/collar_right/shoulder_right/arm_right/hand_right/fingers_right");
			this.cigarette = transform4.Find("cigarette_shaft_0").gameObject;
			this.rightFingers = transform4.gameObject.AddComponent<HandPositionFixer>();
			this.rightHand = FastIKFabric.CreateInstance(transform4, 3, base.transform.Find("ra_hint"));
			this.leftLeg = FastIKFabric.CreateInstance(base.transform.Find("thig_left/knee_left/ankle_left"), 2, base.transform.Find("ll_hint"));
			this.rightLeg = FastIKFabric.CreateInstance(base.transform.Find("thig_right/knee_right/ankle_right"), 2, base.transform.Find("rl_hint"));
			this.towHookPivot = new GameObject("TowPivot").transform;
			this.towHookPivot.parent = base.transform;
			this.towHookPivot.localPosition = new Vector3(0.005f, 0.002f, 0.003f);
			this.SetPlayerInCar(false, null);
		}

		private void Update()
		{
			if (this.currentCar == null)
			{
				Vector3 position = base.transform.position;
				float sqrMagnitude = (position - this.lastPosition).sqrMagnitude;
				this.isWalking = sqrMagnitude > 2E-05f && this.allowMoveAnims;
				bool flag = !this.isWalking && this.currentGesture == -1;
				if (!flag && this.currentIdleAction != 0)
				{
					this.ToggleIdleAction(false);
				}
				else if (flag && this.currentIdleAction == 0)
				{
					this.idleActionTimer -= Time.deltaTime;
					if (this.idleActionTimer < 0f)
					{
						this.ToggleIdleAction(true);
					}
				}
				this.animator.SetBool("walking", this.isWalking);
				this.animator.SetBool("running", this.isRunning && this.allowMoveAnims);
				this.animator.SetInteger("crouch", (int)(this.allowMoveAnims ? this.crouch : 0));
				this.lastPosition = position;
			}
			else
			{
				if (this.currentIdleAction != 0)
				{
					this.ToggleIdleAction(false);
				}
				this.leftHand.Target.position = this.currentCar.driverPivots.steeringWheel.position;
				this.leftHand.Target.rotation = this.currentCar.driverPivots.steeringWheel.rotation;
				this.rightHand.Target.position = this.currentCar.driverPivots.gearStick.position;
				this.rightHand.Target.rotation = this.currentCar.driverPivots.gearStick.rotation;
				this.rightLeg.Target.position = this.currentCar.driverPivots.throttlePedal.position;
				this.rightLeg.Target.rotation = this.currentCar.driverPivots.throttlePedal.rotation;
				if (this.currentCar.driverPivots.clutchPedal == null)
				{
					this.leftLeg.Target.position = this.currentCar.driverPivots.brakePedal.position;
					this.leftLeg.Target.rotation = this.currentCar.driverPivots.brakePedal.rotation;
				}
				else if (!this.brakeClutchLerping)
				{
					this.leftLeg.Target.position = Vector3.Lerp(this.currentCar.driverPivots.brakePedal.position, this.currentCar.driverPivots.clutchPedal.position, this.brakeClutchLerp);
					this.leftLeg.Target.rotation = Quaternion.Lerp(this.currentCar.driverPivots.brakePedal.rotation, this.currentCar.driverPivots.clutchPedal.rotation, this.brakeClutchLerp);
					if (this.currentCar.AxisCarController.brakeInput > 0f || this.currentCar.AxisCarController.clutchInput > 0f)
					{
						float num = 1f;
						if (this.currentCar.AxisCarController.brakeInput > 0f)
						{
							num = 0f;
						}
						if (num != this.brakeClutchLerp)
						{
							this.brakeClutchLerping = true;
							base.StartCoroutine(this.MoveIKTarget(this.leftLeg, this.currentCar.driverPivots.brakePedal, this.currentCar.driverPivots.clutchPedal, this.brakeClutchLerp, num, 0.2f, delegate
							{
								this.brakeClutchLerping = false;
							}));
							this.brakeClutchLerp = num;
						}
					}
				}
			}
			if (this.holdingTowHook)
			{
				this.rightHand.Target.position = this.towHookPivot.position;
				this.rightHand.Target.rotation = base.transform.rotation;
				return;
			}
			if (this.grabbedItem != null)
			{
				this.rightHand.Target.position = this.grabbedItem.position;
				this.rightHand.Target.rotation = base.transform.rotation;
			}
		}

		private void ToggleIdleAction(bool value)
		{
			this.idleActionTimer = Random.Range(5f, 30f);
			this.currentIdleAction = (value ? Random.Range(1, PlayerAnimationManager.idleActionsLengths.Length) : 0);
			this.animator.SetInteger("idleAction", this.currentIdleAction);
			if (this.currentIdleAction != 0)
			{
				base.StartCoroutine(this.C_WaitIdleAction(this.currentIdleAction));
			}
		}

		private IEnumerator C_WaitIdleAction(int action)
		{
			yield return new WaitForSeconds(PlayerAnimationManager.idleActionsLengths[action]);
			if (this.currentIdleAction == action)
			{
				this.ToggleIdleAction(false);
			}
			yield break;
		}

		private IEnumerator MoveIKTarget(FastIKFabric target, Transform from, Transform to, float oldT, float newT, float time, Action onFinished)
		{
			float t = 0f;
			while (t < 1f)
			{
				t += Time.deltaTime * (1f / time);
				float num = Mathf.Lerp(oldT, newT, t);
				target.Target.position = Vector3.Lerp(from.position, to.position, num);
				target.Target.rotation = Quaternion.Lerp(from.rotation, to.rotation, num);
				yield return new WaitForEndOfFrame();
			}
			target.Target.position = to.position;
			target.Target.rotation = to.rotation;
			if (onFinished != null)
			{
				onFinished();
			}
			yield break;
		}

		private IEnumerator Crouch(byte newCrouch)
		{
			float a = ((this.crouch == 0) ? (-0.38f) : ((this.crouch == 1) ? (-0.88f) : (-1.08f)));
			float b = ((newCrouch == 0) ? (-0.38f) : ((newCrouch == 1) ? (-0.88f) : (-1.08f)));
			float t = 0f;
			while (t < 1f)
			{
				t += Time.deltaTime * 3f;
				Vector3 localPosition = this.charTf.localPosition;
				localPosition.y = Mathf.Lerp(a, b, t);
				this.charTf.localPosition = localPosition;
				yield return new WaitForEndOfFrame();
			}
			Vector3 localPosition2 = this.charTf.localPosition;
			localPosition2.y = b;
			this.charTf.localPosition = localPosition2;
			yield break;
		}

		public bool isRunning;

		public byte crouch;

		public FastIKFabric leftHand;

		public FastIKFabric rightHand;

		public FastIKFabric leftLeg;

		public FastIKFabric rightLeg;

		private HandPositionFixer rightFingers;

		internal Transform charTf;

		internal Transform towHookPivot;

		private const float normalY = -0.38f;

		private const float crouchY = -0.88f;

		private const float crouch2Y = -1.08f;

		private Animator animator;

		private Animator animatorGestures;

		private AnimatorSyncer syncer;

		private Vector3 lastPosition;

		private Transform rotationBendPivot;

		private Transform grabbedItem;

		internal bool isWalking;

		internal bool allowMoveAnims = true;

		internal bool holdingTowHook;

		private NetVehicle currentCar;

		private float brakeClutchLerp;

		private bool brakeClutchLerping;

		public static GameEvent clickEvent;

		public static GameEvent crouchEvent;

		public static GameEvent triggerGestureEvent;

		public static GameEvent stopGestureEvent;

		public static GameEvent toggleCigaretteEvent;

		public static GameEvent swearEvent;

		private GameObject cigarette;

		private GameObject beer_bottle;

		private int currentIdleAction;

		private float idleActionTimer = Random.Range(10f, 30f);

		private static readonly float[] idleActionsLengths = new float[]
		{
			0f, 2.5f, 2.25f, 4f, 3.25f, 1.7f, 3.1f, 4f, 0f, 2f,
			6f
		};

		private static readonly string[] gesture_keywords = new string[] { "hitchhike", "wave", "hit", "push", "finger", "smoke", "click", "drinkbeer" };

		private static readonly bool[] gesture_onetime = new bool[] { false, true, true, false, true, false, true, true };

		private static readonly bool[] gesture_lefthand = new bool[] { false, false, false, true, true, false, false, true };

		private static readonly bool[] gesture_righthand = new bool[] { true, true, true, true, true, true, true, false };

		private int currentGesture = -1;

		private Coroutine currentGesture_c;
	}
}
