using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oath
{
    /// <summary>Tên scene và điều hướng giữa menu, chọn màn và các màn chơi.</summary>
    public static class SceneLoader
    {
        public const string MainMenu = "01_MainMenu";
        public const string LevelSelect = "02_LevelSelect";
        public const int LevelCount = 5;
        public const string Endless = "20_Endless";

        public static string LevelScene(int level) => "1" + (level - 1) + "_Level" + level.ToString("00");

        public static void Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        public static void LoadLevel(int level)
        {
            Load(LevelScene(Mathf.Clamp(level, 1, LevelCount)));
        }

        public static void Reload()
        {
            Load(SceneManager.GetActiveScene().name);
        }
    }
}
