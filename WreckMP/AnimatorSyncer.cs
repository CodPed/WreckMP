using System;
using System.Collections.Generic;
using UnityEngine;

namespace WreckMP
{
	internal class AnimatorSyncer : MonoBehaviour
	{
		private void Start()
		{
			this.InitBones();
		}

		private void InitBones()
		{
			int num = 0;
			this.LoopBones(this.sourceSkeleton.Find("pelvis"), "", ref num);
			this.pelvisEndIndex = this.sourceBones.Count;
			this.LoopBones(this.sourceSkeleton.Find("thig_left"), "", ref num);
			this.LoopBones(this.sourceSkeleton.Find("thig_right"), "", ref num);
		}

		private void LoopBones(Transform bone, string subPath, ref int successCount)
		{
			if (subPath != "")
			{
				subPath += "/";
			}
			subPath += bone.name;
			for (int i = 0; i < bone.childCount; i++)
			{
				Transform child = bone.GetChild(i);
				this.LoopBones(child, subPath, ref successCount);
			}
			this.sourceBones.Add(bone);
			this.sourceBones2.Add(this.sourceSkeleton2.Find(subPath));
			Transform transform = base.transform.Find(subPath);
			this.targetBones.Add(transform);
			if (transform != null && bone != null)
			{
				successCount++;
			}
		}

		private void LateUpdate()
		{
			for (int i = 0; i < this.sourceBones.Count; i++)
			{
				string text = this.sourceBones[i].name.ToLower();
				bool flag = i >= this.pelvisEndIndex;
				bool flag2 = text.Contains("head");
				bool flag3 = text.Contains("left");
				bool flag4 = text.Contains("right");
				int num = 0;
				if (flag3 && flag)
				{
					num = this.leftLeg;
				}
				else if (flag4 && flag)
				{
					num = this.rightLeg;
				}
				else if (flag3 && !flag)
				{
					num = this.leftArm;
				}
				else if (flag4 && !flag)
				{
					num = this.rightArm;
				}
				else if (flag2)
				{
					num = this.head;
				}
				if (num != 0)
				{
					this.targetBones[i].localPosition = ((num == 1) ? this.sourceBones : this.sourceBones2)[i].localPosition;
					this.targetBones[i].localEulerAngles = ((num == 1) ? this.sourceBones : this.sourceBones2)[i].localEulerAngles;
				}
			}
		}

		internal Transform sourceSkeleton;

		internal Transform sourceSkeleton2;

		private List<Transform> sourceBones = new List<Transform>();

		private List<Transform> sourceBones2 = new List<Transform>();

		private List<Transform> targetBones = new List<Transform>();

		private int pelvisEndIndex;

		public int head;

		public int leftLeg = 1;

		public int rightLeg = 1;

		public int leftArm = 1;

		public int rightArm = 1;
	}
}
