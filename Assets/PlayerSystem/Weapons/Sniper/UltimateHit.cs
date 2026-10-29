using System;
using System.Collections.Generic;
using DG.Tweening;
using EntitySystem;
using EntitySystem.Events;
using UnityEngine;

namespace PlayerSystem.Weapons.Sniper
{
    public class UltimateHit : MonoBehaviour
    {
        private float timer = 0;
        private int damageTime = 0;
        public float range { get; set; }
        public DamageGiveEvent hit { get; set; }
        public DamageGiveEvent finish { get; set; }
        public UltimateFinished finishObject;
        public HashSet<Entity> colliders = new HashSet<Entity>();
        private Collider2D collider2D;
        private bool finished = false;

        private void Start()
        {
            this.range = Mathf.Sqrt(this.range);
            collider2D = GetComponent<Collider2D>();
            transform.localScale = Vector3.one * 0.3f;

            transform
                .DOScale(Vector3.one * 1.5f * range, 3f)
                .SetEase(Ease.OutQuad);
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (damageTime >= 10)
            {
                if (!finished)
                {
                    var finishObj = Instantiate(finishObject, transform.position, Quaternion.identity);
                    finishObj.transform.localScale = Vector3.one * (1.5f * range);
                    foreach (var ps in finishObj.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = ps.main;
                        // 기본적으로 startSizeMultiplier에 배수 적용
                        main.startSizeMultiplier *= 1.5f * range;
                    }

                    finishObj.damageGiveEvent = this.finish;
                    
                    Destroy(gameObject);
                    this.colliders.Clear();
                }

                finished = true;
                return;
            }

            if (timer < 0.1f)
            {
                collider2D.enabled = true;
            }
            else if (timer < 0.2f)
            {
                collider2D.enabled = false;
            }
            else if (timer >= 0.2f)
            {
                timer -= 0.2f;
                damageTime++;
                this.colliders.Clear();
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            var e = other.GetComponentInParent<Entity>();
            if (e == null || colliders.Contains(e)) return;
            colliders.Add(e);
            if (e == null) return;
            if (e == this.hit.attacker) return;
            this.hit.target = e;
            this.hit.trigger();
        }
    }
}