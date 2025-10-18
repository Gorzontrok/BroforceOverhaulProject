using HarmonyLib;
using RocketLib;
using UnityEngine;

namespace BroCeasarTrained
{
    [HarmonyPatch(typeof(BroCeasar))]
    public class BroCaesarPatches
    {
        public static TrainedSettings TSettings
        {
            get => Main.settings.mod;
        }
        public static VanillaSettings VSettings
        {
            get => Main.settings.vanilla;
        }

        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        public static void Awake(BroCeasar __instance)
        {
            __instance.GetOrAddComponent<TrainedBroCeasar>();

            __instance.useNewPushingFrames = VSettings.usePushingAnimation;
            __instance.useNewLadderClimbingFrames = VSettings.useLadderClimbingAnimation;

            __instance.originalSpecialAmmo = VSettings.maxAmmo;

            if (TSettings.hasHalo)
            {
                var halo = HeroController.GetHeroPrefab(HeroType.Broffy).halo;
                __instance.halo = UnityEngine.Object.Instantiate(halo, halo.transform.localPosition, Quaternion.identity);
                __instance.halo.transform.parent = __instance.transform;
            }
        }

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePostfix(BroCeasar __instance)
        {
            __instance.Sprite().RecalcTexture();
        }

        [HarmonyPatch("SetGunPosition")]
        [HarmonyPostfix]
        public static void SetGunPosition(BroCeasar __instance, float xOffset, float yOffset)
        {
            __instance.gunSprite.transform.localScale = new Vector3(1f, 1f, 1f);
        }

        [HarmonyPatch("UseFire")]
        [HarmonyPrefix]
        private static bool NewUseFire(BroCeasar __instance)
        {
            if (Mod.CantUsePatch)
                return true;

            if (__instance.IsMine)
            {
                if (__instance.GetBool("usingSpecial"))
                {
                    __instance.CallMethod("FireWeapon",
                        __instance.X - __instance.transform.localScale.x * VSettings.specialProjectileSpawnPosition.x,
                        __instance.Y + VSettings.specialProjectileSpawnPosition.y,
                        __instance.transform.localScale.x * VSettings.specialProjectileXSpeed,
                        UnityEngine.Random.Range(VSettings.specialProjectileY.x, VSettings.specialProjectileY.y)
                        );
                }
                else
                {
                    __instance.CallMethod("FireWeapon",
                        __instance.X + __instance.transform.localScale.x * VSettings.projectileSpawnOffset.x,
                        __instance.Y + VSettings.projectileSpawnOffset.y,
                        __instance.transform.localScale.x * VSettings.projectileXSpeed,
                        UnityEngine.Random.Range(VSettings.projectileY.x, VSettings.projectileY.y)
                        );
                }
            }
            __instance.CallMethod("PlayAttackSound");
            Map.DisturbWildLife(__instance.X, __instance.Y, VSettings.disturbWildLifeRange, __instance.playerNum);
            return false;
        }

        // BroBase
        [HarmonyPatch(typeof(BroBase), "StartCustomMelee")]
        [HarmonyPrefix]
        private static bool StartCustomMelee(BroBase __instance)
        {
            if (Mod.CantUsePatch || !__instance.Is<BroCeasar>() || Main.settings.mod.useCustomMelee == Its.No)
                return true;

            TrainedBroCeasar customMelee = __instance.GetOrAddComponent<TrainedBroCeasar>();
            if (customMelee != null)
                customMelee.StartCustomMelee();

            return false;
        }

        [HarmonyPatch(typeof(BroBase), "AnimateCustomMelee")]
        [HarmonyPrefix]
        private static bool AnimateCustomMelee(BroBase __instance)
        {
            if (Mod.CantUsePatch || !__instance.Is<BroCeasar>())
                return true;

            TrainedBroCeasar customMelee = __instance.GetOrAddComponent<TrainedBroCeasar>();
            if (customMelee != null)
                customMelee.AnimateCustomMelee();

            return false;
        }

        [HarmonyPatch(typeof(BroBase), "RunCustomMeleeMovement")]
        [HarmonyPrefix]
        private static bool RunCustomMeleeMovement(BroBase __instance)
        {
            if (Mod.CantUsePatch || !__instance.Is<BroCeasar>())
                return true;

            TrainedBroCeasar customMelee = __instance.GetOrAddComponent<TrainedBroCeasar>();
            if (customMelee != null)
                customMelee.RunCustomMeleeMovement();

            return false;
        }

        [HarmonyPatch(typeof(TestVanDammeAnim), "AnimatePushing")]
        [HarmonyPostfix]
        private static void FixPushing(BroBase __instance)
        {
            if (Mod.CantUsePatch || !__instance.Is<BroCeasar>())
                return;

            __instance.CallMethod("SetGunPosition", -4f, 0f);
            __instance.gunSprite.transform.localScale = new Vector3(-1f, 1f, 1f);
        }
    }
}
