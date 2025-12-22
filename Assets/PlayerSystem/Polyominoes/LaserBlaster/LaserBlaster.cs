using EntitySystem;
using PlayerSystem.Tiling;
using UnityEngine;

namespace PlayerSystem.Polyominoes.LaserBlaster
{
    public class LaserBlaster : Polyomino
    {
        [SerializeField] private Laser laser;

        [Header("Targeting")]
        [SerializeField] private float range = 15f;
        [SerializeField] private LayerMask enemyLayer; // 인스펙터에서 Enemy 레이어 지정

        public override string Name => "LaserBlaster";

        public override string Description =>
            "파워가 입력되면 가장 가까이 있는 적에게 모든 것을 관통하는 레이저를 날려서 직선 상의 적에게 강한 물리 속성 피해를 입힌다. \n" +
            "1 이하의 파워일 경우 power*50의 피해량, 1 이상의 파워일 경우 power*150-100의 피해량을 가진다. \n" +
            this.statBonusDescription;

        public override void trigger(Entity player, float power)
        {
            Entity target = null;

            Vector2 origin = player.transform.position;

            // 15 사정거리 안의 적 콜라이더 전부 수집
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, enemyLayer);
            Debug.Log(hits.Length);

            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (!col) continue;

                // 콜라이더에서 Entity 가져오기 (부모쪽에 붙어있을 수도 있으니)
                Entity e = col.GetComponentInParent<Entity>();
                if (!e) continue;

                // 자기 자신 제외
                if (e == player) continue;

                // (선택) 죽은 적 제외 같은 조건이 있으면 여기서 필터링

                Vector2 p = e.transform.position;
                float sqr = (p - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    target = e;
                }
            }

            if (target)
            {
                Instantiate(laser).init(player.stat.calculate(), target, power);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 타겟팅 범위 확인용
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, range);
        }
#endif
    }
}
