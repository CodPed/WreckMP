using System;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	public static class ObjectUtilities
	{
		public static int GetPlaymakerHash(this PlayMakerFSM fsm)
		{
			return (fsm.transform.GetGameobjectHashString() + "_" + fsm.FsmName).GetHashCode();
		}

		public static string GetGameobjectHashString(this Transform obj)
		{
			if (obj.gameObject.IsPrefab())
			{
				return obj.name + "_PREFAB";
			}
			PlayMakerFSM playMakerFSM = obj.GetComponents<PlayMakerFSM>().FirstOrDefault((PlayMakerFSM f) => f.FsmName == "Use");
			if (playMakerFSM == null)
			{
				return obj.GetPath();
			}
			FsmString fsmString = playMakerFSM.FsmVariables.StringVariables.FirstOrDefault((FsmString s) => s.Name == "ID");
			if (fsmString == null)
			{
				return obj.GetPath();
			}
			if (string.IsNullOrEmpty(fsmString.Value))
			{
				return obj.GetPath();
			}
			return fsmString.Value;
		}

		public static bool IsPrefab(this GameObject go)
		{
			return !go.activeInHierarchy && go.activeSelf && go.transform.parent == null;
		}

		public static string GetPath(this Transform transform)
		{
			string text;
			if (transform.parent == null)
			{
				text = transform.name ?? "";
			}
			else
			{
				text = string.Format("{0}/{1}_{2}", transform.parent.GetFullPath(), transform.name, transform.GetSiblingIndex());
			}
			return text;
		}

		public static string GetFullPath(this Transform transform)
		{
			string text = string.Format("{0}_{1}", transform.name, transform.GetSiblingIndex());
			if (transform.parent == null)
			{
				return text;
			}
			Transform transform2 = transform.parent;
			while (transform2 != null)
			{
				text = string.Format("{0}_{1}/{2}", transform2.name, transform2.GetSiblingIndex(), text);
				transform2 = transform2.parent;
			}
			return text;
		}
	}
}
