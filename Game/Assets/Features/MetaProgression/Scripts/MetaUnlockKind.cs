// SPDX-AI-Disclosure: ai-generated
namespace Game.Features.MetaProgression
{
    /// <summary>
    /// MetaUnlockSO 1件が、購入時にどの効果種別を持つかを表す。
    /// </summary>
    public enum MetaUnlockKind
    {
        /// <summary>初期スタミナ底上げ。MetaUnlockSO.BonusValue を初期 Stamina に加算する。</summary>
        InitialStaminaBonus = 0,

        /// <summary>初期スキル底上げ。MetaUnlockSO.BonusValue を初期 Skill に加算する。</summary>
        InitialSkillBonus = 1,

        /// <summary>初期メンタル底上げ。MetaUnlockSO.BonusValue を初期 Mental に加算する。</summary>
        InitialMentalBonus = 2,

        /// <summary>新規レリックのドラフトプール解放。MetaUnlockSO.TargetRelicId を解放対象とする。</summary>
        UnlockRelic = 3
    }
}
