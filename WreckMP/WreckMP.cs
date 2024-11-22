using System;
using Discord;
using UnityEngine;
using WreckMP.Properties;

namespace WreckMP
{
    internal class WreckMP : MonoBehaviour
    {
        public Discord discord { get; internal set; }

        private static long ApplicationID { get; } = long.Parse(Resources.clientID);

        private void Awake()
        {
            // Tentativa de inicializar a Steam (opcional)
            try
            {
                SteamAPI.Init();
                int appBuildId = SteamApps.GetAppBuildId();
                Debug.Log($"Steam initialized with AppBuildId: {appBuildId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Steam is not available. Running without Steam integration.");
            }

            WreckMP.instance = this;
            Object.DontDestroyOnLoad(base.gameObject);
            this.netman = base.gameObject.AddComponent<CoreManager>();
            Environment.SetEnvironmentVariable("WreckMP-Present", "https://open.spotify.com/artist/0LMqNSBRZMB9CojWaE8eCB");
            
            string text = "~g~a~m~e~v~e~r~";
            string text2 = "00001"; // ID fictício para versão do jogo
            for (int i = 0; i < 10 - text2.Length; i++)
            {
                text += "0";
            }
            text = text + text2 + "\n~w~m~p~v~e~r~";
            string text3 = 520.ToString();
            for (int j = 0; j < 10 - text3.Length; j++)
            {
                text += "0";
            }
            text += text3;
            Console.WriteLine(text);

            // Inicialização do Discord
            try
            {
                this.discord = new Discord(WreckMP.ApplicationID, 1UL);
                if (this.discord != null)
                {
                    WreckMP.activityManager = this.discord.GetActivityManager();
                }
            }
            catch (Exception ex)
            {
                this.discord = null;
                Debug.LogError(ex.Message);
            }
        }

        private void OnLevelWasLoaded(int levelId)
        {
            Object.DontDestroyOnLoad(base.gameObject);
        }

        private void Update()
        {
            if (this.discord != null)
            {
                this.discord.RunCallbacks();
            }

            // Removido: SteamAPI.RunCallbacks();
            
            if (Application.loadedLevelName == "MainMenu" && !this.init)
            {
                Debug.Log("[WreckMP Init]");
                Console.Init();
                this.netman.Init();
                this.init = true;
                WreckMP.ResetActivity();
            }
            
            if (Input.GetKey(48)) // Tecla '0'
            {
                Console.Log(Camera.main.transform.GetGameobjectHashString(), true);
            }
        }

        public static void ResetActivity()
        {
            if (WreckMP.instance.discord == null)
            {
                return;
            }
            WreckMP.activity = new Activity
            {
                State = "Idling",
                Timestamps = new ActivityTimestamps
                {
                    Start = DateTime.Now.ToUnixTimestamp()
                }
            };
            WreckMP.UpdateActivity(WreckMP.activity);
        }

        public static void UpdateActivity(Activity activity)
        {
            if (WreckMP.instance.discord == null)
            {
                return;
            }
            activity.ApplicationId = WreckMP.ApplicationID;
            activity.Assets = new ActivityAssets
            {
                LargeImage = "WreckMP_logo",
                LargeText = "No alcohol is no solution."
            };
            WreckMP.activityManager.UpdateActivity(activity, delegate(Result res)
            {
                if (res == Result.Ok)
                {
                    Console.Log("Discord: Status Updated", false);
                    return;
                }
                Console.LogError(string.Format("Discord: Status Update failed! {0}", res), false);
            });
        }

        private void OnApplicationQuit()
        {
            Environment.SetEnvironmentVariable("WreckMP-Present", null);
            
            // Removido: SteamAPI.Shutdown();
        }

        public static WreckMP instance;

        internal CoreManager netman;

        private bool init;

        public static bool debug;

        private static Activity activity;

        private static ActivityAssets activityAssets;

        private static ActivityManager activityManager;

        private const byte _ver1 = 0;

        private const byte _ver2 = 2;

        private const byte _ver3 = 8;

        public static readonly string version = string.Format("v{0}.{1}.{2}", 0, 2, 8);
    }
}
