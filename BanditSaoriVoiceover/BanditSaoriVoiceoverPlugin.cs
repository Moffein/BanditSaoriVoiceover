using BanditSaoriVoiceover.Components;
using BanditSaoriVoiceoverPlugin.Modules;
using BaseVoiceoverLib;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace BanditSaoriVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Bread.BanditSaoriSkin", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Moffein.BaseVoiceoverLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Schale.BanditSaoriVoiceover", "BanditSaoriVoiceover", "1.0.0")]
    public class BanditSaoriVoiceoverPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<KeyboardShortcut> buttonVanitas, buttonVanitasFull, buttonMuda, buttonHurt, buttonOmoshiroi, buttonMunashii, buttonYes, buttonThanks, buttonTitle, buttonIntro, buttonFormation;

        public static ConfigEntry<bool> enableVoicelines;
        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;
        public static SurvivorDef survivorDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/Base/Bandit2/Bandit2.asset").WaitForCompletion();

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            RoR2.RoR2Application.onLoad += OnLoad;
            new Content().Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BanditSaoriVoiceover.banditsaorivoiceoverbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the BanditSaori Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;

            buttonVanitas = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Vanitas Vanitatum"), KeyboardShortcut.Empty);
            buttonVanitasFull = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Vanitas Vanitatum, et Omnia Vanitas"), KeyboardShortcut.Empty);
            buttonMuda = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Muda da"), KeyboardShortcut.Empty);
            buttonOmoshiroi = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Omoshiroi"), KeyboardShortcut.Empty);
            buttonHurt = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Hurt"), KeyboardShortcut.Empty);
            buttonMunashii = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Munashii"), KeyboardShortcut.Empty);
            buttonYes = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Yes"), KeyboardShortcut.Empty);
            buttonThanks = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Thanks"), KeyboardShortcut.Empty);
            buttonTitle = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Blue Archive"), KeyboardShortcut.Empty);
            buttonIntro = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Introduction"), KeyboardShortcut.Empty);
            buttonFormation = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Formation Select"), KeyboardShortcut.Empty);

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }
        }

        private void EnableVoicelines_SettingChanged(object sender, EventArgs e)
        {
            RefreshNSE();
        }

        private void Start()
        {
            SoundBanks.Init();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));

            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonTitle));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonIntro));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonFormation));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonVanitas));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonVanitasFull));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonMunashii));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonMuda));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonOmoshiroi));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonYes));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonThanks));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonHurt));

            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("texIconSaori"));
        }

        private void OnLoad()
        {
            SkinDef saoriSkin = null;
            SkinDef[] skins = SkinCatalog.FindSkinsForBody(BodyCatalog.FindBodyIndex("Bandit2Body"));
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "SayoriSkin")
                {
                    saoriSkin = skinDef;
                    break;
                }
            }

            if (!saoriSkin)
            {
                Debug.LogError("BanditSaoriVoiceover: Bandit Saori SkinDef not found. Voicelines will not work!");
            }
            else
            {
                VoiceoverInfo voiceoverInfo = new VoiceoverInfo(typeof(BanditSaoriVoiceoverComponent), saoriSkin, "Bandit2Body");
                voiceoverInfo.selectActions += SaoriSelect;
            }

            RefreshNSE();
        }

        private void SaoriSelect(GameObject mannequinObject)
        {
            if (!enableVoicelines.Value) return;
            
            bool played = false;
            if (!playedSeasonalVoiceline)
            {
                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                {
                    Util.PlaySound("Play_BanditSaori_Lobby_Newyear", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 9 && System.DateTime.Today.Day == 3)
                {
                    Util.PlaySound("Play_BanditSaori_Lobby_bday", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                {
                    Util.PlaySound("Play_BanditSaori_Lobby_Halloween", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                {
                    Util.PlaySound("Play_BanditSaori_Lobby_xmas", mannequinObject);
                    played = true;
                }

                if (played)
                {
                    playedSeasonalVoiceline = true;
                }
            }
            if (!played)
            {
                if (Util.CheckRoll(5f))
                {
                    Util.PlaySound("Play_BanditSaori_TitleDrop", mannequinObject);
                }
                else
                {
                    Util.PlaySound("Play_BanditSaori_Lobby", mannequinObject);
                }
            }
        }

        private void InitNSE()
        {
            BanditSaoriVoiceoverComponent.nseShout = RegisterNSE("Play_BanditSaori_Shout");
            BanditSaoriVoiceoverComponent.nseStealth = RegisterNSE("Play_BanditSaori_Stealth");
            BanditSaoriVoiceoverComponent.nseBlock = RegisterNSE("Play_BanditSaori_Block");
            BanditSaoriVoiceoverComponent.nseEx = RegisterNSE("Play_BanditSaori_Ex");
            BanditSaoriVoiceoverComponent.nseExLevel = RegisterNSE("Play_BanditSaori_ExLevel");
            BanditSaoriVoiceoverComponent.nseVanitas = RegisterNSE("Play_BanditSaori_Vanitas");
            BanditSaoriVoiceoverComponent.nseVanitasFull = RegisterNSE("Play_BanditSaori_VanitasFull");
            BanditSaoriVoiceoverComponent.nseMuda = RegisterNSE("Play_BanditSaori_Muda");
            BanditSaoriVoiceoverComponent.nseHurt = RegisterNSE("Play_BanditSaori_TakeDamage");
            BanditSaoriVoiceoverComponent.nseOmoshiroi = RegisterNSE("Play_BanditSaori_Omoshiroi");
            BanditSaoriVoiceoverComponent.nseMunashii = RegisterNSE("Play_BanditSaori_Munashii");
            BanditSaoriVoiceoverComponent.nseYes = RegisterNSE("Play_BanditSaori_Yes");
            BanditSaoriVoiceoverComponent.nseThanks = RegisterNSE("Play_BanditSaori_Thanks");
            BanditSaoriVoiceoverComponent.nseTitle = RegisterNSE("Play_BanditSaori_TitleDrop");
            BanditSaoriVoiceoverComponent.nseIntro = RegisterNSE("Play_BanditSaori_Intro");
            BanditSaoriVoiceoverComponent.nseFormation = RegisterNSE("Play_BanditSaori_Formation_Select");
        }

        private NetworkSoundEventDef RegisterNSE(string eventName)
        {
            NetworkSoundEventDef nse = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            nse.eventName = eventName;
            Content.networkSoundEventDefs.Add(nse);
            nseList.Add(new NSEInfo(nse));
            return nse;
        }

        public void RefreshNSE()
        {
            foreach (NSEInfo nse in nseList)
            {
                nse.ValidateParams();
            }
        }

        public static List<NSEInfo> nseList = new List<NSEInfo>();
        public class NSEInfo
        {
            public NetworkSoundEventDef nse;
            public uint akId = 0u;
            public string eventName = string.Empty;

            public NSEInfo(NetworkSoundEventDef source)
            {
                this.nse = source;
                this.akId = source.akId;
                this.eventName = source.eventName;
            }

            private void DisableSound()
            {
                nse.akId = 0u;
                nse.eventName = string.Empty;
            }

            private void EnableSound()
            {
                nse.akId = this.akId;
                nse.eventName = this.eventName;
            }

            public void ValidateParams()
            {
                if (this.akId == 0u) this.akId = nse.akId;
                if (this.eventName == string.Empty) this.eventName = nse.eventName;

                if (!enableVoicelines.Value)
                {
                    DisableSound();
                }
                else
                {
                    EnableSound();
                }
            }
        }
    }
}
