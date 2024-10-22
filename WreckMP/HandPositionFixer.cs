using System;
using System.Collections.Generic;
using UnityEngine;

namespace WreckMP
{
	internal class HandPositionFixer : MonoBehaviour
	{
		private void Start()
		{
		}

		private void Update()
		{
		}

		public void RecalculatePivot()
		{
			this.hasItem = base.transform.childCount == 1;
			if (!this.hasItem)
			{
				return;
			}
			this.item = base.transform.GetChild(0);
			this.item.localPosition = (this.item.localEulerAngles = Vector3.zero);
			MeshFilter meshFilter = this.item.GetComponent<MeshFilter>();
			if (meshFilter == null)
			{
				MeshFilter[] componentsInChildren = this.item.GetComponentsInChildren<MeshFilter>(true);
				if (componentsInChildren.Length != 0)
				{
					meshFilter = componentsInChildren[0];
				}
			}
			if (meshFilter == null)
			{
				return;
			}
			List<int> list = new List<int>();
			Transform transform = meshFilter.transform;
			while (transform != this.item)
			{
				list.Add(transform.GetSiblingIndex());
				transform = transform.parent;
			}
			Quaternion quaternion = Quaternion.identity;
			Transform child = this.item;
			for (int i = 0; i < list.Count; i++)
			{
				child = child.GetChild(list[i]);
				quaternion *= child.localRotation;
			}
			this.item.localRotation = Quaternion.Inverse(quaternion);
			Bounds bounds = meshFilter.mesh.bounds;
			Transform transform2 = meshFilter.transform;
			while (transform2 != base.transform)
			{
				bounds.size = new Vector3(bounds.size.x * transform2.localScale.x, bounds.size.y * transform2.localScale.y, bounds.size.z * transform2.localScale.z);
				transform2 = transform2.parent;
			}
			bounds.center = base.transform.InverseTransformPoint(meshFilter.transform.TransformPoint(bounds.center));
			this.worldCenter = base.transform.TransformPoint(bounds.center);
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			for (int j = 0; j < 3; j++)
			{
				if (bounds.size[j] < bounds.size[num])
				{
					num = j;
				}
				if (bounds.size[j] > bounds.size[num2])
				{
					num2 = j;
				}
			}
			for (int k = 0; k < 3; k++)
			{
				if (num != k && num2 != k)
				{
					num3 = k;
					break;
				}
			}
			Vector3 vector = -Vector3.forward * (bounds.size[num3] / 2f);
			this.item.localPosition = -bounds.center + vector;
			this.worldCenter = base.transform.TransformPoint(vector);
		}

		private void OnDrawGizmos()
		{
			Gizmos.DrawSphere(this.worldCenter, 0.05f);
		}

		private bool hasItem;

		public Vector3 worldCenter;

		public Transform item;
	}
}
