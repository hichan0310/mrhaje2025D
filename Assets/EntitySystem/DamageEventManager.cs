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

        public void TriggerDmgTakeEvent(DamageTakeEvent dmgEvent)
        {
            if (dmgEvent == null || dmgEvent.realDmg <= 0 || dmgEvent.target == null || dmgDisplay == null)
                return;

            var spawnPos = dmgEvent.target.transform.position;


            var inst = Instantiate(dmgDisplay, spawnPos, Quaternion.identity, worldRoot);


            var mr = inst.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = sortingLayerName;
                mr.sortingOrder = sortingOrder;
            }

            inst.dmgEvent = dmgEvent;
        }
    }
}
