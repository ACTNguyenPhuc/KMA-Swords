using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace Oath
{
    /// <summary>Đảm bảo scene có Camera, ánh sáng 2D toàn cục và EventSystem (dùng cả trong Editor lẫn lúc chạy).</summary>
    public static class SceneSetup
    {
        public static readonly Color WaterColor = new Color(0.278f, 0.671f, 0.663f);

        public static Camera EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = WaterColor;
            cam.transform.position = new Vector3(0, 0, -10);
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            return cam;
        }

        public static void EnsureLight()
        {
            foreach (var l in Object.FindObjectsByType<Light2D>())
                if (l.lightType == Light2D.LightType.Global) return;
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static Camera EnsureAll()
        {
            var c = EnsureCamera();
            EnsureLight();
            EnsureEventSystem();
            return c;
        }
    }
}
