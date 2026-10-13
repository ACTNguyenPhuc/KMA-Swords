using UnityEngine;

namespace Oath
{
    /// <summary>Camera trực giao: kéo/phóng to trong vùng giới hạn của bản đồ, rung nhẹ khi có sự kiện lớn.</summary>
    public class CameraController : MonoBehaviour
    {
        public static CameraController I { get; private set; }

        Camera cam;
        Rect bounds;
        Vector3 basePos;
        float minSize = 4f, maxSizeLimit = 11f;
        float shake;
        float targetSize;

        public Camera Cam => cam;

        public void Init(Camera c, Rect worldBounds, Vector2 focus, float size)
        {
            I = this;
            cam = c;
            bounds = worldBounds;
            cam.orthographic = true;
            targetSize = size;
            cam.orthographicSize = size;
            basePos = new Vector3(focus.x, focus.y, -10f);
            Clamp();
            Apply();
        }

        void OnDestroy() { if (I == this) I = null; }

        float Aspect => Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;

        float MaxSize
        {
            get
            {
                float byH = bounds.height * 0.5f;
                float byW = bounds.width * 0.5f / Aspect;
                return Mathf.Max(minSize, Mathf.Min(maxSizeLimit, Mathf.Min(byH, byW)));
            }
        }

        public void PanScreen(Vector2 screenDelta)
        {
            if (cam == null) return;
            float upp = cam.orthographicSize * 2f / Mathf.Max(1, Screen.height);
            basePos -= (Vector3)(screenDelta * upp);
            Clamp();
        }

        public void Zoom(float factor, Vector2 screenPivot)
        {
            if (cam == null) return;
            Vector3 before = cam.ScreenToWorldPoint(screenPivot);
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize * factor, minSize, MaxSize);
            targetSize = cam.orthographicSize;
            Vector3 after = cam.ScreenToWorldPoint(screenPivot);
            basePos += before - after;
            basePos.z = -10f;
            Clamp();
        }

        public void FocusOn(Vector2 p)
        {
            basePos = new Vector3(p.x, p.y, -10f);
            Clamp();
        }

        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        void Clamp()
        {
            if (cam == null) return;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minSize, MaxSize);
            float halfH = cam.orthographicSize, halfW = halfH * Aspect;
            basePos.x = bounds.width <= halfW * 2f ? bounds.center.x : Mathf.Clamp(basePos.x, bounds.xMin + halfW, bounds.xMax - halfW);
            basePos.y = bounds.height <= halfH * 2f ? bounds.center.y : Mathf.Clamp(basePos.y, bounds.yMin + halfH, bounds.yMax - halfH);
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Clamp();
            Apply();
        }

        void Apply()
        {
            Vector3 off = Vector3.zero;
            if (shake > 0f)
            {
                shake -= Time.unscaledDeltaTime;
                off = (Vector3)(Random.insideUnitCircle * Mathf.Min(0.25f, shake * 0.5f));
            }
            transform.position = basePos + off;
        }
    }
}
