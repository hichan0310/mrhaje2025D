using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using PlayerSystem.Skills;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;
using Random = UnityEngine.Random;

namespace PlayerSystem.Polyominoes.ExecutionFollowupShot
{
    public class Shot : MonoBehaviour, IEntityEventListener
    {
        private float timer = 0;
        public Entity player { get; set; }
        private LineRenderer lineRenderer;

        private AtkTagSet atkTagSet = new AtkTagSet()
            .Add(AtkTags.triggerEffectDamage, AtkTags.physicalDamage, AtkTags.fixedDamage);

        public float r = 1;
        public float speed = 1;
        public List<Shot> shots { get; set; }
        private Entity target;
        [SerializeField] private GameObject hitEffect;

        private void Awake()
        {
            this.lineRenderer = this.GetComponent<LineRenderer>();
        }

        public void targetActive()
        {
            if (!target)
            {
                Destroy(this.gameObject, 0.5f);
                return;
            }

            Vector2 start = this.transform.position;
            Vector2 end = target.transform.position;

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            var stat = player.stat.calculate();
            var tag = new AtkTagSet(this.atkTagSet);
            var dmg = stat.calculateTrueDamage(tag, 500);
            new DamageGiveEvent(dmg, Vector3.zero, player, target, tag, 0).trigger();
            Destroy(lineRenderer.gameObject, 0.5f);
            Destroy(this.gameObject, 0.5f);
            Destroy(
                Instantiate(this.hitEffect,
                    target.transform.position +
                    new Vector3((Random.value - 0.5f) * 0.5f, (Random.value - 0.5f) * 0.5f, 0), Quaternion.identity),
                1f);
            removeSelf();
        }

        public void eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageGiveEvent damageGiveEvent)
            {
                if (damageGiveEvent.atkTags.Contains(AtkTags.normalAttackDamage))
                {
                    target = damageGiveEvent.target;
                    Invoke("targetActive", 0.5f);
                }
            }
        }

        public void registerTarget(Entity target, object args = null)
        {
            this.player = target;
            target.registerListener(this);
        }

        public void removeSelf()
        {
            this.player.removeListener(this);
            this.shots.Remove(this);
        }

        public void update(float deltaTime, Entity target)
        {
            timer += deltaTime;
            Vector2 pos = player.transform.position;
            this.transform.position = pos + r * new Vector2(Mathf.Cos(speed * timer), Mathf.Sin(speed * timer));
        }
    }
}