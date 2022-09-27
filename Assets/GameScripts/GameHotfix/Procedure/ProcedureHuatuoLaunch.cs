// -----------------------------------------------
// Copyright © GameFramework. All rights reserved.
// CreateTime: 2022/8/2   18:20:0
// -----------------------------------------------
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;
using GameEntry = GameFrame.Main.GameEntry;
using UnityGameFramework.Runtime;
using System.Collections.Generic;
using GameFramework.Resource;
using GameFrame.Main;
using UnityEngine;

namespace GameFrame.Hotfix
{
    public class PrefabData
    {
        public string prefabName;
    }

    public class ProcedureHuatuoLaunch : ProcedureBase
    {
        private Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();

        public override bool UseNativeDialog
        {
            get
            {
                return false;
            }
        }

        private bool isLoadSuccess = false;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            m_LoadedFlag.Clear();
            isLoadSuccess = false;
            LoadPreFab("HP Bar");
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (isLoadSuccess) return;

            foreach (KeyValuePair<string, bool> loadedFlag in m_LoadedFlag)
            {
                if (!loadedFlag.Value)
                {
                    return;
                }
            }

            isLoadSuccess = true;
            GameHotfixEntry.Start();
            procedureOwner.SetData<VarInt32>("NextSceneId", GameEntry.Config.GetInt("Scene.Menu"));
            ChangeState<ProcedureChangeScene>(procedureOwner);
        }

        private void LoadPreFab(string perfabName)
        {
            m_LoadedFlag.Add(perfabName, false);
            PrefabData prefabData = new PrefabData() { prefabName = perfabName };
            string assetName = AssetUtility.GetPerfabsAsset(perfabName);
            GameEntry.Resource.LoadAsset(assetName, new LoadAssetCallbacks(OnLoadPerfabAssetSucceed, OnLoadPerfabAssetFailured), prefabData);
        }

        private void OnLoadPerfabAssetSucceed(string assetName, object asset, float duration, object userData)
        {
            GameObject formPrefab = asset as GameObject;
            GameObject.Instantiate(formPrefab, GameEntry.Customs);
            PrefabData prefabData = userData as PrefabData;
            m_LoadedFlag[prefabData.prefabName] = true;
            formPrefab = null;
        }

        private void OnLoadPerfabAssetFailured(string assetName, LoadResourceStatus status, string errorMessage, object userData)
        {
            Log.Error("Can not load {0} from '{1}' with error message '{2}'.", assetName, "PerfabsAsset", errorMessage);
        }
    }
}