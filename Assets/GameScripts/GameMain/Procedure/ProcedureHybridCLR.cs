//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;
using System.Collections.Generic;
using UnityGameFramework.Runtime;
using GameFramework.Resource;
using UnityEngine;
using HybridCLR;

namespace GameFrame.Main
{
    public class ProcedureHybridCLR : ProcedureBase
    {
        public class PrefabData
        {
            public string prefabName;
        }

        public override bool UseNativeDialog
        {
            get
            {
                return true;
            }
        }

        //热更新程序
        private Dictionary<string, byte[]> m_loadedHotifx = new Dictionary<string, byte[]>();
        private Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();
        private bool isLoadSuccess = false;

        private List<string> AOTMetaAssemblyNames = new List<string>()
        {
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
        };

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_LoadedFlag.Clear();

            LoadAOTAssemblies();
#if !UNITY_EDITOR
            LoadHotAssemblies();
#endif
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (isLoadSuccess) return;

            foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag)
            {
                if (!loadedFlag.Value) return;
            }
            isLoadSuccess = true;

            StartHotfixEntry();
        }

        // 进入 热更新程序
        private void StartHotfixEntry()
        {
            LoadMetadataForAOTAssemblies();

#if !UNITY_EDITOR
            LoadForHotAssemblies();
#endif

            Log.Info("<color=green> ProcedureToHuaTuo Load Native/GameHotfixEntry.Perfab </color>");

            string assetName = AssetUtility.GetPerfabsAsset("Native/GameHotfixEntry");
            GameEntry.Resource.LoadAsset(assetName, new LoadAssetCallbacks((assetName, asset, duration, userData) =>
            {//加载成功
                var GameHotfixEntry = GameObject.Instantiate((asset as GameObject), GameEntry.BuiltinData.transform.parent);
                GameHotfixEntry.name = "GameHotfixEntry";
                asset = null;
            }, (assetName, status, errorMessage, userData) =>
            {//加载失败
                Log.Error("Can not load {0} from '{1}' with error message '{2}'.", assetName, "GameHotfixEntry.Perfab", errorMessage);
            }));
        }

        // 加载 AOT 文件
        private void LoadAOTAssemblies()
        {
            foreach (var aotDllName in AOTMetaAssemblyNames)
            {
                LoadAOTDll(aotDllName);
            }
        }

        // 加载 热更DLL 文件
        private void LoadHotAssemblies()
        {
            LoadHotDll("Assembly-CSharp.dll");
            //LoadAOTDll("GameFrame.Main.dll");
        }

        /// <summary>
        /// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
        /// </summary>
        private void LoadMetadataForAOTAssemblies()
        {
            /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
            HomologousImageMode mode = HomologousImageMode.SuperSet;
            foreach (var aotDllName in AOTMetaAssemblyNames)
            {
                byte[] dllBytes = m_loadedHotifx[aotDllName];
                // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, mode);
                Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. mode:{mode} ret:{err}");
            }
        }

        // 加载 热更DLL 文件
        private void LoadForHotAssemblies()
        {
            byte[] csharpBytes = m_loadedHotifx["Assembly-CSharp.dll"];
            System.Reflection.Assembly.Load(csharpBytes);

            //byte[] mainBytes = m_loadedHotifx["GameFrame.Main.dll"];
            //System.Reflection.Assembly.Load(mainBytes);
        }

        private void LoadAOTDll(string aotName)
        {
            m_LoadedFlag.Add(aotName, false);
            PrefabData prefabData = new PrefabData() { prefabName = aotName };
            string assetName = AssetUtility.GetAOTDllAsset(aotName);
            GameEntry.Resource.LoadAsset(assetName, new LoadAssetCallbacks(OnLoadAOTDllSuccess, OnLoadAOTFailured), prefabData);
        }

        private void LoadHotDll(string aotName)
        {
            m_LoadedFlag.Add(aotName, false);
            PrefabData prefabData = new PrefabData() { prefabName = aotName };
            string assetName = AssetUtility.GetHotDllAsset(aotName);
            GameEntry.Resource.LoadAsset(assetName, new LoadAssetCallbacks(OnLoadAOTDllSuccess, OnLoadAOTFailured), prefabData);
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
    }
}
