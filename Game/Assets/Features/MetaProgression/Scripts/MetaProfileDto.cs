// SPDX-AI-Disclosure: ai-generated
using System;

namespace Game.Features.MetaProgression
{
    [Serializable]
    public sealed class MetaProfileDto
    {
        public int Version = 1;
        public int AvailableMetaPoints;
        public int TotalEarnedMetaPoints;
        public int TotalRunsCompleted;
        public int[] UnlockedIds = Array.Empty<int>();
    }
}
