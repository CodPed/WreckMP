using System;
using UnityEngine;

namespace WreckMP
{
	internal class CharacterCustomizationItem : MonoBehaviour
	{
		public int SelectedIndex
		{
			get
			{
				return this.selectedIndex;
			}
		}

		public static CharacterCustomizationItem Init(int clothingIndex, Action clothingChanged, Transform buttonsParent, Transform fieldStringParent, string[] names = null, Texture2D[] textures = null, Material targetMaterial = null, Transform targetParent = null, Transform targetParent2 = null)
		{
			CharacterCustomizationItem characterCustomizationItem = (buttonsParent ?? CharacterCustomizationItem.parentTo).gameObject.AddComponent<CharacterCustomizationItem>();
			if (buttonsParent != null)
			{
				characterCustomizationItem.buttonLeft = buttonsParent.GetChild(1).GetComponent<Collider>();
				characterCustomizationItem.buttonRight = buttonsParent.GetChild(0).GetComponent<Collider>();
			}
			if (fieldStringParent != null)
			{
				characterCustomizationItem.fieldString = fieldStringParent.GetComponent<TextMesh>();
				characterCustomizationItem.fieldStringBackground = fieldStringParent.GetChild(0).GetComponent<TextMesh>();
			}
			characterCustomizationItem.names = names;
			characterCustomizationItem.textures = textures;
			characterCustomizationItem.targetMaterial = targetMaterial;
			characterCustomizationItem.targetParent = targetParent;
			characterCustomizationItem.targetParent2 = targetParent2;
			characterCustomizationItem.clothingIndex = clothingIndex;
			characterCustomizationItem.clothingChanged = clothingChanged;
			return characterCustomizationItem;
		}

		private void Update()
		{
			if (this.buttonLeft != null && this.buttonRight != null && Input.GetMouseButtonDown(0))
			{
				if (Raycaster.Raycast(this.buttonLeft, 1.35f, -1))
				{
					this.SetOption(this.selectedIndex - 1, true);
					return;
				}
				if (Raycaster.Raycast(this.buttonRight, 1.35f, -1))
				{
					this.SetOption(this.selectedIndex + 1, true);
				}
			}
		}

		public void SetOption(int index, bool sendEvent = true)
		{
			int num = ((this.textures == null) ? this.targetParent.childCount : ((this.targetParent == null) ? this.textures.Length : (this.textures.Length + 1)));
			index = Mathf.Clamp(index, 0, num - 1);
			if (this.fieldString != null && this.fieldStringBackground != null)
			{
				string text = ((this.names != null) ? this.names[index] : ((this.targetParent != null && this.textures != null) ? ((index == 0) ? "None" : string.Format("INDEX {0}", index)) : ((this.targetParent != null) ? this.targetParent.GetChild(index).name : string.Format("INDEX {0}", index))));
				text = text.ToUpper();
				this.fieldString.text = (this.fieldStringBackground.text = text);
			}
			if (this.targetParent != null && this.textures != null)
			{
				this.targetParent.gameObject.SetActive(index > 0);
				if (this.targetParent2 != null)
				{
					this.targetParent2.gameObject.SetActive(index > 0);
				}
				this.targetMaterial.mainTexture = this.textures[(index == 0) ? 0 : (index - 1)];
			}
			else if (this.textures != null)
			{
				this.targetMaterial.mainTexture = this.textures[index];
			}
			else
			{
				this.targetParent.GetChild(this.selectedIndex).gameObject.SetActive(false);
				this.targetParent.GetChild(index).gameObject.SetActive(true);
			}
			this.selectedIndex = index;
			if (sendEvent)
			{
				using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
				{
					gameEventWriter.Write(this.clothingIndex);
					gameEventWriter.Write(this.selectedIndex);
					GameEvent<CharacterCustomization>.Send("SkinChange", gameEventWriter, 0UL, true);
				}
			}
			Action action = this.clothingChanged;
			if (action == null)
			{
				return;
			}
			action();
		}

		public Collider buttonLeft;

		public Collider buttonRight;

		public TextMesh fieldString;

		public TextMesh fieldStringBackground;

		public string[] names;

		public Texture2D[] textures;

		public Material targetMaterial;

		public Transform targetParent;

		public Transform targetParent2;

		internal int clothingIndex = -1;

		private int selectedIndex;

		public static Transform parentTo;

		private Action clothingChanged;
	}
}
