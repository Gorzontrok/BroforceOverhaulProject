using System;
using RocketLib;
using UnityEngine;
using HarmonyLib;
using TheGeneralsTraining.Components;

namespace BroffyTrained
{
    [HarmonyPatch(typeof(Broffy))]
    public static class BroffyPatches
    {
        public static TrainedSettings TSettings
        {
            get => Main.settings.mod;
        }
        public static VanillaSettings VSettings
        {
            get => Main.settings.vanilla;
        }

        public static AudioClip[] kickClips;

        [HarmonyPatch(typeof(BroBase), "Awake")]
        [HarmonyPrefix]
        private static void BroffyAwake(BroBase __instance)
        {
            if (Mod.CantUsePatch)
                return;

            if (__instance is Broffy)
            {
                __instance.GetOrAddComponent<TrainedBuffy>();
                //Store Nebro punch sound, to replace Broffy kick sound
                if (kickClips == null)
                    kickClips = HeroController.GetHeroPrefab(HeroType.Nebro).soundHolder.special2Sounds;
            }
        }

        [HarmonyPatch(typeof(BroBase), "CancelMelee")]
        [HarmonyPostfix]
        private static void StopFlyingKick(BroBase __instance)
        {
            if (Mod.CanUsePatch)
            {
                var comp = __instance.GetComponent<TrainedBuffy>();
                if (comp != null)
                {
                    comp.StopFlyingKick();
                }
            }
        }

        [HarmonyPatch("PerformKnifeMeleeAttack")]
        [HarmonyPrefix]
        private static bool NewPerformKnifeMeleeAttack(Broffy __instance, bool shouldTryHitTerrain, bool playMissSound)
        {
            if (Mod.CantUsePatch)
                return true;

            // From method 'Broffy.PerformKnifeMeleeAttack(bool,bool)'

            Traverse t = __instance.GetTraverse();
            Sound sound = t.GetFieldValue<Sound>("sound");

            DamageType damageType = t.GetFieldValue<bool>("dashingMelee") ? DamageType.Melee : DamageType.SilencedBullet;
            if (Mod.IsOnAnimal(__instance) || __instance.actionState == ActionState.ClimbingLadder)
                damageType = DamageType.Knifed;
            Map.DamageDoodads(3, DamageType.Knifed, __instance.X + (float)(__instance.Direction * 4), __instance.Y, 0f, 0f, 6f, __instance.playerNum, out bool flag, __instance);
            t.CallMethod("KickDoors", 24f);

            bool knock = false;
            float xI = 0f;
            float yI = 0f;
            int damage = 4;
            if (damageType == DamageType.Melee)
            {
                xI = (__instance.Direction * TSettings.kickForce.x);
                yI = TSettings.kickForce.y;
                damage = TSettings.kickDamage;
            }
            else if (damageType == DamageType.Knifed)
            {
                knock = true;
                xI = (__instance.Direction * VSettings.knifeForce.x);
                yI = VSettings.knifeForce.y;
                damage = 2;
            }

            TrainedBuffy trained = __instance.GetComponent<TrainedBuffy>();
            if (Map.HitClosestUnit(__instance, __instance.playerNum,
                damage,
                damageType,
                14f, // xRange
                24f, // yRange
                __instance.X + __instance.transform.localScale.x * 8f, // x
                __instance.Y + 8f, //y
                xI, yI,
                knock,
                false, // canGib
                __instance.IsMine,
                false,
                (damageType == DamageType.Knifed || (trained != null && trained.doingFlyingKick) || __instance.GetBool("dashingMelee")) // hitDead
                ))
            {
                // Change the hit sound if Broffy is kicking an opponent
                if (damageType == DamageType.Melee || (trained != null && trained.doingFlyingKick))
                    sound.PlaySoundEffectAt(kickClips, 1f, __instance.transform.position);
                else
                    sound.PlaySoundEffectAt(__instance.soundHolder.meleeHitSound, 1f, __instance.transform.position);
                t.SetFieldValue("meleeHasHit", true);
            }
            else if (playMissSound)
            {
                sound.PlaySoundEffectAt(__instance.soundHolder.missSounds, 0.3f, __instance.transform.position);
            }
            t.SetFieldValue<Unit>("meleeChosenUnit", null);
            if (shouldTryHitTerrain && t.Method("TryMeleeTerrain", 0, 2).GetValue<bool>())
            {
                t.SetFieldValue("meleeHasHit", true);
            }
            return false;
        }

        [HarmonyPatch("AnimateKnifeMelee")]
        [HarmonyPrefix]
        private static bool AnimateMelee(Broffy __instance)
        {
            if (Mod.CantUsePatch)
                return true;

            // From method 'Broffy.AnimateKnifeMelee()'

            __instance.CallMethod("AnimateMeleeCommon");
            bool dashingMelee = __instance.GetBool("dashingMelee");
            int frameCol = (int)TSettings.stabAnimationPosition.x + Mathf.Clamp(__instance.frame, 0, TSettings.stabAnimationMaxFrame);
            int frameRow = (int)TSettings.stabAnimationPosition.y;

            var comp = __instance.GetComponent<TrainedBuffy>();

            if (comp.doingFlyingKick)
            {
                frameCol = TSettings.flyingKick.flyingKickCol + Mathf.Clamp(__instance.frame, 0, TSettings.flyingKick.flyingKickMaxFrame);
                frameRow = TSettings.flyingKick.flyingKickRow;
            }
            else if ((__instance.GetBool("standingMelee") && Mod.IsOnAnimal(__instance)) || __instance.actionState == ActionState.ClimbingLadder)
            {
                frameRow = 1; // knife row
            }
            else if (__instance.GetBool("jumpingMelee"))
            {
                // If jumping and melee, do flying kick
                frameCol = TSettings.flyingKick.flyingKickCol + Mathf.Clamp(__instance.frame, 0, TSettings.flyingKick.flyingKickMaxFrame);
                frameRow = TSettings.flyingKick.flyingKickRow;
                comp.StartFlyingKick();
            }
            // dashingMelee is set to true if the player goes left or right
            else if (dashingMelee)
            {
                // If player is running do Flying Kick, else normal kick
                if (__instance.dashing && !comp.doingFlyingKick && __instance.actionState != ActionState.ClimbingLadder)
                {
                    frameCol = TSettings.flyingKick.flyingKickCol + Mathf.Clamp(__instance.frame, 0, TSettings.flyingKick.flyingKickMaxFrame);
                    frameRow = TSettings.flyingKick.flyingKickRow;
                    comp.StartFlyingKick();
                }
                else
                {
                    frameCol = (int)TSettings.kickAnimationPosition.x + Mathf.Clamp(__instance.frame, 0, TSettings.kickAnimationMaxFrame);
                    frameRow = (int)TSettings.kickAnimationPosition.y;
                    if (__instance.frame == 4)
                    {
                        __instance.counter -= 0.0334f;
                    }
                    else if (__instance.frame == 5)
                    {
                        __instance.counter -= 0.0334f;
                    }
                }
            }
            __instance.SetSpriteLowerLeftPixel(frameCol, frameRow);

            int hitFrame = dashingMelee ? 5 : 3;
            if (__instance.frame == hitFrame || (comp != null && comp.FlyingKickDoesDamage()))
            {
                __instance.counter -= 0.066f;
                __instance.CallMethod("PerformKnifeMeleeAttack", true, true);
            }
            else if (__instance.frame > hitFrame && !__instance.GetBool("meleeHasHit"))
            {
                __instance.CallMethod("PerformKnifeMeleeAttack", false, false);
            }
            int lastFrame = dashingMelee ? 9 : 6;
            TrainedBuffy trained = __instance.GetComponent<TrainedBuffy>();
            if (trained != null && trained.doingFlyingKick)
                lastFrame = TSettings.flyingKick.flyingKickMaxFrame;
            if (__instance.frame >= lastFrame)
            {
                __instance.frame = 0;
                comp.StopFlyingKick();
                __instance.CallMethod("CancelMelee");
            }
            return false;
        }
    }
}