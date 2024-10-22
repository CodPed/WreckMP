using System;
using UnityEngine;

namespace WreckMP
{
	internal class FastIKFabric : MonoBehaviour
	{
		public static FastIKFabric CreateInstance(Transform lastBone, int length, Transform hint)
		{
			FastIKFabric fastIKFabric = lastBone.gameObject.AddComponent<FastIKFabric>();
			fastIKFabric.ChainLength = length;
			fastIKFabric.Pole = hint;
			fastIKFabric.Target = new GameObject(lastBone.name + "_target").transform;
			return fastIKFabric;
		}

		private void Awake()
		{
			this.Init();
		}

		private void Init()
		{
			this.Bones = new Transform[this.ChainLength + 1];
			this.Positions = new Vector3[this.ChainLength + 1];
			this.BonesLength = new float[this.ChainLength];
			this.StartDirectionSucc = new Vector3[this.ChainLength + 1];
			this.StartRotationBone = new Quaternion[this.ChainLength + 1];
			this.Root = base.transform;
			for (int i = 0; i <= this.ChainLength; i++)
			{
				if (this.Root == null)
				{
					throw new UnityException("The chain value is longer than the ancestor chain!");
				}
				this.Root = this.Root.parent;
			}
			if (this.Target == null)
			{
				this.Target = new GameObject(base.gameObject.name + " Target").transform;
				this.SetPositionRootSpace(this.Target, this.GetPositionRootSpace(base.transform));
			}
			this.StartRotationTarget = this.GetRotationRootSpace(this.Target);
			Transform transform = base.transform;
			this.CompleteLength = 0f;
			for (int j = this.Bones.Length - 1; j >= 0; j--)
			{
				this.Bones[j] = transform;
				this.StartRotationBone[j] = this.GetRotationRootSpace(transform);
				if (j == this.Bones.Length - 1)
				{
					this.StartDirectionSucc[j] = this.GetPositionRootSpace(this.Target) - this.GetPositionRootSpace(transform);
				}
				else
				{
					this.StartDirectionSucc[j] = this.GetPositionRootSpace(this.Bones[j + 1]) - this.GetPositionRootSpace(transform);
					this.BonesLength[j] = this.StartDirectionSucc[j].magnitude;
					this.CompleteLength += this.BonesLength[j];
				}
				transform = transform.parent;
			}
		}

		private void Update()
		{
		}

		private void LateUpdate()
		{
			this.ResolveIK();
		}

		private void ResolveIK()
		{
			if (this.Target == null || !this.AllowIK)
			{
				return;
			}
			if (this.BonesLength.Length != this.ChainLength)
			{
				this.Init();
			}
			for (int i = 0; i < this.Bones.Length; i++)
			{
				this.Positions[i] = this.GetPositionRootSpace(this.Bones[i]);
			}
			Vector3 positionRootSpace = this.GetPositionRootSpace(this.Target);
			Quaternion rotationRootSpace = this.GetRotationRootSpace(this.Target);
			if ((positionRootSpace - this.GetPositionRootSpace(this.Bones[0])).sqrMagnitude >= this.CompleteLength * this.CompleteLength)
			{
				Vector3 normalized = (positionRootSpace - this.Positions[0]).normalized;
				for (int j = 1; j < this.Positions.Length; j++)
				{
					this.Positions[j] = this.Positions[j - 1] + normalized * this.BonesLength[j - 1];
				}
			}
			else
			{
				for (int k = 0; k < this.Positions.Length - 1; k++)
				{
					this.Positions[k + 1] = Vector3.Lerp(this.Positions[k + 1], this.Positions[k] + this.StartDirectionSucc[k], this.SnapBackStrength);
				}
				for (int l = 0; l < this.Iterations; l++)
				{
					for (int m = this.Positions.Length - 1; m > 0; m--)
					{
						if (m == this.Positions.Length - 1)
						{
							this.Positions[m] = positionRootSpace;
						}
						else
						{
							this.Positions[m] = this.Positions[m + 1] + (this.Positions[m] - this.Positions[m + 1]).normalized * this.BonesLength[m];
						}
					}
					for (int n = 1; n < this.Positions.Length; n++)
					{
						this.Positions[n] = this.Positions[n - 1] + (this.Positions[n] - this.Positions[n - 1]).normalized * this.BonesLength[n - 1];
					}
					if ((this.Positions[this.Positions.Length - 1] - positionRootSpace).sqrMagnitude < this.Delta * this.Delta)
					{
						break;
					}
				}
			}
			if (this.Pole != null)
			{
				Vector3 positionRootSpace2 = this.GetPositionRootSpace(this.Pole);
				for (int num = 1; num < this.Positions.Length - 1; num++)
				{
					Plane plane;
					plane..ctor(this.Positions[num + 1] - this.Positions[num - 1], this.Positions[num - 1]);
					Vector3 vector = this.ClosestPointOnPlane(plane, positionRootSpace2);
					float num2 = FastIKFabric.SignedAngle(this.ClosestPointOnPlane(plane, this.Positions[num]) - this.Positions[num - 1], vector - this.Positions[num - 1], plane.normal);
					this.Positions[num] = Quaternion.AngleAxis(num2, plane.normal) * (this.Positions[num] - this.Positions[num - 1]) + this.Positions[num - 1];
				}
			}
			for (int num3 = 0; num3 < this.Positions.Length; num3++)
			{
				if (num3 == this.Positions.Length - 1)
				{
					this.SetRotationRootSpace(this.Bones[num3], Quaternion.Inverse(rotationRootSpace) * this.StartRotationTarget * Quaternion.Inverse(this.StartRotationBone[num3]));
				}
				else
				{
					this.SetRotationRootSpace(this.Bones[num3], Quaternion.FromToRotation(this.StartDirectionSucc[num3], this.Positions[num3 + 1] - this.Positions[num3]) * Quaternion.Inverse(this.StartRotationBone[num3]));
				}
				this.SetPositionRootSpace(this.Bones[num3], this.Positions[num3]);
			}
		}

		private Vector3 ClosestPointOnPlane(Plane plane, Vector3 point)
		{
			float num = Vector3.Dot(plane.normal, point) + plane.distance;
			return point - plane.normal * num;
		}

		public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
		{
			float num = Vector3.Angle(from, to);
			float num2 = from.y * to.z - from.z * to.y;
			float num3 = from.z * to.x - from.x * to.z;
			float num4 = from.x * to.y - from.y * to.x;
			float num5 = Mathf.Sign(axis.x * num2 + axis.y * num3 + axis.z * num4);
			return num * num5;
		}

		private Vector3 GetPositionRootSpace(Transform current)
		{
			if (this.Root == null)
			{
				return current.position;
			}
			return Quaternion.Inverse(this.Root.rotation) * (current.position - this.Root.position);
		}

		private void SetPositionRootSpace(Transform current, Vector3 position)
		{
			if (this.Root == null)
			{
				current.position = position;
				return;
			}
			current.position = this.Root.rotation * position + this.Root.position;
		}

		private Quaternion GetRotationRootSpace(Transform current)
		{
			if (this.Root == null)
			{
				return current.rotation;
			}
			return Quaternion.Inverse(current.rotation) * this.Root.rotation;
		}

		private void SetRotationRootSpace(Transform current, Quaternion rotation)
		{
			if (this.Root == null)
			{
				current.rotation = rotation;
				return;
			}
			current.rotation = this.Root.rotation * rotation;
		}

		public int ChainLength = 2;

		public Transform Target;

		public Transform Pole;

		[Header("Solver Parameters")]
		public int Iterations = 10;

		public float Delta = 0.001f;

		[Range(0f, 1f)]
		public float SnapBackStrength = 1f;

		public bool AllowIK = true;

		protected float[] BonesLength;

		protected float CompleteLength;

		protected Transform[] Bones;

		protected Vector3[] Positions;

		protected Vector3[] StartDirectionSucc;

		protected Quaternion[] StartRotationBone;

		protected Quaternion StartRotationTarget;

		protected Transform Root;
	}
}
