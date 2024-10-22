using System;
using UnityEngine;

namespace WreckMP
{
	internal class GameobjectToggleWatcher : MonoBehaviour
	{
		private void OnEnable()
		{
			Action<bool> action = this.toggled;
			if (action == null)
			{
				return;
			}
			action(true);
		}

		private void OnDisable()
		{
			Action<bool> action = this.toggled;
			if (action == null)
			{
				return;
			}
			action(false);
		}

		public Action<bool> toggled;
	}
}
