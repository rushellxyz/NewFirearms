using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NewFirearms
{
    public class FirearmHandlingCourse : TutorialCourse
    {
        public const int LEFT_PAW = 14;
        internal const string BestTimeKey = "NewFirearms.FirearmHandling.BestSurvivalSeconds";
        private static readonly string[] Weapons = { "newfirearms.m1911", "newfirearms.mp5", "newfirearms.saiga" };
        private static readonly string[] EnemyTypes = { "wallbiter", "thornbackyoung" };
        private readonly List<GameObject> spawned = new List<GameObject>();
        private readonly List<GameObject> medicine = new List<GameObject>();
        private readonly List<BuildingEntity> enemies = new List<BuildingEntity>();
        private readonly List<FirearmCourseTarget> targets = new List<FirearmCourseTarget>();
        private readonly List<GunJson> supplyWeapons = new List<GunJson>();
        private readonly HashSet<string> ammunitionIds = new HashSet<string>();
        private TutorialHandler handler;
        private Body trainee;
        private FirearmCourseResults results;
        private bool previousForceAlive, previousEscapeCheck, previousForceWalk;
        private bool initialized, replenish, survival, finished;
        private bool supplying;
        private float survivalSeconds, nextSupply, nextEnemyWave;
        private int survivalWave;

        public override string LocaleName() => "newfirearms.firearmhandlingcourse";

        public override IEnumerator Sequence()
        {
            handler = tutorial;
            trainee = body;
            previousForceAlive = handler.forcePlayerAlive;
            previousEscapeCheck = handler.checkForEscape;
            previousForceWalk = trainee.forceWalk;
            initialized = true;
            handler.forcePlayerAlive = false;
            // The sandbox lies outside vanilla tutorial bounds. Check turrets separately.
            handler.checkForEscape = false;
            trainee.forceWalk = true;
            trainee.skills.AddExp(2, 1000f);
            foreach (FirearmCourseResults previous in FindObjectsOfType<FirearmCourseResults>())
                Destroy(previous.gameObject);
            results = new GameObject("FirearmCourseResults").AddComponent<FirearmCourseResults>();
            DontDestroyOnLoad(results.gameObject);
            yield return Say(ModLocale.Course("dialogue.intro"));
            yield return MedicalLesson();
            yield return EquipContainer("fannypack", "torsofront");
            yield return EquipContainer("bandolier", "bandolier");
            yield return WeaponLesson();
            yield return MoveToRange();
            replenish = true;
            yield return TargetPractice();
            yield return CombatPractice();
            yield return BeginSurvival();
        }

        private IEnumerator Say(string text)
        {
            handler.Speak(text);
            // The tutorial speaker may stop updating while the player is in the
            // sandbox. Its completion flag must not hold the course indefinitely.
            float remaining = Mathf.Max(6f, text.Length / 18f + 6f);
            yield return null;
            while (handler != null && !handler.finishedTalking && remaining > 0f)
            {
                if (!PauseHandler.paused && Time.timeScale > 0f)
                    remaining -= Mathf.Min(Time.unscaledDeltaTime, 0.2f);
                yield return null;
            }
            if (handler != null && !handler.finishedTalking)
            {
                Debug.LogWarning("[NewFirearms] Course speaker did not finish; continuing after the dialogue timeout.");
                handler.speakerTalker.Skip();
            }
        }

        private static IEnumerator WaitUnpausedSeconds(float seconds)
        {
            while (seconds > 0f)
            {
                yield return null;
                if (!PauseHandler.paused && Time.timeScale > 0f)
                    seconds -= Mathf.Min(Time.unscaledDeltaTime, 0.2f);
            }
        }

        private IEnumerator MedicalLesson()
        {
            yield return FetchInstructorItem(Weapons[0], trainee.transform.position + Vector3.right * 4f + Vector3.up * 3f);
            GameObject demonstration = handler.lastSpawnedObject;
            yield return WaitForGun(demonstration);
            RshGun gun = demonstration.GetComponent<RshGun>();
            Limb leg = trainee.limbs[LEFT_PAW];
            Coroutine aiming = StartCoroutine(AimInstructorItem(demonstration.transform, leg.transform));
            yield return WaitUnpausedSeconds(1f);
            Sound.Play(gun.prop.shootAudio, demonstration.transform.position);
            ShootManager.DrawVisuals(new ShootVisuals
            {
                start = demonstration.transform.position + demonstration.transform.up * gun.prop.barrelOffset,
                ends = new List<Vector2> { leg.transform.position },
                hitNumbers = new List<(Vector2, ushort)>(),
                knockbacks = new List<(ushort, Vector2)>()
            });
            ShootManager.InduceDamage(leg, gun.prop.ammoTypes[0].playerDamage, 1f);
            leg.MendBone();
            leg.skinHealth = Mathf.Max(leg.skinHealth, 60f);
            leg.muscleHealth = Mathf.Max(leg.muscleHealth, 70f);
            leg.bleedAmount = 15f;
            leg.shrapnel = 5;
            yield return WaitUnpausedSeconds(1f);
            handler.handPos = new Vector2(10f, 27f);
            yield return new WaitUntil(() => handler.handPos == handler.handPosCurrent);
            StopCoroutine(aiming);
            handler.Ungrab();
            Destroy(demonstration);
            yield return Say(ModLocale.Course("dialogue.medical"));
            yield return FetchInstructorItem("medkit", trainee.transform.position + Vector3.up * 4f);
            GameObject kit = handler.lastSpawnedObject;
            medicine.Add(kit);
            yield return null;
            Container container = kit.GetComponent<Container>();
            if (container == null)
                throw new InvalidOperationException("[NewFirearms] Course medkit is missing its Container component.");
            // :tourniqet:
            // Ha, didnt thoght that you can modify maximum holding weight of individual containers
            container.maxWeight = Mathf.Max(container.maxWeight, 8f);
            container.maxWeightPerItem = Mathf.Max(container.maxWeightPerItem, 3f);
            container.tagRestriction = new string[0];
            container.itemsVisible = false;
            foreach (string id in new[] { "tweezers", "rippeddressing", "bandage", "antisepticmush", "tourniquet", "icepack" })
                GiveMedicine(id, container);
            yield return WaitUnpausedSeconds(0.5f);
            handler.Ungrab();
            handler.showHealthPanelHighlight = true;
            yield return new WaitUntil(() => leg.shrapnel == 0 && leg.bleedAmount < 0.1f);
            handler.showHealthPanelHighlight = false;
            yield return Say(ModLocale.Course("dialogue.medicalComplete", WoundView.specimenId));
        }

        private IEnumerator FetchInstructorItem(string id, Vector2 destination)
        {
            // Same dispenser and arm movement as TutorialCourse.GiveItemToBody,
            // but retain the grip so we can aim the gun or fill the medkit first.
            handler.objectToCreate = id;
            yield return new WaitUntil(() => string.IsNullOrEmpty(handler.objectToCreate) &&
                handler.lastSpawnedObject != null && handler.grabInfo.grabbed == handler.lastSpawnedObject.transform &&
                handler.lastSpawnedObject.GetComponent<Item>().id == id);
            TrackSpawned(handler.lastSpawnedObject);
            StartCoroutine(FollowInstructorHand(handler.lastSpawnedObject.GetComponent<Item>()));
            handler.handPos.y -= 15f;
            yield return new WaitUntil(() => handler.handPos == handler.handPosCurrent);
            handler.handPos = destination;
            yield return new WaitUntil(() => handler.handPos == handler.handPosCurrent);
        }

        private IEnumerator FollowInstructorHand(Item item)
        {
            SpriteRenderer visual = item.GetComponent<SpriteRenderer>();
            while (handler != null && item != null && handler.grabInfo.grabbed == item.transform)
            {
                // Item.Update may disable physics at the offscreen dispenser, where
                // TutorialHandler's Rigidbody2D.MovePosition cannot carry the item.
                item.rb.position = handler.handPosCurrent;
                item.transform.position = handler.handPosCurrent;
                if (visual != null) visual.enabled = true;
                yield return null;
            }
        }

        private IEnumerator AimInstructorItem(Transform item, Transform target)
        {
            while (item != null && target != null && handler.grabInfo.grabbed == item)
            {
                Vector2 aim = target.position - item.position;
                item.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
                yield return null;
            }
        }

        private IEnumerator EquipContainer(string id, string slot)
        {
            if (trainee.GetWearableBySlotID(slot) != null) yield break;
            GameObject obj = Spawn(id, trainee.transform.position);
            yield return null;
            trainee.WearWearable(obj.GetComponent<Item>());
        }

        private IEnumerator WaitForGun(GameObject obj)
        {
            float timeout = 10f;
            while (obj != null && (obj.GetComponent<RshGun>() == null || !obj.GetComponent<RshGun>().existed) && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (obj == null || obj.GetComponent<RshGun>() == null || !obj.GetComponent<RshGun>().existed)
                throw new InvalidOperationException("[NewFirearms] Course gun failed to initialize through CUCoreLib.");
            RshGun gun = obj.GetComponent<RshGun>();
            gun.MagazineCreated += TrackSpawned;
            ammunitionIds.Add(FindMagazine(gun.prop));
            foreach (AmmoInfo ammo in gun.prop.ammoTypes) ammunitionIds.Add(ammo.round);
        }

        private IEnumerator WeaponLesson()
        {
            GameObject obj = Spawn(Weapons[0], trainee.transform.position);
            yield return WaitForGun(obj);
            RshGun gun = obj.GetComponent<RshGun>();
            gun.rounds = Enumerable.Repeat((sbyte)-2, gun.prop.internalCapacity).ToList();
            gun.mag = "";
            gun.racked = false;
            gun.UpdateSprite();
            trainee.AutoPickUpItem(obj.GetComponent<Item>());
            yield return Say(ModLocale.Course("dialogue.rack"));
            yield return new WaitUntil(() => gun == null || gun.racked);
            if (gun == null) yield break;
            yield return SupplyMagazine(gun.prop, 0);
            yield return Say(ModLocale.Course("dialogue.insert"));
            yield return new WaitUntil(() => gun == null || !string.IsNullOrEmpty(gun.mag));
            if (gun == null) yield break;
            yield return new WaitUntil(() => gun == null || !gun.racked);
            if (gun == null) yield break;
            yield return Say(ModLocale.Course("dialogue.eject"));
            yield return new WaitUntil(() => gun == null || string.IsNullOrEmpty(gun.mag));
            if (gun == null) yield break;
            yield return SupplyMagazine(gun.prop, 0);
            yield return Say(ModLocale.Course("dialogue.reinsert"));
            yield return new WaitUntil(() => gun == null || !string.IsNullOrEmpty(gun.mag));
            if (gun == null) yield break;
            GameObject empty = Spawn(FindMagazine(gun.prop), trainee.transform.position + Vector3.right);
            yield return InitializeMagazine(empty, 0);
            PutInWearable(empty.GetComponent<Item>(), "torsofront");
            for (int i = 0; i < 6; i++) GiveAmmo("9mmround");
            RshMag mag = empty.GetComponent<RshMag>();
            yield return Say(ModLocale.Course("dialogue.looseRound"));
            yield return new WaitUntil(() => mag == null || mag.TotalRounds() > 0);
        }

        private IEnumerator MoveToRange()
        {
            yield return Say(ModLocale.Course("dialogue.range"));
            GlobalDark.main.Darken();
            yield return new WaitUntil(() => !GlobalDark.main.IsDarkening());
            trainee.transform.position = Vector3.down * 106f;
            trainee.rb.velocity = Vector2.zero;
            PlayerCamera.main.transform.position = trainee.transform.position;
            handler.handPos = trainee.transform.position + Vector3.up * 8f;
        }

        private IEnumerator TargetPractice()
        {
            foreach (string id in Weapons)
            {
                replenish = false;
                yield return new WaitUntil(() => !supplying);
                ClearTargets();
                RemoveCourseGuns();
                RemoveCourseAmmo();
                supplyWeapons.Clear();
                GameObject obj = Spawn(id, trainee.transform.position);
                yield return WaitForGun(obj);
                RshGun gun = obj.GetComponent<RshGun>();
                supplyWeapons.Add(gun.prop);
                trainee.AutoPickUpItem(obj.GetComponent<Item>());
                yield return SupplyMagazine(gun.prop, 0);
                yield return SupplyMagazine(gun.prop, 1);
                replenish = true;
                Vector2 origin = Vector2.down * 106f;
                for (int i = 0; i < 3; i++) targets.Add(FirearmCourseTarget.Create(FindFloor(origin + Vector2.right * (6f + i * 4f)), gun));
                yield return Say(ModLocale.Course("dialogue.targets", gun.prop.fullName));
                yield return new WaitUntil(() => targets.All(target => target == null || target.Destroyed));
            }
            ClearTargets();
        }

        private IEnumerator CombatPractice()
        {
            replenish = false;
            yield return new WaitUntil(() => !supplying);
            supplyWeapons.Clear();
            RemoveCourseGuns();
            RemoveCourseAmmo();
            foreach (string id in Weapons)
            {
                GameObject obj = Spawn(id, trainee.transform.position + Vector3.right * 2f);
                yield return WaitForGun(obj);
                GunJson prop = obj.GetComponent<RshGun>().prop;
                supplyWeapons.Add(prop);
                yield return SupplyMagazine(prop, 0);
            }
            replenish = true;
            yield return Say(ModLocale.Course("dialogue.combat"));
            for (int wave = 0; wave < 3; wave++)
            {
                Debug.Log($"[NewFirearms] Course practice wave {wave + 1}/3 starting.");
                trainee.forceWalk = false;
                SpawnWave(2 + wave, wave);
                yield return new WaitUntil(() => LivingEnemies() == 0);
                trainee.forceWalk = true;
                Debug.Log($"[NewFirearms] Course practice wave {wave + 1}/3 cleared.");
                yield return Say(ModLocale.Course("dialogue.waveCleared", wave + 1));
                yield return WaitUnpausedSeconds(5f);
            }
        }

        private IEnumerator BeginSurvival()
        {
            replenish = false;
            supplyWeapons.Clear();
            // Let any already-started refill finish before replacing the training stock.
            yield return new WaitUntil(() => !supplying);
            RemoveCourseGuns();
            RemoveCourseAmmo();
            ClearEnemies();
            yield return Say(ModLocale.Course("dialogue.survival"));
            trainee.transform.position = Vector3.down * 106f;
            trainee.rb.velocity = Vector2.zero;
            PlayerCamera.main.transform.position = trainee.transform.position;
            foreach (string id in Weapons)
            {
                GameObject obj = Spawn(id, trainee.transform.position + Vector3.right * UnityEngine.Random.Range(-2f, 2f));
                yield return WaitForGun(obj);
                GunJson prop = obj.GetComponent<RshGun>().prop;
                for (int i = 0; i < 8; i++) yield return SupplyMagazine(prop, i);
                for (int i = 0; i < 60; i++) GiveAmmo(prop.ammoTypes[0].round);
            }
            foreach (GameObject obj in medicine) if (obj != null) Destroy(obj);
            foreach (Item item in trainee.GetAllItemsThorough().ToArray())
                if (item != null && (item.Stats.category == "medical" || item.id == "autopump" || item.id == "medkit"))
                {
                    if (item.Stats.wearable) trainee.DropWearable(item);
                    Destroy(item.gameObject);
                }
            RestoreTrainingHealth();
            handler.forcePlayerAlive = false;
            survivalSeconds = 0f;
            survivalWave = 0;
            nextEnemyWave = 0f;
            trainee.forceWalk = false;
            survival = true;
            handler.Speak(ModLocale.Course("dialogue.survivalStart"));
        }

        private void Update()
        {
            if (!initialized || finished || trainee == null) return;
            if (InterruptForTurrets()) return;
            if (survival)
            {
                if (!trainee.alive) { FinishSurvival("died"); return; }
                if (PauseHandler.paused || Time.timeScale <= 0f) return;
                survivalSeconds += Time.unscaledDeltaTime;
                nextEnemyWave -= Time.deltaTime;
                if (nextEnemyWave <= 0f)
                {
                    SpawnWave(Mathf.Min(2 + survivalWave / 2, 6), survivalWave);
                    survivalWave++;
                    nextEnemyWave = Mathf.Max(6f, 20f - survivalWave);
                }
            }
            if (replenish && !supplying && Time.time >= nextSupply)
            {
                nextSupply = Time.time + 4f;
                StartCoroutine(ReplenishAmmo());
            }
        }

        private bool InterruptForTurrets()
        {
            if (handler == null || handler.turrets == null) return false;
            foreach (TurretScript turret in handler.turrets)
            {
                // Match TutorialHandler: firing or destroying a turret starts escape.
                if (turret != null && turret.timeSinceFired >= 5f) continue;
                TutorialHandler owner = handler;
                // Destroy is deferred, so restore our overrides before EscapeCourse starts.
                CleanupCourse();
                owner.escapeSequenceActive = true;
                owner.EndCourse();
                owner.StartCourse<EscapeCourse>();
                return true;
            }
            return false;
        }

        private IEnumerator ReplenishAmmo()
        {
            supplying = true;
            try
            {
                foreach (GameObject obj in spawned)
                    if (obj != null && obj.TryGetComponent<RshMag>(out var empty) && empty.existed && empty.TotalRounds() == 0)
                        Destroy(obj);
                foreach (GunJson gun in supplyWeapons.ToArray())
                {
                    if (!replenish) yield break;
                    string id = FindMagazine(gun);
                    int count = AvailableItems().Count(item => item.id == id && item.TryGetComponent<RshMag>(out var mag) && mag.TotalRounds() > 0);
                    if (count < 3) yield return SupplyMagazine(gun, count);
                    int loose = AvailableItems().Count(item => item.id == gun.ammoTypes[0].round);
                    for (int i = loose; i < 12 && replenish; i++) GiveAmmo(gun.ammoTypes[0].round);
                }
            }
            finally { supplying = false; }
        }

        private IEnumerable<Item> AvailableItems()
        {
            return trainee.GetAllItemsThorough().Concat(spawned.Where(obj => obj != null && Vector2.Distance(obj.transform.position, trainee.transform.position) < 12f)
                .SelectMany(obj => obj.GetComponentsInChildren<Item>())).Where(item => item != null).Distinct();
        }

        private string FindMagazine(GunJson gun)
        {
            foreach (var entry in Plugin.MagazineDefinitions)
                if (entry.Value.magazineType.Intersect(gun.magazineType).Any()) return entry.Key;
            throw new InvalidOperationException("[NewFirearms] Course could not find a magazine for " + gun.fullName);
        }

        private IEnumerator InitializeMagazine(GameObject obj, int amount)
        {
            float timeout = 10f;
            RshMag mag = null;
            while (obj != null && timeout > 0f)
            {
                mag = obj.GetComponent<RshMag>();
                if (mag != null && mag.existed) break;
                timeout -= Time.deltaTime;
                yield return null;
            }
            if (mag == null || !mag.existed) throw new InvalidOperationException("[NewFirearms] Course magazine failed to initialize through CUCoreLib.");
            mag.Fill(0, amount < 0 ? mag.prop.capacity : amount);
        }

        private IEnumerator SupplyMagazine(GunJson gun, int offset)
        {
            GameObject obj = Spawn(FindMagazine(gun), trainee.transform.position + Vector3.right * (1f + offset % 4));
            yield return InitializeMagazine(obj, -1);
            PutInWearable(obj.GetComponent<Item>(), "torsofront");
        }

        private void GiveAmmo(string id)
        {
            Item item = Spawn(id, trainee.transform.position + Vector3.right).GetComponent<Item>();
            PutInWearable(item, "bandolier");
        }

        private void PutInWearable(Item item, string slot)
        {
            Item wearable = trainee.GetWearableBySlotID(slot);
            if (wearable != null && wearable.TryGetComponent<Container>(out var container)) container.LoadItem(item);
        }

        private void GiveMedicine(string id, Container container)
        {
            Vector2 position = container != null ? (Vector2)container.transform.position : (Vector2)trainee.transform.position + Vector2.right * 2f;
            GameObject obj = Spawn(id, position);
            medicine.Add(obj);
            if (container != null)
            {
                container.LoadItem(obj.GetComponent<Item>());
                if (obj.transform.parent != container.transform)
                    throw new InvalidOperationException("[NewFirearms] Could not put course supply into medkit: " + id);
            }
        }

        private GameObject Spawn(string id, Vector2 position)
        {
            GameObject obj = Utils.Create(id, position, 0f);
            if (obj == null) throw new InvalidOperationException("[NewFirearms] Course could not spawn " + id);
            TrackSpawned(obj);
            return obj;
        }

        private void TrackSpawned(GameObject obj)
        {
            spawned.RemoveAll(entry => entry == null);
            spawned.Add(obj);
        }

        private static Vector2 FindFloor(Vector2 near)
        {
            RaycastHit2D hit = Physics2D.Raycast(near + Vector2.up * 4f, Vector2.down, 12f, LayerMask.GetMask("Ground"));
            return hit.collider != null ? hit.point + Vector2.up * 1.5f : near;
        }

        private void SpawnWave(int count, int wave)
        {
            LivingEnemies();
            // Keep the active population bounded without stopping the endless spawns.
            count = Mathf.Min(count, 16);
            while (enemies.Count + count > 16)
            {
                BuildingEntity oldest = enemies[0];
                enemies.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }
            for (int i = 0; i < count; i++)
            {
                float side = (i + wave) % 2 == 0 ? 1f : -1f;
                Vector2 near = (Vector2)trainee.transform.position + Vector2.right * side * UnityEngine.Random.Range(9f, 16f);
                Vector2 position = FindFloor(near);
                // These prefabs use a 4.8-unit floor offset in WorldGeneration.DistributeEntities.
                GameObject obj = Spawn(EnemyTypes[(i + wave) % EnemyTypes.Length], position + Vector2.up * 3.3f);
                BuildingEntity enemy = obj.GetComponent<BuildingEntity>();
                if (enemy == null) throw new InvalidOperationException("[NewFirearms] Course enemy has no BuildingEntity: " + obj.name);
                enemy.itemsDropOnDestroy = new ItemDrop[0];
                enemy.alwaysDrop = new ItemDrop[0];
                enemy.guaranteedDropAmount = 0;
                enemy.ignoreBodyOptimize = true;
                RaycastHit2D ground = Physics2D.Raycast(position + Vector2.up, Vector2.down, 8f, LayerMask.GetMask("Ground"));
                if (ground.collider != null)
                    enemy.blockPlacedOn = WorldGeneration.world.WorldToBlockPos(ground.point + Vector2.down * 0.5f);
                else
                    enemy.requireGround = false;
                if (obj.TryGetComponent<SpiderHandler>(out var spider))
                {
                    spider.stunTime = Mathf.Max(spider.stunTime, 5f);
                    spider.aggro = 1f;
                    spider.seeDistance = 40f;
                    spider.retreatHealth = -1f;
                }
                enemies.Add(enemy);
            }
        }

        private int LivingEnemies()
        {
            enemies.RemoveAll(enemy => enemy == null || enemy.health < 0.5f);
            return enemies.Count;
        }

        private void RestoreTrainingHealth()
        {
            trainee.bloodVolume = trainee.bloodOxygen = trainee.brainHealth = 100f;
            trainee.shock = trainee.fibrillationProgress = 0f;
            trainee.fibrillationForced = false;
            trainee.internalBleeding = trainee.venomTotal = trainee.strokeAmount = trainee.septicShock = 0f;
            trainee.heartRate = 80f;
            trainee.bloodPressure = 120f;
            trainee.consciousness = 100f;
            trainee.hunger = trainee.thirst = trainee.energy = trainee.stamina = 100f;
            foreach (Limb limb in trainee.limbs)
            {
                limb.bleedAmount = 0f;
                limb.shrapnel = 0;
                limb.skinHealth = limb.muscleHealth = 100f;
                limb.pain = 0f;
                limb.infected = false;
                limb.infectionAmount = 0f;
                limb.MendBone();
                limb.UnDislocate();
            }
        }

        private void RemoveCourseGuns()
        {
            foreach (GameObject obj in spawned) if (obj != null && obj.GetComponent<RshGun>() != null) Destroy(obj);
        }

        private void RemoveCourseAmmo()
        {
            foreach (GameObject obj in spawned)
                if (obj != null && (obj.GetComponent<RshMag>() != null ||
                    obj.TryGetComponent<Item>(out var item) && ammunitionIds.Contains(item.id))) Destroy(obj);
        }

        private void ClearTargets()
        {
            foreach (FirearmCourseTarget target in targets) if (target != null) Destroy(target.gameObject);
            targets.Clear();
        }

        private void ClearEnemies()
        {
            foreach (BuildingEntity enemy in enemies) if (enemy != null) Destroy(enemy.gameObject);
            enemies.Clear();
        }

        private void FinishSurvival(string reason)
        {
            if (!survival || finished) return;
            finished = true;
            survival = false;
            if (trainee != null) trainee.forceWalk = true;
            ClearEnemies();
            float best = Mathf.Max(PlayerPrefs.GetFloat(BestTimeKey, 0f), survivalSeconds);
            PlayerPrefs.SetFloat(BestTimeKey, best);
            PlayerPrefs.Save();
            if (results != null) results.Show(ModLocale.Course("result." + reason), survivalSeconds, best);
        }

        private void OnDestroy()
        {
            CleanupCourse();
        }

        private void CleanupCourse()
        {
            if (!initialized) return;
            initialized = false;
            FinishSurvival("leftCourse");
            replenish = false;
            if (results != null && !finished) Destroy(results.gameObject);
            if (handler != null)
            {
                handler.objectToCreate = "";
                if (handler.grabInfo.grabbed != null && spawned.Any(obj => obj != null && obj.transform == handler.grabInfo.grabbed))
                    handler.Ungrab();
                handler.forcePlayerAlive = previousForceAlive;
                handler.checkForEscape = previousEscapeCheck;
                handler.showHealthPanelHighlight = handler.showUiHighlight = false;
            }
            if (trainee != null) trainee.forceWalk = previousForceWalk;
            ClearTargets();
            ClearEnemies();
            foreach (GameObject obj in spawned) if (obj != null) Destroy(obj);
        }
    }
}
