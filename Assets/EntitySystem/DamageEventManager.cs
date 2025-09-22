using EntitySystem.Events;
using UnityEngine;

namespace EntitySystem
{
    public class DamageEventManager : MonoBehaviour
    {
        public static DamageEventManager Instance { get; private set; }

        public DamageDisplay dmgDisplay;
        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this; // Singleton 초기화
            }
            else
            {
                Destroy(gameObject); // 중복된 매니저 제거
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void TriggerDmgTakeEvent(DamageTakeEvent dmgEvent)
        {
            // 이벤트 처리 로직
            if(dmgEvent.realDmg > 0)
                Instantiate(dmgDisplay).dmgEvent=dmgEvent;
        }
    }
}