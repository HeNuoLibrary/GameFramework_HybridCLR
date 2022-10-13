using UnityGameFramework.Editor.ResourceTools;
using GameFramework;
using UnityEditor;
using System;
using HybridCLR.Editor.Commands;
using UnityEngine;
using HybridCLR.Editor;
using System.IO;

namespace HybridCLR.Builder
{
    public class HybridCLRBuilderController 
    {
        public void CompileHotfixDll(int platformIndex)
        {
            BuildTarget buildTarget = GetBuildTargetByPlatformIndex(platformIndex);

            // Build Hotfix Dll
            CompileDllCommand.CompileDll(buildTarget);

            // Copy Hotfix Dll
            string hotfixDllSrcDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(buildTarget);//dll 输出路径
            string HotfixDllPath = $"{Application.dataPath}/GameMain/HotFixDll";
            foreach (var dll in SettingsUtil.HotUpdateAssemblyFiles)
            {
                string dllPath = $"{hotfixDllSrcDir}/{dll}";
                string dllBytesPath = $"{HotfixDllPath}/{dll}.bytes";
                File.Copy(dllPath, dllBytesPath, true);
                Debug.Log($"[BuildAssetBundles] copy hotfix dll {dllPath} -> {dllBytesPath}");
            }

            // Copy AOT Dll
            string aotDllDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(buildTarget);
            //HotUpdateAssemblyManifest manifest = Resources.Load<HotUpdateAssemblyManifest>("HotUpdateAssemblyManifest");
            HotUpdateAssemblyManifest manifest = AssetDatabase.LoadAssetAtPath<HotUpdateAssemblyManifest>("Assets/GameMain/HotFixDll/HotUpdateAssemblyManifest.asset");

            if (manifest == null)
            {
                throw new Exception($"asset:{nameof(HotUpdateAssemblyManifest)} 配置不存在，请在Assets/GameMain/HotFixDll/目录下创建");
            }
            foreach (var dll in manifest.AOTMetadataDlls)
            {
                string dllPath = $"{aotDllDir}/{dll}.dll";
                if (!File.Exists(dllPath))
                {
                    Debug.LogError($"ab中添加AOT补充元数据dll:{dllPath} 时发生错误,文件不存在。裁剪后的AOT dll在BuildPlayer时才能生成，因此需要你先构建一次游戏App后再打包。");
                    continue;
                }
                string dllBytesPath = $"{HotfixDllPath}/{dll}.bytes";
                File.Copy(dllPath, dllBytesPath, true);
                Debug.Log($"[BuildAssetBundles] copy AOT dll {dllPath} -> {dllBytesPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Hotfix dll build complete.");
        }

        public void GeneratorPreBuildAsset()
        {
            PrebuildCommand.GenerateAll();
            AssetDatabase.Refresh();
            Debug.Log("GeneratorPreBuildAsset complete.");
        }

        private BuildTarget GetBuildTargetByPlatformIndex(int platformIndex)
        {
            string[] PlatformNames = Enum.GetNames(typeof(Platform));
            Platform platform = (Platform)Enum.Parse(typeof(Platform), PlatformNames[platformIndex]);
            return GetBuildTarget(platform);
        }

        private BuildTarget GetBuildTarget(Platform platform)
        {
            switch (platform)
            {
                case Platform.Windows:
                    return BuildTarget.StandaloneWindows;

                case Platform.Windows64:
                    return BuildTarget.StandaloneWindows64;

                case Platform.MacOS:
#if UNITY_2017_3_OR_NEWER
                    return BuildTarget.StandaloneOSX;
#else
                    return BuildTarget.StandaloneOSXUniversal;
#endif
                case Platform.Linux:
                    return BuildTarget.StandaloneLinux64;

                case Platform.IOS:
                    return BuildTarget.iOS;

                case Platform.Android:
                    return BuildTarget.Android;

                case Platform.WindowsStore:
                    return BuildTarget.WSAPlayer;

                case Platform.WebGL:
                    return BuildTarget.WebGL;

                default:
                    throw new GameFrameworkException("Platform is invalid.");
            }
        }
    }
}