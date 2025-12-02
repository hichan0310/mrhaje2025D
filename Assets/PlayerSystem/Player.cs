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
        [Header("Movement")] 
        [SerializeField] private float coyoteTime = 0.15f;
        [SerializeField] private float jumpBuffer = 0.15f;
        [SerializeField] private Transform groundCheck = null;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundMask = -1;
        [SerializeField] private LayerMask dropPlatformMask = 0;
        [SerializeField] private float fallThroughDuration = 0.35f;
        
        [Header("Interaction")] [SerializeField]
        private float interactRadius = 1.5f;

        [SerializeField] private LayerMask interactMask = -1;

        [Header("Input")] [SerializeField] private string horizontalAxis = "Horizontal";
        private KeyCode jumpKey = KeyCode.Space;
        private KeyCode downKey = KeyCode.S;
        private KeyCode interactKey = KeyCode.F;
        private KeyCode inventoryKey = KeyCode.I;
        [SerializeField] private Inventory inventory;
        
        private KeyCode dodgeKey = KeyCode.LeftShift;
        [SerializeField] private Weapon weapon;
        
        public EntityStat statCache { get; private set; }

        private Rigidbody2D body = null;
        private Collider2D bodyCollider = null;

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
            this.stat = new EntityStat(this, 10000, 100, 100);
            this.statCache = this.stat.calculate();
            if (weapon)
            {
                weapon = Instantiate(weapon);
                weapon.registerTarget(this);
            }
        }

        private float energyChargeICD = 0;

        public override void eventActive(EventArgs e)
        {
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
            this.statCache = this.stat.calculate();
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


            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                Debug.Log(
                    $"[Player Move] input={horizontalInput}, " +
                    $"speed={statCache.speed}, " +
                    $"groundAccel={statCache.groundAcceleration}, " +
                    $"airAccel={statCache.airAcceleration}, " +
                    $"isDodging={isDodging}"
                );
            }

            
            if (isDodging)
            {
                velocity.x = Mathf.Sign(transform.localScale.x) * this.stat.dodgeSpeed;
            }
            else
            {
                float targetSpeed = horizontalInput * statCache.speed;
                float accel = grounded ? statCache.groundAcceleration : statCache.airAcceleration;
                velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * fixedDelta);
            }

            if (jumpQueued)
            {
                velocity.y = statCache.jumpPower;
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

        public void TryDodge(bool ignoreTimer = false)
        {
            if (!ignoreTimer && (isDodging || dodgeCooldownTimer > 0f))
            {
                return;
            }

            isDodging = true;
            this.body.linearVelocityY = 0.1f;
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

