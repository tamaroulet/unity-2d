// SPDX-AI-Disclosure: ai-generated
using UnityEngine;

namespace Game.Features.MetaProgression
{
    /// <summary>
    /// メタ進行で購入可能なアンロック1件の定義データ（ID、必要ポイント、効果種別、効果値）。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/MetaProgression/MetaUnlock", fileName = "MetaUnlock")]
    public class MetaUnlockSO : ScriptableObject
    {
        [SerializeField] private int _unlockId;
        [SerializeField] private string _unlockName;
        [SerializeField] private int _cost;
        [SerializeField] private MetaUnlockKind _kind;
        [SerializeField] private int _bonusValue;
        [SerializeField] private int _targetRelicId;

        /// <summary>一意キー。MetaProfileState.UnlockedIds に格納される値と一致する。</summary>
        public int UnlockId => _unlockId;

        /// <summary>フレーバー文字列を持たないメタデータキー。例: Unlock_Stat_Stamina_01。</summary>
        public string UnlockName => _unlockName;

        /// <summary>購入に必要な MetaPoints。負値はデータ不整合として Resolver 側で拒否される。</summary>
        public int Cost => _cost;

        public MetaUnlockKind Kind => _kind;

        /// <summary>Kind が InitialXxxBonus のとき、初期パラメータへ加算する値。</summary>
        public int BonusValue => _bonusValue;

        /// <summary>Kind が UnlockRelic のとき、解放対象となる RelicSO.RelicId。</summary>
        public int TargetRelicId => _targetRelicId;
    }
}
