using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlayerSystem.UltimateSkills.DamageEndingSniper
{
    public class SnipeAim:MonoBehaviour
    {
        [SerializeField] private GameObject snipeEffect;
        
        private Queue<GameObject> snipeEffectPositions = new();

        public List<Vector3> snipePositions=>snipeEffectPositions.Select(x=>x.transform.position).ToList();

        public void destroy()
        {
            foreach (var pos in snipeEffectPositions)
            {
                Destroy(pos);
            }
            Destroy(this.gameObject);
        }
        
        private void Update()
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = -9;
            this.transform.position = mouseWorld;
            if (Input.GetMouseButtonDown(0))
            {
                var s = Instantiate(snipeEffect);
                s.transform.position = mouseWorld;
                snipeEffectPositions.Enqueue(s);
                while(snipeEffectPositions.Count > 5)Destroy(snipeEffectPositions.Dequeue());
            }
        }
    }
}