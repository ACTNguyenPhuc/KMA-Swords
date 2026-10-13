using System;
using UnityEngine;

namespace Oath
{
    public enum GameState { Intro, Playing, Paused, Victory, Defeat }

    /// <summary>
    /// Quản lý trạng thái màn chơi (Intro, Playing, Paused, Victory, Defeat), điều kiện thắng/thua,
    /// thống kê và lưu kết quả.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public LevelConfig Config { get; private set; }
        public GameState State { get; private set; } = GameState.Intro;
        public event Action<GameState> OnStateChanged;

        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public int UnitsLost { get; private set; }
        public float Speed { get; private set; } = 1f;
        public string DefeatReason { get; private set; }
        public int StarsEarned { get; private set; }
        public int VillagesTotal { get; private set; }
        public bool Started { get; private set; }

        public int VillagesAlive
        {
            get
            {
                int n = 0;
                foreach (var b in World.Buildings) if (b.type == BuildingType.Village && b.health.Alive) n++;
                return n;
            }
        }

        public void Init(LevelConfig cfg)
        {
            I = this;
            Config = cfg;
            Speed = 1f;
            foreach (var b in World.Buildings) if (b.type == BuildingType.Village) VillagesTotal++;
            Unit.AnyDied += OnUnitDied;
            Building.AnyDestroyed += OnBuildingDestroyed;
            SetState(GameState.Intro);
        }

        void OnDestroy()
        {
            Unit.AnyDied -= OnUnitDied;
            Building.AnyDestroyed -= OnBuildingDestroyed;
            if (I == this) I = null;
            Time.timeScale = 1f;
        }

        void OnUnitDied(Unit u)
        {
            if (u.def.team == Team.Enemy) Kills++;
            else
            {
                UnitsLost++;
                if (u.def.id != "pawn") GameEvents.Message(u.def.name + " đã hy sinh.", MessageKind.Warning);
            }
        }

        void OnBuildingDestroyed(Building b)
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            if (b.type == BuildingType.Castle)
            {
                Defeat("Lâu đài đã bị phá hủy!");
                return;
            }
            if (b.type == BuildingType.Village)
            {
                int alive = VillagesAlive;
                GameEvents.Message("Một ngôi nhà dân đã bị phá hủy! (" + alive + "/" + VillagesTotal + ")", MessageKind.Danger);
                if (Config.protectVillage && alive < Config.villageMinAlive)
                    Defeat("Làng đã bị tàn phá — nhiệm vụ bảo vệ dân làng thất bại!");
                return;
            }
            GameEvents.Message(b.displayName + " đã bị phá hủy!", MessageKind.Danger);
        }

        public void StartPlaying()
        {
            if (Started) return;
            Started = true;
            SetState(GameState.Playing);
            if (WaveManager.I != null) WaveManager.I.BeginFirstPrep();
        }

        void SetState(GameState s)
        {
            State = s;
            Time.timeScale = s == GameState.Playing ? Speed : 0f;
            OnStateChanged?.Invoke(s);
        }

        public void Pause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void ToggleSpeed()
        {
            Speed = Speed > 1.5f ? 1f : 2f;
            if (State == GameState.Playing) Time.timeScale = Speed;
            OnStateChanged?.Invoke(State);
        }

        public void Victory()
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            float r = World.Castle != null ? World.Castle.health.Ratio : 0f;
            int stars = r >= 0.7f ? 3 : (r >= 0.35f ? 2 : 1);
            if (Config.protectVillage && VillagesAlive < VillagesTotal) stars = Mathf.Min(stars, 2);
            StarsEarned = stars;
            SaveManager.CompleteLevel(Config.levelIndex, stars, Elapsed);
            AudioManager.Play("victory", 0.9f);
            SetState(GameState.Victory);
        }

        public void Defeat(string reason)
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            DefeatReason = reason;
            if (Config.endless && WaveManager.I != null) SaveManager.RecordEndless(WaveManager.I.WavesCleared);
            AudioManager.Play("defeat", 0.9f);
            if (CameraController.I != null) CameraController.I.Shake(0.6f);
            SetState(GameState.Defeat);
        }

        void Update()
        {
            if (State == GameState.Playing) Elapsed += Time.deltaTime;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Pause();
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && !Application.isEditor) Pause();
        }

        public void Restart() => SceneLoader.Reload();
        public void ToMenu() => SceneLoader.Load(SceneLoader.MainMenu);

        public void NextLevel()
        {
            if (Config.levelIndex >= SceneLoader.LevelCount)
            {
                MenuBootstrap.ShowEndingOnLoad = true;
                SceneLoader.Load(SceneLoader.MainMenu);
            }
            else SceneLoader.LoadLevel(Config.levelIndex + 1);
        }
    }
}
