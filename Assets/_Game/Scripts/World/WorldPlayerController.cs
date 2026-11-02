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

        private Rigidbody2D body;
        private Camera followCamera;
        private WorldDefinition definition;
        private Vector2 minimumPosition;
        private Vector2 maximumPosition;
        private Vector2 keyboardInput;
        private Vector2 touchInput;
        private Vector3 cameraVelocity;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;

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
                followCamera.transform.position = new Vector3(body.position.x, -0.15f, followCamera.transform.position.z);
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

        private void Update()
        {
            keyboardInput = ReadKeyboardInput();
        }

        private void FixedUpdate()
        {
            if (body == null || definition == null) return;
            Vector2 input = touchInput.sqrMagnitude > 0.01f ? touchInput : keyboardInput;
            Vector2 next = body.position + input * Speed * Time.fixedDeltaTime;
            body.MovePosition(ClampToBounds(next, minimumPosition, maximumPosition));
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

            float x = 0f;
            float y = 0f;
            if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) x += 1f;
            if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) y -= 1f;
            if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) y += 1f;
            var input = new Vector2(x, y);
            return input.sqrMagnitude > 1f ? input.normalized : input;
        }
    }
}
