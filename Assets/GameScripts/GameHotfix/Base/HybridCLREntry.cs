using HybridCLR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using GameFramework.Resource;
using UnityGameFramework.Runtime;
using UnityEngine.SceneManagement;
using GameEntry = GameFrame.Main.GameEntry;
using GameFrame.Main;
using GameFrame.Hotfix;

struct MyValue
{
    public int x;
    public float y;
    public string s;
}

public class HybridCLREntry
{
    //热更新程序
    private static Dictionary<string, byte[]> m_loadedHotifx = new Dictionary<string, byte[]>();
    private static Dictionary<string, bool> m_LoadedFlag = new Dictionary<string, bool>();
    private static bool manifestLoadSuccess = false;
    private static bool isLoadSuccess = false;

    public static int Main()
    {
#if !UNITY_EDITOR
        LoadMetadataForAOTAssembly();
#else
        manifestLoadSuccess = true;
#endif
        Debug.Log("=======hello, HybridCLR 看到此条日志代表你成功运行了示例项目的热更新代码=======");
        return 0;
    }

    public static void Update()
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
        GameHotfixEntry.Awake();
    }

    public static void LoadMetadataForAOTAssembly()
    {
        HotUpdateAssemblyManifest hotUpdateAssemblyManifest = Resources.Load<HotUpdateAssemblyManifest>("HotUpdateAssemblyManifest");
        foreach (var aotDllName in hotUpdateAssemblyManifest.AOTMetadataDlls)
        {
            LoadAOTDll(aotDllName);
        }

        manifestLoadSuccess = true;
    }

    ///// <summary>
    ///// 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
    ///// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行
    ///// </summary>
    //public static unsafe void LoadMetadataForAOTAssembly()
    //{
    //    // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
    //    // 我们在BuildProcessor_xxx里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

    //    /// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
    //    /// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
    //    /// 
    //    string[] aotDllList = Resources.Load<HotUpdateAssemblyManifest>("HotUpdateAssemblyManifest").AOTMetadataDlls;

    //    AssetBundle dllAB = LoadDll.AssemblyAssetBundle;
    //    foreach (var aotDllName in aotDllList)
    //    {
    //        Debug.Log($"{aotDllName}");
    //        byte[] dllBytes = dllAB.LoadAsset<TextAsset>(aotDllName).bytes;
    //        fixed (byte* ptr = dllBytes)
    //        {
    //            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
    //            LoadImageErrorCode err = (LoadImageErrorCode)RuntimeApi.LoadMetadataForAOTAssembly((IntPtr)ptr, dllBytes.Length);
    //            Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
    //        }
    //    }
    //}

    private static void TestRefAOTGenericTypeAndMethods()
    {
        new Dictionary<int, List<string>>();
        var o = (object)new ValueTuple<int, float, string>();
        Array.Empty<Vector2>();

        var g1 = new GenericType<float>();
        g1.Foo<ulong>();
        var g2 = new GenericType<double>();
        g2.Foo<ushort>();
    }

    private static void StartGameFramework()
    {
        isLoadSuccess = true;

        UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.None);
        SceneManager.LoadScene("GameFramework");
    }

    public class PrefabData
    {
        public string prefabName;
    }

    private static unsafe void LoadAOTDll(string aotName)
    {
        m_LoadedFlag.Add(aotName, false);
        PrefabData prefabData = new PrefabData() { prefabName = aotName };
        string assetName = AssetUtility.GetHotDllAsset(aotName);
        GameEntry.Resource.LoadAsset(assetName, new LoadAssetCallbacks(OnLoadAOTDllSuccess, OnLoadAOTFailured), prefabData);
    }

    private static unsafe void OnLoadAOTDllSuccess(string assetName, object asset, float duration, object userData)
    {
        PrefabData prefabData = userData as PrefabData;
        m_LoadedFlag[prefabData.prefabName] = true;

        byte[] dllBytes = ((TextAsset)asset).bytes;
        fixed (byte* ptr = dllBytes)
        {
            // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
            LoadImageErrorCode err = (LoadImageErrorCode)RuntimeApi.LoadMetadataForAOTAssembly((IntPtr)ptr, dllBytes.Length);
            Debug.Log($"LoadMetadataForAOTAssembly:{prefabData.prefabName}. ret:{err}");
        }
    }

    private static void OnLoadAOTFailured(string assetName, LoadResourceStatus status, string errorMessage, object userData)
    {
        Log.Error("Can not load {0} from '{1}' with error message '{2}'.", assetName, "AOTbsAsset", errorMessage);
    }
}

public class GenericType<T>
{
    public void Show()
    {
        new Queue<T>();
    }

    public void Foo<U>()
    {
        new Dictionary<Vector3, U>();
    }
}
