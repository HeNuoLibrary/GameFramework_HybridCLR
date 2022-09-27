using UnityGameFramework.Runtime;
using System.Collections.Generic;
using GameFramework.Resource;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;
using System.Collections;

namespace GameFrame.Main
{
    public class HuatuoComponent : GameFrameworkComponent
    {
        private Assembly gameAss;

        //加载热更新脚本
        public void LoadHotfix()
        {
            GameEntry.Resource.LoadAsset(AssetUtility.GetHotDllAsset("Assembly-CSharp.dll"), Constant.AssetPriority.ScriptsAsset,
                new LoadAssetCallbacks(
                    //加载成功的回调
                    (assetName, asset, duration, userData) =>
                    {
#if !UNITY_EDITOR
                        gameAss = Assembly.Load(((TextAsset)asset).bytes);
#else
                        gameAss = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "Assembly-CSharp");
#endif
                        if (gameAss == null)
                        {
                            Debug.LogError("dll未加载成功");
                            return;
                        }
                        Log.Info("Load hotfix Assembly-CSharp OK.");
                        LoadGameDll();
                    },
                    //加载失败的回调
                    (assetName, status, errorMessage, userData) =>
                    {
                        Log.Error("Can not load hotfix Assembly-CSharp from '{0}' with error message '{1}'.", assetName, errorMessage);
                    }
            ));
        }

        public void LoadGameDll()
        {
            RunMain();
            StartCoroutine(RunUpdate());
        }

        private void RunMain()
        {
            var hotfixEntry = gameAss.GetType("HybridCLREntry");
            var startMethod = hotfixEntry.GetMethod("Main");
            startMethod?.Invoke(null, null);
        }

        private IEnumerator RunUpdate()
        {
            var hotfixEntry = gameAss.GetType("HybridCLREntry");
            var updateMethod = hotfixEntry.GetMethod("Update");
           
            while (this.gameObject.activeInHierarchy)
            {
                yield return new WaitForEndOfFrame();
                updateMethod?.Invoke(null, null);
            }
        }
    }
}