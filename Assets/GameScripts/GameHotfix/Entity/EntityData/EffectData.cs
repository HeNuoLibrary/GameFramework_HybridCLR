﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFrame.Main;
using UnityEngine;
using System;

namespace GameFrame.Hotfix
{
    [Serializable]
    public class EffectData : EntityData
    {
        [SerializeField]
        private float m_KeepTime = 0f;

        public EffectData(int entityId, int typeId)
            : base(entityId, typeId)
        {
            m_KeepTime = 3f;
        }

        public float KeepTime
        {
            get
            {
                return m_KeepTime;
            }
        }
    }
}
