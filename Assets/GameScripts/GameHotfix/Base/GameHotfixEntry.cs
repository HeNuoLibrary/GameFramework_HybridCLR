// -----------------------------------------------
// Copyright © GameFramework. All rights reserved.
// CreateTime: 2022/8/3   14:57:35
// -----------------------------------------------
using GameEntry = GameFrame.Main.GameEntry;
using ProcedureBase = GameFrame.Main.ProcedureBase;
using UnityGameFramework.Runtime;
using System.Collections.Generic;
using GameFramework.Procedure;
using GameFramework.Fsm;
using GameFrame.Main;
using GameFramework;

namespace GameFrame.Hotfix
{

    public static class GameHotfixEntry
    {
        public static HPBarComponent HPBar
        {
            get;
            private set;
        }

        public static void Awake()
        {
            Log.Info("<color=green> GameHotfixEntry.Awake </color>");
            // 重置流程组件，初始化热更新流程。
            GameEntry.Fsm.DestroyFsm<IProcedureManager>();
            var procedureManager = GameFrameworkEntry.GetModule<IProcedureManager>();
            ProcedureBase[] procedures =
            {
                new ProcedureChangeScene(),
                new ProcedureMain(),
                new ProcedureMenu(),
                new ProcedurePreload(),
                new ProcedureHuatuoLaunch(),
            };
            procedureManager.Initialize(GameFrameworkEntry.GetModule<IFsmManager>(), procedures);
            procedureManager.StartProcedure<ProcedurePreload>();
        }

        public static void Start()
        {
            Log.Info("<color=green> GameHotfixEntry.Start </color>");
            HPBar = UnityGameFramework.Runtime.GameEntry.GetComponent<HPBarComponent>();
        }
    }
}
