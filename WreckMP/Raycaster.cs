using System;
using System.Collections.Generic;
using UnityEngine;

namespace WreckMP
{
	internal static class Raycaster
	{
		public static bool Raycast(Collider collider, float distance = 1f, int layerMask = -1)
		{
			RaycastHit raycastHit;
			return Raycaster.Raycast(out raycastHit, distance, layerMask) && raycastHit.collider == collider;
		}

		public static bool Raycast(out RaycastHit hit, float distance = 1f, int layerMask = -1)
		{
			if (Time.frameCount != Raycaster.lastRaycastFrame)
			{
				Raycaster.raycasts.Clear();
				Raycaster.camera = Camera.main;
				Raycaster.lastRaycastFrame = Time.frameCount;
			}
			Ray ray = Raycaster.camera.ScreenPointToRay(Input.mousePosition);
			if (Raycaster.raycasts.ContainsKey(layerMask))
			{
				RaycastHit raycastHit = Raycaster.raycasts[layerMask];
				if (raycastHit.distance >= distance || raycastHit.collider != null)
				{
					hit = raycastHit;
					return raycastHit.distance < distance;
				}
			}
			Physics.Raycast(ray, ref hit, distance, layerMask);
			Raycaster.raycasts[layerMask] = hit;
			return hit.collider;
		}

		internal static readonly Dictionary<int, RaycastHit> raycasts = new Dictionary<int, RaycastHit>();

		private static int lastRaycastFrame;

		private static Camera camera;
	}
}
