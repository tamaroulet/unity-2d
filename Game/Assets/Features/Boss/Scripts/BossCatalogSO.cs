// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Boss
{
    /// <summary>
    /// 全ボス一覧を保持するカタログ（Boss_Act1_01 / Boss_Act2_01 / Boss_Act3_01 等）。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Boss/BossCatalog", fileName = "BossCatalog")]
    public class BossCatalogSO : ScriptableObject
    {
        [SerializeField] private List<BossSO> _bosses = new List<BossSO>();

        public IReadOnlyList<BossSO> Bosses => _bosses;

        /// <summary>
        /// 指定された ID のボスを検索して返す。見つからない場合は null。
        /// </summary>
        public BossSO FindById(int bossId)
        {
            for (int i = 0; i < _bosses.Count; i++)
            {
                if (_bosses[i] != null && _bosses[i].BossId == bossId)
                {
                    return _bosses[i];
                }
            }

            return null;
        }
    }
}
