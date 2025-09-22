using EntitySystem.Events;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EntitySystem
{
    public class DamageDisplay : MonoBehaviour
    {
        private float moveSpeed=1; // 텍스트 이동속도
        private float timer = 0;
        private float destroyTime=0.8f;
        private TextMeshPro text;

        public DamageTakeEvent dmgEvent
        {
            set
            {
                float x = Random.Range(-0.3f, 0.3f); // X 좌표: -1 ~ 1
                float y = Random.Range(-0.3f, 0.3f); // Y 좌표: -1 ~ 1
                text=GetComponent<TextMeshPro>();
                transform.position=value.target.transform.position+new Vector3(x, y, 0f);
                text.text = value.realDmg.ToString();
                if (value.atkTags.Contains(AtkTags.criticalHit)) text.fontSize = 6; 
                else text.fontSize = 4;
                text.color=Color.black;
            }
        }

        private void Awake()
        {
            Invoke("destroy", destroyTime);
        }

        void Update()
        {
            timer += Time.deltaTime;
            transform.Translate(new Vector3(0, moveSpeed * Time.deltaTime, 0));
            if (timer >= 0.4f)
            {
                var col = text.color;
                col.a = Mathf.Lerp(1, 0, (timer-0.5f)/(destroyTime-0.5f));
                text.color = col;
            }
        }

        void destroy()
        {
            Destroy(gameObject);
        }
    }
}