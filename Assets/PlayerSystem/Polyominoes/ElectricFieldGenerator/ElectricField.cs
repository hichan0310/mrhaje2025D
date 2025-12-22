using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlayerSystem.Polyominoes.ElectricFieldGenerator
{
    public class ElectricField:MonoBehaviour
    {
        public EntityStat entityStat{get;set;}
        private HashSet<Entity> entities = new HashSet<Entity>();
        private AtkTagSet atkTagSet=new AtkTagSet().Add(AtkTags.combatModuleDamage, AtkTags.electricalDamage);
        public float power { get; set; }
        
        [SerializeField] private Shock shock;

        private void Start()
        {
            var scale = (1f + (entityStat.skillRange - 1) / 3);
            this.transform.localScale = Vector3.one * scale;
            foreach (var ps in this.gameObject.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.startSizeMultiplier *= scale;
            }
            Destroy(this.gameObject, 1);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Entity entity = other.gameObject.GetComponent<Entity>();
            if(entity==null) return;
            if(entities.Contains(entity)) return;
            entities.Add(entity);
            if(entity==entityStat.entity) return;

            var tag = new AtkTagSet(atkTagSet);
            var damage = entityStat.calculateTrueDamage(tag, (power + 1) * 50);
            new DamageGiveEvent(damage, Vector3.back, entityStat.entity, entity, tag, (entities.Count==0)?5:0).trigger();
            
            if (power > 2f)
            {
                var s = Instantiate(shock, entity.transform);
                s.transform.localPosition = Vector3.zero;
                s.Init(entityStat, entity, power*30-50, power);
            }
        }
    }
}