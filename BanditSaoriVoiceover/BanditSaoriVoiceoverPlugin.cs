﻿using BanditSaoriVoiceover.Components;
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
        public static ConfigEntry<KeyboardShortcut> buttonVanitas, buttonVanitasFull, buttonMuda, buttonHurt, buttonOmoshiroi, buttonMunashii, buttonYes, buttonThanks;

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

            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonVanitas));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonVanitasFull));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonMuda));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonHurt));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonOmoshiroi));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonMunashii));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonYes));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonThanks));

            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("texIconSaori"));
        }

        private void OnLoad()
        {
            bool foundSkin = false;

            SkinDef[] skins = SkinCatalog.FindSkinsForBody(BodyCatalog.FindBodyIndex("Bandit2Body"));
            foreach (SkinDef skinDef in skins)
            {
                if (skinDef.name == "SayoriSkin")
                {
                    foundSkin = true;
                    BanditSaoriVoiceoverComponent.requiredSkinDefs.Add(skinDef);
                    break;
                }
            }

            if (!foundSkin)
            {
                Debug.LogError("BanditSaoriVoiceover: Bandit Saori SkinDef not found. Voicelines will not work!");
            }
            else if (survivorDef)
            {
                On.RoR2.CharacterBody.Start += AttachVoiceoverComponent;
                On.RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance += (orig, self) =>
                {
                    orig(self);
                    if (self.currentSurvivorDef == survivorDef)
                    {
                        //Loadout isn't loaded first time this is called, so we need to manually get it.
                        //Probably not the most elegant way to resolve this.
                        if (self.loadoutDirty)
                        {
                            if (self.networkUser)
                            {
                                self.networkUser.networkLoadout.CopyLoadout(self.currentLoadout);
                            }
                        }

                        //Check SkinDef
                        BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(self.currentSurvivorDef.survivorIndex);
                        int skinIndex = (int)self.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
                        SkinDef safe = HG.ArrayUtils.GetSafe<SkinDef>(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
                        if (true && enableVoicelines.Value)// && BanditSaoriVoiceoverComponent.requiredSkinDefs.Contains(safe)
                        {
                            bool played = false;
                            if (!playedSeasonalVoiceline)
                            {
                                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                                {
                                    Util.PlaySound("Play_BanditSaori_Lobby_Newyear", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 9 && System.DateTime.Today.Day == 3)
                                {
                                    Util.PlaySound("Play_BanditSaori_Lobby_bday", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                                {
                                    Util.PlaySound("Play_BanditSaori_Lobby_Halloween", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                                {
                                    Util.PlaySound("Play_BanditSaori_Lobby_xmas", self.mannequinInstanceTransform.gameObject);
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
                                    Util.PlaySound("Play_BanditSaori_TitleDrop", self.mannequinInstanceTransform.gameObject);
                                }
                                else
                                {
                                    Util.PlaySound("Play_BanditSaori_Lobby", self.mannequinInstanceTransform.gameObject);
                                }
                            }
                        }
                    }
                };
            }

            RefreshNSE();
        }

        private void AttachVoiceoverComponent(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self)
            {
                if (self.bodyIndex == BodyCatalog.FindBodyIndex("Bandit2Body") && (BanditSaoriVoiceoverComponent.requiredSkinDefs.Contains(SkinCatalog.GetBodySkinDef(self.bodyIndex, (int)self.skinIndex))))
                {
                    BaseVoiceoverComponent existingVoiceoverComponent = self.GetComponent<BaseVoiceoverComponent>();
                    if (!existingVoiceoverComponent) self.gameObject.AddComponent<BanditSaoriVoiceoverComponent>();
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
