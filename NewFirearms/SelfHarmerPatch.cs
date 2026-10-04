using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;
using HarmonyLib;

namespace NewFirearms
{
    [HarmonyPatch(typeof(SelfHarmer), "AttemptHarm")]
    public class SelfHarmerPatch
    {
        public const string MSGID_SUICIDE_REQUEST = "NewFirearms_SuicideRequest";

        static bool Prepare()
        {
            return !Plugin.catPatchActive;
        }

        static bool Prefix(SelfHarmer __instance)
        {
            if (__instance.body.FindByIdThorough("plushie", out var _))
                return true;
            if (!__instance.body.FindByTagThorough("gun", out Item it))
                return true;

            float threshold = -90f;

            if (threshold < __instance.body.totalHappiness)
                return true;

            __instance.StartCoroutine(GunSuicide(__instance, it));
            return false;
        }

        public static IEnumerator GunSuicide(SelfHarmer __instance, Item it)
        {
            try
            {
                if (it.transform.parent.TryGetComponent<Container>(out var component))
                {
                    component.UnloadItem(it);
                }
                __instance.body.DropItem(__instance.body.handSlot);
                __instance.body.DropItem(it);
                __instance.body.PickUpItem(it, __instance.body.handSlot);
                __instance.body.armsAnimator.Play("ArmsGunSuicide");
                __instance.body.bodyAnimator.Play("Grounded");
                __instance.body.idleTime = 0f;
                __instance.cooldownTime = 300f;
                __instance.harming = false;

                __instance.body.forceWalk = true;
                if (-70f > __instance.body.totalHappiness)
                    Sound.Play("observerlaugh", Vector2.zero, twoDimensional: true, pitchShift: false, null, 1f, 1f, noReverb: true, ignoreMixer: true);
           else     __instance.body.eyeScareTime = 2f;
                yield return new WaitForSeconds(2f);

                __instance.body.eyeCloseTime = 2f;
                __instance.body.talker.Talk("...", null, force: true);
                if (__instance.body == PlayerCamera.main.body)
                    Observer.main.GunSuicide();
                yield return new WaitForSeconds(1f);
                bool flag = false;
                if (__instance.body.GetItem(__instance.body.handSlot) != it)
                {
                    flag = true;
                }
                if (__instance.body.HoldingItem(it) && __instance.body.conscious)
                {
                    ShootManager.DrawVisuals(new ShootVisuals {
                        start = it.transform.position,
                        ends = new List<Vector2> { __instance.body.limbs[0].transform.position },
                        hitNumbers = new List<(Vector2, ushort)>(),
                        knockbacks = new List<(ushort, Vector2)>(),
                    });
                    __instance.body.consciousness = 0f;
                    __instance.body.brainHealth = 30f;
                    __instance.body.internalBleeding += 10f;
                    __instance.body.strokeAmount += 0.1f;
                    __instance.body.limbs[0].skinHealth -= 100f;
                    __instance.body.limbs[0].muscleHealth -= 100f;
                    __instance.body.limbs[0].bleedAmount += 20f;
                    __instance.body.limbs[0].shrapnel = 5;
                    __instance.body.limbs[0].BreakBone();
                    __instance.body.Disfigure();
                    __instance.body.RemoveEye();
                    __instance.body.lastHappiness[0] = -101f;
                    __instance.body.lastHappiness[1] = -101f;
                    __instance.body.lastHappiness[2] = -101f;
                    __instance.body.lastHappiness[3] = -101f;
                    __instance.body.lastHappiness[4] = -101f;
                    __instance.body.lastHappiness[5] = -101f;
                    __instance.body.lastHappiness[6] = -101f;
                    __instance.body.lastHappiness[7] = -101f;
                    __instance.body.lastHappiness[8] = -101f;
                    __instance.body.lastHappiness[9] = -101f;
                    Sound.Play(it.GetComponent<RshGun>().prop.shootAudio, it.transform.position);
                }
                yield return new WaitForSeconds(0.3f);
                Sound.Play("harmSting", Vector2.zero, twoDimensional: true, pitchShift: false, null, 0.7f);
                __instance.body.forceWalk = false;
                if (flag && -70f > __instance.body.totalHappiness)
                {
                    yield return new WaitForSeconds(1f);
                    yield return __instance.Suicide();
                }
            }
            finally
            {
                __instance.body.forceWalk = false;
            }
        }

        public static void RequestGunSuicide()
        {
            Together.Net.Client_Send(LiteNetLib.DeliveryMethod.ReliableUnordered, Together.Multiplayer.CreateNamedWriter(MSGID_SUICIDE_REQUEST));
        }
    }

    // idfk tf is going on in krok's selfharmer sync, its broken beyond repair
    class YOU_SHOULD_KILL_YOURSELF_NOOOWPatch
    {
        static bool Prefix(Together.knetid servid, ref LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out byte result);
            Together.MyLiteNetLibExtensions.Get(reader, out Together.LimbNetId result2);
            if (!result2.TryGetNetBodyAndLimb(out var nb, out var limb))
            {
                return false;
            }
            Body body = nb.body;
            switch (result)
            {
                case 20:
                {
                    if (body.FindByIdThorough("plushie", out var _))
                    {
                        Together.YOU_SHOULD_KILL_YOURSELF_NOOOW.Force(body.GetComponent<SelfHarmer>());
                    }
                    break;
                }
                case 10:
                {
                    if (body.FindByTagThorough("gun", out var it))
                    {
                        // why og code had GetComponent<SelfHarmer>() if its already cacched in harmer?
                        body.harmer.StartCoroutine(SelfHarmerPatch.GunSuicide(body.harmer, it));
                    }
                    break;
                }
                default:
                    if (!CasualtiesTogetherUtils.Util.IsBodyLocal(body))
                    {
                        Together.YOU_SHOULD_KILL_YOURSELF_NOOOW.SelfHarm_Copypasted(result, body, ref limb);
                    }
                    break;
            }
            return false;
        }

        public static void ReciveGunSuicideRequest(Together.ScavPlayer plr, LiteNetLib.Utils.NetDataReader reader)
        {
            if (null == plr.body || !plr.body.alive || !plr.body.conscious || !plr.body.FindByTagThorough("gun", out var it) || !it.TryGetComponent<RshGun>(out var rshGun) || (Plugin.sett.strictSync && !rshGun.IsReady()))
            {
                plr.Server_DoAlertSingle("Denied gun suicide");
                return;
            }
            float oldHappiness = plr.body.happiness;
            plr.body.happiness = -100000f;
            plr.body.harmer.AttemptHarm();
            plr.body.happiness = oldHappiness;
        }
    }
}
