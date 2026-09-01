using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewFirearms
{
    public class ShootVisuals
    {
        public Vector2 start;
        public List<Vector2> ends;
        public List<(Vector2, ushort)> hitNumbers;
        public List<(ushort netId, Vector2 impulse)> knockbacks;

        public void Serialize(ref LiteNetLib.Utils.NetDataWriter writer)
        {
            writer.Put((float)start.x);
            writer.Put((float)start.y);
            writer.Put((ushort)ends.Count);
            foreach (Vector2 end in ends)
            {
                writer.Put((float)end.x);
                writer.Put((float)end.y);
            }
            writer.Put((ushort)hitNumbers.Count);
            foreach ((Vector2, ushort) hitNumber in hitNumbers)
            {
                writer.Put((float)hitNumber.Item1.x);
                writer.Put((float)hitNumber.Item1.y);
                writer.Put((ushort)hitNumber.Item2);
            }
            writer.Put((ushort)knockbacks.Count);
            foreach ((uint netId, Vector2 impulse) in knockbacks)
            {
                writer.Put((ushort)netId);
                writer.Put((float)impulse.x);
                writer.Put((float)impulse.y);
            }
        }

        public static ShootVisuals Deserialize(ref LiteNetLib.Utils.NetDataReader reader)
        {
            ShootVisuals visual = new ShootVisuals();
            reader.Get(out float startX);
            reader.Get(out float startY);
            visual.start = new Vector2(startX, startY);
            reader.Get(out ushort endsCount);
            visual.ends = new List<Vector2>(endsCount);
            for (int i = 0; i < endsCount; i++)
            {
                reader.Get(out float endX);
                reader.Get(out float endY);
                visual.ends.Add(new Vector2(endX, endY));
            }
            reader.Get(out ushort hitNumbersCount);
            visual.hitNumbers = new List<(Vector2, ushort)>(hitNumbersCount);
            for (int i = 0; i < hitNumbersCount; i++)
            {
                reader.Get(out float endX);
                reader.Get(out float endY);
                reader.Get(out ushort number);
                visual.hitNumbers.Add((new Vector2(endX, endY), number));
            }
            reader.Get(out ushort knockbacksCount);
            visual.knockbacks = new List<(ushort, Vector2)>(knockbacksCount);
            for (int i = 0; i < knockbacksCount; i++)
            {
                reader.Get(out ushort netId);
                reader.Get(out float impulseX);
                reader.Get(out float impulseY);
                visual.knockbacks.Add((netId, new Vector2(impulseX, impulseY)));
            }
            return visual;
        }
    }

    public static class ShootManager
    {
        // shooter is null when commiting suicide
        public static void ShootLogic(AmmoInfo info, Vector2 pos, Vector2 dir, Body shooter, ref ShootVisuals visuals)
        {
            Physics2D.queriesHitTriggers = true;
            RaycastHit2D[] array = Physics2D.RaycastAll(pos, dir, 200f, LayerMask.GetMask("Ground", "Body", "Limb", "Descriptor"));
            Physics2D.queriesHitTriggers = false;
            Vector2 vector = pos + dir * 200f;
            float num = WorldGeneration.GetRunSettingFloat("newfirearms.gundmgmult") * UnityEngine.Random.Range(0.85f, 1.15f);
            List<Body> list = new List<Body>();
            RaycastHit2D[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                RaycastHit2D raycastHit2D = array2[i];
                /*if (raycastHit2D.transform == info.ignoreTrans)
                {
                    continue;
                }*/

                if (raycastHit2D.collider.TryGetComponent<BuildingEntity>(out var component) && !component.cantHit)
                {
                    if (null != info.explosionParams)
                    {
                        info.explosionParams.position = raycastHit2D.point;
                        WorldGeneration.CreateExplosion(info.explosionParams);
                        vector = raycastHit2D.point;
                        break;
                    }
                    component.health -= (component.animal ? (info.animalDamage * num) : (info.structureDamage * num));
                    if (component.animal)
                    {
                        component.gameObject.SendMessage("AnimalHit", info.animalDamage * num);
                        Sound.Play("gore" + UnityEngine.Random.Range(1, 6), raycastHit2D.point);
                        visuals.hitNumbers.Add((raycastHit2D.point, (ushort)(info.animalDamage * num)));
                    }
                    else
                    {
                        visuals.hitNumbers.Add((raycastHit2D.point, (ushort)(info.structureDamage * num)));
                    }
                }
           else if (raycastHit2D.collider.gameObject.name.StartsWith("StunHitbox"))
                { // TODO it when hitting spider it hits stun hitbox AND spider, deling twice (or trice) stun time :hmm:
                    Transform spiderTran = raycastHit2D.collider.gameObject.transform.parent;
                    if (null == spiderTran)
                    {
                        UnityEngine.Debug.LogError("[NewFirearms] Hit a stun hitbox without parent :tourniqet");
                        continue;
                    }
                    if (!spiderTran.TryGetComponent<SpiderHandler>(out var spider))
                    {
                        UnityEngine.Debug.LogError("[NewFirearms] Hit a stun hitbox with parent no spider :tourniqet");
                        continue;
                    }
                    spider.stunTime += info.animalDamage * num * spider.stunMult;
                    if (spider.stunTime > 0.1f)
                    {
                        for (int j = 0; j < spider.targets.Length; j++)
                        {
                            spider.targets[j] += UnityEngine.Random.insideUnitCircle;
                        }
                    }
                    continue;
                }
                else if (raycastHit2D.collider.gameObject.layer == 6)
                {
                    if (null != info.explosionParams)
                    {
                        info.explosionParams.position = raycastHit2D.point;
                        WorldGeneration.CreateExplosion(info.explosionParams);
                        vector = raycastHit2D.point;
                        break;
                    }
                    WorldGeneration.world.DamageBlock(raycastHit2D.point + dir * 0.5f, info.structureDamage * num);
                    visuals.hitNumbers.Add((raycastHit2D.point, (ushort)(info.structureDamage * num)));
                    vector = raycastHit2D.point;
                    break;
                }
                if ((bool)raycastHit2D.rigidbody)
                {
                    if (null != info.explosionParams)
                    {
                        info.explosionParams.position = raycastHit2D.point;
                        WorldGeneration.CreateExplosion(info.explosionParams);
                        vector = raycastHit2D.point;
                        break;
                    }
                    raycastHit2D.rigidbody.AddForceAtPosition(dir * info.structureDamage, raycastHit2D.point, ForceMode2D.Impulse);
                }
                PlayerCamera.main.body.CreateCloudSmall(raycastHit2D.point, Vector2.zero);
                if (!Plugin.krokMpEnabled || !IsPvpEnabled())
                {
                    continue;
                }
                Body victim = null;
                Limb limb = null;
                if (raycastHit2D.collider.TryGetComponent<Limb>(out limb))
                {
                    victim = limb.body;
                }
           else if (raycastHit2D.collider.TryGetComponent<Body>(out victim))
                {
                    limb = victim.GetClosestLimb(raycastHit2D.point);
                }
           else {
                    UnityEngine.Debug.LogWarning($"[NewFirearms] Raycast of shoot hit something unknown!? {raycastHit2D.collider.gameObject}");
                    continue;
                }
                if (victim == shooter)
                    continue;

                if ((Plugin.krokMpEnabled && CantHitThisPlayer(shooter, victim)) || victim == shooter)
                {
                    continue;
                }
                if (null != info.explosionParams)
                {
                    info.explosionParams.position = raycastHit2D.point;
                    WorldGeneration.CreateExplosion(info.explosionParams);
                    vector = raycastHit2D.point;
                    break;
                }
                if (list.Contains(victim))
                    break;
                list.Add(victim);
                Vector2 impulse = dir * info.knockback;
                if (Plugin.krokMpEnabled)
                    RecordShootKnockback(victim, impulse, visuals);
           else     ApplyShootKnockback(victim, impulse);
                InduceDamage(limb, info.playerDamage, num * PvpDamageMult() / limb.GetArmorReduction());
                if (null != shooter && victim != shooter)
                {
                    shooter.happiness -= info.playerDamage.traumaAmount * PvpMoodDebuff();
                    victim.traumaAmount += info.playerDamage.traumaAmount * num * PvpMoodDebuff();
                }
            }
            visuals.ends.Add(vector);
        }

        public static void ApplyShootKnockback(Body victim, Vector2 impulse)
        {
            victim.rb.velocity += impulse;
            victim.lastTimeStepVelocity += impulse;
            victim.DoGoreSound();
            victim.Ragdoll();
        }

        public static void InduceDamage(Limb limb, PlayerDamage info, float mult)
        {
            Body victim = limb.body;

            bool isBulletproof = IsBulletproof(limb);

            limb.body.adrenaline += info.adrenaline * mult;
            if (isBulletproof)
                limb.pain += info.pain * mult * 0.5f;
       else     limb.pain += info.pain * mult;
            if (isBulletproof)
                limb.skinHealth -= info.skinDamage * mult * 0.2f;
       else     limb.skinHealth -= info.skinDamage * mult;
            if (isBulletproof)
                limb.muscleHealth -= info.muscleDamage * mult * 0.5f;
       else     limb.muscleHealth -= info.muscleDamage * mult;
            if (isBulletproof)
                limb.bleedAmount += info.bleedAmount * mult * 0.2f;
       else     limb.bleedAmount += info.bleedAmount * mult;
            if (!isBulletproof)
                limb.body.venomTotal += info.venomAmount * mult;
            limb.DamageWearables(1f * info.wearableDamage);

            if (!isBulletproof && UnityEngine.Random.value < info.shrapnelChance * mult)
                limb.shrapnel = 5;
            if (!isBulletproof && UnityEngine.Random.value < info.infectionChance * mult)
            {
                limb.infectionAmount += 1f;
                limb.infected = true;
            }
            if (UnityEngine.Random.value < info.dislocationChance * mult * (1f - limb.muscleHealth * 0.01f))
                limb.Dislocate();
            if (!isBulletproof && UnityEngine.Random.value < info.fractureChance * mult * (1f - limb.muscleHealth * 0.01f))
                limb.BreakBone();

            if (isBulletproof)
                mult *= 0.5f;

            if (limb.isVital || limb.isHead)
            {
                victim.internalBleeding += info.internalBleed * mult;
            }

            if (limb.isHead)
            {
                victim.consciousness = 0f;
                victim.brainHealth -= info.brainDamage * mult * UnityEngine.Random.Range(0.5f, 1.5f);
                if (UnityEngine.Random.value < info.disfigureChance * mult)
                {
                    if (UnityEngine.Random.value < 0.5f)
                    {
                        if (UnityEngine.Random.value < 4f - limb.muscleHealth * 0.04f)
                            victim.Disfigure();
                    }
               else     victim.RemoveEye();
                }
            }

            if (0 < info.chloroformAmount)
                Liquids.Registry["chloroform"].onDrink(info.chloroformAmount * mult, victim);

            if (limb.isVital && 0f > limb.muscleHealth)
                limb.body.fibrillationProgress -= limb.muscleHealth;
       else if (limb.isHead && 0f > limb.muscleHealth)
                limb.body.brainHealth += limb.muscleHealth;

            if (Plugin.krokMpEnabled && PvpDismemberAllowed())
                DismemberCheck(limb);
        }

        public static bool IsBulletproof(Limb limb)
        {
            foreach (Item it in limb.GetLimbWearables())
            {
                if (it.Stats.HasTag("bulletproof"))
                    return true;
            }
            return false;
        }

        public static void DismemberCheck(Limb limb)
        {
            if (limb.isVital || limb.isAbdomen)
                return;

            if (limb.body.alive && limb.isLegLimb)
            {
                /* so, yeah you could notice that elder thorn back and amputation minigame doesnt let you amputate leg limb if another is already missing
                 * i saw bug report in target planet, that says you can walk with both legs missing, on which moffe sayed that there are no situation in which you can lose both legs
                 * fun fact: dune skips that check, so there is situation in which you can lose both legs
                 * but anyway, og check would means that losing only one paw protects you from further leg damage
                 * this check is less.. hostile, now you need to lose any thigh to not lose remaining leg limb
                 */
                if (limb.body.limbs[9].dismembered || limb.body.limbs[12].dismembered)
                    return;
            }

            float multiplier;
            if (limb.isHead && !limb.body.disfigured)
                multiplier = -0.005f;
       else     multiplier = -0.01f;

            if (limb.broken)
                multiplier *= 0.5f;
            if (limb.dislocated)
                multiplier *= 0.5f;

            if (UnityEngine.Random.value > (limb.muscleHealth - 10f) * multiplier)
                return;

            KrokoshaCasualtiesMP.CombatStuff.BetterDismember(limb, do_checks_to_not_break_the_game: false);
            /*limb.Dismember();

            if (!limb.isHead)
                return;
            limb.body.brainHealth = 0f;
            limb.body.disfigured = false;
            limb.body.bothEyesGone = false;
            limb.body.eyeGone = false;*/
        }

        public static void DrawVisuals(ShootVisuals visual)
        {
            foreach (Vector2 end in visual.ends)
            {
                GameObject obj = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("Special/TurretLine"), Vector2.zero, Quaternion.identity);
                obj.GetComponent<LineRenderer>().SetPosition(0, visual.start);
                obj.GetComponent<LineRenderer>().SetPosition(1, end);
                UnityEngine.Object.Destroy(obj, 0.05f);
            }
            foreach ((Vector2, int) hitNumber in visual.hitNumbers)
            {
                WorldGeneration.CreateDamageNumber(hitNumber.Item1, hitNumber.Item2);
            }
            if (!Plugin.krokMpEnabled)
                return;
            foreach ((ushort netId, Vector2 impulse) in visual.knockbacks)
            {
                ApplyShootKnockback(GetBodyFromId(netId), impulse);
            }
        }

        public static Body GetBodyFromId(ushort netId)
        {
            if (!KrokoshaCasualtiesMP.NetBody.TryGetNetBodyFromId(new KrokoshaCasualtiesMP.knetid(netId), out var netBody) || null == netBody.body)
                throw new Exception($"[NewFirearms] Attempt to apply knockback for non-registred body {netId}! Wtf?");
            return netBody.body;
        }

        public static bool IsPvpEnabled()
        {
            return KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.rules.PVP;
        }

        public static float PvpDamageMult()
        {
            return KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.rules.PVPDamageMultiplier;
        }

        public static float PvpMoodDebuff()
        {
            return KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.rules.PVPMoodDebuff;
        }

        public static bool CantHitThisPlayer(Body shooter, Body victim)
        {
            if (shooter == victim)
                return false;
            if (!KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.rules.Teams)
                return false;
            return !KrokoshaCasualtiesMP.NetPlayer.BodyToPlayerDict[shooter].IsInSameTeamAs(KrokoshaCasualtiesMP.NetPlayer.BodyToPlayerDict[victim]);
        }

        public static bool PvpDismemberAllowed()
        {
            return KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.rules.PVPCombatDismember;
        }

        public static void RecordShootKnockback(Body victim, Vector2 impulse, ShootVisuals visuals)
        {
            visuals.knockbacks.Add((KrokoshaCasualtiesMP.NetPlayer.GetClientIdFromBody(victim), impulse));
        }
    }
}
