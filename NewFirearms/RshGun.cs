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
    public partial class RshGun : RshComponent, GunMinigame.IMinigameGun, GunMinigame.IMinigameJammedGun
    {
        public const float MUZZLE_RISE_MODIFIER = 8f;
        public const string MSGID_SYNC = "NewFirearms_RshGunSync";
        public const string MSGID_ACTION = "NewFirearms_RshGunAction";
        public const string MSGID_SHOOT = "NewFirearms_Shoot";
        public const string MSGID_SHOOT_VISUALS = "NewFirearms_ShootVisuals";
        public const string MSGID_EJECT_VISUALS = "NewFirearms_EjectVisuals";
        internal const byte SilentMagazineRemoval = 0x80;

        public GunJson prop;
        internal event Action<GameObject> MagazineCreated; // :hmm:

        [JsonProperty("existed")]
        public bool existed;
        [JsonProperty("mag")]
        public string mag;
        [JsonProperty("rounds")]
        public List<sbyte> rounds = new List<sbyte>();
        [JsonProperty("spentAmmoTypes")]
        private Dictionary<int, sbyte> spentAmmoTypes = new Dictionary<int, sbyte>();
        [JsonProperty("fireMode")]
        public byte fireMode;
        [JsonProperty("racked")]
        public bool racked;
        [JsonProperty("laserEnabled")]
        public bool laserEnabled = true;

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

        public byte CurrentFireMode()
         => fireMode;

        public string CurrentMag()
         => mag;

        public bool JamChance()
        {
            if (!jamming)
                return false;
            float condition = Mathf.Clamp01(it.condition);
            float num = condition <= 0.5f
                ? Mathf.Lerp(0.1f, 0.0025f, condition * 2f)
                : Mathf.Lerp(0.0025f, 0f, (condition - 0.5f) * 2f);
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
            if (IsJammed) return;
            if (gasTime <= 0f) return;
            gasTime -= Time.deltaTime;
            if (gasTime <= 0f)
            {
                gasTime = 0f;
                if (4 == prop.feedType || prop.feedType == 3 && fireMode == BurstFireMode)
                    RotateChambers();
           else if (!(1 < rounds.Count && 0 > rounds[1]))
                    Rack(manual: false);
                ContinueFiring();
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
            Together.ScavPlayer plr = Together.ScavPlayer.GetPlayerFromBody(body);
            if (!Together.NetBody.TryGetNetBodyFromId(plr.playerId, out var netBody))
                return false;
            return null != netBody.piggybacking_on;
        }

        public override void LoadDefinition()
        {
            Item item = GetComponent<Item>();
            if (item == null || string.IsNullOrEmpty(item.id)) return;
            it = item;
            Plugin.GunDefinitions.TryGetValue(item.id, out prop);
        }

        public void Start()
        {
            if (TryGetComponent<LaserPointer>(out var laser))
                laser.enabled = laserEnabled;
            jamming = WorldGeneration.GetRunSettingBool("newfirearms.gunjamming");
            if (!existed)
            {
                rounds = Enumerable.Repeat((sbyte)-2, prop.internalCapacity).ToList();
                fireMode = prop.fireModes.Min();
                if (Plugin.togetherMpEnabled)
                    MpStartOp();
           else if (!prop.spawnUnracked)
                    Rack(manual: true);
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
                racked = !prop.spawnUnracked;
                if (racked)
                    Sound.Play(prop.rackAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                UpdateSprite();
            }
            InvokeRepeating("SyncIfHostt", Plugin.sett.gunSyncRate, Plugin.sett.gunSyncRate);
        }

        public bool IsReady()
        {
            return !IsJammed && !racked && 0 != fireMode && rounds.Count > 0 && 0 <= rounds[0] && 0.005f < it.condition;
        }

        private bool FireRound(Body body)
        {
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
            {
                rounds[0] = -2;
                spentAmmoTypes.Remove(0);
            }
            else
            {
                spentAmmoTypes[0] = rounds[0];
                rounds[0] = -1;
            }
            if (1 == fireMode)
            {
                gasTime = 0f;
                if (Plugin.togetherMpEnabled)
                    SyncIfHost();
            }
       else {
                if (0f < prop.desiredGasTime || fireMode == BurstFireMode)
                {
                    gasTime = fireMode == BurstFireMode ? Mathf.Max(0.05f, prop.desiredGasTime) : prop.desiredGasTime;
                    if (4 != prop.feedType && !(prop.feedType == 3 && fireMode == BurstFireMode))
                        Rack(manual: false);
                    if (Plugin.togetherMpEnabled)
                        SyncIfHost();
                }
           else {
                    RotateChambers();
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
            if (manual)
            {
                StopFiring();
                gasTime = 0f;
            }
            if (IsJammed) return;
            racked = !racked;
            if (manual)
            {
                if (racked)
                    Sound.Play(prop.rackAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
           else     Sound.Play(prop.unrackAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            }
            if (2 >= prop.feedType)
            {
                if (racked)
                    EjectRound(0);
                else if (-2 == rounds[0])
                {
                    RotateChambers();
                    if (rounds[0] >= 0 && JamChance())
                        BeginJam(GunJam.Feed, 0, rounds[0]);
                }
            }
            if (IsJammed) return;

            /*
             The ejection runs on the host and syncs to clients, but there’s a bug: `RemoveMag(null)` falls back to `PlayerCamera.main.body` on the host, so a client’s ejected magazine could go into the host’s inventory.
             The rack request needs to pass the requesting player’s body through to `RemoveMag`. Live MP hasn’t been tested yet.
            */
            if (manual && prop.removeMagazineOnRack)
                RemoveMag(null, playSound: false);
            if (Plugin.togetherMpEnabled)
            {
                if (manual)
                    SyncIfHost((byte)(5 | (prop.removeMagazineOnRack ? SilentMagazineRemoval : 0)));
           else     SyncIfHost();
            }
            UpdateSprite();
        }

        public void EjectRound(int index, bool allowJam = true)
        {
            if (IsJammed) return;
            if (allowJam && rounds[index] == -1 && JamChance())
            {
                sbyte ammo = spentAmmoTypes.TryGetValue(index, out var spent) ? spent : (sbyte)-1;
                BeginJam(GunJam.Ejection, index, ammo);
                return;
            }
            if (-2 == rounds[index])
                return;
            string round = "casing";
            if (0 <= rounds[index])
                round = prop.ammoTypes[rounds[index]].round;
            GameObject dropped = round == "casing" ? CreateSpentCasing(index)
                : Utils.Create(round, transform.position, transform.rotation.eulerAngles.z);
            dropped.GetComponent<Rigidbody2D>().velocity = transform.up * 12f;
            if (round != "casing")
                NotifyEjectionVisual(rounds[index], rounds[index]);
            rounds[index] = -2;
        }

        private GameObject CreateSpentCasing(int chamber)
        {
            GameObject casing = Utils.Create("casing", transform.position, transform.rotation.eulerAngles.z);
            sbyte visualAmmoIndex = -1;
            if (spentAmmoTypes.TryGetValue(chamber, out sbyte ammoIndex))
            {
                visualAmmoIndex = ammoIndex;
                if (ammoIndex >= 0 && ammoIndex < prop.ammoTypes.Count &&
                    prop.ammoTypes[ammoIndex].loadedCasingSprite != null)
                    CasingAppearance.Attach(casing, it.id, ammoIndex);
                spentAmmoTypes.Remove(chamber);
            }
            NotifyEjectionVisual(-1, visualAmmoIndex);
            return casing;
        }

        internal void ShowEjectionVisual(int round, int ammoIndex)
        {
            var manager = GunMinigame.MinigameManager.instance;
            if (!GunMinigame.Plugin.useMinigame || manager == null || !manager.active ||
                !ReferenceEquals(manager.gun, this)) return;
            Sprite sprite = round == -1 && ammoIndex >= 0 && ammoIndex < prop.ammoTypes.Count
                ? prop.ammoTypes[ammoIndex].loadedMinigameCasingSprite : null;
            manager.CreateCasing(round, sprite);
        }

        private void NotifyEjectionVisual(sbyte round, sbyte ammoIndex)
        {
            ShowEjectionVisual(round, ammoIndex);
            if (!Plugin.togetherMpEnabled) return;
            SendEjectionVisual(round, ammoIndex);
        }

        // TODO This also to be rewritten, maybe even combine with casing apperance
        private void SendEjectionVisual(sbyte round, sbyte ammoIndex)
        {
            if (!Together.Net.IsServer) return;
            var tracker = GetMpTracker(false) as Together.SyncInfoGameObjectTracker;
            if (tracker == null) return;
            var writer = Together.Multiplayer.CreateNamedWriter(MSGID_EJECT_VISUALS);
            writer.Put((ushort)tracker.syncId);
            writer.Put(round);
            writer.Put(ammoIndex);
            Together.Net.Server_SendToClients(LiteNetLib.DeliveryMethod.ReliableUnordered, in writer,
                Together.ServerMain.AllClientIdsExceptHost);
        }


        private void RotateChambers()
        {
            if (rounds.Count <= 1) return;
            Plugin.ShiftLeft(ref rounds);
            spentAmmoTypes = spentAmmoTypes.ToDictionary(
                entry => (entry.Key + rounds.Count - 1) % rounds.Count, entry => entry.Value);
        }

        public bool CanFitMag(MagJson newMag)
        {
            if (prop == null || newMag == null) return false;
            if (newMag.pseudoMagazine && !racked) return false;
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
            if (IsJammed) return false;
            if (Plugin.togetherMpEnabled && RequestHostIfClient(2, item))
                return true;
            if (item.TryGetComponent<RshMag>(out var newMag))
            {
                if (newMag.prop == null || newMag.rounds == null || newMag.rounds.Count == 0)
                    return false;
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
                return true;
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
                                GameObject go = CreateSpentCasing(casing);
                                rounds[casing] = i;
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
            => RemoveMag(body, playSound: true);

        private void RemoveMag(Body body, bool playSound)
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(3))
                return;
            StopFiring();
            if (3 <= prop.feedType && racked)
            {
                if (playSound)
                    Sound.Play(prop.removeMagAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                for (int i = 0; i < rounds.Count(); i++)
                    EjectRound(i);
                if (Plugin.togetherMpEnabled)
                    SyncIfHost(playSound ? (byte)1 : SilentMagazineRemoval);
            }
       else if (!string.IsNullOrEmpty(mag))
            {
                string magazineId = mag;
                var magazineRounds = rounds.Skip(1).ToList();
                bool discard = Plugin.MagazineDefinitions.TryGetValue(magazineId, out var definition) &&
                    definition.pseudoMagazine && !magazineRounds.Any(round => round >= 0);
                // Detach immediately so repeated requests cannot eject the same magazine twice.
                rounds = new List<sbyte> { rounds[0] };
                spentAmmoTypes = spentAmmoTypes.Where(entry => entry.Key == 0)
                    .ToDictionary(entry => entry.Key, entry => entry.Value);
                mag = "";
                if (playSound)
                    Sound.Play(prop.removeMagAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
                UpdateSprite();
                if (Plugin.togetherMpEnabled)
                    SyncIfHost(playSound ? (byte)0 : SilentMagazineRemoval);
                if (discard) return;

                if (null == body)
                    body = PlayerCamera.main.body;
                GameObject go = Utils.Create(magazineId, transform.position, 0f);
                MagazineCreated?.Invoke(go);
                // CUCoreLib installs custom components as the item starts up.
                var _ = RemoveMagPart2WhenCuCore(body, go, magazineRounds);
            }
        }

        private async Task RemoveMagPart2WhenCuCore(Body body, GameObject go, List<sbyte> magazineRounds)
        {
            RshMag rshMag = null;
            for (int i = 0; i < 100 && null == rshMag; i++)
            {
                await Task.Delay(100);
                if (go == null) return;
                rshMag = go.GetComponent<RshMag>();
            }
            if (rshMag == null)
            {
                Debug.LogError("[NewFirearms] Ejected magazine did not initialize its RshMag component.");
                return;
            }
            rshMag.rounds = magazineRounds;
            rshMag.existed = true;
            if (body != null)
                body.AutoPickUpItem(go.GetComponent<Item>());
            rshMag.UpdateSprite();
            if (Plugin.togetherMpEnabled)
                rshMag.SyncIfHost();
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

        public void ToggleFireMode()
        {
            if (Plugin.togetherMpEnabled && RequestHostIfClient(4))
                return;

            StopFiring();
            Sound.Play(prop.toggleFireModeAudio, transform.position, twoDimensional: PlayerCamera.main.body.HoldingItem(it));
            fireMode = prop.fireModes[(prop.fireModes.IndexOf(fireMode) + 1) % prop.fireModes.Count];
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
            composedSprite = ItemLoader.TextureToSprite(composedTexture, prop.ppu);
            gameObject.GetComponent<SpriteRenderer>().sprite = composedSprite;
            CUCoreLib.Helpers.CUCoreUtils.SetItemSprite(it, composedSprite);
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
        * High bit - Suppress magazine removal audio
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
            GunNetwork.WriteJamState(this, writer);

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


        public static readonly Dictionary<byte, string> ACTIONS_NAME = new Dictionary<byte, string>
        {
            {0, "newfirearms.action.shoot"},
            {1, "newfirearms.action.rack"},
            {2, "newfirearms.action.chamber_load"},
            {3, "newfirearms.action.remove_mag"},
            {4, "newfirearms.action.toggle_fire_mode"},
            {5, "newfirearms.action.stop_shooting"},
            {6, "newfirearms.action.clear_jam"},
        };
    }
}
