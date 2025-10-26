using System;
using System.Collections.Generic;
using EntitySystem;
using EntitySystem.Events;
using GameBackend;
using PlayerSystem.Effects;
using PlayerSystem.Weapons;
using UnityEngine;

namespace PlayerSystem
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PlayerMemoryBinder))]
    public class Player : Entity, IEntityEventListener
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float groundAcceleration = 20f;
        [SerializeField] private float airAcceleration = 12f;
        [SerializeField] private float jumpForce = 16f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBuffer = 0.15f;
        [SerializeField] private Transform groundCheck = null;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private float fallThroughDuration = 0.35f;

        [Header("Combat")]
        [SerializeField] private Projectile defaultProjectile = null;
        [SerializeField] private Transform firePoint = null;
        [SerializeField] private float fireCooldown = 0.2f;
        [SerializeField] private TriggerEffectAsset fallbackSkillEffect = null;
        [SerializeField] private TriggerEffectAsset fallbackUltimateEffect = null;
        [SerializeField] private float skillCooldown = 5f;
        [SerializeField] private float ultimateCooldown = 12f;

        [Header("Mobility")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 1.2f;
        [SerializeField] private float dodgeDuration = 0.35f;
        [SerializeField] private float dodgeCooldown = 1.5f;
        [SerializeField] private float justDodgeWindow = 0.2f;

        [Header("Interaction")]
        [SerializeField] private float interactRadius = 1.5f;
        [SerializeField] private LayerMask interactMask = -1;

        [Header("Input")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode downKey = KeyCode.S;
        [SerializeField] private KeyCode fireKey = KeyCode.J;
        [SerializeField] private KeyCode skillKey = KeyCode.K;
        [SerializeField] private KeyCode ultimateKey = KeyCode.L;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode dodgeKey = KeyCode.LeftControl;

        private Rigidbody2D body = null;
        private Collider2D bodyCollider = null;
        private PlayerMemoryBinder memoryBinder = null;

        private float fireTimer;
        private float skillTimer;
        private float ultimateTimer;
        private float dashTimer;
        private float dashCooldownTimer;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private float perfectDodgeTimer;
        private float coyoteTimerValue;
        private float jumpBufferTimer;
        private float fallThroughTimer;
        private float horizontalInput;
        private bool jumpQueued;
        private bool grounded;
        private bool wasGrounded;
        private bool isDashing;
        private bool isDodging;
        private float dashDirection = 1f;

        protected override void Start()
        {
            base.Start();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            memoryBinder = GetComponent<PlayerMemoryBinder>();
            registerListener(this);
        }

        protected override void Update()
        {
            float deltaTime = TimeManager.deltaTime;
            CacheGroundedState();
            ReadInput(deltaTime);
            UpdateTimers(deltaTime);
            base.Update();
        }

        private void ActivateMemory(ActionTriggerType triggerType, float power, Action<MemoryTriggerContext?>? continuation = null)
        {
            if (memoryBinder)
            {
                memoryBinder.Trigger(triggerType, power, continuation);
            }
        }

        private void FixedUpdate()
        {
            ApplyMovement();
            HandleFallThrough();
        }

        private void CacheGroundedState()
        {
            wasGrounded = grounded;
            Vector3 origin = groundCheck ? groundCheck.position : transform.position;
            grounded = Physics2D.OverlapCircle(origin, groundCheckRadius, groundMask);

            if (grounded)
            {
                coyoteTimerValue = coyoteTime;
            }
            else
            {
                coyoteTimerValue = Mathf.Max(0f, coyoteTimerValue - TimeManager.deltaTime);
            }
        }

        private void ReadInput(float deltaTime)
        {
            horizontalInput = Input.GetAxisRaw(horizontalAxis);
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                transform.localScale = new Vector3(Mathf.Sign(horizontalInput), 1f, 1f);
            }

            if (Input.GetKeyDown(jumpKey))
            {
                jumpBufferTimer = jumpBuffer;
            }
            else
            {
                jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - deltaTime);
            }

            if (jumpBufferTimer > 0f && (grounded || coyoteTimerValue > 0f))
            {
                jumpQueued = true;
                jumpBufferTimer = 0f;
                coyoteTimerValue = 0f;
                ActivateMemory(ActionTriggerType.Jump, 1f);
            }

            if (Input.GetKey(downKey) && Input.GetKeyDown(jumpKey))
            {
                FallThrough();
            }

            if (Input.GetKeyDown(fireKey))
            {
                TryFire();
            }

            if (Input.GetKeyDown(skillKey))
            {
                TrySkill();
            }

            if (Input.GetKeyDown(ultimateKey))
            {
                TryUltimate();
            }

            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }

            if (Input.GetKeyDown(dashKey))
            {
                TryDash();
            }

            if (Input.GetKeyDown(dodgeKey))
            {
                TryDodge();
            }
        }

        private void UpdateTimers(float deltaTime)
        {
            fireTimer = Mathf.Max(0f, fireTimer - deltaTime);
            skillTimer = Mathf.Max(0f, skillTimer - deltaTime);
            ultimateTimer = Mathf.Max(0f, ultimateTimer - deltaTime);
            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - deltaTime);
            dodgeCooldownTimer = Mathf.Max(0f, dodgeCooldownTimer - deltaTime);
            perfectDodgeTimer = Mathf.Max(0f, perfectDodgeTimer - deltaTime);
            fallThroughTimer = Mathf.Max(0f, fallThroughTimer - deltaTime);

            if (isDashing)
            {
                dashTimer -= deltaTime;
                if (dashTimer <= 0f)
                {
                    isDashing = false;
                }
            }

            if (isDodging)
            {
                dodgeTimer -= deltaTime;
                if (dodgeTimer <= 0f)
                {
                    isDodging = false;
                }
            }
        }

        private void ApplyMovement()
        {
            if (!body)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            float fixedDelta = Time.fixedDeltaTime;

            if (isDashing)
            {
                velocity.x = dashDirection * dashSpeed;
            }
            else
            {
                float targetSpeed = horizontalInput * moveSpeed;
                float accel = grounded ? groundAcceleration : airAcceleration;
                velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * fixedDelta);
            }

            if (jumpQueued)
            {
                velocity.y = jumpForce;
                jumpQueued = false;
            }

            body.linearVelocity = velocity;
        }

        private void HandleFallThrough()
        {
            if (!bodyCollider)
            {
                return;
            }

            bodyCollider.enabled = fallThroughTimer <= 0f;
        }

        private float ResolveReinforcementMultiplier(ActionTriggerType triggerType, MemoryTriggerContext? context)
        {
            if (context != null)
            {
                return context.Board?.GetBoardReinforcementMultiplier() ?? 1f;
            }

            if (memoryBinder && memoryBinder.TryGetBoard(triggerType, out var board))
            {
                return board.GetBoardReinforcementMultiplier();
            }

            return 1f;
        }

        private void SpawnDefaultProjectile(MemoryTriggerContext? context)
        {
            if (!defaultProjectile || !firePoint)
            {
                return;
            }

            float direction = Mathf.Sign(transform.localScale.x);
            Vector2 dir = new Vector2(direction, 0f);
            var instance = Instantiate(defaultProjectile, firePoint.position, Quaternion.identity);
            float basePower = context?.BasePower ?? 1f;
            float reinforcementMultiplier = ResolveReinforcementMultiplier(ActionTriggerType.BasicAttack, context);
            float projectilePower = basePower * reinforcementMultiplier;
            instance.Initialize(this, dir, projectilePower, 0f);
            context?.ApplyToProjectile(instance);
        }

        private void TryFire()
        {
            if (fireTimer > 0f)
            {
                return;
            }

            bool projectileSpawned = false;
            ActivateMemory(ActionTriggerType.BasicAttack, 1f, context =>
            {
                projectileSpawned = true;
                SpawnDefaultProjectile(context);
            });

            if (!projectileSpawned)
            {
                SpawnDefaultProjectile(null);
            }

            fireTimer = fireCooldown;
        }

        private void TrySkill()
        {
            if (skillTimer > 0f)
            {
                return;
            }

            bool effectTriggered = false;
            ActivateMemory(ActionTriggerType.HeavyAttack, 1f, context =>
            {
                effectTriggered = true;
                float power = context?.BasePower ?? 1f;
                fallbackSkillEffect?.trigger(this, power);
            });

            if (!effectTriggered)
            {
                fallbackSkillEffect?.trigger(this, 1f);
            }

            skillTimer = skillCooldown;
        }

        private void TryUltimate()
        {
            if (ultimateTimer > 0f)
            {
                return;
            }

            bool effectTriggered = false;
            ActivateMemory(ActionTriggerType.Ultimate, 2f, context =>
            {
                effectTriggered = true;
                float power = context?.BasePower ?? 2f;
                fallbackUltimateEffect?.trigger(this, power);
            });

            if (!effectTriggered)
            {
                fallbackUltimateEffect?.trigger(this, 2f);
            }

            ultimateTimer = ultimateCooldown;
        }

        private void TryInteract()
        {
            Collider2D[] results = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactMask);
            List<IInteractable> interactables = new List<IInteractable>();
            foreach (var hit in results)
            {
                if (hit && hit.TryGetComponent(out IInteractable interactable) && !interactables.Contains(interactable))
                {
                    interactables.Add(interactable);
                }
            }

            if (interactables.Count == 0)
            {
                return;
            }

            interactables.Sort((a, b) =>
                Vector3.Distance(transform.position, a.WorldPosition).CompareTo(
                    Vector3.Distance(transform.position, b.WorldPosition)));

            interactables[0].Interact(this);
            ActivateMemory(ActionTriggerType.Interact, 1f);
        }

        private void TryDash()
        {
            if (isDashing || dashCooldownTimer > 0f)
            {
                return;
            }

            dashDirection = horizontalInput == 0f ? Mathf.Sign(transform.localScale.x) : Mathf.Sign(horizontalInput);
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            isDashing = true;
            ActivateMemory(ActionTriggerType.Dash, 1f);
        }

        private void TryDodge()
        {
            if (isDodging || dodgeCooldownTimer > 0f)
            {
                return;
            }

            isDodging = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;
            perfectDodgeTimer = justDodgeWindow;
            ActivateMemory(ActionTriggerType.Dodge, 1f);
        }

        private void FallThrough()
        {
            fallThroughTimer = fallThroughDuration;
            ActivateMemory(ActionTriggerType.DropDown, 1f);
        }

        public bool TryInterceptAttack(Entity attacker)
        {
            if (isDodging)
            {
                float power = perfectDodgeTimer > 0f ? 2f : 1f;
                ActivateMemory(ActionTriggerType.Dodge, power);
                isDodging = false;
                dodgeTimer = 0f;
                return true;
            }

            return false;
        }

        void IEntityEventListener.eventActive(EventArgs eventArgs)
        {
            if (eventArgs is DamageTakeEvent damage && damage.target == this)
            {
                float power = Mathf.Max(1f, damage.realDmg / 10f);
                ActivateMemory(ActionTriggerType.Hit, power);
            }
        }

        void IEntityEventListener.registerTarget(Entity target, object args)
        {
        }

        void IEntityEventListener.removeSelf()
        {
        }

        void IEntityEventListener.update(float deltaTime, Entity target)
        {
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (groundCheck)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactRadius);
        }
#endif
    }
}
