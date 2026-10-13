using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Oath.EditorTools
{
    /// <summary>
    /// Thiết lập tự động toàn bộ dự án: kho hình ảnh (GameArt), LevelConfig, các scene,
    /// Build Settings và Player Settings cho Android; build APK.
    /// </summary>
    public static class OathSetup
    {
        const string ArtPath = "Assets/Resources/GameArt.asset";
        const string LevelDir = "Assets/ScriptableObjects/Levels";
        const string SceneDir = "Assets/Scenes";
        const string FontPath = "Assets/Fonts/Inter-SemiBold.ttf";

        static readonly string[] SpriteFolders =
        {
            "Assets/Tiny Swords/Units/Blue Units",
            "Assets/Tiny Swords/Units/Red Units",
            "Assets/Tiny Swords/Units/Extra",
            "Assets/Tiny Swords/Pawn and Resources/Pawn/Blue Pawn",
            "Assets/Tiny Swords/Pawn and Resources/Wood",
            "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Stones",
            "Assets/Tiny Swords/Pawn and Resources/Meat/Sheep",
            "Assets/Tiny Swords/Buildings/Blue Buildings",
            "Assets/Tiny Swords/Buildings/Red Buildings",
            "Assets/Tiny Swords/Buildings/Yellow Buildings",
            "Assets/Tiny Swords/Terrain/Decorations",
            "Assets/Tiny Swords/Terrain/Tileset",
            "Assets/Tiny Swords/Particle FX",
            "Assets/Tiny Swords/UI Elements",
            "Assets/Layer Lab/2D Characters-CasualMonsters/PNG/All",
            "Assets/PotionJunkies/7Enemies/Texture",
            "Assets/Stone Tower/PNG",
        };

        // Bỏ các sheet không dùng để giảm dung lượng bản build
        static readonly string[] Exclude = { "Defence", "Guard", "Unity Tile Guide", "Highlight", "Enemy Pack", "Cursors", "Shadow.png" };

        [MenuItem("Oath/1. Thiết lập toàn bộ (Setup All)", priority = 1)]
        public static string SetupAll()
        {
            CopyFont();
            ConfigureStoneTowerImport();
            BuildArt();
            var levels = CreateLevels();
            CreateScenes(levels);
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Oath] Thiết lập hoàn tất.");
            return "setup done";
        }

        [MenuItem("Oath/2. Chỉ cập nhật dữ liệu màn (Levels)", priority = 2)]
        public static string UpdateLevels()
        {
            CreateLevels();
            AssetDatabase.SaveAssets();
            return "levels updated";
        }

        [MenuItem("Oath/3. Build APK Android", priority = 3)]
        public static string BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigurePlayer();
            Directory.CreateDirectory("Builds");
            var opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = "Builds/LoiTheQuanDao.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None,
            };
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(opts);
            string r = report.summary.result + " size=" + report.summary.totalSize + " errors=" + report.summary.totalErrors;
            Debug.Log("[Oath] Build: " + r);
            return r;
        }

        // ------------------------------------------------------------

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static void CopyFont()
        {
            EnsureFolder("Assets/Fonts");
            if (File.Exists(FontPath)) return;
            string src = Path.Combine(EditorApplication.applicationContentsPath, "Resources/Fonts/Inter-SemiBold.ttf");
            File.Copy(src, FontPath);
            File.WriteAllText("Assets/Fonts/Inter-LICENSE.txt",
                "Inter font — Copyright (c) The Inter Project Authors (https://github.com/rsms/inter).\n" +
                "Licensed under the SIL Open Font License, Version 1.1 (https://openfontlicense.org).\n");
            AssetDatabase.ImportAsset(FontPath);
        }

        /// <summary>Ảnh gói Stone Tower: sprite đơn, 78 px = 1 ô (đế tháp rộng ~2 ô), lọc mượt.</summary>
        public static void ConfigureStoneTowerImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Stone Tower/PNG" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single ||
                             Mathf.Abs(ti.spritePixelsPerUnit - TowerCatalog.StonePPU) > 0.01f || ti.mipmapEnabled;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = TowerCatalog.StonePPU;
                ti.mipmapEnabled = false;
                ti.filterMode = FilterMode.Bilinear;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }

        static int FrameIndex(string name)
        {
            var m = Regex.Match(name, @"_(\d+)$");
            return m.Success ? int.Parse(m.Groups[1].Value) : 0;
        }

        public static void BuildArt()
        {
            EnsureFolder("Assets/Resources");
            var art = AssetDatabase.LoadAssetAtPath<GameArt>(ArtPath);
            if (art == null)
            {
                art = ScriptableObject.CreateInstance<GameArt>();
                AssetDatabase.CreateAsset(art, ArtPath);
            }
            art.font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            art.sets = new List<SpriteSet>();
            art.prefabs = new List<NamedPrefab>();
            art.tiles = new List<NamedTile>();

            foreach (var folder in SpriteFolders)
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".png") || Exclude.Any(e => path.Contains(e))) continue;
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                        .OrderBy(s => FrameIndex(s.name)).ThenBy(s => s.name).ToArray();
                    if (sprites.Length == 0) continue;
                    string key = path.Substring("Assets/".Length, path.Length - "Assets/".Length - 4);
                    art.sets.Add(new SpriteSet { key = key, frames = sprites });
                }
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/OldStyleCartoonMobsPack1/Prefabs" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string key = path.Substring("Assets/".Length, path.Length - "Assets/".Length - ".prefab".Length);
                art.prefabs.Add(new NamedPrefab { key = key, prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) });
            }

            art.tiles.Add(new NamedTile { key = "water", tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Water Background color.asset") });
            art.tiles.Add(new NamedTile { key = "foam", tile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Water Tile animated.asset") });

            EditorUtility.SetDirty(art);
            Debug.Log("[Oath] GameArt: " + art.sets.Count + " sprite set, " + art.prefabs.Count + " prefab.");
        }

        static LevelConfig[] CreateLevels()
        {
            EnsureFolder(LevelDir);
            var result = new LevelConfig[SceneLoader.LevelCount];
            for (int i = 1; i <= SceneLoader.LevelCount; i++)
            {
                string path = LevelDir + "/Level" + i.ToString("00") + ".asset";
                var cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                if (cfg == null)
                {
                    cfg = ScriptableObject.CreateInstance<LevelConfig>();
                    AssetDatabase.CreateAsset(cfg, path);
                }
                LevelDesigns.Fill(cfg, i);
                EditorUtility.SetDirty(cfg);
                result[i - 1] = cfg;
            }
            EndlessConfig();
            return result;
        }

        static LevelConfig EndlessConfig()
        {
            string path = LevelDir + "/Endless.asset";
            var cfg = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(cfg, path);
            }
            LevelDesigns.FillEndless(cfg);
            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        static void NewScene(string name, System.Action build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneSetup.EnsureAll();
            build();
            EditorSceneManager.SaveScene(scene, SceneDir + "/" + name + ".unity");
        }

        static void CreateScenes(LevelConfig[] levels)
        {
            EnsureFolder(SceneDir);
            NewScene(SceneLoader.MainMenu, () => new GameObject("MenuBootstrap").AddComponent<MenuBootstrap>());
            NewScene(SceneLoader.LevelSelect, () =>
            {
                var b = new GameObject("LevelSelectBootstrap").AddComponent<LevelSelectBootstrap>();
                b.levels = levels;
            });
            for (int i = 1; i <= SceneLoader.LevelCount; i++)
            {
                var cfg = levels[i - 1];
                NewScene(SceneLoader.LevelScene(i), () => new GameObject("LevelBootstrap").AddComponent<LevelBootstrap>().config = cfg);
            }

            var endless = EndlessConfig();
            NewScene(SceneLoader.Endless, () => new GameObject("LevelBootstrap").AddComponent<LevelBootstrap>().config = endless);

            var list = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(SceneDir + "/" + SceneLoader.MainMenu + ".unity", true),
                new EditorBuildSettingsScene(SceneDir + "/" + SceneLoader.LevelSelect + ".unity", true),
            };
            for (int i = 1; i <= SceneLoader.LevelCount; i++)
                list.Add(new EditorBuildSettingsScene(SceneDir + "/" + SceneLoader.LevelScene(i) + ".unity", true));
            list.Add(new EditorBuildSettingsScene(SceneDir + "/" + SceneLoader.Endless + ".unity", true));
            EditorBuildSettings.scenes = list.ToArray();
            EditorSceneManager.OpenScene(SceneDir + "/" + SceneLoader.MainMenu + ".unity");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.productName = "Lời Thề Quần Đảo";
            PlayerSettings.companyName = "KMA";
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.kma.oathoftheisles");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.runInBackground = true; // Android luôn tạm dừng khi ở nền; bật để Editor chạy thử khi mất focus

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/AppIcon.png");
            if (icon != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        }
    }
}
