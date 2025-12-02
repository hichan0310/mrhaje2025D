using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerSystem.Tiling
{
    public class Bag:MonoBehaviour
    {
        [SerializeField]private List<Polyomino> bag = new List<Polyomino>();
        public IGetBagItem getBagItem { get; set; }

        private void Start()
        {
            Debug.Log(bag.Count);
            foreach (Polyomino p in bag)
            {
                p.rt.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            }
        }

        private Vector2 ind2pos(int idx)
        {
            var x=230+(idx%3)*100;
            var y=250-(idx/3)*100;
            return new Vector2(x,y);
        }
        
        public bool show { get; set; }
        
        private void Update()
        {
            if (show)
            {
                int i;
                for (i = 0; i < bag.Count && i < 15; i++)
                {
                    bag[i].rt.anchoredPosition = ind2pos(i);
                    bag[i].gameObject.SetActive(true);
                }

                for (; i < bag.Count; i++)
                {
                    bag[i].Hide();
                }
            }
            else
            {
                foreach (Polyomino b in bag) b.Hide();
                this.gameObject.SetActive(false);
            }
        }

        public void click(int idx)
        {
            Debug.Log(idx);
            if (getBagItem != null)
            {
                if (idx < bag.Count)
                {
                    bag[idx].rt.localScale = new Vector3(1, 1, 1);
                    getBagItem.somethingSelected(this.bag[idx]);
                    this.bag.RemoveAt(idx);
                }
                else
                {
                    getBagItem.somethingSelected(null);
                }
            }
        }

        public void addBagItem(Polyomino p)
        {
            if(!p) return;
            p.rt.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            bag.Add(p);
        }

        public void addBagItem(IEnumerable<Polyomino> ps)
        {
            foreach (var p in ps)
            {
                addBagItem(p);
            }
        }
    }
}