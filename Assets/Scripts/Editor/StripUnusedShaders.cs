using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace Oath.EditorTools
{
    /// <summary>
    /// Loại các compute shader của gói AI (Inference/Sentis) khỏi bản build game:
    /// game không dùng tới, và việc biên dịch chúng cho Android rất tốn bộ nhớ (có thể làm build thất bại).
    /// </summary>
    class StripUnusedShaders : IPreprocessComputeShaders
    {
        public int callbackOrder => 0;

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            if (path.StartsWith("Packages/com.unity.ai.")) data.Clear();
        }
    }
}
