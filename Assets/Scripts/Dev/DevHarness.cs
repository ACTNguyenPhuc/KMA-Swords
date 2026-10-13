#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Oath.Dev
{
    /// <summary>
    /// Công cụ kiểm thử tự động (chỉ trong Editor): khi có tệp Temp/oath_capture.txt,
    /// tự bỏ qua hội thoại, mô phỏng người chơi đơn giản, chụp màn hình và ghi log trạng thái.
    /// </summary>
    public class DevHarness : MonoBehaviour
    {
        static readonly string CfgPath = "Temp/oath_capture.txt";
        static readonly string LogPath = "Temp/oath_play.txt";
        Dictionary<string, string> cfg = new Dictionary<string, string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (!File.Exists(CfgPath)) return;
            var go = new GameObject("DevHarness");
            DontDestroyOnLoad(go);
            go.AddComponent<DevHarness>();
        }

        string Get(string k, string d) => cfg.TryGetValue(k, out var v) ? v : d;
        float F(string k, float d) => float.TryParse(Get(k, d.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : d;

        void Log(string s)
        {
            File.AppendAllText(LogPath, Time.realtimeSinceStartup.ToString("0.0") + " | " + s + "\n");
        }

        IEnumerator Start()
        {
            foreach (var line in File.ReadAllLines(CfgPath))
            {
                int i = line.IndexOf('=');
                if (i > 0) cfg[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
            File.WriteAllText(LogPath, "");
            string outDir = Get("out", "Temp/shots");
            Directory.CreateDirectory(outDir);
            float duration = F("duration", 20f);
            float interval = F("interval", 5f);
            float speed = F("speed", 1f);
            bool autoplay = Get("autoplay", "0") == "1";
            bool skip = Get("skip", "1") == "1";
            string action = Get("action", "");

            yield return null;
            yield return null;
            Log("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + " screen=" + Screen.width + "x" + Screen.height);
            if (Get("shot0", "1") == "1") yield return Shot(outDir + "/00_start.png");

            if (skip && GameManager.I != null && GameManager.I.State == GameState.Intro)
            {
                foreach (var d in FindObjectsByType<DialogPanel>()) Destroy(d.gameObject);
                GameManager.I.StartPlaying();
            }
            if (action == "pause" && GameManager.I != null) GameManager.I.Pause();
            if (action == "units" && SelectionManager.I != null) { foreach (var b in World.Buildings) if (b.type == BuildingType.Barracks) { BuildingManager.I.TryTrain(b, "warrior"); BuildingManager.I.TryTrain(b, "archer"); } }
            if (action == "zoo")
            {
                int i = 0;
                foreach (var d in UnitCatalog.All)
                {
                    var pos = new Vector2(6 + (i % 11) * 3f, 20 - (i / 11) * 4.2f);
                    var u = Unit.Spawn(d.id, pos);
                    u.enabled = false;
                    u.visual.Face(-1);
                    FX.Text(pos + Vector2.down * 0.4f, d.name, Color.white, 0.6f);
                    i++;
                }
                foreach (var c in FindObjectsByType<Canvas>()) c.enabled = false;
                CameraController.I.Zoom(3f, new Vector2(Screen.width / 2f, Screen.height / 2f));
                CameraController.I.FocusOn(new Vector2(21, 14));
                Time.timeScale = 0.3f;
            }
            if (action == "towers")
            {
                int i = 0;
                foreach (var p in World.Plots)
                {
                    if (p.Occupied) continue;
                    var b = Building.Create(BuildingType.Tower, p.cells, false, p, 0f, TowerCatalog.All[i % 5]);
                    for (int k = 0; k < (i / 5) * 2; k++) b.Upgrade();
                    i++;
                }
                WaveManager.I.CallEarly();
                CameraController.I.Zoom(0.55f, new Vector2(Screen.width / 2f, Screen.height / 2f));
                CameraController.I.FocusOn(new Vector2(F("fx", 40f), F("fy", 14f)));
            }
            if (action == "plot" && SelectionManager.I != null)
            {
                foreach (var p in World.Plots) if (!p.Occupied) { SelectionManager.I.OnTap(p.Center); CameraController.I.FocusOn(p.Center + Vector2.up * 2f); break; }
            }
            if (action == "help") Panels.Help(FindAnyObjectByType<Canvas>().transform);
            if (action == "select" && SelectionManager.I != null)
            {
                foreach (var b in World.Buildings) if (b.type == BuildingType.Barracks) SelectionManager.I.SelectBuilding(b);
            }

            if (action == "gallery")
            {
                var list = new List<Building>();
                int i = 0;
                foreach (var p in World.Plots)
                {
                    if (p.Occupied || i >= 12) continue;
                    var tt = TowerCatalog.All[1 + i % 4];
                    var b = Building.Create(BuildingType.Tower, p.cells, false, p, 0f, tt);
                    for (int k = 0; k < i / 4; k++) b.Upgrade();
                    list.Add(b);
                    i++;
                }
                foreach (var c in FindObjectsByType<Canvas>()) c.enabled = false;
                CameraController.I.Zoom(0.45f, new Vector2(Screen.width / 2f, Screen.height / 2f));
                int n = 0;
                foreach (var b in list)
                {
                    CameraController.I.FocusOn(b.Base + Vector2.up * 1.2f);
                    for (int e = 0; e < 3; e++)
                    {
                        var u = WaveManager.I.SpawnEnemy("red_warrior", 0);
                        u.transform.position = b.Base + new Vector2(-2.5f + e * 1.2f, -1.6f);
                        u.enemyAI.enabled = false;
                    }
                    yield return new WaitForSecondsRealtime(b.towerType == TowerType.Quake ? 3.3f : 2.3f);
                    yield return Shot(outDir + "/g" + (n++).ToString("00") + "_" + b.towerType + b.level + ".png");
                }
            }
            float t0 = Time.realtimeSinceStartup;
            float nextShot = interval;
            float nextAct = 0f;
            int shot = 1;
            while (Time.realtimeSinceStartup - t0 < duration)
            {
                if (GameManager.I != null && GameManager.I.State == GameState.Playing && Mathf.Abs(Time.timeScale - speed) > 0.01f)
                    Time.timeScale = speed;
                float el = Time.realtimeSinceStartup - t0;
                if (autoplay && el >= nextAct) { nextAct = el + 1f; AutoPlay(); }
                if (el >= nextShot)
                {
                    nextShot += interval;
                    yield return Shot(outDir + "/" + shot.ToString("00") + ".png");
                    shot++;
                    LogState();
                }
                if (GameManager.I != null && (GameManager.I.State == GameState.Victory || GameManager.I.State == GameState.Defeat))
                {
                    LogState();
                    yield return new WaitForSecondsRealtime(2.5f);
                    yield return Shot(outDir + "/99_end.png");
                    break;
                }
                yield return null;
            }
            LogState();
            Log("DONE");
            File.Delete(CfgPath);
            UnityEditor.EditorApplication.isPlaying = false;
        }

        IEnumerator Shot(string path)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
        }

        void LogState()
        {
            if (GameManager.I == null) { Log("no game"); return; }
            var ps = new Dictionary<string, int>();
            foreach (var u in World.Units) if (u.IsAlive && u.pawn != null) { ps.TryGetValue(u.pawn.State.ToString(), out int n); ps[u.pawn.State.ToString()] = n + 1; }
            var sb = new System.Text.StringBuilder("pawns:");
            foreach (var kv in ps) sb.Append(' ').Append(kv.Key).Append('=').Append(kv.Value);
            Log(sb.ToString());
            var wm = WaveManager.I;
            var rm = ResourceManager.I;
            Log(string.Format("state={0} wave={1}/{2} phase={3} castle={4:0}/{5:0} wood={6} gold={7} pop={8}/{9} allies={10} enemies={11} kills={12} lost={13} t={14:0}",
                GameManager.I.State, wm.DisplayWave, wm.TotalWaves, wm.Phase,
                World.Castle != null ? World.Castle.health.current : 0, World.Castle != null ? World.Castle.health.max : 0,
                rm.Wood, rm.Gold, rm.PopulationUsed, rm.PopulationCap,
                World.CountUnits(Team.Player, true), World.CountUnits(Team.Enemy), GameManager.I.Kills, GameManager.I.UnitsLost, GameManager.I.Elapsed));
        }

        /// <summary>Người chơi mô phỏng: xây tháp khi đủ gỗ, tuyển quân khi đủ vàng, dồn quân ra trước lâu đài.</summary>
        void AutoPlay()
        {
            if (GameManager.I == null || GameManager.I.State != GameState.Playing) return;
            var bm = BuildingManager.I;
            var rm = ResourceManager.I;
            // Xây tháp ở ô gần đường nhất
            bool capped = rm.PopulationUsed >= rm.PopulationCap - 1;
            foreach (var p in World.Plots)
            {
                if (p.Occupied) continue;
                var type = capped ? BuildingType.House : BuildingType.Tower;
                if (World.HostileNear(p.Center, Team.Player, 3f)) continue;
                if (type == BuildingType.Tower)
                {
                    var tt = TowerCatalog.All[Random.Range(0, TowerCatalog.All.Length)];
                    var td = TowerCatalog.Get(tt);
                    if (bm.IsTowerAllowed(tt) && rm.CanAfford(td.wood[0], td.gold[0]) && bm.TryBuildTower(p, tt)) Log("build " + tt + " " + p.cells);
                }
                else if (rm.CanAfford(BuildingManager.CostWood(type), BuildingManager.CostGold(type)) && bm.TryBuild(p, type)) Log("build " + type + " " + p.cells);
                break;
            }
            foreach (var b in World.Buildings)
            {
                if (b.type == BuildingType.Tower && bm.CanUpgrade(b, out int uw, out int ug) && rm.Wood >= uw + 20 && rm.Gold >= ug + 60) bm.TryUpgrade(b);
                if (b.type == BuildingType.Barracks && b.queue.Count < 2)
                {
                    string id = rm.Wood < 120 || Random.value < 0.5f ? "warrior" : (Random.value < 0.5f ? "archer" : "lancer");
                    if (rm.Gold > 120 && bm.TryTrain(b, id)) Log("train " + id);
                }
                if (b.type == BuildingType.Castle && World.CountUnits(Team.Player) - World.CountUnits(Team.Player, true) < 4 && rm.Gold > 60 && b.queue.Count == 0)
                    bm.TryTrain(b, "pawn");
                if (b.health.Ratio < 0.5f && rm.Wood > 60) bm.TryRepair(b);
            }
            Unit threat = null; float best = float.MaxValue;
            foreach (var u in World.Units)
                if (u.IsAlive && u.def.team == Team.Enemy && World.Castle != null)
                {
                    float d = World.Castle.DistanceTo(u.Pos);
                    if (d < best) { best = d; threat = u; }
                }
            if (threat != null && best < 12f)
                foreach (var u in World.Units)
                    if (u.IsAlive && u.def.team == Team.Player && u.pawn == null && u.target == null)
                        u.CommandMove(threat.Pos, true);
            if (WaveManager.I.CanCallEarly && WaveManager.I.PrepRemaining < F("prep", 10f)) WaveManager.I.CallEarly();
        }
    }
}
#endif
