using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem.Weapons
{
    public class HaveTrailDestroy : MonoBehaviour
    {
        // 이 오브젝트의 child 중 TrailRenderer를 가진 오브젝트들
        public List<GameObject> trails;

        public void destroy()
        {
            // 1. 부모 오브젝트는 바로 안 보이게 만들기
            //    - trail 오브젝트에 붙어있는 Renderer만 빼고 전부 끈다.
            var allRenderers = GetComponentsInChildren<Renderer>();

            foreach (var renderer in allRenderers)
            {
                bool isTrailRenderer = false;

                foreach (var trailObj in trails)
                {
                    if (trailObj == null) continue;

                    // trail 오브젝트 자신이거나 그 자식에 있는 Renderer면 살려둔다.
                    if (renderer.gameObject == trailObj ||
                        renderer.transform.IsChildOf(trailObj.transform))
                    {
                        isTrailRenderer = true;
                        break;
                    }
                }

                if (!isTrailRenderer)
                {
                    renderer.enabled = false;
                }
            }

            // 2. Trail이 다 사라질 때까지 기다렸다가 마지막에 Destroy
            StartCoroutine(WaitAndDestroyAfterTrails());
        }

        private IEnumerator WaitAndDestroyAfterTrails()
        {
            while (true)
            {
                bool anyTrailAlive = false;

                foreach (var trailObj in trails)
                {
                    if (trailObj == null) continue;

                    var trailRenderer = trailObj.GetComponent<TrailRenderer>();
                    if (trailRenderer != null && trailRenderer.positionCount > 0)
                    {
                        // 아직 남아 있는 trail이 있다.
                        anyTrailAlive = true;
                        break;
                    }
                }

                if (!anyTrailAlive)
                {
                    break;
                }

                yield return null;
            }

            // 모든 trail이 사라진 뒤 이 오브젝트를 파괴
            Destroy(gameObject);
        }
    }
}
