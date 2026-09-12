// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Relic
{
    /// <summary>
    /// 全レリック一覧を保持するカタログ。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Relic/RelicCatalog", fileName = "RelicCatalog")]
    public class RelicCatalogSO : ScriptableObject
    {
        [SerializeField] private List<RelicSO> _relics = new List<RelicSO>();

        public IReadOnlyList<RelicSO> Relics => _relics;

        /// <summary>
        /// 指定された ID のレリックを検索して返す。見つからない場合は null。
        /// </summary>
        public RelicSO FindById(int relicId)
        {
            for (int i = 0; i < _relics.Count; i++)
            {
                if (_relics[i] != null && _relics[i].RelicId == relicId)
                {
                    return _relics[i];
                }
            }

            return null;
        }
    }
}
