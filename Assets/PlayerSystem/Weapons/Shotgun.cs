using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public class Shotgun : ChargedWeapon
    {
        [Header("Shotgun")]
        [SerializeField, Min(1)] private int pellets = 8;
        [SerializeField] private float pelletCoefficient = 18f;
        [SerializeField] private float spreadDegrees = 20f;
        [SerializeField] private float chargingSeconds = 5f;
        [SerializeField] private float energyPerChargedPellet = 6f;
        [SerializeField] private float targetRadius = 15f;
        [SerializeField] private float ultimateCoefficientPerGun = 400f;
        [SerializeField, Range(0f, 1f)] private float minimumTimeScale = 0f;
        [Header("Ultimate choreography (real seconds)")]
        [SerializeField, Min(0.1f)] private float slowSeconds = 0.85f;
        [SerializeField, Min(0.1f)] private float flightSeconds = 1.65f;
        [SerializeField, Min(1f)] private float flightArcHeight = 4.2f;
        [SerializeField, Range(0, 2)] private int flightRollTurns = 1;
        [SerializeField, Min(0.05f)] private float aimHoldSeconds = 0.22f;
        [SerializeField, Min(0.05f)] private float recoverySeconds = 0.22f;
        [SerializeField, Min(1.5f)] private float standOffDistance = 2.6f;
        [SerializeField, Min(0)] private float normalVolleyEnergy = 0.5f;
        private float chargingRemaining;
        private float previousTimeScale = 1f;
        private TimeScaler timeScaler;
        private bool performingUltimate;
        private Player lockedPlayer;
        private bool bodyWasVisible;
        private string ultimatePhase;
        public bool IsPerformingUltimate => performingUltimate;
        private readonly List<GameObject> ultimateGuns = new();
        protected override bool CanAct => base.CanAct && !performingUltimate;

        protected override string HudTitle => "샷건";
        protected override string HudRole => "E 이후 명중한 탄환마다 필살기 에너지 충전";
        protected override string SkillTitle => "에너지 충전";
        protected override string UltimateTitle => "시간 정지 · 집중 사격";
        protected override string HudBlock => performingUltimate ? "필살기 시전 중" : "";
        private float hudTargetCheck;
        private bool hudHasTarget;

        public override void WriteHud(WeaponHudState state)
        {
            base.WriteHud(state);
            if (player && Time.unscaledTime >= hudTargetCheck)
            {
                hudTargetCheck = Time.unscaledTime + 0.2f;
                hudHasTarget = Enemies(Origin, targetRadius).Count > 0;
            }
            state.Entries[1] = WeaponHudEntry.Ultimate(UltimateTitle, Energy, MaxEnergy, ultimateCost,
                performingUltimate ? ultimatePhase : !hudHasTarget ? "사거리 내 대상 없음" : "", performingUltimate);
            if (chargingRemaining > 0)
                state.Entries.Add(new WeaponHudEntry("", "충전 탄환", $"{chargingRemaining:0.0}초",
                    $"명중 탄환당 에너지 +{energyPerChargedPellet:0}", chargingRemaining / Mathf.Max(0.1f, chargingSeconds),
                    WeaponHudAccent.Effect, active: true, compact: true));
        }

        private void Reset()
        {
            displayName = "Shotgun";
            fireInterval = 0.7f;
            skillRechargeSeconds = 5f;
        }

        public override void fire()
        {
            if (!BeginFire()) return;
            int count = Mathf.Max(1, pellets + Mathf.FloorToInt(player.stat.calculate().bulletRate) - 1);
            float energy = WeaponRules.ShotgunPelletEnergy(chargingRemaining > 0, count, normalVolleyEnergy, energyPerChargedPellet);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0 : Mathf.Lerp(-spreadDegrees / 2, spreadDegrees / 2, (float)i / (count - 1));
                Shoot(pelletCoefficient, energy, 35f, offset);
            }
        }

        public override void skill()
        {
            if (BeginSkill()) chargingRemaining = chargingSeconds;
        }

        public override void ultimate()
        {
            if (!CanAct || Time.timeScale <= 0) return;
            var targets = Enemies(Origin, targetRadius);
            targets.Sort((a, b) =>
            {
                int order = b.stat.nowHp.CompareTo(a.stat.nowHp);
                return order != 0 ? order : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            if (targets.Count == 0 || !BeginUltimate()) return;
            previousTimeScale = Time.timeScale;
            timeScaler = TimeScaler.Instance;
            performingUltimate = true;
            lockedPlayer = player;
            lockedPlayer.AcquireControlLock(this);
            bodyWasVisible = weaponSprite && weaponSprite.enabled;
            if (weaponSprite) weaponSprite.enabled = false;
            StartCoroutine(ExecuteUltimate(targets));
        }

        protected override void TickWeapon(float deltaTime)
        {
            chargingRemaining = Mathf.Max(0, chargingRemaining - deltaTime);
        }

        public override void registerTarget(Entity target, object args = null)
        {
            StopAllCoroutines();
            CleanupUltimate();
            base.registerTarget(target, args);
        }

        private IEnumerator ExecuteUltimate(List<EnemyBase> targets)
        {
            int gunCount = WeaponRules.ShotgunCount(player.stat.calculate().bulletRate);
            var allocation = WeaponRules.AllocateShotguns(targets.Count, gunCount);
            var assigned = new List<EnemyBase>();
            var offsets = new List<Vector3>();
            var targetPositions = new List<Vector3>();
            Vector3 origin = player.transform.position;
            var material = weaponSprite ? weaponSprite.sharedMaterial : null;
            for (int i = 0; i < allocation.Length; i++)
                for (int j = 0; j < allocation[i]; j++)
                {
                    assigned.Add(targets[i]);
                    targetPositions.Add(targets[i].transform.position);
                    Vector3 approach = origin - targets[i].transform.position;
                    float angle = Mathf.Atan2(approach.y, approach.x) + j * Mathf.PI * 2 / allocation[i];
                    offsets.Add(new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * standOffDistance);
                    var gun = CreateFloatingWeapon(origin);
                    ultimateGuns.Add(gun);
                    if (material)
                        WeaponVfxStroke.Create("Shotgun flight trail", origin, material,
                            new Color(0.4f, 0.85f, 1f, 0.5f), 0.025f, new Vector3[10], gun.transform);
                }

            ultimatePhase = "사출 준비";
            // Lock immediately, but allow the world to run before easing its time scale down.
            yield return null;
            float elapsed = 0;
            float duration = Mathf.Max(slowSeconds, flightSeconds);
            while (elapsed < duration && IsOwnerAlive)
            {
                elapsed += Time.unscaledDeltaTime;
                float slowdown = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, slowSeconds));
                SetTimeFactor(Mathf.Lerp(1f, minimumTimeScale, slowdown * slowdown));
                ultimatePhase = slowdown < 1 ? "시간 감속 · 사출" : "목표 접근";
                for (int i = 0; i < assigned.Count; i++)
                {
                    if (assigned[i] && assigned[i].stat != null && assigned[i].stat.nowHp > 0)
                        targetPositions[i] = assigned[i].transform.position;
                    float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                    Vector3 end = targetPositions[i] + offsets[i];
                    Vector3 position = FlightPosition(origin, end, i, gunCount, t, flightArcHeight);
                    Vector3 next = FlightPosition(origin, end, i, gunCount, Mathf.Min(1, t + 0.01f), flightArcHeight);
                    float tangent = Mathf.Atan2(next.y - position.y, next.x - position.x) * Mathf.Rad2Deg;
                    Vector3 aim = targetPositions[i] - position;
                    float finalAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
                    float settle = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.72f, 1f, t));
                    float roll = (i % 2 == 0 ? 1 : -1) * flightRollTurns * 360f *
                        Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / 0.8f));
                    var gun = ultimateGuns[i];
                    gun.transform.SetPositionAndRotation(position,
                        Quaternion.Euler(0, 0, Mathf.LerpAngle(tangent, finalAngle, settle) + roll));
                    var renderer = gun.GetComponent<SpriteRenderer>();
                    if (renderer) renderer.flipY = gun.transform.right.x < 0;
                    var stroke = gun.GetComponentInChildren<WeaponVfxStroke>();
                    if (stroke)
                    {
                        // Keep the ribbon in world space so the weapon's rotation does not twist its history.
                        stroke.Line.useWorldSpace = true;
                        for (int k = stroke.Line.positionCount - 1; k > 0; k--)
                            stroke.Line.SetPosition(k, stroke.Line.GetPosition(k - 1));
                        stroke.Line.SetPosition(0, position);
                        if (elapsed <= Time.unscaledDeltaTime * 1.1f)
                            for (int k = 1; k < stroke.Line.positionCount; k++) stroke.Line.SetPosition(k, origin);
                        stroke.SetTint(new Color(0.4f, 0.85f, 1f, 0.5f * (1 - settle)));
                    }
                }
                yield return null;
            }
            if (!IsOwnerAlive) { CleanupUltimate(); yield break; }
            ultimatePhase = "조준 고정";
            var lockPoints = new HashSet<Vector3>();
            if (material)
                for (int i = 0; i < assigned.Count; i++)
                {
                    if (!lockPoints.Add(targetPositions[i])) continue;
                    WeaponVfxStroke.Ring("Shotgun lock-on", targetPositions[i], 0.6f,
                        material, new Color(0.6f, 0.9f, 1f, 0.7f)).Fade(aimHoldSeconds + 0.15f, 0.6f, true);
                }
            yield return new WaitForSecondsRealtime(aimHoldSeconds);
            if (!IsOwnerAlive) { CleanupUltimate(); yield break; }
            ultimatePhase = "일제 사격";
            var snapshot = player.stat.calculate();
            var hits = new List<DamageGiveEvent>();
            var impactPoints = new HashSet<Vector3>();
            // Every gun fires, including guns whose assigned enemy died during the approach.
            // Dispatch damage only after all muzzle flashes and tracers exist: the first kill
            // must not suppress the rest of a volley aimed at the same target.
            for (int i = 0; i < assigned.Count; i++)
            {
                var enemy = assigned[i];
                var gun = ultimateGuns[i];
                Vector3 targetPoint = targetPositions[i];
                Vector3 direction = (targetPoint - gun.transform.position).normalized;
                gun.transform.right = direction;
                Vector3 muzzle = gun.transform.position + direction * 0.7f;
                var flash = WeaponVisualEffects.Play(muzzleVisualPrefab, muzzle, gun.transform.rotation,
                    muzzleVisualScale * 2f, 0.6f, true);
                if (flash) flash.name = "Shotgun ultimate muzzle";
                if (impactPoints.Add(targetPoint))
                    WeaponVisualEffects.Play(impactVisualPrefab, targetPoint,
                        Quaternion.identity, impactVisualScale * 1.8f, 0.7f, true);
                if (material)
                {
                    WeaponVfxStroke.Create("Shotgun ultimate blast", muzzle, material, new Color(0.85f, 0.97f, 1f),
                        0.12f, new[] { Vector3.zero, targetPoint - muzzle }).Fade(0.18f, 1, true);
                    WeaponVfxStroke.Ring("Shotgun muzzle pulse", muzzle, 0.1f, material,
                        new Color(0.9f, 0.98f, 1f), 0.08f).Fade(0.16f, 2.5f, true);
                }
                gun.transform.position -= direction * 0.3f;
                if (!enemy || enemy.stat == null || enemy.stat.nowHp <= 0) continue;
                var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.ultimateDamage);
                hits.Add(new DamageGiveEvent(snapshot.calculateTrueDamage(tags, ultimateCoefficientPerGun),
                    Vector3.zero, player, enemy, tags, 0));
            }
            foreach (var hit in hits)
                if (hit.target && hit.target.stat != null && hit.target.stat.nowHp > 0) hit.trigger();
            yield return new WaitForSecondsRealtime(0.18f);
            ultimatePhase = "시간 복귀";
            elapsed = 0;
            while (elapsed < recoverySeconds && IsOwnerAlive)
            {
                elapsed += Time.unscaledDeltaTime;
                SetTimeFactor(Mathf.Lerp(minimumTimeScale, 1f, Mathf.SmoothStep(0, 1, elapsed / recoverySeconds)));
                yield return null;
            }
            CleanupUltimate();
        }

        private bool IsOwnerAlive => player && player.stat != null && player.stat.nowHp > 0 && isActiveAndEnabled;

        // One broad bow from the body to the firing position, with no orbit or oscillation.
        // Neighbouring guns take opposite sides; outer lanes use a slightly wider curve.
        public static Vector3 FlightPosition(Vector3 origin, Vector3 destination, int index, int count,
            float t, float arcHeight = 4.2f)
        {
            t = Mathf.Clamp01(t);
            Vector3 travel = destination - origin;
            float distance = travel.magnitude;
            Vector3 direction = distance > 0.001f ? travel / distance : Vector3.right;
            Vector3 normal = Vector3.Cross(Vector3.forward, direction);
            float lane = (index / 2) / (float)Mathf.Max(1, (count - 1) / 2);
            float bend = Mathf.Max(1, arcHeight) * (1 + lane * 0.45f) * (index % 2 == 0 ? 1 : -1);
            Vector3 controlA = origin + direction * distance * 0.15f + normal * bend;
            Vector3 controlB = destination - direction * distance * 0.15f + normal * bend;
            float u = 1 - t;
            return u * u * u * origin + 3 * u * u * t * controlA +
                3 * u * t * t * controlB + t * t * t * destination;
        }

        private void SetTimeFactor(float factor)
        {
            // Restore the captured scale exactly, including cancellation near zero.
            // Dividing by the previous factor amplifies floating-point errors at the freeze boundary.
            if (timeScaler) timeScaler.SetFactor(this, Mathf.Clamp01(factor));
            else Time.timeScale = previousTimeScale * Mathf.Clamp01(factor);
        }

        private void CleanupUltimate()
        {
            if (performingUltimate)
            {
                if (timeScaler) timeScaler.RemoveFactor(this);
                else SetTimeFactor(1f);
            }
            timeScaler = null;
            if (lockedPlayer) lockedPlayer.ReleaseControlLock(this);
            lockedPlayer = null;
            if (performingUltimate && weaponSprite) weaponSprite.enabled = bodyWasVisible;
            performingUltimate = false;
            foreach (var gun in ultimateGuns)
            {
                if (!gun) continue;
                var line = gun.GetComponent<LineRenderer>();
                if (line && line.sharedMaterial) Destroy(line.sharedMaterial);
                Destroy(gun);
            }
            ultimateGuns.Clear();
        }

        protected override void OnDisable()
        {
            StopAllCoroutines();
            CleanupUltimate();
            base.OnDisable();
        }
    }
}
