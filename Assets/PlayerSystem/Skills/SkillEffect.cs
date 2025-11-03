using System;
using System.Collections.Generic;
using System.Linq;
using GameBackend;
using UnityEngine;

namespace PlayerSystem.Skills
{
    public abstract class SkillEffect : MonoBehaviour
{
    protected readonly InvokeManager invokeManager = new();
    protected float timer = 0;
    protected Animator animator;
    protected SpriteRenderer spriteRenderer;
    protected bool timeIgnore = false;
    protected Rigidbody2D rigidbody2D;
    protected float nowRate;

    void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody2D = GetComponent<Rigidbody2D>();
    }
    protected void setAlpha(float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        spriteRenderer.color = color;
    }

    protected void checkAlpha(float start, float end, float startAlpha, float endAlpha)
    {
        if (start <= timer && timer < end)
        {
            setAlpha(Mathf.Lerp(startAlpha, endAlpha, (timer - start) / (end - start)));
        }
    }

    protected void checkMove(float start, float end, Vector3 startPos, Vector3 endPos)
    {
        if (start <= timer && timer < end)
        {
            Vector3 pos = Vector3.Lerp(startPos, endPos, (timer - start) / (end - start));
            this.transform.localPosition = pos;
        }
    }

    protected void checkScale(float start, float end, Vector3 startScale, Vector3 endScale)
    {
        if (start <= timer && timer < end)
        {
            Vector3 pos = Vector3.Lerp(startScale, endScale, (timer - start) / (end - start));
            this.transform.localScale = pos;
        }
    }

    protected void checkDestroy(float time)
    {
        if (time <= timer)
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        if (timeIgnore) update(Time.deltaTime);
        else            update(TimeManager.deltaTime);
    }
    
    protected abstract void update(float deltaTime);

    // void OnEnable()
    // {
    //     nowRate = TimeManager.timeRate;
    //     // 스폰 시점의 초기 속도 보정
    //     rigidbody2D.linearVelocity *= nowRate;
    // }
    //
    // 
    //
    // // 논물리 로직은 그대로
    //
    //
    // // 물리 속도 관련은 FixedUpdate로 이동
    // void FixedUpdate()
    // {
    //     var newRate = TimeManager.timeRate;
    //     if (!Mathf.Approximately(newRate, nowRate))
    //     {
    //         rigidbody2D.linearVelocity *= newRate / nowRate;
    //         rigidbody2D.angularVelocity *= newRate / nowRate; // 회전도 원하면
    //         nowRate = newRate;
    //     }
    // }

    protected virtual void OnTriggerEnter2D(Collider2D other) { }

    protected List<Collider2D> getTouchedColliders(Collider2D col)
    {
        if (col is CircleCollider2D circle)
        {
            var center = (Vector2)circle.bounds.center;
            var radius = circle.radius * Mathf.Max(circle.transform.lossyScale.x, circle.transform.lossyScale.y);
            return Physics2D.OverlapCircleAll(center, radius, Physics2D.AllLayers).ToList();
        }
        if (col is BoxCollider2D box)
        {
            var center = (Vector2)box.bounds.center;
            // bounds.size는 이미 월드 스케일 반영됨
            var size = (Vector2)box.bounds.size;
            var angle = box.transform.eulerAngles.z;
            return Physics2D.OverlapBoxAll(center, size, angle).ToList();
        }
        return new List<Collider2D>();
    }
}

}