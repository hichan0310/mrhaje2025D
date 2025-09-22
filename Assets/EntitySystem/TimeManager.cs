using System.Collections.Generic;
using EntitySystem;
using UnityEngine;

namespace GameBackend
{
    public static class TimeManager
    {
        private static float _timeRate=1;
        private static List<Entity> entities = new();
        public static float timeRate
        {
            get => _timeRate;
            set
            {
                _timeRate = value;
                foreach (Entity entity in entities)
                {
                    if(entity.animator)
                        entity.animator.speed = _timeRate*entity.stat.speed;
                }
            }
        }

        public static void registrarEntity(Entity entity)
        {
            entities.Add(entity);
        }

        public static void removerEntity(Entity entity)
        {
            entities.Remove(entity);
        }

        public static float deltaTime
        {
            get { return Time.deltaTime * timeRate; }
        }
    }
}