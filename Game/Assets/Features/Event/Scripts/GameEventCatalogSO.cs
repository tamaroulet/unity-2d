// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.Event
{
    /// <summary>
    /// イベント一覧と、判定に用いるパラメータ上下限値を保持する。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Event/GameEventCatalog", fileName = "GameEventCatalog")]
    public class GameEventCatalogSO : ScriptableObject
    {
        [SerializeField] private List<GameEventSO> _events = new List<GameEventSO>();
        [SerializeField] private int _paramMin;
        [SerializeField] private int _paramMax;

        public IReadOnlyList<GameEventSO> Events => _events;

        public int ParamMin => _paramMin;

        public int ParamMax => _paramMax;
    }
}
