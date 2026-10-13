using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Oath.EditorTools
{
    /// <summary>
    /// Cầu nối lệnh cho tự động hoá: đọc Temp/oath_cmd.txt và thực thi lệnh trong Editor.
    /// Kết quả ghi ra Temp/oath_result.txt. Chỉ dùng trong quá trình phát triển.
    /// </summary>
    [InitializeOnLoad]
    public static class OathBridge
    {
        static readonly string CmdPath = Path.GetFullPath("Temp/oath_cmd.txt");
        static readonly string ResultPath = Path.GetFullPath("Temp/oath_result.txt");
        static double nextCheck;

        static readonly string LogPath = Path.GetFullPath("Temp/oath_log.txt");

        static OathBridge()
        {
            EditorApplication.update += Poll;
            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (asm, msgs) =>
            {
                foreach (var m in msgs)
                    File.AppendAllText(LogPath, "[compile " + m.type + "] " + m.message + "\n");
            };
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Log && !msg.StartsWith("[Oath]")) return;
                try { File.AppendAllText(LogPath, "[" + type + "] " + msg + (type == LogType.Exception || type == LogType.Error ? "\n" + stack : "") + "\n"); } catch { }
            };
        }

        static void Poll()
        {
            // Giữ vòng lặp game chạy khi Editor không được focus (phục vụ kiểm thử tự động)
            if (EditorApplication.isPlaying && !EditorApplication.isPaused) EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 0.5;
            if (!File.Exists(CmdPath)) return;

            string cmd;
            try { cmd = File.ReadAllText(CmdPath).Trim(); File.Delete(CmdPath); }
            catch { return; }

            string result;
            try { result = Execute(cmd); }
            catch (Exception e) { result = "ERROR " + e; }
            File.WriteAllText(ResultPath, DateTime.Now.ToString("HH:mm:ss") + " " + cmd + "\n" + result);
        }

        static string Execute(string cmd)
        {
            string[] parts = cmd.Split(new[] { ' ' }, 2);
            string arg = parts.Length > 1 ? parts[1] : "";
            switch (parts[0])
            {
                case "ping":
                    return "pong compiling=" + EditorApplication.isCompiling + " playing=" + EditorApplication.isPlaying;
                case "stat":
                    return "frame=" + Time.frameCount + " time=" + Time.time + " scale=" + Time.timeScale + " focused=" + UnityEditorInternal.InternalEditorUtility.isApplicationActive;
                case "clearlog":
                    if (File.Exists(LogPath)) File.Delete(LogPath);
                    return "cleared";
                case "refresh":
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    return "refreshed";
                case "exec":
                {
                    // exec Namespace.Type.Method
                    int dot = arg.LastIndexOf('.');
                    var type = FindType(arg.Substring(0, dot));
                    if (type == null) return "type not found";
                    var m = type.GetMethod(arg.Substring(dot + 1),
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (m == null) return "method not found";
                    object r = m.Invoke(null, null);
                    return "ok " + r;
                }
                case "play":
                    if (!string.IsNullOrEmpty(arg))
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(arg);
                    EditorApplication.isPlaying = true;
                    return "entering play";
                case "stop":
                    EditorApplication.isPlaying = false;
                    return "stopped";
                default:
                    return "unknown command";
            }
        }

        static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }
    }
}
