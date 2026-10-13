using UnityEngine;

namespace Oath
{
    /// <summary>
    /// Khởi tạo một màn chơi từ LevelConfig: dựng bản đồ, công trình, hệ thống (tài nguyên, đợt, xây dựng,
    /// chọn/điều khiển, camera), HUD rồi chuyển sang phần giới thiệu cốt truyện.
    /// </summary>
    public class LevelBootstrap : MonoBehaviour
    {
        public LevelConfig config;

        void Awake()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            GameEvents.ResetAll();
            World.Clear();

            if (config == null)
            {
                Debug.LogError("LevelBootstrap chưa được gán LevelConfig.");
                return;
            }

            var cam = SceneSetup.EnsureAll();
            var worldRoot = new GameObject("World").transform;
            World.Root = worldRoot;

            var map = MapData.Parse(config.MapRows());
            MapBuilder.BuildTerrain(map, config.groundColor, config.roadColor, worldRoot);
            MapBuilder.BuildGameplay(map, config);

            var sys = new GameObject("Systems");
            var gm = sys.AddComponent<GameManager>();
            var rm = sys.AddComponent<ResourceManager>();
            var wm = sys.AddComponent<WaveManager>();
            var bm = sys.AddComponent<BuildingManager>();
            var sel = sys.AddComponent<SelectionManager>();
            var input = sys.AddComponent<TouchInputController>();

            rm.Init(config);
            wm.Init(config);
            bm.Init(config);
            sel.Init();

            var camCtl = cam.gameObject.GetComponent<CameraController>();
            if (camCtl == null) camCtl = cam.gameObject.AddComponent<CameraController>();
            var b = map.Bounds;
            var rect = new Rect(b.xMin - 2f, b.yMin - 2f, b.width + 4f, b.height + 4f);
            Vector2 focus = World.Castle != null ? World.Castle.Base + new Vector2(3f, 1f) : rect.center;
            camCtl.Init(cam, rect, focus, 7.5f);

            input.Init(cam);
            input.OnTapWorld += sel.OnTap;

            // Thợ ban đầu
            if (World.Castle != null)
                for (int i = 0; i < config.startPawns; i++)
                {
                    Vector2 p = World.Castle.DoorPoint + new Vector2((i - (config.startPawns - 1) * 0.5f) * 0.7f, -0.3f);
                    if (!World.Nav.IsWalkable(p)) p = World.Castle.DoorPoint;
                    Unit.Spawn("pawn", p);
                }

            gm.Init(config);
            AudioManager.Warmup();

            var hud = new GameObject("HUD").AddComponent<HUD>();
            hud.Init(config);
        }
    }
}
