using System;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlayerSystem.Weapons.Sniper
{
    public class UltimateBullet:MonoBehaviour
    {
        public Entity player { get; set; }
        public Vector2 pos { get; set; }
        public UltimateHit ultimateHitPrefab;
        public AtkTagSet ultimateTag { get; set; }
        public Rigidbody2D rigidbody2D;
        public EntityStat stat { get; set; }
        public HaveTrailDestroy trailDestroy;

        private void Start()
        {
            Vector2 s = this.transform.position;
            var len = (pos - s).magnitude;
            Invoke("arrive", len/40);
        }

        public void arrive()
        {
            var uHitObj = Instantiate(this.ultimateHitPrefab, pos, Quaternion.identity);
            
            var hitTag = new AtkTagSet(this.ultimateTag);
            var finishTag = new AtkTagSet(this.ultimateTag);
            var dmgHit = stat.calculateTrueDamage(hitTag, 30);
            var dmgFinish = stat.calculateTrueDamage(finishTag, 700);
            uHitObj.range = stat.skillRange;
            uHitObj.hit = new DamageGiveEvent(dmgHit, Vector3.zero, player, null, hitTag, 1);
            uHitObj.finish = new DamageGiveEvent(dmgFinish, Vector3.zero, player, null, finishTag, 10);
            this.rigidbody2D.linearVelocity=Vector2.zero;
            this.trailDestroy.destroy();
        }
    }
}