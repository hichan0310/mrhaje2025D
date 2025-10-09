using EntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;


namespace PlayerSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerActionController : MonoBehaviour
    {
        [Header("Component References")]
        [SerializeField]
        private Player player;

        [SerializeField]
        private Rigidbody2D body;

        [SerializeField]
        private PlayerInput playerInput;

        [SerializeField]
        private Transform groundCheck;

        [Header("Movement")]
        [SerializeField]
        private float moveSpeed = 8f;

        [SerializeField]
        private float acceleration = 28f;

        [SerializeField]
        private float deceleration = 40f;

        [SerializeField]
        private float airControlMultiplier = 0.5f;

        [Header("Jump")]
        [SerializeField]
        private float jumpForce = 12f;

        [SerializeField]
        private float coyoteTime = 0.15f;

        [SerializeField]
        private float jumpBufferTime = 0.12f;

        [SerializeField]
        private float fallGravityMultiplier = 2.2f;

        [SerializeField]
        private float lowJumpGravityMultiplier = 2.8f;

        [SerializeField]
        private float groundCheckRadius = 0.18f;

        [SerializeField]
        private LayerMask groundLayers = -1;

        [Header("Drop Through Platforms")]
        [SerializeField]
        private LayerMask dropThroughLayers = 0;

        [SerializeField]
        private float dropDuration = 0.35f;

        [Header("Dodge")]
        [SerializeField]
        private float dodgeSpeed = 14f;

        [SerializeField]
        private float dodgeDuration = 0.25f;

        [SerializeField]
        private float dodgeCooldown = 0.55f;

        [SerializeField]
        private AnimationCurve dodgeSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [SerializeField]
        private bool dodgeCancelsVelocity = true;

        [Header("Combat Actions")]
        [SerializeField]
        private bool allowHoldFire = true;

        [SerializeField]
        private float fireInterval = 0.2f;

        [SerializeField]
        private float minAimMagnitude = 0.15f;

        [SerializeField]
        private float skillChargePowerMultiplier = 1f;

        [SerializeField]
        private float ultimateChargePowerMultiplier = 2f;

        [Header("Input Actions")]
        [SerializeField]
        private InputActionReference moveAction;

        [SerializeField]
        private InputActionReference jumpAction;

        [SerializeField]
        private InputActionReference dropAction;

        [SerializeField]
        private InputActionReference fireAction;

        [SerializeField]
        private InputActionReference skillAction;

        [SerializeField]
        private InputActionReference ultimateAction;

        [SerializeField]
        private InputActionReference interactAction;

        [SerializeField]
        private InputActionReference dodgeAction;

        [SerializeField]
        private InputActionReference aimAction;

        [Header("Action Effects")]
        [SerializeField]
        private List<PlayerActionEffectConfig> actionEffects = new List<PlayerActionEffectConfig>();

        [SerializeField]
        private List<ActionEventBinding> actionEvents = new List<ActionEventBinding>();

        private readonly Dictionary<PlayerActionType, PlayerActionEffectConfig> effectLookup = new Dictionary<PlayerActionType, PlayerActionEffectConfig>();
        private readonly Dictionary<PlayerActionType, PlayerActionUnityEvent> eventLookup = new Dictionary<PlayerActionType, PlayerActionUnityEvent>();
        private readonly List<Action> unbinders = new List<Action>();
        private readonly List<InputAction> enabledActions = new List<InputAction>();
        private readonly HashSet<string> missingActionWarnings = new HashSet<string>();

        private Vector2 moveInput;
        private bool jumpHeld;
        private float jumpBufferTimer;
        private float coyoteTimer;
        private bool grounded;
        private bool isDropping;
        private float dropTimer;

        private Collider2D playerCollider;

        private bool isDodging;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private Vector2 dodgeDirection = Vector2.right;

        private bool isFiring;
        private float fireTimer;
        private float fireStartTime;

        private bool skillCharging;
        private float skillChargeStart;

        private bool ultimateCharging;
        private float ultimateChargeStart;

        private float justDodgeTimer;
        private Vector2 facingDirection = Vector2.right;
        private Entity primaryTarget;

        private void Reset()
        {
            player = GetComponent<Player>();
            body = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            groundCheck = transform;
            playerCollider = GetComponent<Collider2D>();

            EnsureLayerMasksConfigured();
            EnsureRigidbodySetup();
            EnsurePlayerLayerAssignment();
        }

        private void Awake()
        {
            CacheComponents();
            EnsureLayerMasksConfigured();
            EnsureColliderSetup();
            EnsureRigidbodySetup();
            EnsurePlayerLayerAssignment();
            BuildEffectLookup();
            BuildEventLookup();
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            acceleration = Mathf.Max(0f, acceleration);
            deceleration = Mathf.Max(0f, deceleration);
            airControlMultiplier = Mathf.Clamp01(airControlMultiplier);
            jumpForce = Mathf.Max(0f, jumpForce);
            coyoteTime = Mathf.Max(0f, coyoteTime);
            jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
            fallGravityMultiplier = Mathf.Max(0.1f, fallGravityMultiplier);
            lowJumpGravityMultiplier = Mathf.Max(0.1f, lowJumpGravityMultiplier);
            groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
            dropDuration = Mathf.Max(0f, dropDuration);
            dodgeSpeed = Mathf.Max(0f, dodgeSpeed);
            dodgeDuration = Mathf.Max(0.01f, dodgeDuration);
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown);
            fireInterval = Mathf.Max(0.01f, fireInterval);
            minAimMagnitude = Mathf.Max(0.01f, minAimMagnitude);
            CacheComponents();
            EnsureLayerMasksConfigured();
            EnsureColliderSetup();
            EnsureRigidbodySetup();
            EnsurePlayerLayerAssignment();
            BuildEffectLookup();
            BuildEventLookup();
        }

        private void OnEnable()
        {
            BuildEffectLookup();
            BuildEventLookup();
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
            CleanupEffects();
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            UpdateGroundState();
            UpdateJumpTimers(deltaTime);
            UpdateDrop(deltaTime);
            UpdateDodge(deltaTime);
            UpdateFire(deltaTime);
            UpdateChargeTimers();
            UpdateJustDodgeTimer(deltaTime);
        }

        private void FixedUpdate()
        {
            var deltaTime = Time.fixedDeltaTime;
            ApplyHorizontalMovement(deltaTime);
            ApplyJump();
            ApplyGravityModifiers();
            DispatchMovementAction(deltaTime);
        }

        public void SetPrimaryTarget(Entity target)
        {
            primaryTarget = target;
        }

        public void EnterJustDodgeWindow(float duration)
        {
            justDodgeTimer = Mathf.Max(justDodgeTimer, duration);
        }

        private void EnableInput()
        {
            CacheComponents();
            missingActionWarnings.Clear();
            BindAction(moveAction, "Move", OnMovePerformed, OnMoveCanceled);
            BindAction(jumpAction, "Jump", OnJumpStarted, OnJumpPerformed, OnJumpCanceled);
            BindAction(dropAction, "DownJump|Drop", null, OnDropPerformed);
            BindAction(fireAction, "Attack|Fire", OnFireStarted, OnFirePerformed, OnFireCanceled);
            BindAction(skillAction, "Skill", OnSkillStarted, null, OnSkillCanceled);
            BindAction(ultimateAction, "Ultimate", OnUltimateStarted, null, OnUltimateCanceled);
            BindAction(interactAction, "Interact", null, OnInteractPerformed);
            BindAction(dodgeAction, "Dodge", OnDodgeStarted, null, null);

            var aim = ResolveAction(aimAction, "Look|Aim", logIfMissing: false);
            if (aim != null)
            {
                if (!aim.enabled)
                {
                    aim.Enable();
                }

                if (!enabledActions.Contains(aim))
                {
                    enabledActions.Add(aim);
                }
            }
        }

        private void DisableInput()
        {
            foreach (var unbinder in unbinders)
            {
                unbinder?.Invoke();
            }
            unbinders.Clear();

            foreach (var action in enabledActions)
            {
                action.Disable();
            }
            enabledActions.Clear();

            isFiring = false;
            skillCharging = false;
            ultimateCharging = false;
        }

        private void BindAction(InputActionReference reference,
            string fallbackActionName,
            Action<InputAction.CallbackContext> started,
            Action<InputAction.CallbackContext> performed,
            Action<InputAction.CallbackContext> canceled = null)
        {
            var action = ResolveAction(reference, fallbackActionName);
            if (action == null)
            {
                return;
            }

            if (started != null)
            {
                action.started += started;
                unbinders.Add(() => action.started -= started);
            }

            if (performed != null)
            {
                action.performed += performed;
                unbinders.Add(() => action.performed -= performed);
            }

            if (canceled != null)
            {
                action.canceled += canceled;
                unbinders.Add(() => action.canceled -= canceled);
            }

            if (!action.enabled)
            {
                action.Enable();
            }

            if (!enabledActions.Contains(action))
            {
                enabledActions.Add(action);
            }
        }

        private InputAction ResolveAction(InputActionReference reference, string fallbackActionNames, bool logIfMissing = true)
        {
            InputAction action = null;

            if (reference != null)
            {
                try
                {
                    action = reference.action;
                }
                catch (InvalidOperationException)
                {
                    action = null;
                }
            }

#if ENABLE_INPUT_SYSTEM
            if (action == null && playerInput != null && !string.IsNullOrEmpty(fallbackActionNames))
            {
                var actions = playerInput.actions;
                if (actions != null)
                {
                    var names = fallbackActionNames.Split('|');
                    for (var i = 0; i < names.Length; i++)
                    {
                        var candidate = names[i].Trim();
                        if (string.IsNullOrEmpty(candidate))
                        {
                            continue;
                        }

                        action = actions.FindAction(candidate, throwIfNotFound: false);
                        if (action != null)
                        {
                            break;
                        }
                    }
                }
            }
#endif

            if (logIfMissing && action == null && !string.IsNullOrEmpty(fallbackActionNames) && missingActionWarnings.Add(fallbackActionNames))
            {
                Debug.LogWarning($"입력 액션 '{fallbackActionNames}'을 찾지 못했습니다. PlayerInput 또는 InputActionReference 설정을 확인하세요.", this);
            }

            return action;
        }

        private void CacheComponents()
        {
            player = player != null ? player : GetComponent<Player>();
            body = body != null ? body : GetComponent<Rigidbody2D>();
            playerInput = playerInput != null ? playerInput : GetComponent<PlayerInput>();
            groundCheck = groundCheck != null ? groundCheck : transform;
            playerCollider = playerCollider != null ? playerCollider : GetComponent<Collider2D>();
        }

        private void OnMovePerformed(InputAction.CallbackContext ctx)
        {
            moveInput = ReadVector2(ctx);
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                facingDirection = new Vector2(Mathf.Sign(moveInput.x), 0f);
            }
        }

        private void OnMoveCanceled(InputAction.CallbackContext ctx)
        {
            moveInput = Vector2.zero;
        }

        private void OnJumpStarted(InputAction.CallbackContext ctx)
        {
            jumpHeld = true;
            jumpBufferTimer = jumpBufferTime;
        }

        private void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            jumpHeld = true;
            jumpBufferTimer = jumpBufferTime;
        }

        private void OnJumpCanceled(InputAction.CallbackContext ctx)
        {
            jumpHeld = false;
        }

        private void OnDropPerformed(InputAction.CallbackContext ctx)
        {
            if (!grounded)
            {
                return;
            }

            StartDropThroughPlatforms();
        }

        private void OnFireStarted(InputAction.CallbackContext ctx)
        {
            fireStartTime = Time.time;
            fireTimer = 0f;
            if (allowHoldFire)
            {
                isFiring = true;
                FireProjectile(0f);
            }
        }

        private void OnFirePerformed(InputAction.CallbackContext ctx)
        {
            if (!allowHoldFire)
            {
                FireProjectile(Time.time - fireStartTime);
            }
        }

        private void OnFireCanceled(InputAction.CallbackContext ctx)
        {
            isFiring = false;
        }

        private void OnSkillStarted(InputAction.CallbackContext ctx)
        {
            skillCharging = true;
            skillChargeStart = Time.time;
        }

        private void OnSkillCanceled(InputAction.CallbackContext ctx)
        {
            if (!skillCharging)
            {
                return;
            }

            var charge = Mathf.Max(0f, Time.time - skillChargeStart);
            skillCharging = false;
            HandleAction(PlayerActionType.Skill, 1f + charge * skillChargePowerMultiplier, charge, DetermineAimDirection(), primaryTarget, charge, false);
        }

        private void OnUltimateStarted(InputAction.CallbackContext ctx)
        {
            ultimateCharging = true;
            ultimateChargeStart = Time.time;
        }

        private void OnUltimateCanceled(InputAction.CallbackContext ctx)
        {
            if (!ultimateCharging)
            {
                return;
            }

            var charge = Mathf.Max(0f, Time.time - ultimateChargeStart);
            ultimateCharging = false;
            HandleAction(PlayerActionType.Ultimate, 1f + charge * ultimateChargePowerMultiplier, charge, DetermineAimDirection(), primaryTarget, charge, false);
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            HandleAction(PlayerActionType.Interact, 1f, 0f, DetermineAimDirection(), primaryTarget, 0f, false);
        }

        private void OnDodgeStarted(InputAction.CallbackContext ctx)
        {
            TryStartDodge();
        }

        private void UpdateGroundState()
        {
            grounded = CheckGrounded();
            if (grounded)
            {
                coyoteTimer = coyoteTime;
            }
        }

        private bool CheckGrounded()
        {
            var position = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
            var hit = Physics2D.OverlapCircle(position, groundCheckRadius, groundLayers);
            return hit != null;
        }

        private void UpdateJumpTimers(float deltaTime)
        {
            if (coyoteTimer > 0f)
            {
                coyoteTimer -= deltaTime;
            }

            if (jumpBufferTimer > 0f)
            {
                jumpBufferTimer -= deltaTime;
            }
        }

        private void UpdateDrop(float deltaTime)
        {
            if (!isDropping)
            {
                return;
            }

            dropTimer -= deltaTime;
            if (dropTimer <= 0f)
            {
                StopDropThroughPlatforms();
            }
        }

        private void UpdateDodge(float deltaTime)
        {
            if (dodgeCooldownTimer > 0f)
            {
                dodgeCooldownTimer -= deltaTime;
            }

            if (!isDodging)
            {
                return;
            }

            dodgeTimer -= deltaTime;
            var normalized = 1f - Mathf.Clamp01(dodgeTimer / Mathf.Max(0.0001f, dodgeDuration));
            var curveValue = dodgeSpeedCurve != null ? dodgeSpeedCurve.Evaluate(normalized) : 1f;
            var velocity = dodgeDirection * dodgeSpeed * curveValue;
            body.velocity = new Vector2(velocity.x, body.velocity.y);

            if (dodgeTimer <= 0f)
            {
                isDodging = false;
            }
        }

        private void UpdateFire(float deltaTime)
        {
            if (!isFiring)
            {
                return;
            }

            fireTimer -= deltaTime;
            if (fireTimer <= 0f)
            {
                var holdTime = Time.time - fireStartTime;
                FireProjectile(holdTime);
                fireTimer = fireInterval;
            }
        }

        private void UpdateChargeTimers()
        {
            if (skillCharging && skillAction != null)
            {
                var action = skillAction.action;
                if (action == null || !action.inProgress)
                {
                    skillCharging = false;
                }
            }

            if (ultimateCharging && ultimateAction != null)
            {
                var action = ultimateAction.action;
                if (action == null || !action.inProgress)
                {
                    ultimateCharging = false;
                }
            }
        }

        private void UpdateJustDodgeTimer(float deltaTime)
        {
            if (justDodgeTimer > 0f)
            {
                justDodgeTimer -= deltaTime;
            }
        }

        private void ApplyHorizontalMovement(float deltaTime)
        {
            if (isDodging)
            {
                return;
            }

            var targetSpeed = moveInput.x * moveSpeed;
            var control = grounded ? 1f : airControlMultiplier;
            var accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            accelRate *= control;

            var velocity = body.velocity;
            var maxDelta = accelRate * deltaTime;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, maxDelta);
            body.velocity = velocity;
        }

        private void ApplyJump()
        {
            if (jumpBufferTimer <= 0f)
            {
                return;
            }

            if (grounded || coyoteTimer > 0f)
            {
                var velocity = body.velocity;
                velocity.y = jumpForce;
                body.velocity = velocity;
                grounded = false;
                coyoteTimer = 0f;
                jumpBufferTimer = 0f;
                HandleAction(PlayerActionType.Jump, 1f, 0f, Vector2.up, primaryTarget, 0f, false);
            }
        }

        private void ApplyGravityModifiers()
        {
            if (body.velocity.y < -0.01f)
            {
                body.gravityScale = fallGravityMultiplier;
            }
            else if (body.velocity.y > 0.01f && !jumpHeld)
            {
                body.gravityScale = lowJumpGravityMultiplier;
            }
            else
            {
                body.gravityScale = 1f;
            }
        }

        private void DispatchMovementAction(float deltaTime)
        {
            if (Mathf.Abs(moveInput.x) < 0.01f)
            {
                return;
            }

            var aim = new Vector2(Mathf.Sign(moveInput.x), 0f);
            var intensity = Mathf.Clamp01(Mathf.Abs(moveInput.x));
            HandleAction(PlayerActionType.Move, intensity, 0f, aim, primaryTarget, deltaTime, false);
        }

        private void FireProjectile(float holdTime)
        {
            var aim = DetermineAimDirection();
            HandleAction(PlayerActionType.Fire, 1f, holdTime, aim, primaryTarget, 0f, false);
            fireTimer = fireInterval;
        }

        private Vector2 DetermineAimDirection()
        {
            Vector2 aim = Vector2.zero;
            if (aimAction != null && aimAction.action != null)
            {
                aim = ReadVector2(aimAction.action);
            }

            if (aim.sqrMagnitude < minAimMagnitude * minAimMagnitude)
            {
                if (Mathf.Abs(moveInput.x) > 0.01f)
                {
                    aim = new Vector2(Mathf.Sign(moveInput.x), 0f);
                }
                else
                {
                    aim = facingDirection.sqrMagnitude > 0.01f ? facingDirection : Vector2.right;
                }
            }

            return aim.normalized;
        }

        private static Vector2 ReadVector2(InputAction.CallbackContext ctx)
        {
            var type = ctx.valueType;
            if (type == typeof(Vector2))
            {
                return ctx.ReadValue<Vector2>();
            }

            if (type == typeof(Vector3))
            {
                var value = ctx.ReadValue<Vector3>();
                return new Vector2(value.x, value.y);
            }

            if (type == typeof(Vector4))
            {
                var value = ctx.ReadValue<Vector4>();
                return new Vector2(value.x, value.y);
            }

            if (type == typeof(float))
            {
                return new Vector2(ctx.ReadValue<float>(), 0f);
            }

            if (type == typeof(int))
            {
                return new Vector2(ctx.ReadValue<int>(), 0f);
            }

            return Vector2.zero;
        }

        private static Vector2 ReadVector2(InputAction action)
        {
            if (action == null) return Vector2.zero;

            object val;
            try
            {
                val = action.ReadValueAsObject();
            }
            catch
            {
                return Vector2.zero;
            }

            switch (val)
            {
                case Vector2 v2: return v2;
                case Vector3 v3: return new Vector2(v3.x, v3.y);
                case Vector4 v4: return new Vector2(v4.x, v4.y);
                case float f: return new Vector2(f, 0f);
                case double d: return new Vector2((float)d, 0f);
                case int i: return new Vector2(i, 0f);
                case bool b: return new Vector2(b ? 1f : 0f, 0f);
            }

            var ctrl = action.activeControl ?? action.controls.FirstOrDefault();
            var t = ctrl?.valueType;
            if (t == typeof(Vector2)) return action.ReadValue<Vector2>();
            if (t == typeof(Vector3)) { var v = action.ReadValue<Vector3>(); return new Vector2(v.x, v.y); }
            if (t == typeof(Vector4)) { var v = action.ReadValue<Vector4>(); return new Vector2(v.x, v.y); }
            if (t == typeof(float)) return new Vector2(action.ReadValue<float>(), 0f);
            if (t == typeof(int)) return new Vector2(action.ReadValue<int>(), 0f);

            return Vector2.zero;
        }

        private void StartDropThroughPlatforms()
        {
            if (isDropping)
            {
                return;
            }

            var layers = ExtractLayers(dropThroughLayers);
            if (layers.Count == 0)
            {
                return;
            }

            foreach (var layer in layers)
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, layer, true);
            }

            isDropping = true;
            dropTimer = dropDuration;
            HandleAction(PlayerActionType.DropThrough, 1f, 0f, Vector2.down, primaryTarget, dropDuration, false);
        }

        private void StopDropThroughPlatforms()
        {
            var layers = ExtractLayers(dropThroughLayers);
            foreach (var layer in layers)
            {
                Physics2D.IgnoreLayerCollision(gameObject.layer, layer, false);
            }

            isDropping = false;
            dropTimer = 0f;
        }

        private List<int> ExtractLayers(LayerMask mask)
        {
            var layers = new List<int>();
            for (var i = 0; i < 32; i++)
            {
                if (((1 << i) & mask.value) != 0)
                {
                    layers.Add(i);
                }
            }

            return layers;
        }

        private void EnsureLayerMasksConfigured()
        {
            if (groundLayers.value == 0)
            {
                var combined = 0;
                var groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                {
                    combined |= 1 << groundLayer;
                }

                var platformLayer = LayerMask.NameToLayer("Platform");
                if (platformLayer >= 0)
                {
                    combined |= 1 << platformLayer;
                }

                groundLayers = combined != 0 ? combined : ~0;
            }

            if (dropThroughLayers.value == 0)
            {
                var platformLayer = LayerMask.NameToLayer("Platform");
                if (platformLayer >= 0)
                {
                    dropThroughLayers = 1 << platformLayer;
                }
            }
        }

        private void EnsureColliderSetup()
        {
            if (playerCollider == null)
            {
                Debug.LogWarning("PlayerActionController에 Collider2D가 필요합니다. 플레이어 오브젝트에 Collider2D를 추가하세요.", this);
                return;
            }

            if (playerCollider.isTrigger)
            {
                Debug.LogWarning("플레이어 Collider2D가 Trigger로 설정되어 있어 충돌 판정이 되지 않습니다. Trigger 옵션을 해제하세요.", playerCollider);
            }
        }

        private void EnsureRigidbodySetup()
        {
            if (body == null)
            {
                Debug.LogWarning("PlayerActionController에 Rigidbody2D가 필요합니다. 플레이어 오브젝트에 Rigidbody2D를 추가하세요.", this);
                return;
            }

            if (body.bodyType != RigidbodyType2D.Dynamic)
            {
                Debug.LogWarning($"플레이어 Rigidbody2D가 {body.bodyType}로 설정되어 있습니다. 플랫폼 이동을 위해 Dynamic으로 변경합니다.", body);
                body.bodyType = RigidbodyType2D.Dynamic;
            }

            if ((body.constraints & RigidbodyConstraints2D.FreezeRotation) == 0)
            {
                body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            }

            if (Mathf.Abs(body.rotation) > 0.01f || Mathf.Abs(body.angularVelocity) > 0.01f)
            {
                body.rotation = 0f;
                body.angularVelocity = 0f;
            }
        }

        private void EnsurePlayerLayerAssignment()
        {
            var playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0 && gameObject.layer != playerLayer)
            {
                gameObject.layer = playerLayer;
            }
        }

        private void TryStartDodge()
        {
            if (isDodging || dodgeCooldownTimer > 0f)
            {
                return;
            }

            var direction = moveInput.sqrMagnitude > 0.01f ? new Vector2(Mathf.Sign(moveInput.x), 0f) : facingDirection;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.right;
            }

            var wasJust = justDodgeTimer > 0f;
            justDodgeTimer = 0f;

            dodgeDirection = direction.normalized;
            isDodging = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;

            if (dodgeCancelsVelocity)
            {
                body.velocity = Vector2.zero;
            }

            HandleAction(PlayerActionType.Dodge, 1f, 0f, dodgeDirection, primaryTarget, dodgeDuration, wasJust);
        }

        private void BuildEffectLookup()
        {
            effectLookup.Clear();
            foreach (var config in actionEffects)
            {
                if (config == null)
                {
                    continue;
                }

                if (!effectLookup.ContainsKey(config.Action))
                {
                    effectLookup.Add(config.Action, config);
                }
            }
        }

        private void BuildEventLookup()
        {
            eventLookup.Clear();
            foreach (var binding in actionEvents)
            {
                if (binding == null)
                {
                    continue;
                }

                if (!eventLookup.ContainsKey(binding.Action))
                {
                    eventLookup.Add(binding.Action, binding.Event);
                }
            }
        }

        private void CleanupEffects()
        {
            foreach (var config in actionEffects)
            {
                config?.Cleanup();
            }
        }

        private void HandleAction(
            PlayerActionType actionType,
            float powerMultiplier,
            float chargeTime,
            Vector2 aimDirection,
            Entity target,
            float duration,
            bool isJust)
        {
            var resolvedTarget = target != null ? target : primaryTarget;
            var normalizedAim = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : facingDirection;
            var currentSpeed = body != null ? body.velocity.magnitude : 0f;
            var groundedState = grounded;

            var resolvedPower = powerMultiplier;
            if (effectLookup.TryGetValue(actionType, out var config))
            {
                resolvedPower = config.ResolvePower(powerMultiplier, chargeTime, isJust);
                var fallbackTarget = resolvedTarget != null ? resolvedTarget : player;
                config.InvokeEffects(player, fallbackTarget, resolvedPower, chargeTime, isJust);
            }

            var context = new PlayerActionContext(
                player,
                actionType,
                moveInput,
                normalizedAim,
                chargeTime,
                powerMultiplier,
                isJust,
                groundedState,
                resolvedTarget,
                duration,
                currentSpeed,
                resolvedPower);

            var args = new PlayerActionEventArgs(player, context);
            args.trigger();

            if (eventLookup.TryGetValue(actionType, out var unityEvent))
            {
                unityEvent?.Invoke(context);
            }
        }

        [Serializable]
        private class ActionEventBinding
        {
            [SerializeField]
            private PlayerActionType action = PlayerActionType.Move;

            [SerializeField]
            private PlayerActionUnityEvent onAction = new PlayerActionUnityEvent();

            public PlayerActionType Action => action;
            public PlayerActionUnityEvent Event => onAction;
        }

        [Serializable]
        private class PlayerActionUnityEvent : UnityEvent<PlayerActionContext>
        {
        }

        [Serializable]
        private class PlayerActionEffectConfig
        {
            public PlayerActionType Action => action;

            [SerializeField]
            private PlayerActionType action = PlayerActionType.Move;

            [SerializeField]
            private float basePower = 1f;

            [SerializeField]
            private bool scaleWithInputIntensity = true;

            [SerializeField]
            private bool useChargeCurve = false;

            [SerializeField]
            private AnimationCurve chargePowerCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

            [SerializeField]
            private float maxChargeTime = 1f;

            [SerializeField]
            private bool boostOnJustDodge = true;

            [SerializeField]
            private float justDodgeMultiplier = 1.5f;

            [SerializeField]
            private ActionTargetMode targetMode = ActionTargetMode.Self;

            [SerializeField]
            private Entity explicitTarget;

            [SerializeReference]
            private List<ITriggerEffect> triggerEffects = new List<ITriggerEffect>();

            [SerializeField]
            private bool removeEffectsOnDisable = true;

            public float ResolvePower(float powerMultiplier, float chargeTime, bool isJustDodge)
            {
                var resolved = basePower;
                if (useChargeCurve && chargePowerCurve != null)
                {
                    var normalized = maxChargeTime > 0f ? Mathf.Clamp01(chargeTime / maxChargeTime) : 0f;
                    resolved *= chargePowerCurve.Evaluate(normalized);
                }

                if (scaleWithInputIntensity)
                {
                    resolved *= powerMultiplier;
                }

                if (isJustDodge && boostOnJustDodge)
                {
                    resolved *= justDodgeMultiplier;
                }

                return Mathf.Max(0f, resolved);
            }

            public void InvokeEffects(Player source, Entity fallbackTarget, float power, float chargeTime, bool isJustDodge)
            {
                if (triggerEffects == null || triggerEffects.Count == 0)
                {
                    return;
                }

                var target = DetermineTarget(source, fallbackTarget);
                if (target == null)
                {
                    return;
                }

                foreach (var effect in triggerEffects)
                {
                    effect?.trigger(target, power);
                }
            }

            public void Cleanup()
            {
                if (!removeEffectsOnDisable || triggerEffects == null)
                {
                    return;
                }

                foreach (var effect in triggerEffects)
                {
                    if (effect is IBuff buff)
                    {
                        buff.removeSelf();
                    }
                }
            }

            private Entity DetermineTarget(Player source, Entity fallbackTarget)
            {
                switch (targetMode)
                {
                    case ActionTargetMode.Explicit:
                        return explicitTarget != null ? explicitTarget : fallbackTarget;
                    default:
                        return fallbackTarget != null ? fallbackTarget : source;
                }
            }
        }

        private enum ActionTargetMode
        {
            Self,
            Explicit
        }
    }
}
