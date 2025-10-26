using EntitySystem.Events;
using UnityEngine;

namespace EntitySystem
{
    public class DamageEventManager : MonoBehaviour
    {
        public static DamageEventManager Instance { get; private set; }

        [Header("Prefab (World TextMeshPro)")]
        public DamageDisplay dmgDisplay;              // 3D TextMeshPro가 붙은 프리팹

        [Header("Hierarchy")]
        public Transform worldRoot;                   // 월드스페이스 텍스트를 담을 부모(선택)

        [Header("Sorting (MeshRenderer)")]
        public string sortingLayerName = "Default";   // 스프라이트 위에 보이게 할 레이어명
        public int sortingOrder = 5000;               // 오더 크게

        public void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void TriggerDmgTakeEvent(DamageTakeEvent dmgEvent)
        {
            if (dmgEvent == null || dmgEvent.realDmg <= 0 || dmgEvent.target == null || dmgDisplay == null)
                return;

            // 타겟 위치 기준으로 스폰 (랜덤 오프셋은 DamageDisplay에서 처리)
            var spawnPos = dmgEvent.target.transform.position;

            // 인스턴스 생성(월드 스페이스)
            var inst = Instantiate(dmgDisplay, spawnPos, Quaternion.identity, worldRoot);

            // 월드 텍스트가 가려지지 않도록 정렬 보정
            var mr = inst.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = sortingLayerName;
                mr.sortingOrder = sortingOrder;
            }

            // 내용 세팅(폰트/색/페이드 등은 DamageDisplay에서 처리)
            inst.dmgEvent = dmgEvent;
        }
    }
}
