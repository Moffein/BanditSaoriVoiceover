using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace BanditSaoriVoiceover.Components
{
    public class BanditSaoriVoiceoverComponent : BaseVoiceoverComponent
    {
        public static List<SkinDef> requiredSkinDefs = new List<SkinDef>();
        public static ItemIndex ScepterIndex;

        public static NetworkSoundEventDef nseShout, nseStealth, nseBlock, nseExLevel, nseEx;

        private float levelCooldown = 0f;
        private float blockedCooldown = 0f;
        private float lowHealthCooldown = 0f;
        private float specialCooldown = 0f;
        //private float utilityCooldown = 0f;

        private bool acquiredScepter = false;

        protected override void Awake()
        {
            spawnVoicelineDelay = 3f;
            if (Run.instance && Run.instance.stageClearCount == 0)
            {
                spawnVoicelineDelay = 6.5f;
            }
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            if (inventory && inventory.GetItemCount(ScepterIndex) > 0) acquiredScepter = true;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (specialCooldown > 0f) specialCooldown -= Time.fixedDeltaTime;
            //if (utilityCooldown > 0f) utilityCooldown -= Time.fixedDeltaTime;
            if (levelCooldown > 0f) levelCooldown -= Time.fixedDeltaTime;
            if (blockedCooldown > 0f) blockedCooldown -= Time.fixedDeltaTime;
            if (lowHealthCooldown > 0f) lowHealthCooldown -= Time.fixedDeltaTime;
        }

        public override void PlayDamageBlockedServer()
        {
            if (!NetworkServer.active || blockedCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseBlock, 0.55f, false);
            if (played) blockedCooldown = 30f;
        }

        public override void PlayDeath()
        {
            TryPlaySound("Play_BanditSaori_Defeat", 4f, true);
        }

        public override void PlayHurt(float percentHPLost)
        {
            if (percentHPLost >= 0.1f)
            {
                TryPlaySound("Play_BanditSaori_TakeDamage", 0f, false);
            }
        }

        public override void PlayJump() { }

        public override void PlayLevelUp()
        {
            if (levelCooldown > 0f) return;
            bool played;
            if (Util.CheckRoll(50f))
            {
                played = TryPlaySound("Play_BanditSaori_LevelUp_Short", 1.35f, false);
            }
            else
            {
                played = TryPlaySound("Play_BanditSaori_LevelUp_Long", 6.6f, false);
            }
            if (played) levelCooldown = 60f;
        }

        public override void PlayLowHealth()
        {
            if (lowHealthCooldown > 0f) return;
            bool playedSound;

            if (Util.CheckRoll(50f))
            {
                playedSound = TryPlaySound("Play_BanditSaori_LowHealth", 0f, false);
            }
            else if (Util.CheckRoll(90f))
            {
                playedSound = TryPlaySound("Play_BanditSaori_Lobby5", 3.8f, false);
            }
            else
            {
                if (Util.CheckRoll(50f))
                {
                    playedSound = TryPlaySound("Play_BanditSaori_Memorial2", 7.35f, false);
                }
                else
                {
                    playedSound = TryPlaySound("Play_BanditSaori_Memorial4", 12.8f, false);
                }
            }
                
            if (playedSound) lowHealthCooldown = 60f;
        }

        public override void PlayPrimaryAuthority() { }

        public override void PlaySecondaryAuthority()
        {
            TryPlayNetworkSound(nseShout, 0f, false);
        }

        public override void PlaySpawn()
        {
            TryPlaySound("Play_BanditSaori_Spawn", 2.4f, true);
        }

        public override void PlaySpecialAuthority()
        {
            if (specialCooldown > 0f) return;
            if (TryPlayNetworkSound(nseExLevel, 1.9f, false)) specialCooldown = 8f;
        }

        public override void PlayTeleporterFinish()
        {
            TryPlaySound("Play_BanditSaori_Victory", 2.7f, false);
        }

        public override void PlayTeleporterStart()
        {
            TryPlaySound("Play_BanditSaori_Ex", 4.4f, true);
        }

        //Seemed unfitting
        /*public override void PlayUtilityAuthority()
        {
            if (utilityCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseStealth, 1.9f, false);
            if (played) utilityCooldown = 30f;
        }*/
        public override void PlayUtilityAuthority() { }

        public override void PlayVictory()
        {
            //TryPlaySound("Play_BanditSaori_VanitasFull", 2.9f, true);
            TryPlaySound("Play_BanditSaori_Lobby3", 4.7f, true);
        }

        protected override void Inventory_onItemAddedClient(ItemIndex itemIndex)
        {
            base.Inventory_onItemAddedClient(itemIndex);
            if (ScepterIndex != ItemIndex.None && itemIndex == ScepterIndex)
            {
                PlayAcquireScepter();
            }
            else
            {
                ItemDef id = ItemCatalog.GetItemDef(itemIndex);
                if (id == RoR2Content.Items.Squid || id == RoR2Content.Items.Plant || id == RoR2Content.Items.SlowOnHit)
                {
                    PlayBadItem();
                }
                else if (id && id.deprecatedTier == ItemTier.Tier3)
                {
                    PlayAcquireLegendary();
                }
            }
        }

        public void PlayAcquireScepter()
        {
            if (acquiredScepter) return;
            TryPlaySound("Play_BanditSaori_AcquireScepter", 22.75f, true);
            acquiredScepter = true;
        }
        public void PlayBadItem()
        {
            if (Util.CheckRoll(50f))
            {
                TryPlaySound("Play_BanditSaori_Omoshiroi", 0.75f, false);
            }
            else
            {
                if (Util.CheckRoll(50f))
                {
                    TryPlaySound("Play_BanditSaori_Vanitas", 1.2f, false);
                }
                else
                {
                    TryPlaySound("Play_BanditSaori_VanitasFull", 2.9f, false);
                }
            }
        }

        public void PlayAcquireLegendary()
        {
            if (Util.CheckRoll(75f))
            {
                TryPlaySound("Play_BanditSaori_Relationship", 3.7f, false);
            }
            else
            {
                TryPlaySound("Play_BanditSaori_Relationship_Long", 12.25f, false);
            }
        }
    }
}
