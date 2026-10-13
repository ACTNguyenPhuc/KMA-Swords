using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace Oath
{
    /// <summary>
    /// Xử lý chạm: chạm nhanh = chọn/ra lệnh, kéo một ngón = di chuyển camera, hai ngón = phóng to/thu nhỏ.
    /// Bỏ qua thao tác bắt đầu trên giao diện để tránh xung đột với nút bấm. Hỗ trợ chuột khi chạy trong Editor.
    /// </summary>
    public class TouchInputController : MonoBehaviour
    {
        public static TouchInputController I { get; private set; }
        public event Action<Vector2> OnTapWorld;

        Camera cam;
        bool pressing, dragging, ignoreUntilRelease;
        Vector2 pressPos, lastPos;
        float pressTime;
        bool pinching;
        float lastPinchDist;
        Vector2 lastMid;

        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        float DragThreshold => Mathf.Max(14f, (Screen.dpi > 0 ? Screen.dpi : 160f) * 0.09f);

        public void Init(Camera c)
        {
            I = this;
            cam = c;
        }

        void OnEnable() { EnhancedTouchSupport.Enable(); }
        void OnDisable() { EnhancedTouchSupport.Disable(); }
        void OnDestroy() { if (I == this) I = null; }

        public static bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var ped = new PointerEventData(es) { position = screenPos };
            hits.Clear();
            es.RaycastAll(ped, hits);
            return hits.Count > 0;
        }

        void Update()
        {
            if (cam == null) return;
            if (Touchscreen.current != null && Touch.activeTouches.Count > 0) HandleTouches();
            else HandleMouse();
        }

        void HandleTouches()
        {
            var ts = Touch.activeTouches;
            if (ts.Count >= 2)
            {
                Vector2 a = ts[0].screenPosition, b = ts[1].screenPosition;
                float dist = Vector2.Distance(a, b);
                Vector2 mid = (a + b) * 0.5f;
                if (!pinching)
                {
                    pinching = true;
                    pressing = false;
                    ignoreUntilRelease = true;
                    lastPinchDist = dist;
                    lastMid = mid;
                    return;
                }
                if (CameraController.I != null && dist > 1f)
                {
                    CameraController.I.Zoom(lastPinchDist / dist, mid);
                    CameraController.I.PanScreen(mid - lastMid);
                }
                lastPinchDist = dist;
                lastMid = mid;
                return;
            }
            pinching = false;

            var t = ts[0];
            Vector2 p = t.screenPosition;
            switch (t.phase)
            {
                case TouchPhase.Began:
                    StartPress(p);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (!pressing && !ignoreUntilRelease) StartPress(p);
                    MovePress(p);
                    break;
                case TouchPhase.Ended:
                    EndPress(p);
                    ignoreUntilRelease = false;
                    break;
                case TouchPhase.Canceled:
                    pressing = false;
                    ignoreUntilRelease = false;
                    break;
            }
        }

        void HandleMouse()
        {
            if (pinching) { pinching = false; ignoreUntilRelease = false; }
            var m = Mouse.current;
            if (m == null) return;
            Vector2 p = m.position.ReadValue();
            float scroll = m.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f && CameraController.I != null && !IsOverUI(p))
                CameraController.I.Zoom(scroll > 0 ? 0.9f : 1.1f, p);

            if (m.leftButton.wasPressedThisFrame) StartPress(p);
            else if (m.leftButton.isPressed) MovePress(p);
            if (m.leftButton.wasReleasedThisFrame) EndPress(p);

            // Chuột phải/giữa: kéo bản đồ
            if (m.rightButton.isPressed || m.middleButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue();
                if (CameraController.I != null) CameraController.I.PanScreen(d);
            }
        }

        void StartPress(Vector2 p)
        {
            pressing = true;
            dragging = false;
            ignoreUntilRelease = IsOverUI(p);
            pressPos = lastPos = p;
            pressTime = Time.unscaledTime;
        }

        void MovePress(Vector2 p)
        {
            if (!pressing || ignoreUntilRelease) { lastPos = p; return; }
            if (!dragging && (p - pressPos).magnitude > DragThreshold) dragging = true;
            if (dragging && CameraController.I != null) CameraController.I.PanScreen(p - lastPos);
            lastPos = p;
        }

        void EndPress(Vector2 p)
        {
            if (pressing && !ignoreUntilRelease && !dragging && Time.unscaledTime - pressTime < 0.6f)
            {
                Vector2 w = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, -cam.transform.position.z));
                OnTapWorld?.Invoke(w);
            }
            pressing = false;
            dragging = false;
        }
    }
}
