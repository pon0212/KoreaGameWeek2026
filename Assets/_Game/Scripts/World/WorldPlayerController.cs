using TastePassport.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TastePassport.World
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class WorldPlayerController : MonoBehaviour
    {
        private const float Speed = 3.2f;
        private const float CollisionInset = 0.28f;
        private const float CameraFollowTime = 0.18f;
        private const float TownCameraClearance = 10f;

        private Rigidbody2D body;
        private Camera followCamera;
        private WorldDefinition definition;
        private Vector2 minimumPosition;
        private Vector2 maximumPosition;
        private Vector2 keyboardInput;
        private Vector2 touchInput;
        private Vector3 cameraVelocity;
        private WorldCharacterModelBuilder characterModel;
        private bool movementEnabled = true;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public bool HasMoved { get; private set; }

        private void Awake()
        {
            EnsureBody();
        }

        private void EnsureBody()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (body == null) return;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;

            var collider = GetComponent<CircleCollider2D>();
            collider.radius = 0.32f;
        }

        public void Initialize(Camera camera, WorldDefinition world, Vector2 startPosition)
        {
            EnsureBody();
            followCamera = camera;
            definition = world;
            minimumPosition = world.MinimumPosition + new Vector2(CollisionInset, CollisionInset);
            maximumPosition = world.MaximumPosition - new Vector2(CollisionInset, CollisionInset);
            if (body == null) return;
            body.position = ClampToBounds(startPosition, minimumPosition, maximumPosition);

            if (followCamera != null)
            {
                followCamera.orthographic = true;
                followCamera.orthographicSize = 3.3f;
                // A restrained isometric angle gives the generated town visible side faces while
                // preserving the existing XY movement plane and orthographic coordinate scale.
                followCamera.transform.rotation = Quaternion.Euler(6f, -8f, 0f);
                // The runtime town uses real depth around z=0. Keep its meshes behind the
                // camera-facing world-space HUD, whose plane sits just beyond the near clip.
                float cameraDepth = Mathf.Min(followCamera.transform.position.z, -TownCameraClearance);
                followCamera.transform.position = new Vector3(body.position.x, -0.15f, cameraDepth);
            }
        }

        public void SetPosition(Vector2 position)
        {
            EnsureBody();
            if (body == null) return;
            Vector2 bounded = ClampToBounds(position, minimumPosition, maximumPosition);
            body.position = bounded;
            transform.position = new Vector3(bounded.x, bounded.y, transform.position.z);
            if (followCamera != null)
                followCamera.transform.position = new Vector3(bounded.x, bounded.y, followCamera.transform.position.z);
        }

        public void SetTouchInput(Vector2 input)
        {
            touchInput = Vector2.ClampMagnitude(input, 1f);
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!enabled)
            {
                keyboardInput = Vector2.zero;
                touchInput = Vector2.zero;
            }
        }

        public void SetCharacterModel(WorldCharacterModelBuilder model)
        {
            characterModel = model;
            if (characterModel != null) characterModel.SetFacingLeft(false);
        }

        private void Update()
        {
            keyboardInput = movementEnabled ? ReadKeyboardInput() : Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (body == null || definition == null) return;
            Vector2 input = movementEnabled ? (touchInput.sqrMagnitude > 0.01f ? touchInput : keyboardInput) : Vector2.zero;
            Vector2 next = body.position + input * Speed * Time.fixedDeltaTime;
            Vector2 bounded = ClampToBounds(next, minimumPosition, maximumPosition);
            if (Vector2.SqrMagnitude(bounded - body.position) > 0.0001f) HasMoved = true;
            body.MovePosition(bounded);
            AnimateVisual(input);
        }

        private void AnimateVisual(Vector2 input)
        {
            bool walking = input.sqrMagnitude > 0.01f;
            if (characterModel == null) return;
            characterModel.SetWalking(walking);
            if (Mathf.Abs(input.x) > 0.01f) characterModel.SetFacingLeft(input.x < 0f);
        }

        private void LateUpdate()
        {
            if (followCamera == null || definition == null) return;
            float halfWidth = followCamera.orthographicSize * followCamera.aspect;
            float cameraMinX = definition.MinimumPosition.x + halfWidth;
            float cameraMaxX = definition.MaximumPosition.x - halfWidth;
            float targetX = cameraMinX > cameraMaxX
                ? (definition.MinimumPosition.x + definition.MaximumPosition.x) * 0.5f
                : Mathf.Clamp(transform.position.x, cameraMinX, cameraMaxX);
            float targetY = Mathf.Clamp(transform.position.y + 0.45f, -0.45f, 0.35f);
            Vector3 target = new Vector3(targetX, targetY, followCamera.transform.position.z);
            followCamera.transform.position = Vector3.SmoothDamp(followCamera.transform.position, target, ref cameraVelocity, CameraFollowTime);
        }

        public static Vector2 ClampToBounds(Vector2 position, Vector2 minimum, Vector2 maximum)
        {
            return new Vector2(
                Mathf.Clamp(position.x, minimum.x, maximum.x),
                Mathf.Clamp(position.y, minimum.y, maximum.y));
        }

        private static Vector2 ReadKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            return GetKeyboardMovement(
                keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed,
                keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed,
                keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed,
                keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed);
        }

        public static Vector2 GetKeyboardMovement(bool leftPressed, bool rightPressed, bool downPressed, bool upPressed)
        {
            float x = (rightPressed ? 1f : 0f) - (leftPressed ? 1f : 0f);
            float y = (upPressed ? 1f : 0f) - (downPressed ? 1f : 0f);
            var input = new Vector2(x, y);
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }
    }
}
