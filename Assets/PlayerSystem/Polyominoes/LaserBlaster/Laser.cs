using System.Collections;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;

namespace PlayerSystem.Polyominoes.LaserBlaster
{
    public class Laser : MonoBehaviour
    {
        private Entity player;
        private EntityStat stat;
        private Entity target;
        private float power;
        private HashSet<Entity> entities = new HashSet<Entity>();
        private AtkTagSet atkTags=new AtkTagSet().Add(AtkTags.combatModuleDamage, AtkTags.physicalDamage);

        [SerializeField] private BoxCollider2D boxCollider;
        [SerializeField] private LineRenderer laserLine;

        public void init(EntityStat stat, Entity target, float power)
        {
            var tmp = stat.bulletRate+stat.skillRange;
            stat.dmgUp[(int)AtkTags.all] += 20 * tmp;
            
            this.stat = stat;
            this.target = target;
            this.power = power;
            this.player = stat.entity;

            this.boxCollider.enabled = false;
            this.transform.position = this.player.transform.position;

            Invoke("execute", 0.2f);
            this.laserLine = Instantiate(laserLine);
        }

        private void execute()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 from = this.transform.position;
            Vector3 to = target.transform.position;
            Vector2 dir = ((Vector2)(to - from)).normalized;

            // 현재 위치에서 타겟을 바라보도록 회전 (+x가 정면)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            this.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // 라인 설정 (타겟 방향으로 30)
            laserLine.widthMultiplier = 1f;
            laserLine.SetPosition(0, from);
            laserLine.SetPosition(1, from + (Vector3)(dir * 30f));

            this.boxCollider.enabled = true;

            StartCoroutine(LaserLifeRoutine());
        }

        private IEnumerator LaserLifeRoutine()
        {
            // 0.5초 유지
            yield return new WaitForSeconds(0.5f);

            // 콜라이더 끄기
            this.boxCollider.enabled = false;

            // 0.1초 동안 width 1 -> 0 선형 감소
            float fadeDuration = 0.1f;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / fadeDuration);
                laserLine.widthMultiplier = Mathf.Lerp(1f, 0f, a);
                yield return null;
            }
            laserLine.widthMultiplier = 0f;

            // 파괴
            if (laserLine != null) Destroy(laserLine.gameObject);
            Destroy(this.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Entity entity = other.gameObject.GetComponent<Entity>();
            if(entity==null) return;
            if(this.player == entity) return;
            if(this.entities.Contains(entity)) return;
            this.entities.Add(entity);

            var tag = new AtkTagSet(this.atkTags);
            var coef = (power > 1) ? (power - 1) * 150 + 50 : power * 50;
            var dmg = stat.calculateTrueDamage(tag, coef);
            new DamageGiveEvent(dmg, Vector3.zero, player, entity, tag, 1*power).trigger();
            Debug.Log(dmg);
        }
    }
}
