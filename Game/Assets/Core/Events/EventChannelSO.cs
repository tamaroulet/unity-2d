// SPDX-AI-Disclosure: ai-generated
using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// 値の型 T を購読者へ通知する、ScriptableObject 経由のイベントチャンネルの基底クラス。
    /// Unity はジェネリックな ScriptableObject の直接生成に対応していないため、
    /// [CreateAssetMenu] は具象型側にのみ付与する。
    /// </summary>
    public abstract class EventChannelSO<T> : ScriptableObject
    {
        public event Action<T> OnEventRaised;

        /// <summary>
        /// 購読者に値を通知する。購読者が0件でも例外を投げない。
        /// </summary>
        public void Raise(T value)
        {
            OnEventRaised?.Invoke(value);
        }

        /// <summary>
        /// エディタの Play モード切り替え時に購読が重複して残るのを防ぐため、
        /// 無効化のタイミングで購読をすべて解除する。
        /// </summary>
        private void OnDisable()
        {
            OnEventRaised = null;
        }
    }
}
