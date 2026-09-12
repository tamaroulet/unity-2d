// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.Boss
{
    /// <summary>
    /// ボスがその戦闘ターンに取る行動の種別。
    /// BossSO.ActionPattern に並べた順で、BattleTurn に応じて周期的に選択される。
    /// </summary>
    public enum BossActionKind
    {
        /// <summary>プレイヤーのスタミナにダメージ（メンタルによる被ダメージ軽減の対象）。</summary>
        Attack,

        /// <summary>プレイヤーのメンタルに直接ダメージ。</summary>
        MentalPressure,

        /// <summary>自身に Shield を付与し、次の被ダメージを軽減する。</summary>
        Guard,

        /// <summary>Attack の強化版。大きなスタミナダメージを与える。</summary>
        SpecialAttack
    }
}
