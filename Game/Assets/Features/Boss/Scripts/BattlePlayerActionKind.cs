// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.Boss
{
    /// <summary>
    /// ボス戦闘中にプレイヤーが選択できる戦闘コマンドの種別。
    /// 育成パラメータ（スタミナ＝AP、スキル＝ダメージ、メンタル＝シールド維持）を
    /// 戦闘計算の入力として直接使用する（GameDesignMaster.md 4章）。
    /// </summary>
    public enum BattlePlayerActionKind
    {
        /// <summary>スタミナを消費し、Skill に応じたダメージをボスへ与える。</summary>
        Attack,

        /// <summary>スタミナを回復する（AP を温存する防御行動）。</summary>
        Defend,

        /// <summary>メンタルを回復する（精神統一。被ダメージ軽減シールドの源）。</summary>
        MentalFocus
    }
}
