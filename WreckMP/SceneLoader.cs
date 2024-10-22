using System;
using UnityEngine;

namespace WreckMP
{
	internal class SceneLoader
	{
		public static void LoadScene(GameScene scene)
		{
			if (scene == GameScene.Unknown)
			{
				return;
			}
			Application.LoadLevel(scene.ToString());
		}
	}
}
