// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.MetaProgression
{
    /// <summary>
    /// 全 MetaUnlockSO 一覧を保持するカタログ。
    /// MetaProfileState.UnlockedIds（int の一覧）から実際の MetaUnlockSO（効果種別・効果値）を
    /// 引くための唯一の経路とする。RelicCatalogSO / BossCatalogSO と同一の設計方針。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/MetaProgression/MetaUnlockCatalog", fileName = "MetaUnlockCatalog")]
    public class MetaUnlockCatalogSO : ScriptableObject
    {
        [SerializeField] private List<MetaUnlockSO> _unlocks = new List<MetaUnlockSO>();

        public IReadOnlyList<MetaUnlockSO> Unlocks => _unlocks;

        /// <summary>
        /// 指定された ID のアンロックを検索して返す。見つからない場合は null。
        /// </summary>
        public MetaUnlockSO FindById(int unlockId)
        {
            for (int i = 0; i < _unlocks.Count; i++)
            {
                if (_unlocks[i] != null && _unlocks[i].UnlockId == unlockId)
                {
                    return _unlocks[i];
                }
            }

            return null;
        }
    }
}
