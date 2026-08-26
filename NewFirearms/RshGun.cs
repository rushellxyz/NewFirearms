using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using UnityEngine;
using Newtonsoft.Json;

namespace NewFirearms
{
    // MonoBehaviour script for each individual gun
    [Saveable]
    [JsonObject(MemberSerialization.OptIn)]
    public class RshGun : RshComponent, GunMinigame.IMinigameGun
    {
        public const float MUZZLE_RISE_MODIFIER = 8f;
        public const string MSGID_SYNC = "NewFirearms_RshGunSync";
        public const string MSGID_ACTION = "NewFirearms_RshGunAction";
        public const string MSGID_SHOOT = "NewFirearms_Shoot";
        public const string MSGID_SHOOT_VISUALS = "NewFirearms_ShootVisuals";

        public GunJson prop;

        [JsonProperty("existed")]
        public bool existed;
        [JsonProperty("mag")]
        public string mag;
        [JsonProperty("rounds")]
        public List<sbyte> rounds;
        [JsonProperty("fireMode")]
        public byte fireMode;
        [JsonProperty("racked")]
        public bool racked;

        public float gasTime = 0f;
        public bool currentlyShooting;
        public Texture2D composedTexture;
        public Sprite composedSprite;
        /*>=0 ready to shoot
        * -1 casing
        * -2 nothing
        */
        public Body shooter;

        private static bool jamming;
        /*
         * 2. If vanilla code looks up GetRunSetting in Update loops, should you change it?
         * If the vanilla game code does it, you don't strictly have to change it, but it is still highly recommended.
         * Here is why:
         * The Vanilla Tax: Games built with Unity often feature unoptimized code because the developers didn't have time to clean it up.
         * The Modder's Constraint: A player might play the vanilla game fine. But when they install 50 different mods, and every single mod adds extra string-dictionary lookups inside Update(), the frame rate tanks.
         * The Verdict: Caching WorldGeneration.GetRunSettingBool("newfirearms.gunjamming") inside Start() takes 10 seconds to write, costs zero extra memory, and guarantees your mod won't contribute to micro-stutters. It is a hallmark of high-quality mod writing.
         *
         * yup, i use ai to analyze my code, and id recomend you too
         */

        [Obsolete("you stupid! dont use that overload")]
        public void Rack()
         => Rack(manual: true);

         [Obsolete("dont use this either!")]
         public void RemoveMag()
          => RemoveMag(null);

        public bool IsRacked()
         => racked;

        public string CurrentMag()
         => mag;

        public bool JamChance()
        {
            if (!jamming)
                return false;
            float num = ((!(it.condition > 0.5f)) ? Body.Remap(it.condition, 0f, 0.5f, 0.0025f, 0.1f) : Body.Remap(it.condition, 0.5f, 1f, 0f, 0.0025f));
            if (it.isWet)
            {
                num += 0.045f;
            }
            return UnityEngine.Random.value < num;
        }

        public void Update()
        {
            if (Plugin.togetherMpEnabled && !Plugin.IsHost())
            {
                if (currentlyShooting && !Input.GetKey(KeyBinds.GetBind("attack")))
                {
                    RequestHostIfClient(5);
                    currentlyShooting = false;
                }
                return;
            }
            // There's theoreticly a miniascure chance for gun to in periodic subtraction subtract exactly to 0f, and thus skip second rack
            // Tho, even if this happend, player would consider this as a jamming, so, lol
            if (0f > gasTime)
            {
                gasTime = 0f;
                if (JamChance())
                {
                    Sound.Play(prop.jamAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                    shooter = null;
                    return;
                }

                if (4 == prop.feedType)
                    Plugin.ShiftLeft(ref rounds);
           else if (!(1 < rounds.Count && 0 > rounds[1]))
                    Rack(manual: false);
                if (3 == fireMode && null != shooter)
                {
                    if (shooter == PlayerCamera.main.body)
                    {
                        if (!Input.GetKey(KeyBinds.GetBind("attack")) || !Shoot(shooter))
                            shooter = null;
                    }
               else {
                        if (!Shoot(shooter))
                            shooter = null;
                    }
                }
            }
       else if (0f < gasTime)
            {
                gasTime -= Time.deltaTime;
            }
        }

        public void FixedUpdate()
        {   // TODO Move that into Update?
            if (!Plugin.togetherMpEnabled)
                return;
            if (null == transform.parent || !transform.parent.TryGetComponent<InventorySlot>(out InventorySlot slot) || slot.slot != slot.body.handSlot)
                return;
            if (!IsOnBack(slot.body))
                return;
            Limb limb = slot.limb;
            float num = (limb.body.isRight ? 1f : (-1f));
            Vector2 vector = (Vector2)limb.body.targetLookPos - limb.rb.position;
            if (vector.sqrMagnitude > 2f)
            {
                float num2 = Mathf.Atan2(vector.x, 0f - vector.y) * 57.29578f;
                float f = num2 - limb.rb.rotation;
                float num3 = 400f * Time.fixedDeltaTime * limb.totalForce * Mathf.Clamp01(1f - gasTime * 4f);
                float angle = Mathf.MoveTowardsAngle(limb.rb.rotation, num2, num3 * (30f + Mathf.Abs(f)));
                limb.rb.MoveRotation(angle);
                Limb limb2 = limb.connectedLimbs[0];
                Limb limb3 = limb2.connectedLimbs[0];
                limb2.rb.MoveRotation(Mathf.MoveTowardsAngle(limb2.rb.rotation, num2 + 20f * num, num3 * 8f));
                limb3.rb.MoveRotation(Mathf.MoveTowardsAngle(limb3.rb.rotation, num2 - 20f * num, num3 * 12f));
                limb.rb.MoveRotation(angle);
            }
        }

        bool IsOnBack(Body body)
        {
            Together.ScavPlayer plr = Together.ScavPlayer.GetNetPlayerFromBody(body);
            if (!Together.NetBody.TryGetNetBodyFromId(plr.playerId, out var netBody))
                return false;
            return null != netBody.piggybacking_on;
        }

        public override void LoadPropFromCuCore()
        {
            CUCoreLib.Registries.ItemRegistry.TryGetCustomData<GunJson>(GetComponent<Item>(), "prop", out prop);
        }

        public void Start()
        {
            jamming = WorldGeneration.GetRunSettingBool("newfirearms.gunjamming");
            if (!existed)
            {
                rounds = Enumerable.Repeat((sbyte)-2, prop.internalCapacity).ToList();
                fireMode = prop.fireModes.Min();
                if (Plugin.togetherMpEnabled)
                    MpStartOp();
           else     Rack(manual: true);
                existed = true;
            }
       else if (Plugin.togetherMpEnabled)
            {
                MpStartOp();
            }
            UpdateSprite();
        }

        public void MpStartOp()
        {
            InvokeRepeating("MpScareCheck", Plugin.sett.gunSyncRate, Plugin.sett.gunSyncRate * 0.2f);
            if (!Together.Net.IsServer)
                return;
            if (!existed)
            {
                racked = true;
                Sound.Play(prop.rackAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                UpdateSprite();
            }
            InvokeRepeating("SyncIfHostt", Plugin.sett.gunSyncRate, Plugin.sett.gunSyncRate);
        }

        public bool IsReady()
        {
            return !racked && 0 != fireMode && 0 <= rounds[0] && 0.005f < it.condition;
        }

        public bool Shoot(Body body = null)
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(0))
            {
                currentlyShooting = true;
                return true;
            }
            if (null == body)
                body = PlayerCamera.main.body;
            Vector2 barrelPosition = transform.position + transform.up * prop.barrelOffset;
            shooter = body;
            if (!IsReady())
            {
                Sound.Play(prop.unshootAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                if (Plugin.togetherMpEnabled)
                    SyncIfHost(2);
                return false;
            }
            if (JamChance())
            {
                Sound.Play(prop.jamAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                if (Plugin.togetherMpEnabled)
                    SyncIfHost(3);
                return false;
            }
            AmmoInfo info = prop.ammoTypes[rounds[0]];
            float num = (body.isRight ? 1f : (-1f));
            ShootVisuals visuals = new ShootVisuals
            {
                start = barrelPosition,
                ends = new List<Vector2>(),
                hitNumbers = new List<(Vector2, ushort)>(),
                knockbacks = new List<(ushort, Vector2)>(),
            };
            for (int i = 0; i < info.pellets; i++)
            {
                ShootManager.ShootLogic(info, barrelPosition, (transform.right + transform.up * (UnityEngine.Random.Range(-1f, 1f) * info.verticalSpread)) * num, shooter, ref visuals);
            }
            if (!Plugin.togetherMpEnabled || (Plugin.togetherMpEnabled && !Plugin.IsDedicated()))
            {
                ShootManager.DrawVisuals(visuals);
                Sound.Play(prop.shootAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it), pitchShift: false);
            }
            if (Plugin.togetherMpEnabled)
                NotifyShoot(visuals, info.knockback, info.muzzleRise, info.ragdollRecoil);
            body.rb.velocity -= (Vector2)base.transform.right * num * info.knockback;
            body.lastTimeStepVelocity -= (Vector2)base.transform.right * num * info.knockback;
            if (info.ragdollRecoil)
                body.Ragdoll();
            // even more bug fix vanila one line below
            body.eyeScareTime = Mathf.Max(body.eyeScareTime, 1f);
            body.hearingLoss += info.loudness * ((body.hearingLoss > 20f) ? 0.2f : 1f);
            it.condition -= info.conditionLoss;
            body.armsAnimator.SetFloat("gunangle", body.armsAnimator.GetFloat("gunangle") + info.muzzleRise * MUZZLE_RISE_MODIFIER);
            if (PlayerCamera.main.body == shooter && GunMinigame.Plugin.useMinigame)
                GunMinigame.MinigameManager.GetOrAddInstance().AddRecoil(info.knockback, info.muzzleRise);
            if (info.caseless || !WorldGeneration.GetRunSettingBool("newfirearms.gunejectcasing"))
                rounds[0] = -2;
       else     rounds[0] = -1;
            if (1 == fireMode)
            {
                gasTime = 0f;
                if (Plugin.togetherMpEnabled)
                    SyncIfHost();
            }
       else {
                if (0f < prop.desiredGasTime)
                {
                    if (JamChance())
                    {
                        Sound.Play(prop.jamAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                        if (Plugin.togetherMpEnabled)
                            SyncIfHost(3);
                        return true;
                    }

                    gasTime = prop.desiredGasTime;
                    if (4 != prop.feedType)
                        Rack(manual: false);
                    if (Plugin.togetherMpEnabled)
                        SyncIfHost();
                }
           else {
                    Plugin.ShiftLeft(ref rounds);
                    if (Plugin.togetherMpEnabled)
                        SyncIfHost();
                }
            }
            return true;
        }

        public void Rack(bool manual)
        {
            if (manual && Plugin.togetherMpEnabled && RequestHostIfClient(1))
                return;
            racked = !racked;
            if (manual)
            {
                if (JamChance())
                {
                    Sound.Play(prop.jamAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                    if (Plugin.togetherMpEnabled)
                        SyncIfHost(3);
                    return;
                }

                if (racked)
                    Sound.Play(prop.rackAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
           else     Sound.Play(prop.unrackAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
            if (2 >= prop.feedType)
            {
                if (racked)
                    EjectRound(0);
                else if (-2 == rounds[0])
                    Plugin.ShiftLeft(ref rounds);
            }
            if (Plugin.togetherMpEnabled)
            {
                if (manual)
                    SyncIfHost(5);
           else     SyncIfHost();
            }
            UpdateSprite();
        }

        public void EjectRound(int index)
        {
            if (-2 == rounds[index])
                return;
            string round = "casing";
            if (!Plugin.togetherMpEnabled && GunMinigame.Plugin.useMinigame)
                GunMinigame.MinigameManager.GetOrAddInstance().CreateCasing();
            if (0 <= rounds[index])
                round = prop.ammoTypes[rounds[index]].round;
            Utils.Create(round, transform.position, transform.rotation.eulerAngles.z).GetComponent<Rigidbody2D>().velocity = transform.up * 12f;
            rounds[index] = -2;
        }

        public bool CanFitMag(MagJson newMag)
        {
            if (1 != prop.feedType)
                return false;
            if (null != mag && "" != mag)
                return false;
            return 0 < newMag.magazineType.Intersect(prop.magazineType).Count();
        }

        public void OnInventoryUse()
        {
            if (Plugin.togetherMpEnabled && !Plugin.IsHost())
                return;
            RemoveMag(null);
        }

        public bool DragOnto(Item item)
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(2, item))
                return true;
            if (item.TryGetComponent<RshMag>(out var newMag))
            {
                if (!CanFitMag(newMag.prop))
                    return false;

                if (GunMinigame.Plugin.useMinigame) // TODO MP Sync
                    GunMinigame.MinigameManager.GetOrAddInstance().AddRecoil(1f, 1f);

                Sound.Play(prop.loadMagAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                rounds.AddRange(newMag.rounds);
                mag = newMag.it.id;
                UnityEngine.Object.Destroy(item.gameObject);
                UpdateSprite();
                if (Plugin.togetherMpEnabled)
                    SyncIfHost();
            }
       else {
                for (sbyte i = 0; i < prop.ammoTypes.Count(); i++)
                {
                    AmmoInfo ammo = prop.ammoTypes[i];
                    if (ammo.round != item.id)
                        continue;

                    if (1 == prop.feedType)
                    {
                        if (!racked || -2 != rounds[0])
                            return false;
                        rounds[0] = i;
                    }
               else if (2 == prop.feedType)
                    { // TODO for tube feed, option to shift rounds in tube forward, like it does irl
                        int begin = 1;
                        if (racked)
                            begin = 0;
                        bool inserted = false;
                        for (int j = begin; j < rounds.Count(); j++)
                        {
                            if (-2 != rounds[j])
                                continue;

                            rounds[j] = i;
                            inserted = true;
                            break;
                        }
                        if (!inserted)
                            return false;
                    }
               else if (racked)
                    {
                        bool inserted = false;
                        int casing = int.MaxValue;
                        for (int j = 0; j < rounds.Count(); j++)
                        {
                            if (-1 == rounds[j] && j < casing)
                                casing = j;

                            if (-2 != rounds[j])
                                continue;

                            rounds[j] = i;
                            inserted = true;
                            break;
                        }
                        if (!inserted)
                        {
                            if (int.MaxValue != casing)
                            {
                                rounds[casing] = i;
                                GameObject go = Utils.Create("casing", transform.position, transform.rotation.eulerAngles.z);
                                if (!Input.GetKey(KeyCode.LeftShift))
                                    PlayerCamera.main.body.AutoPickUpItem(go.GetComponent<Item>());
                            }
                    else     return false;
                        }
                    }
               else     return false;
                    Sound.Play(prop.loadRoundAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));

                    if (GunMinigame.Plugin.useMinigame) // TODO MP Sync
                        GunMinigame.MinigameManager.GetOrAddInstance().AddRecoil(0.5f, 0.5f);
                    UnityEngine.Object.Destroy(item.gameObject);
                    UpdateSprite();
                    if (Plugin.togetherMpEnabled)
                        SyncIfHost(4);
                    return true;
                }
            }
            return false;
        }

        public void RemoveMag(Body body=null)
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(3))
                return;
            if (3 <= prop.feedType && racked)
            {
                Sound.Play(prop.removeMagAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                for (int i = 0; i < rounds.Count(); i++)
                    EjectRound(i);
                if (Plugin.togetherMpEnabled)
                    SyncIfHost(1);
            }
       else if (!string.IsNullOrEmpty(mag))
            {
                if (null == body)
                    body = PlayerCamera.main.body;
                GameObject go = Utils.Create(mag, transform.position, 0f);
                // RshLib calls onSpawn immediantly once item is created
                // Meanwhile CUCoreLib adds custom compoennts in Start of Item
                if (Plugin.useCuCore)
                {
                    var _ = RemoveMagPart2WhenCuCore(body, go);
                }
           else {
                    RshMag rshMag = go.GetComponent<RshMag>();
                    sbyte chamber = rounds[0];
                    rounds.RemoveAt(0);
                    rshMag.rounds = rounds;
                    rshMag.existed = true;
                    rounds = new List<sbyte> { chamber };
                    mag = "";
                    Sound.Play(prop.removeMagAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                    body.AutoPickUpItem(go.GetComponent<Item>());
                    UpdateSprite();
                    if (Plugin.togetherMpEnabled)
                    {
                        rshMag.SyncIfHost();
                        SyncIfHost();
                    }
                }
            }
        }

        public async Task RemoveMagPart2WhenCuCore(Body body, GameObject go)
        {
            RshMag rshMag = null;
            for (int i = 0; i < 100 && null == rshMag; i++)
            {
                await Task.Delay(100);
                rshMag = go.GetComponent<RshMag>();
            }
            sbyte chamber = rounds[0];
            rounds.RemoveAt(0);
            rshMag.rounds = rounds;
            rshMag.existed = true;
            rounds = new List<sbyte> { chamber };
            mag = "";
            Sound.Play(prop.removeMagAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            body.AutoPickUpItem(go.GetComponent<Item>());
            UpdateSprite();
            if (Plugin.togetherMpEnabled)
            {
                rshMag.SyncIfHost();
                SyncIfHost();
            }
        }

        public void ForceReady()
        {
            rounds[0] = 0;
            racked = false;
            fireMode = prop.fireModes.Max();
            it.condition = Mathf.Max(0.014f, it.condition);
            UpdateSprite();
            if (Plugin.togetherMpEnabled)
                SyncIfHost();
        }

        void DestroyComposedSprite()
        {
            UnityEngine.Object.Destroy(composedSprite);
            UnityEngine.Object.Destroy(composedTexture);
        }

        public void UpdateSprite()
        {
            List<Texture2D> textures = new List<Texture2D>();
            if (racked)
                textures.Add(prop.rackedTexture);
       else     textures.Add(prop.normalTexture);
            if (!string.IsNullOrEmpty(mag))
            {
                if (prop.magazineTextures.TryGetValue(mag, out var magTexture))
                {
                    textures.Add(magTexture);
                }
           else     UnityEngine.Debug.LogError($"Magazine {mag} have no texture for gun {prop.fullName}!");
            }

            DestroyComposedSprite();
            composedTexture = Plugin.CombineTextures(textures);
            composedSprite = Plugin.TextureToSprite(composedTexture, prop.ppu);
            gameObject.GetComponent<SpriteRenderer>().sprite = composedSprite;
        }

        void OnDestroy()
        {
            DestroyComposedSprite();
        }


        public void SyncIfHostt()
         => SyncIfHost(reliable: false);



       /*
        * 0 - Try to shoot
        * 1 - Manual rack
        * 2 - Drag onto
        * 3 - Remove mag
        * 4 - Toggle fire mode
        * 5 - Stop shooting
        */
        public bool RequestHostIfClient(byte action, Item second=null)
        {
            if (Together.Net.IsServer)
                return false;
            LiteNetLib.Utils.NetDataWriter writer = Together.Multiplayer.CreateNamedWriter(MSGID_ACTION);
            Together.SyncInfoGameObjectTracker tracker = (GetMpTracker(true) as Together.SyncInfoGameObjectTracker);
            writer.Put(tracker.syncId);
            writer.Put((byte)action);
            if (null != second)
            {
                if (!second.TryGetComponent<Together.SyncInfoGameObjectTracker>(out var ksmgont2))
                    throw new Exception("[NewFirearms] Attempt to gun action on drag onto without ScavMultiGameObjectNetworkTracker :hmm:");
                writer.Put((ushort)ksmgont2.syncId);
            }
            Together.Net.Client_Send(LiteNetLib.DeliveryMethod.ReliableUnordered, in writer);
            return true;
        }
        /*
        * 1 - Play removeMag audio
        * 2 - Play unshoot audio
        * 3 - Play jam audio
        * 4 - Play load round audio
        * 5 - Play rack audio
        */
        public override void SyncIfHost(byte extraData=0, bool reliable=true)
        {
            if (!Together.Net.IsServer)
                return;
            Together.SyncInfoGameObjectTracker tracker = (GetMpTracker(reliable) as Together.SyncInfoGameObjectTracker);
            if (null == tracker)
                return;
            if (!tracker.isVisible)
                return;
            LiteNetLib.Utils.NetDataWriter writer = Together.Multiplayer.CreateNamedWriter(MSGID_SYNC);

            writer.Put((ushort)tracker.syncId);
            writer.Put((byte)fireMode);
            writer.Put((bool)racked);
            if (string.IsNullOrEmpty(mag))
            {
                writer.Put((bool)false);
            }
       else {
                writer.Put((bool)true);
                Together.MyLiteNetLibExtensions.Put(writer, (string)mag, oneByteChars:true);
            }

            writer.Put((ushort)rounds.Count());
            for (int i = 0; i < rounds.Count(); i++)
                writer.Put((sbyte)rounds[i]);

            writer.Put((byte)extraData);

            LiteNetLib.DeliveryMethod method;
            if (reliable)
                method = LiteNetLib.DeliveryMethod.ReliableUnordered;
       else     method = LiteNetLib.DeliveryMethod.Unreliable;
            Together.Net.Server_SendToClients(method, in writer, Together.ServerMain.AllClientIdsExceptHost);
        }

        void MpScareCheck()
        {
            if (!Together.Multiplayer.rules.PVP)
                return;
            if (null == transform.parent || !transform.parent.TryGetComponent<InventorySlot>(out InventorySlot slot) || slot.slot != slot.body.handSlot)
                return;

            Body holder = slot.body;
            float side = holder.isRight ? 1f : -1f;
            Vector2 direction = (Vector2)transform.right * side;
            Vector2 barrelPos = (Vector2)transform.position + (Vector2)(transform.up * prop.barrelOffset);
            RaycastHit2D[] hits = Physics2D.RaycastAll(barrelPos, direction, 4f);
            foreach (RaycastHit2D hit in hits)
            { // TODO REwrite thta
                Limb limb = null;
                if (hit.collider.TryGetComponent<Limb>(out var hitLimb) && hitLimb.isVital)
                    limb = hitLimb;
                if (null == limb)
                {
                    if (!hit.collider.TryGetComponent<Together.NetBody>(out var netBody))
                        continue;
                    Vector2 toHead = ((Vector2)netBody.GetHeadPos() - barrelPos).normalized;
                    if (0.9f > Vector2.Dot(direction, toHead))
                        continue;
                    limb = netBody.head;
                }
                // Yet another bug fix here
                // 1. It doesnt account for teams, lol
                // 2. It compare to happiness not totalHappiness
                if (ShootManager.CantHitThisPlayer(limb.body, holder) || -40f > limb.body.totalHappiness)
                    continue;
                limb.body.eyeScareTime = Math.Max(limb.body.eyeScareTime, 0.3f);
            }
        }

        public void NotifyShoot(ShootVisuals visuals, float knockback, float muzzleRise, bool ragdollRecoil)
        {
            if (!Together.Net.IsServer)
                return;
            LiteNetLib.Utils.NetDataWriter writer = Together.Multiplayer.CreateNamedWriter(MSGID_SHOOT_VISUALS);
            Together.SyncInfoGameObjectTracker tracker = (GetMpTracker(false) as Together.SyncInfoGameObjectTracker);
            if (null == tracker)
                return;
            writer.Put((ushort)tracker.syncId);
            writer.Put((float)knockback);
            writer.Put((float)muzzleRise);
            writer.Put((bool)ragdollRecoil);
            visuals.Serialize(ref writer);
            Together.Net.Server_SendToClients(LiteNetLib.DeliveryMethod.ReliableUnordered, in writer, Together.ServerMain.AllClientIdsExceptHost);
        }

        public static void ClientSync(LiteNetLib.Utils.NetDataReader reader)
        { // the var is evil
            reader.Get(out ushort syncId);
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var _, out var it))
                throw new Exception("[NewFirearms] Recived gun sync info for non-registred gun!");
            if (!it.TryGetComponent<RshGun>(out var rshGun))
                throw new Exception("[NewFirearms] Gun sync packet refering to item without RshGun component :tourniqet:");
            rshGun.existed = true;
            bool shouldUpdateSprite = false;

            reader.Get(out byte newFireMode);
            if (newFireMode != rshGun.fireMode)
                Sound.Play(rshGun.prop.toggleFireModeAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            rshGun.fireMode = newFireMode;

            reader.Get(out bool newRack);

            string newMag = null;
            reader.Get(out bool haveMag);
            if (haveMag)
                Together.MyLiteNetLibExtensions.Get(reader, out newMag, oneByteChars: true);
            if (string.IsNullOrEmpty(newMag) && !string.IsNullOrEmpty(rshGun.mag))
            {
                shouldUpdateSprite = true;
                Sound.Play(rshGun.prop.removeMagAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
            else if (!string.IsNullOrEmpty(newMag) && string.IsNullOrEmpty(rshGun.mag))
            {
                shouldUpdateSprite = true;
                Sound.Play(rshGun.prop.loadMagAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
            rshGun.mag = newMag;

            reader.Get(out ushort roundsCount);
            if (0 == roundsCount)
                throw new Exception("[NewFirearms] Attempt to initalize rounds with count of 0!");
            rshGun.rounds.Clear();
            for (ushort i = 0; i < roundsCount; i++)
            {
                reader.Get(out sbyte newRound);
                rshGun.rounds.Add(newRound);
            }

            reader.Get(out byte extraData);
            if (1 == extraData)
                Sound.Play(rshGun.prop.removeMagAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (2 == extraData)
                Sound.Play(rshGun.prop.unshootAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (3 == extraData)
                Sound.Play(rshGun.prop.jamAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
       else if (4 == extraData)
                Sound.Play(rshGun.prop.loadRoundAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));

            if (newRack && !rshGun.racked)
            {
                shouldUpdateSprite = true;
                if (5 == extraData)
                    Sound.Play(rshGun.prop.rackAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
       else if (!newRack && rshGun.racked)
            {
                shouldUpdateSprite = true;
                if (5 == extraData)
                    Sound.Play(rshGun.prop.unrackAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
            rshGun.racked = newRack;

            if (shouldUpdateSprite)
                rshGun.UpdateSprite();
        }

        public static readonly Dictionary<byte, string> ACTIONS_NAME = new Dictionary<byte, string>
        {
            {0, "shoot"},
            {1, "rack"},
            {2, "chamber load"},
            {3, "remove mag"},
            {4, "toggle fire mode"},
            {5, "stop shooting"},
        };

        public static void ServerReceiver(Together.ScavPlayer plr, LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            reader.Get(out byte action);

            if (6 <= action)
            {
                DenyAction(plr, "Denied unknown gun action");
                return;
            }
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var si, out Item it))
            {
                DenyAction(plr, $"Denied {ACTIONS_NAME[action]}\nGun is not registred!");
                return;
            }
            if (!it.TryGetComponent<RshGun>(out var rshGun))
            {
                DenyAction(plr, $"Denied {ACTIONS_NAME[action]}\nThis is not a gun?!");
                return;
            }
            if (!plr.TryGetNetBody(out var pb))
            {
                DenyAction(plr, $"Denied {ACTIONS_NAME[action]}\nYou dont have a body, whaaa??!!");
                return;
            }
            if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(si, pb.body, check_obstruction:true))
            {
                DenyAction(plr, $"Denied {ACTIONS_NAME[action]}\nYou can't reach that gun.", canBeFixedWithSwitchingStrictSync:true);
                return;
            }

            if (0 == action)
                rshGun.Shoot(pb.body);
       else if (1 == action)
                rshGun.Rack(manual: true);
       else if (2 == action)
            {
                reader.Get(out ushort ontoSyncId);
                if (!Together.ItemSync.TryGetItem(new Together.knetid(ontoSyncId), out var ontoSi, out var ontoIt))
                {
                    DenyAction(plr, "Denied chamber load\nRound is not registred!");
                    return;
                }
                if (Plugin.sett.strictSync && !Together.ItemSync.CheckIfBodyReachThisItem(ontoSi, pb.body, check_obstruction:true))
                {
                    DenyAction(plr, "Denied chamber load\nYou can't reach that round.", canBeFixedWithSwitchingStrictSync:true);
                    return;
                }
                rshGun.DragOnto(ontoIt);
            }
       else if (3 == action)
                rshGun.RemoveMag(pb.body);
       else if (4 == action)
            {
                Sound.Play(rshGun.prop.toggleFireModeAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                rshGun.fireMode = rshGun.prop.fireModes[(rshGun.prop.fireModes.IndexOf(rshGun.fireMode) + 1) % rshGun.prop.fireModes.Count];
                rshGun.SyncIfHost();
            }
       else if (5 == action)
                rshGun.shooter = null;
        }

        public static void ReceiveShoot(LiteNetLib.Utils.NetDataReader reader)
        {
            reader.Get(out ushort syncId);
            RshGun rshGun = null;
            if (!Together.ItemSync.TryGetItem(new Together.knetid(syncId), out var si, out var _1))
                UnityEngine.Debug.LogWarning("[NewFirearms] Recived shoot notify for non-registred item! Are you missing addon?");
       else if (!si.IsItem())
                UnityEngine.Debug.LogWarning("[NewFirearms] Shoot notify refering to not item :tourniqet:");
       else if (!si.go.TryGetComponent<RshGun>(out rshGun))
                UnityEngine.Debug.LogWarning("[NewFirearms] Shoot notify refering to item without RshGun component :tourniqet:");

            reader.Get(out float knockback);
            reader.Get(out float muzzleRise);
            reader.Get(out bool ragdollRecoil);
            if (null != rshGun)
                Sound.Play(rshGun.prop.shootAudio, rshGun.transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(si.item), pitchShift: false);
            if (null != rshGun && null != rshGun.transform.parent && rshGun.transform.parent.TryGetComponent<InventorySlot>(out InventorySlot slot))
            {
                float num = slot.body.isRight ? 1f : (-1f);
                slot.body.rb.velocity -= (Vector2)rshGun.transform.right * num * knockback;
                slot.body.lastTimeStepVelocity -= (Vector2)rshGun.transform.right * num * knockback;
                slot.body.armsAnimator.SetFloat("gunangle", slot.body.armsAnimator.GetFloat("gunangle") + muzzleRise * MUZZLE_RISE_MODIFIER);
                if (PlayerCamera.main.body == slot.body && GunMinigame.Plugin.useMinigame)
                    GunMinigame.MinigameManager.GetOrAddInstance().AddRecoil(knockback, muzzleRise);
                if (ragdollRecoil)
                    slot.body.Ragdoll();
            }
       else     UnityEngine.Debug.LogWarning("[NewFirearms] Host says that gun is being shot, but im unable to find who shoots it, ummm, okay ig???");
            ShootManager.DrawVisuals(ShootVisuals.Deserialize(ref reader));
        }
    }
}
