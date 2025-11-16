using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using EntitySystem;
using EntitySystem.Events;
using EntitySystem.StatSystem;
using GameBackend;
using PlayerSystem.Effects;
using PlayerSystem.Effects.EnergyGun;
using PlayerSystem.Skills.ElectricShock;
using PlayerSystem.Tiling;
using PlayerSystem.Weapons;
using UnityEngine;
using EventArgs = EntitySystem.Events.EventArgs;

namespace PlayerSystem
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Player : Entity
    {
        [Header("Movement")] [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float groundAcceleration = 20f;
        [SerializeField] private float airAcceleration = 12f;
        [SerializeField] private float jumpForce = 16f;
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBuffer = 0.15f;
        [SerializeField] private Transform groundCheck = null;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private LayerMask dropPlatformMask = 0;
        [SerializeField] private float fallThroughDuration = 0.35f;

        [Header("Combat")] [SerializeField] private Projectile defaultProjectile = null;
        [SerializeField] private Transform firePoint = null;
        [SerializeField] private float fireCooldown = 0.2f;
        [SerializeField] private TriggerEffectAsset fallbackSkillEffect = null;
        [SerializeField] private TriggerEffectAsset fallbackUltimateEffect = null;

        [SerializeField] public Skill skill;
        [SerializeField] public Ultimate ultimate;
        
        [Header("Interaction")] [SerializeField]
        private float interactRadius = 1.5f;

        [SerializeField] private LayerMask interactMask = -1;

        [Header("Input")] [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode downKey = KeyCode.S;
        [SerializeField] private KeyCode fireKey = KeyCode.J;
        [SerializeField] private KeyCode skillKey = KeyCode.K;
        [SerializeField] private KeyCode ultimateKey = KeyCode.L;
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private KeyCode inventoryKey = KeyCode.I;
        [SerializeField] private Inventory inventory;
        
        private KeyCode dodgeKey = KeyCode.LeftShift;

        private Rigidbody2D body = null;
        private Collider2D bodyCollider = null;

        private float fireTimer;
        private float skillTimer;
        private float ultimateTimer;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private float coyoteTimerValue;
        private float jumpBufferTimer;
        private float fallThroughTimer;
        private float horizontalInput;
        private bool jumpQueued;
        private bool grounded;
        private bool wasGrounded;
        private bool isDodging;
        private bool isFallingThrough;
        private readonly List<Collider2D> fallingThroughPlatforms = new List<Collider2D>();

        private void Awake()
        {
            this.inventory.entity = this;
        }

        protected override void Start()
        {
            base.Start();
            EnsureLayerMasksConfigured();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            skill = Instantiate(skill);
            skill.registerTarget(this);
            ultimate = Instantiate(ultimate);
            ultimate.registerTarget(this);
            this.stat = new EntityStat(this, 10000, 100, 100);
        }

        private float energyChargeICD = 0;

        public override void eventActive(EventArgs e)
        {
            if (e is DamageGiveEvent damagegiveEvent)
            {
                var stat = this.stat.calculate();
                if (energyChargeICD <= 0)
                {
                    if (damagegiveEvent.atkTags.Contains(AtkTags.ultimateDamage))
                    {
                        this.stat.energy += (int)(30f * stat.energyRecharge);
                        if(this.stat.energy > 100) this.stat.energy = 100;
                    }
                    else if (damagegiveEvent.atkTags.Contains(AtkTags.skillDamage))
                    {
                        this.stat.energy += (int)(50f * stat.energyRecharge);
                        if(this.stat.energy > 100) this.stat.energy = 100;
                    }
                    else if (damagegiveEvent.atkTags.Contains(AtkTags.normalAttackDamage))
                    {
                        this.stat.energy += (int)(20f * stat.energyRecharge);
                        if(this.stat.energy > 100) this.stat.energy = 100;
                        energyChargeICD = 0.2f;
                    }
                    else
                    {
                        this.stat.energy += (int)(10f * stat.energyRecharge);
                        if(this.stat.energy > 100) this.stat.energy = 100;
                        energyChargeICD = 0.2f;
                    }
                }
            }

            if (e is JustDodgeEvent dodgeEvent)
            {
                if (this == dodgeEvent.entity)
                {
                    TimeScaler.Instance.changeScaleForRealTime(0.2f, 0.5f);
                    
                    dodgeTimer = this.stat.dodgeTime;
                    dodgeCooldownTimer = 0;
                }
            }

            base.eventActive(e);
        }

        protected override void update(float deltaTime)
        {
            CacheGroundedState();
            ReadInput(deltaTime);
            UpdateTimers(deltaTime);
            energyChargeICD -= deltaTime;
            if (energyChargeICD < 0) energyChargeICD = 0;
            
            base.update(deltaTime);
            
        }




        private void FixedUpdate()
        {
            ApplyMovement();
            HandleFallThrough();
        }

        private void EnsureLayerMasksConfigured()
        {
            if (groundMask == -1)
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                {
                    groundMask = 1 << groundLayer;
                }
            }

            if (dropPlatformMask == 0)
            {
                int platformLayer = LayerMask.NameToLayer("Platform");
                if (platformLayer >= 0)
                {
                    dropPlatformMask = 1 << platformLayer;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureLayerMasksConfigured();
        }
#endif

        private void CacheGroundedState()
        {
            wasGrounded = grounded;
            Vector3 origin = groundCheck ? groundCheck.position : transform.position;
            bool onGround = Physics2D.OverlapCircle(origin, groundCheckRadius, groundMask);
            bool onDropPlatform =
                !isFallingThrough && Physics2D.OverlapCircle(origin, groundCheckRadius, dropPlatformMask);
            grounded = onGround || onDropPlatform;

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

            bool downHeld = Input.GetKey(downKey);
            bool jumpPressed = Input.GetKeyDown(jumpKey);

            if (jumpPressed)
            {
                if (downHeld)
                {
                    jumpBufferTimer = 0f;
                    FallThrough();
                }
                else
                {
                    jumpBufferTimer = jumpBuffer;
                }
            }
            else
            {
                jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - deltaTime);
            }

            if (!downHeld && jumpBufferTimer > 0f && (grounded || coyoteTimerValue > 0f))
            {
                jumpQueued = true;
                jumpBufferTimer = 0f;
                coyoteTimerValue = 0f;
                var jumpPower = 1f;
                //ActivateMemory(ActionTriggerType.Jump, 1f);
                new JumpEvent(this, jumpPower).trigger();
            }

            if (Input.GetKeyDown(fireKey))
            {
                TryFire();
            }

            if (Input.GetKeyDown(skillKey))
            {
                this.skill.execute();
            }

            if (Input.GetKeyDown(ultimateKey))
            {
                this.ultimate.execute();
            }

            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }

            if (Input.GetKeyDown(dodgeKey))
            {
                TryDodge();
            }

            if (Input.GetKeyUp(inventoryKey))
            {
                inventory.gameObject.SetActive(true);
                inventory.show = true;
            }
        }

        private void UpdateTimers(float deltaTime)
        {
            fireTimer = Mathf.Max(0f, fireTimer - deltaTime);
            skillTimer = Mathf.Max(0f, skillTimer - deltaTime);
            ultimateTimer = Mathf.Max(0f, ultimateTimer - deltaTime);
            dodgeCooldownTimer = Mathf.Max(0f, dodgeCooldownTimer - deltaTime);
            fallThroughTimer = Mathf.Max(0f, fallThroughTimer - deltaTime);

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

            if (isDodging)
            {
                velocity.x = Mathf.Sign(transform.localScale.x) * this.stat.dodgeSpeed;
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
            if (!bodyCollider || !isFallingThrough)
            {
                return;
            }

            if (fallThroughTimer > 0f)
            {
                return;
            }

            ResetFallThroughState();
        }

        private void TryFire()
        {
            if (fireTimer > 0f)
            {
                return;
            }

            //ActivateMemory(ActionTriggerType.BasicAttack, 1f);
            var targetPos = Vector3.zero;
            new BasicAttackExecuteEvent(this, targetPos).trigger();

            if (defaultProjectile && firePoint)
            {
                float direction = Mathf.Sign(transform.localScale.x);
                Vector2 dir = new Vector2(direction, 0f);
                var instance = Instantiate(defaultProjectile, firePoint.position, Quaternion.identity);
                Debug.Log(instance);
                instance.Initialize(this, dir, 1f, 0f);
                if (MemoryTriggerContext.TryGetActive(this, out var context))
                    context.ApplyToProjectile(instance);
            }
            fireTimer = fireCooldown;
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
            new InteractionEvent(this, interactables[0]).trigger();
            //ActivateMemory(ActionTriggerType.Interact, 1f);
        }

        private void TryDodge()
        {
            if (isDodging || dodgeCooldownTimer > 0f)
            {
                return;
            }

            isDodging = true;
            dodgeTimer = this.stat.dodgeTime;
            dodgeCooldownTimer = this.stat.dodgeCooldown;
            new DodgeEvent(this).trigger();
            // ActivateMemory(ActionTriggerType.Dodge, 1f);
        }

        private void FallThrough()
        {
            if (!bodyCollider || isFallingThrough)
            {
                return;
            }

            Vector3 origin = groundCheck ? groundCheck.position : transform.position;
            Collider2D[] platforms = Physics2D.OverlapCircleAll(origin, groundCheckRadius + 0.05f, dropPlatformMask);
            if (platforms == null || platforms.Length == 0)
            {
                return;
            }

            foreach (var platform in platforms)
            {
                if (!platform || fallingThroughPlatforms.Contains(platform))
                {
                    continue;
                }

                Physics2D.IgnoreCollision(bodyCollider, platform, true);
                fallingThroughPlatforms.Add(platform);
            }

            if (fallingThroughPlatforms.Count == 0)
            {
                return;
            }

            fallThroughTimer = fallThroughDuration;
            isFallingThrough = true;
        }

        private void ResetFallThroughState()
        {
            if (!bodyCollider)
            {
                return;
            }

            foreach (var platform in fallingThroughPlatforms)
            {
                if (platform)
                {
                    Physics2D.IgnoreCollision(bodyCollider, platform, false);
                }
            }

            fallingThroughPlatforms.Clear();
            isFallingThrough = false;
            fallThroughTimer = 0f;
        }

        private void OnDisable()
        {
            ResetFallThroughState();
        }

        public bool TryInterceptAttack(Entity attacker, DamageGiveEvent giveEvent)
        {
            // if (isDodging)
            // {
            //     float power = perfectDodgeTimer > 0f ? 2f : 1f;
            //     isDodging = false;
            //     dodgeTimer = 0f;
            //     return true;
            // }
            //
            // return false;
            return isDodging;
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

