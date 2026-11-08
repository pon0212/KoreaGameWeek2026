using UnityEngine;

namespace TastePassport.UI
{
    /// <summary>Fits an owned world-space UI to a camera without changing the camera.</summary>
    public static class WorldSpaceCanvasFitter
    {
        public static bool Configure(Canvas canvas, Camera camera)
        {
            if (canvas == null || camera == null)
            {
                Debug.LogWarning("Cannot fit the world-space UI: a Canvas and main Camera are required.", canvas);
                return false;
            }

            var rect = canvas.transform as RectTransform;
            if (rect == null || camera.pixelWidth <= 0 || camera.pixelHeight <= 0 ||
                camera.farClipPlane <= camera.nearClipPlane)
            {
                Debug.LogWarning("Cannot fit the world-space UI: a RectTransform and a non-empty camera viewport/clipping range are required.", canvas);
                return false;
            }

            Vector3 parentScale = rect.parent == null ? Vector3.one : rect.parent.lossyScale;
            if (parentScale.x <= 0f || parentScale.y <= 0f || parentScale.z <= 0f)
            {
                Debug.LogWarning("Cannot fit the world-space UI under a zero or mirrored parent scale.", canvas);
                return false;
            }

            // Expand the reference area on tall/wide displays, never crop its 720 x 1280 layout.
            float referenceScale = Mathf.Min(camera.pixelWidth / 720f, camera.pixelHeight / 1280f);
            var logicalSize = new Vector2(camera.pixelWidth / referenceScale, camera.pixelHeight / referenceScale);
            // Keep the fitted Canvas just beyond the near plane and in front of town geometry.
            float distance = camera.nearClipPlane + Mathf.Min(0.02f, (camera.farClipPlane - camera.nearClipPlane) * 0.5f);
            float worldScale = MeasureWorldScale(camera, logicalSize.y, distance);

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.pixelPerfect = false;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = logicalSize;
            rect.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(.5f, .5f, distance)), camera.transform.rotation);
            rect.localScale = new Vector3(worldScale / parentScale.x, worldScale / parentScale.y, worldScale / parentScale.z);
            return true;
        }

        private static float MeasureWorldScale(Camera camera, float logicalHeight, float distance)
        {
            Vector3 bottom = camera.ViewportToWorldPoint(new Vector3(.5f, 0f, distance));
            Vector3 top = camera.ViewportToWorldPoint(new Vector3(.5f, 1f, distance));
            return Vector3.Distance(bottom, top) / logicalHeight;
        }
    }
}
