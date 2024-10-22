using System;
using System.IO;
using System.Linq;
using HutongGames.PlayMaker;
using UnityEngine;

namespace WreckMP
{
	internal class CharacterCustomization : MonoBehaviour
	{
		public static void LoadTextures(AssetBundle ab)
		{
			CharacterCustomization.faces = new Texture2D[17];
			CharacterCustomization.pants = new Texture2D[21];
			CharacterCustomization.shirts = new Texture2D[47];
			CharacterCustomization.shoes = new Texture2D[7];
			for (int i = 1; i <= Mathf.Max(new int[] { 21, 7, 47, 17 }); i++)
			{
				string text = i.ToString();
				if (i < 10)
				{
					text = "0" + text;
				}
				if (i <= 17)
				{
					CharacterCustomization.faces[i - 1] = ab.LoadAsset<Texture2D>("char_face" + text + ".png");
				}
				if (i <= 21)
				{
					CharacterCustomization.pants[i - 1] = ab.LoadAsset<Texture2D>("char_pants" + text + ".png");
				}
				if (i <= 47)
				{
					CharacterCustomization.shirts[i - 1] = ab.LoadAsset<Texture2D>("char_shirt" + text + ".png");
				}
				if (i <= 7)
				{
					CharacterCustomization.shoes[i - 1] = ab.LoadAsset<Texture2D>("char_shoes" + text + ".png");
				}
			}
		}

		public static CharacterCustomization Init(AssetBundle ab)
		{
			Console.Log(string.Format("Enter charcustom init {0}", ab == null), false);
			GameObject gameObject = Object.Instantiate<GameObject>(ab.LoadAsset<GameObject>("LocalPlayerSkinRender.prefab"));
			gameObject.transform.position = Vector3.up * -10f;
			gameObject.name = "LocalPlayerSkinRender";
			Console.Log("Enter charcustom init 0.1", false);
			GameObject gameObject2 = Object.Instantiate<GameObject>(ab.LoadAsset<GameObject>("char.prefab"));
			Console.Log("Enter charcustom init 0.2", false);
			gameObject2.name = "localPlayerModel";
			gameObject2.transform.parent = gameObject.transform;
			gameObject2.transform.localPosition = (gameObject2.transform.localEulerAngles = Vector3.zero);
			gameObject2.transform.Find("char/Camera").gameObject.SetActive(true);
			gameObject2.SetActive(false);
			Console.Log("Enter charcustom init 1", false);
			GameObject gameObject3 = Object.Instantiate<GameObject>(ab.LoadAsset<GameObject>("Settings_Char.prefab"));
			gameObject3.transform.SetParent(GameObject.Find("Systems").transform.Find("OptionsMenu"));
			gameObject3.name = "PlayerCustomization";
			gameObject3.transform.localPosition = new Vector3(4f, -0.1f, 0f);
			gameObject3.transform.localEulerAngles = new Vector3(270f, 0f, 0f);
			gameObject3.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
			gameObject3.SetActive(false);
			Console.Log("Enter charcustom init 2", false);
			GameObject gameObject4 = GameObject.Find("Systems").transform.Find("OptionsMenu/Menu").gameObject;
			gameObject4.transform.Find("Table 5").localPosition = new Vector3(0f, -1.1f, 0.01f);
			gameObject4.transform.Find("Table 5").localScale = new Vector3(1f, 1.3f, 1f);
			gameObject4.transform.Find("Table 3").localPosition = new Vector3(0f, 1.55f, 0.01f);
			gameObject4.transform.Find("Header 4").localPosition = new Vector3(0f, 2.55f, 0.02f);
			gameObject4.transform.Find("BoxBG").localPosition = new Vector3(0f, -0.7f, 1f);
			gameObject4.transform.Find("BoxBG").localScale = new Vector3(6.5f, 7.75f, 1f);
			gameObject4.transform.Find("Btn_Resume").localPosition = new Vector3(2.5f, 1.5f, -0.1f);
			gameObject4.transform.Find("Btn_Quit/GUITextLabel").GetComponent<TextMesh>().text = (gameObject4.transform.Find("Btn_Quit/GUITextLabel/GUITextLabelShadow").GetComponent<TextMesh>().text = "DISCONNECT");
			PlayMakerFSM component = GameObject.Find("Systems").transform.Find("OptionsMenu/Menu/Btn_ConfirmQuit/Button").GetComponent<PlayMakerFSM>();
			component.Initialize();
			FsmState fsmState = component.FsmStates.First((FsmState s) => s.Name == "State 3");
			FsmStateAction[] array = new PM_Hook[]
			{
				new PM_Hook(new Action(CoreManager.Disconnect), false)
			};
			fsmState.Actions = array;
			Console.Log("Enter charcustom init 3", false);
			CharacterCustomization characterCustomization = gameObject4.AddComponent<CharacterCustomization>();
			characterCustomization.guiCharacter = gameObject3;
			characterCustomization.playerModel = gameObject2;
			Console.Log("Registered Skinchange events", false);
			new GameEvent<CharacterCustomization>("InitSkinSync", new Action<ulong, GameEventReader>(characterCustomization.OnInitialSkinSync), GameScene.GAME);
			new GameEvent<CharacterCustomization>("SkinChange", new Action<ulong, GameEventReader>(characterCustomization.OnSkinChange), GameScene.GAME);
			Console.Log("Registered Skinchange events done", false);
			characterCustomization._Awake();
			return characterCustomization;
		}

		private void _Awake()
		{
			this.hud = GameObject.Find("Systems").transform.Find("OptionsMenu/Menu");
			this.optionsmenu = GameObject.Find("Systems").transform.Find("OptionsMenu").gameObject;
			this.settingTab = Object.Instantiate<GameObject>(this.hud.transform.Find("Btn_Graphics").gameObject);
			this.settingTab.transform.SetParent(this.hud);
			this.settingTab.name = "Btn_PlayerCustomization";
			this.settingTab.transform.localPosition = new Vector3(2.5f, 0f, -0.1f);
			Object.Destroy(this.settingTab.transform.Find("Button").GetComponent<PlayMakerFSM>());
			this.guiTextLabel = this.settingTab.transform.Find("GUITextLabel");
			this.guiTextLabel.GetComponent<TextMesh>().text = "SKIN CONFIG";
			this.guiTextLabel.GetChild(0).GetComponent<TextMesh>().text = "SKIN CONFIG";
			this.buttonCollider = this.settingTab.transform.Find("Button").GetComponent<Collider>();
			this.othermenus = new GameObject[]
			{
				this.optionsmenu.transform.Find("DEBUG").gameObject,
				this.optionsmenu.transform.Find("Graphics").gameObject,
				this.optionsmenu.transform.Find("VehicleControls").gameObject,
				this.optionsmenu.transform.Find("PlayerControls").gameObject
			};
			Transform transform = this.guiCharacter.transform.Find("Page/Buttons");
			Transform transform2 = this.guiCharacter.transform.Find("Page/FieldString");
			SkinnedMeshRenderer component = this.playerModel.transform.Find("char/bodymesh").GetComponent<SkinnedMeshRenderer>();
			component.materials[0] = new Material(component.materials[0]);
			component.materials[1] = new Material(component.materials[1]);
			component.materials[2] = new Material(component.materials[2]);
			MeshRenderer component2 = this.playerModel.transform.Find("char/skeleton/thig_left/knee_left/ankle_left/shoeLeft").GetComponent<MeshRenderer>();
			MeshRenderer component3 = this.playerModel.transform.Find("char/skeleton/thig_right/knee_right/ankle_right/shoeRight").GetComponent<MeshRenderer>();
			Material material = new Material(component2.material);
			component2.material = material;
			component3.material = material;
			this.characterCustomizationItems = new CharacterCustomizationItem[]
			{
				CharacterCustomizationItem.Init(0, new Action(this.SaveSkin), transform.GetChild(0), transform2.GetChild(0), null, null, null, this.playerModel.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/glasses"), null),
				CharacterCustomizationItem.Init(1, new Action(this.SaveSkin), transform.GetChild(1), transform2.GetChild(1), null, null, null, this.playerModel.transform.Find("char/skeleton/pelvis/RotationBendPivot/spine_middle/spine_upper/headPivot/HeadRotationPivot/head/head_end"), null),
				CharacterCustomizationItem.Init(2, new Action(this.SaveSkin), transform.GetChild(2), transform2.GetChild(2), null, CharacterCustomization.faces, component.materials[2], null, null),
				CharacterCustomizationItem.Init(3, new Action(this.SaveSkin), transform.GetChild(3), transform2.GetChild(3), CharacterCustomization.NAMES_SHIRTS, CharacterCustomization.shirts, component.materials[0], null, null),
				CharacterCustomizationItem.Init(4, new Action(this.SaveSkin), transform.GetChild(4), transform2.GetChild(4), CharacterCustomization.NAMES_PANTS, CharacterCustomization.pants, component.materials[1], null, null),
				CharacterCustomizationItem.Init(5, new Action(this.SaveSkin), transform.GetChild(5), transform2.GetChild(5), null, CharacterCustomization.shoes, material, component2.transform, component3.transform)
			};
			this.LoadSkin();
		}

		private void LoadSkin()
		{
			if (File.Exists(this.skinPresetFilePath_old))
			{
				File.Copy(this.skinPresetFilePath_old, this.skinPresetFilePath, true);
				File.Delete(this.skinPresetFilePath_old);
			}
			int[] array = new int[] { 0, 0, 6, 12, 4, 3 };
			if (File.Exists(this.skinPresetFilePath))
			{
				byte[] array2 = File.ReadAllBytes(this.skinPresetFilePath);
				if (array2.Length == 8)
				{
					long num = BitConverter.ToInt64(array2, 0);
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = (int)((num >> i * 6) & 63L);
					}
				}
			}
			for (int j = 0; j < array.Length; j++)
			{
				this.characterCustomizationItems[j].SetOption(array[j], false);
			}
			this.InitialSkinSync(array, 0UL);
		}

		private void SaveSkin()
		{
			long num = 0L;
			for (int i = 0; i < this.characterCustomizationItems.Length; i++)
			{
				num |= (long)((long)this.characterCustomizationItems[i].SelectedIndex << i * 6);
			}
			File.WriteAllBytes(this.skinPresetFilePath, BitConverter.GetBytes(num));
		}

		public void InitialSkinSync(int[] skinPreset, ulong sendTo = 0UL)
		{
			if (skinPreset == null)
			{
				skinPreset = new int[this.characterCustomizationItems.Length];
				for (int i = 0; i < skinPreset.Length; i++)
				{
					skinPreset[i] = this.characterCustomizationItems[i].SelectedIndex;
				}
			}
			using (GameEventWriter gameEventWriter = GameEvent.EmptyWriter(""))
			{
				gameEventWriter.Write(skinPreset[0]);
				gameEventWriter.Write(skinPreset[1]);
				gameEventWriter.Write(skinPreset[2]);
				gameEventWriter.Write(skinPreset[3]);
				gameEventWriter.Write(skinPreset[4]);
				gameEventWriter.Write(skinPreset[5]);
				if (sendTo == 0UL)
				{
					GameEvent<CharacterCustomization>.Send("InitSkinSync", gameEventWriter, 0UL, true);
				}
				else
				{
					GameEvent<CharacterCustomization>.Send("InitSkinSync", gameEventWriter, sendTo, true);
				}
			}
		}

		private void OnInitialSkinSync(ulong player, GameEventReader p)
		{
			Player player2 = CoreManager.Players[player];
			if (player2 == null)
			{
				Console.LogError(string.Format("CharacterCustomization.OnInitSkinSync: NetPlayer with ID {0} is null", player), false);
				return;
			}
			player2.OnInitialSkinSync(new int[]
			{
				p.ReadInt32(),
				p.ReadInt32(),
				p.ReadInt32(),
				p.ReadInt32(),
				p.ReadInt32(),
				p.ReadInt32()
			});
		}

		private void OnSkinChange(ulong player, GameEventReader p)
		{
			Player player2 = CoreManager.Players[player];
			if (player2 == null)
			{
				Console.LogError(string.Format("CharacterCustomization.OnSkinChange: NetPlayer with ID {0} is null", player), false);
				return;
			}
			player2.OnSkinChange(p.ReadInt32(), p.ReadInt32());
		}

		private void Update()
		{
			bool flag = Raycaster.Raycast(this.buttonCollider, 135f, 16384);
			if (flag != this.mouseOver)
			{
				this.guiTextLabel.localScale = Vector3.one * (flag ? 0.95f : 1f);
				this.mouseOver = flag;
			}
			if (this.mouseOver && !this.uiVisible && Input.GetMouseButton(0))
			{
				this.guiCharacter.SetActive(true);
				this.playerModel.SetActive(true);
				for (int i = 0; i < this.othermenus.Length; i++)
				{
					this.othermenus[i].SetActive(false);
				}
				this.uiVisible = true;
			}
			if (this.uiVisible)
			{
				if (this.othermenus.Any((GameObject go) => go.activeSelf))
				{
					this.guiCharacter.SetActive(false);
					this.playerModel.SetActive(false);
					this.uiVisible = false;
				}
			}
		}

		public static readonly string[] NAMES_PANTS = new string[]
		{
			"Blue", "Checked black", "Stripped blue", "Beige", "Mirrored shorts", "Blue jeans", "BP pants", "Black", "Hayosiko shorts", "Blue with belt",
			"Teal", "Checked brown", "Checked blue", "Checked green", "Checked orange", "Checked red", "Green shorts", "Red shorts", "Stripped black", "Cop jeans",
			"White shorts"
		};

		public static readonly string[] NAMES_SHIRTS = new string[]
		{
			"None", "Teimo", "Gifu", "Yellow", "Grey", "Teal", "Van craze", "White shirt", "Black", "Blue amis",
			"Arvo", "Office", "Yellow FUstreet", "Black shirt", "Green shirt", "Suski", "White shirt 2", "BP top", "White shirt 3", "Blue shirt",
			"intcost", "Black shirt 2", "intcost 2", "Kekmet", "Balfield", "White FUstreet", "RCO", "CORRIS", "Voittous", "Slate blue",
			"Teal shirt", "Satsuma shirt", "Yellow amis", "CORRIS 2", "Polaventris", "Hayosiko", "Hayosiko 2", "CORRIS 3", "Suvi sprint 1992", "Talvi sprint 1993",
			"Office 2", "Blue shirt", "Mafia shirt", "Dassler shirt", "Black shirt 3", "Cop", "Cop 2"
		};

		public static Texture2D[] faces;

		public static Texture2D[] shirts;

		public static Texture2D[] pants;

		public static Texture2D[] shoes;

		private const int numFaces = 17;

		private const int numPants = 21;

		private const int numShirts = 47;

		private const int numShoes = 7;

		public GameObject settingTab;

		public GameObject guiCharacter;

		public GameObject playerModel;

		private bool mouseOver;

		private bool uiVisible;

		public Transform hud;

		public const string initSyncEvent = "InitSkinSync";

		public const string skinChangeEvent = "SkinChange";

		private readonly string skinPresetFilePath = Path.Combine(Application.persistentDataPath, "WreckMP_playerskin");

		private readonly string skinPresetFilePath_old = Path.Combine(Application.persistentDataPath, "BeerMP_playerskin");

		private GameObject optionsmenu;

		private Transform guiTextLabel;

		private Collider buttonCollider;

		private GameObject[] othermenus;

		private CharacterCustomizationItem[] characterCustomizationItems;
	}
}
