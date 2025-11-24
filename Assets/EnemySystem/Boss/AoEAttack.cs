using UnityEngine;
using EntitySystem;
using EntitySystem.Events;

public class AoEAttack : MonoBehaviour
{
    public float duration = 1.5f;
    public int damage = 50;
    public float radius = 1.5f;
    public Entity owner; // 사이버 브루트 넣기

    private float timer;

    private void Start()
    {
        transform.localScale = new Vector3(radius*11, radius, 1);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= duration)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        Entity target = col.GetComponentInParent<Entity>();
        if (target != null && target != owner)
        {
            new DamageGiveEvent(damage, Vector3.zero, owner, target,
                new AtkTagSet().Add(AtkTags.physicalDamage), 1).trigger();
        }
    }
}
