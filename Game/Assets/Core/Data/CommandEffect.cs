// SPDX-AI-Disclosure: ai-generated
namespace Game.Core
{
    /// <summary>
    /// コマンドが GameState に与える効果値を表す、シリアライズしない不変の値型。
    /// </summary>
    public readonly struct CommandEffect
    {
        public int StaminaDelta { get; }

        public int SkillDelta { get; }

        public int MentalDelta { get; }

        public int StaminaCost { get; }

        public CommandEffect(int staminaDelta, int skillDelta, int mentalDelta, int staminaCost)
        {
            StaminaDelta = staminaDelta;
            SkillDelta = skillDelta;
            MentalDelta = mentalDelta;
            StaminaCost = staminaCost;
        }
    }
}
