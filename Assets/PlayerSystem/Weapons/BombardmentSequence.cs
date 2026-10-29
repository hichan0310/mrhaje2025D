using System.Collections;
using EntitySystem;
using EntitySystem.Events;
using GameBackend;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    /// <summary>Endpoint-driven bombardment: falling visuals cannot detonate early on an unrelated collider.</summary>
    public sealed class BombardmentSequence : MonoBehaviour
    {
        public sealed class Config
        {
            public Player Owner;
            public Vector2 Center;
            public int Count;
            public float ScatterRadius, BlastRadius, Interval, Coefficient, DotCoefficient;
            public float WarningSeconds = 0.55f, FallSeconds = 0.65f, HeavyHoldSeconds = 0.4f;
            public AtkTags[] Elements;
            public Sprite BombSprite;
            public Material Material;
            public GameObject SmallVisual, HeavyVisual, ExplosionVisual, EmbersVisual;
        }
        private Config config;
        private int pending;
        public int Impacts { get; private set; }
        public static BombardmentSequence Launch(Config settings)
        {
            var sequence = new GameObject("Support bombardment").AddComponent<BombardmentSequence>();
            sequence.config = settings;
            sequence.StartCoroutine(sequence.Barrage());
            return sequence;
        }

        private IEnumerator Barrage()
        {
            if (config.Material)
                WeaponVfxStroke.Ring("Bombardment zone", config.Center, config.ScatterRadius,
                    config.Material, new Color(1, .55f, .2f, .38f), .025f, transform).Fade(0.7f, 1.06f);
            for (int i = 0; i < Mathf.Max(1, config.Count) && OwnerAlive; i++)
            {
                bool heavy = i == Mathf.Max(1, config.Count) - 1;
                Vector2 point = heavy ? config.Center : config.Center + Random.insideUnitCircle * config.ScatterRadius;
                AtkTags element = config.Elements != null && config.Elements.Length > 0 ?
                    config.Elements[Random.Range(0, config.Elements.Length)] : AtkTags.heatDamage;
                pending++;
                StartCoroutine(Strike(point, heavy, element));
                yield return new WaitForSeconds(Mathf.Max(0.02f, config.Interval));
            }
            while (pending > 0 && OwnerAlive) yield return null;
            Destroy(gameObject);
        }

        private bool OwnerAlive => config.Owner && config.Owner.stat != null && config.Owner.stat.nowHp > 0;

        private IEnumerator Strike(Vector2 point, bool heavy, AtkTags element)
        {
            var root = new GameObject(heavy ? "Heavy bombardment strike" : "Bombardment strike");
            root.transform.SetParent(transform, false);
            Color tint = element == AtkTags.electricalDamage ? new Color(.4f, .8f, 1) :
                element == AtkTags.physicalDamage ? new Color(.9f, .72f, .5f) : new Color(1, .35f, .08f);
            WeaponVfxStroke warning = null, closing = null;
            if (config.Material)
            {
                warning = WeaponVfxStroke.Ring("Impact area", point, config.BlastRadius, config.Material,
                    new Color(tint.r, tint.g, tint.b, .55f), heavy ? .07f : .035f, root.transform);
                closing = WeaponVfxStroke.Ring("Closing target ring", point, config.BlastRadius * 1.3f,
                    config.Material, tint, .025f, root.transform);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 direction = Quaternion.Euler(0, 0, i * 90) * Vector3.right;
                    WeaponVfxStroke.Create("Target tick", point, config.Material, tint, .04f,
                        new[] {direction * config.BlastRadius * .9f, direction * config.BlastRadius * 1.1f}, root.transform);
                }
            }
            float elapsed = 0;
            while (elapsed < config.WarningSeconds && OwnerAlive)
            {
                elapsed += TimeManager.deltaTime;
                if (closing) closing.transform.localScale = Vector3.one * Mathf.Lerp(1, .35f, elapsed / Mathf.Max(.01f, config.WarningSeconds));
                yield return null;
            }
            var bomb = new GameObject(heavy ? "Heavy bomb" : "Falling bomb");
            bomb.transform.SetParent(root.transform, false);
            Vector3 end = point;
            Vector3 start = end + new Vector3(heavy ? -4.5f : -3, heavy ? 12 : 10, 0);
            bomb.transform.position = start;
            var renderer = bomb.AddComponent<SpriteRenderer>();
            renderer.sprite = config.BombSprite;
            if (config.Material) renderer.sharedMaterial = config.Material;
            renderer.sortingOrder = 28;
            if (renderer.sprite) bomb.transform.localScale = Vector3.one * ((heavy ? 1.1f : .55f) / Mathf.Max(.01f, renderer.sprite.bounds.size.x));
            GameObject visualPrefab = heavy ? config.HeavyVisual : config.SmallVisual;
            if (visualPrefab)
            {
                var visual = Instantiate(visualPrefab, root.transform);
                // The reference meteor is authored to travel down the screen with its flame above it.
                visual.transform.localScale = Vector3.one * (heavy ? .8f : .4f);
                visual.transform.position = start;
                foreach (var particles in visual.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = particles.main; main.startColor = tint;
                    particles.Play();
                }
                foreach (var r in visual.GetComponentsInChildren<Renderer>()) r.sortingOrder = 27;
                visual.transform.SetParent(bomb.transform, true);
            }
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg - 90;
            bomb.transform.rotation = Quaternion.Euler(0, 0, angle);
            elapsed = 0;
            Vector3 hoverPoint = end + new Vector3(-.45f, 1.5f, 0);
            while (elapsed < config.FallSeconds && OwnerAlive)
            {
                elapsed += TimeManager.deltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.01f, config.FallSeconds));
                bomb.transform.position = Vector3.Lerp(start, heavy ? hoverPoint : end, t * t);
                yield return null;
            }
            if (heavy)
            {
                // Alveria's large meteor builds light before release; this uses a brief hover rather than a global freeze.
                elapsed = 0;
                WeaponVfxStroke charge = config.Material ? WeaponVfxStroke.Ring("Heavy bomb charge", hoverPoint,
                    .25f, config.Material, new Color(1, .85f, .5f, .8f), .16f, root.transform) : null;
                while (elapsed < config.HeavyHoldSeconds && OwnerAlive)
                {
                    elapsed += TimeManager.deltaTime;
                    float t = elapsed / Mathf.Max(.01f, config.HeavyHoldSeconds);
                    if (charge) charge.transform.localScale = Vector3.one * Mathf.Lerp(1, 5, t);
                    if (warning) warning.SetTint(Color.Lerp(tint, new Color(1, .95f, .8f), t));
                    yield return null;
                }
                elapsed = 0;
                while (elapsed < .12f && OwnerAlive)
                {
                    elapsed += TimeManager.deltaTime;
                    bomb.transform.position = Vector3.Lerp(hoverPoint, end, Mathf.Clamp01(elapsed / .12f));
                    yield return null;
                }
            }
            if (OwnerAlive)
            {
                Impacts++;
                ApplyDamage(point, element);
                Impact(point, heavy, tint);
            }
            Destroy(root);
            pending--;
        }

        private void ApplyDamage(Vector2 point, AtkTags element)
        {
            var snapshot = config.Owner.stat.calculate();
            foreach (var enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                if (!enemy.isActiveAndEnabled || enemy.stat == null || enemy.stat.nowHp <= 0 ||
                    ((Vector2)enemy.transform.position - point).sqrMagnitude > config.BlastRadius * config.BlastRadius) continue;
                var tags = new AtkTagSet().Add(AtkTags.heatDamage, AtkTags.ultimateDamage);
                new DamageGiveEvent(snapshot.calculateTrueDamage(tags, config.Coefficient), Vector3.zero,
                    config.Owner, enemy, tags, 2).trigger();
                if (enemy && enemy.stat.nowHp > 0)
                    WeaponDamageOverTime.Apply(config.Owner, enemy, element, config.DotCoefficient, 6f);
            }
        }

        private void Impact(Vector3 point, bool heavy, Color tint)
        {
            WeaponVisualEffects.Play(config.ExplosionVisual, point, Quaternion.identity, heavy ? 1.8f : .9f, 1.5f);
            WeaponVisualEffects.Play(config.EmbersVisual, point, Quaternion.identity, heavy ? .5f : .3f, 2);
            if (!config.Material) return;
            WeaponVfxStroke.Ring("Impact flash", point, .25f, config.Material, new Color(1, .95f, .8f),
                heavy ? .22f : .12f).Fade(.16f, config.BlastRadius * 3);
            WeaponVfxStroke.Ring("Blast shockwave", point, config.BlastRadius * .55f, config.Material,
                new Color(tint.r, tint.g, tint.b, .8f), heavy ? .1f : .055f).Fade(.4f, heavy ? 2.7f : 2);
            for (int i=0; i < (heavy ? 14 : 7); i++)
            {
                Vector3 direction = Quaternion.Euler(0, 0, i * 360f / (heavy ? 14 : 7) + Random.Range(-8, 8)) * Vector3.right;
                WeaponVfxStroke.Create("Blast debris streak", point, config.Material, tint, .035f,
                    new[]{ direction * .25f, direction * Random.Range(.8f, 1.6f) }).Fade(.25f, 1.6f);
            }
        }
    }
}
