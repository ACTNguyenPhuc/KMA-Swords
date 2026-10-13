using UnityEngine;

namespace Oath
{
    /// <summary>Lưu màn đã mở khóa, số sao, thời gian tốt nhất và cài đặt bằng PlayerPrefs (chơi offline).</summary>
    public static class SaveManager
    {
        const string KUnlocked = "oath_unlocked";
        const string KStars = "oath_stars_";
        const string KTime = "oath_time_";
        const string KSfx = "oath_sfx";
        const string KVibrate = "oath_vibrate";
        const string KSpeed = "oath_speed";
        const string KEnding = "oath_ending";
        const string KEndless = "oath_endless_best";

        public static int BestEndless => PlayerPrefs.GetInt(KEndless, 0);

        public static void RecordEndless(int waves)
        {
            if (waves <= BestEndless) return;
            PlayerPrefs.SetInt(KEndless, waves);
            PlayerPrefs.Save();
        }

        public static int UnlockedLevel => Mathf.Clamp(PlayerPrefs.GetInt(KUnlocked, 1), 1, SceneLoader.LevelCount);
        public static int Stars(int level) => PlayerPrefs.GetInt(KStars + level, 0);
        public static float BestTime(int level) => PlayerPrefs.GetFloat(KTime + level, 0f);
        public static bool Completed(int level) => Stars(level) > 0;
        public static bool SawEnding => PlayerPrefs.GetInt(KEnding, 0) == 1;

        public static int TotalStars
        {
            get
            {
                int s = 0;
                for (int i = 1; i <= SceneLoader.LevelCount; i++) s += Stars(i);
                return s;
            }
        }

        public static void CompleteLevel(int level, int stars, float time)
        {
            if (stars > Stars(level)) PlayerPrefs.SetInt(KStars + level, stars);
            float best = BestTime(level);
            if (best <= 0f || time < best) PlayerPrefs.SetFloat(KTime + level, time);
            if (level + 1 > PlayerPrefs.GetInt(KUnlocked, 1))
                PlayerPrefs.SetInt(KUnlocked, Mathf.Min(level + 1, SceneLoader.LevelCount));
            if (level >= SceneLoader.LevelCount) PlayerPrefs.SetInt(KEnding, 1);
            PlayerPrefs.Save();
        }

        public static bool Sfx
        {
            get => PlayerPrefs.GetInt(KSfx, 1) == 1;
            set { PlayerPrefs.SetInt(KSfx, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Vibrate
        {
            get => PlayerPrefs.GetInt(KVibrate, 1) == 1;
            set { PlayerPrefs.SetInt(KVibrate, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static float GameSpeed
        {
            get => PlayerPrefs.GetFloat(KSpeed, 1f);
            set { PlayerPrefs.SetFloat(KSpeed, value); PlayerPrefs.Save(); }
        }

        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(KUnlocked);
            PlayerPrefs.DeleteKey(KEnding);
            PlayerPrefs.DeleteKey(KEndless);
            for (int i = 1; i <= SceneLoader.LevelCount; i++)
            {
                PlayerPrefs.DeleteKey(KStars + i);
                PlayerPrefs.DeleteKey(KTime + i);
            }
            PlayerPrefs.Save();
        }
    }
}
