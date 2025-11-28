// Assets/EnemySystem/Drone/MeleeDroneController.cs
using System.Collections;
using UnityEngine;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;

namespace EnemySystem
{
    public class MeleeDroneController : DroneBase
    {
        [Header("Range")]
        [SerializeField] private float chaseRange = 5f;
        [SerializeField] private float hitRange = 1.2f;

        [Header("Attack")]
        [SerializeField] private int contactDamage = 30;
        [SerializeField] private float stopAfterHitDuration = 0.5f;

        private bool stunned = false;

        
[Header("Wobble")]
[SerializeField] private float wobbleAmplitude = 0.4f;   // how far it wobbles sideways
[SerializeField] private float wobbleFrequency = 2.0f;   // wobble speed
[SerializeField] private float velocitySmooth = 8.0f;    // how fast velocity follows desired

        protected override void TickAI(float deltaTime)
        {
            if (isDead) return;
            if (stunned) return;
            if (!HasTarget) return;

            float dist = DistanceToTarget();

            // If close enough, try to hit
            if (dist <= hitRange)
            {
                DoHit();
            }
            // Otherwise movement is handled in TickMovement
        }

protected override void TickMovement(float fixedDeltaTime)
{
    if (rb == null) return;
    if (isDead) return;

    if (!HasTarget || stunned)
    {
        rb.linearVelocity = Vector2.zero;
        return;
    }

    Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
    float distSqr = toTarget.sqrMagnitude;

    // Do nothing if too far or already in hit range (AI will handle hit in TickAI)
    if (distSqr <= hitRange * hitRange || distSqr > chaseRange * chaseRange)
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, velocitySmooth * fixedDeltaTime);
        return;
    }

    // Base direction towards the target
    Vector2 baseDir = toTarget.normalized;

    // Perpendicular vector for sideways wobble (rotate 90 degrees)
    Vector2 perp = new Vector2(-baseDir.y, baseDir.x);

    // Time-based wobble
    float wobble = Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmplitude;

    // Combine forward direction and sideways wobble
    Vector2 desiredDir = (baseDir + perp * wobble).normalized;

    // Final desired velocity
    Vector2 desiredVel = desiredDir * chaseSpeed;

    // Smoothly move current velocity toward desired velocity
    rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, desiredVel, velocitySmooth * fixedDeltaTime);
}


        private void DoHit()
        {
            if (!HasTarget) return;

            Entity playerEntity = target.GetComponent<Entity>();
            if (playerEntity != null)
            {
                var tags = new AtkTagSet().Add(AtkTags.physicalDamage, AtkTags.normalAttackDamage);
                new DamageGiveEvent(contactDamage, transform.position, this, playerEntity, tags, 1).trigger();
            }

            StartCoroutine(StunCoroutine());
        }

        private IEnumerator StunCoroutine()
        {
            stunned = true;

            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(stopAfterHitDuration);

            stunned = false;
        }

        protected override void OnDie(Entity attacker)
        {
            // EnemyBase already sets isDead = true before calling this

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.isKinematic = true;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Destroy(gameObject, 1.5f);
        }

        protected override void OnEvent(EventArgs e)
        {
            // Optional: react to events (DamageTakeEvent, etc.)
        }
    }
}
