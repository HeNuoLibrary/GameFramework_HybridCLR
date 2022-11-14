using UnityGameFramework.Runtime;
using System.Collections.Generic;
using GameFramework.Resource;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;
using System.Collections;
using HybridCLR;

namespace GameFrame.Main
{
    public class PrefabData
    {
        public string prefabName;
    }
    /// <summary>
    /// 加载热更脚本Assembly-CSharp.dll
    /// </summary>
    public class HuatuoComponent : GameFrameworkComponent
    {
        //热更新程序
        private static Dictionary<string, byte[]> m_loadedHotifx = new Dictionary<string, byte[]>();
        private static Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();
        private static HotUpdateAssemblyManifest assemblyManifest;
        private static bool manifestLoadSuccess = false;
        private static bool isLoadSuccess = false;

        public void LoadAOTDllManifestToStart()
        {
            string manifest = "Assets/GameMain/HotFixDll/HotUpdateAssemblyManifest.asset";
            PrefabData prefabData = new PrefabData() { prefabName = manifest };
            GameEntry.Resource.LoadAsset(manifest, new LoadAssetCallbacks(OnLoadOTDllManifestSuccess, OnLoadAOTFailured), prefabData);
        }

        private void StartGame()
        {
            LoadMetadataForAOTAssemblies();
            
#if !UNITY_EDITOR
            var gameAss = System.Reflection.Assembly.Load(m_loadedHotifx["Assembly-CSharp.dll"]);
#else
            var gameAss = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "Assembly-CSharp");
#endif
            var hotfixEntry = gameAss.GetType("GameFrame.Hotfix.GameHotfixEntry");
            var startMethod = hotfixEntry.GetMethod("Awake");
            startMethod?.Invoke(null, null);
        }

        private void LoadAOTDll(string aotName)
        {
            m_LoadedFlag.Add(aotName, false);
            PrefabData prefabData = new PrefabData() { prefabName = aotName };
            string assetName = AssetUtility.GetHotDllAsset(aotName);
            GameEntry.Resource.LoadAsset(assetName, new LoadAssetCallbacks(OnLoadAOTDllSuccess, OnLoadAOTFailured), prefabData);
        }

        private void OnLoadOTDllManifestSuccess(string assetName, object asset, float duration, object userData)
        {
            assemblyManifest = asset as HotUpdateAssemblyManifest;
            LoadAOTDll("Assembly-CSharp.dll");
            foreach (var aotDllName in assemblyManifest.AOTMetadataDlls)
            {
                LoadAOTDll(aotDllName);
            }
            manifestLoadSuccess = true;
        }

        private void OnLoadAOTDllSuccess(string assetName, object asset, float duration, object userData)
        {
            PrefabData prefabData = userData as PrefabData;
            m_LoadedFlag[prefabData.prefabName] = true;
            m_loadedHotifx[prefabData.prefabName] = ((TextAsset)asset).bytes;
        }

        private void OnLoadAOTFailured(string assetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            Log.Error("Can not load {0} from '{1}' with error message '{2}'.", assetName, "AOTAsset", errorMessage);
        }

        ///// <summary>
        ///// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        ///// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        ///// </summary>
        //private static void LoadMetadataForAOTAssemblies()
        //{
        //    List<string> AOTMetaAssemblyNames = new List<string>()
        //    {
        //        "mscorlib.dll",
        //        "System.dll",
        //        "System.Core.dll",
        //    };
        //    // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
        //    // 我们在BuildProcessors里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

        //    /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        //    /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
        //    /// 
        //    HomologousImageMode mode = HomologousImageMode.SuperSet;
        //    foreach (var aotDllName in AOTMetaAssemblyNames)
        //    {
        //        byte[] dllBytes = GetAssetData(aotDllName);
        //        // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
        //        LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
        //        Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
        //    }
        //}

        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// </summary>
        private static void LoadMetadataForAOTAssemblies()
        {
            // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
            // 我们在BuildProcessors里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

            // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            // 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误 
            HomologousImageMode mode = HomologousImageMode.SuperSet;
            foreach (var aotDllName in assemblyManifest.AOTMetadataDlls)
            {
                byte[] dllBytes = m_loadedHotifx[aotDllName];
                // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
            }
        }

        private void Update()
        {
            if (!manifestLoadSuccess) return;
            if (isLoadSuccess) return;

            foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag)
            {
                if (!loadedFlag.Value)
                {
                    return;
                }
            }
            isLoadSuccess = true;

            StartGame();
        }
    }
}