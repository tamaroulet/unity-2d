using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Game.Core;
using Game.Features.MetaProgression;
using NUnit.Framework;

namespace StandaloneCore.Tests
{
    /// <summary>
    /// MS3 のゴールデンマスタと MetaPointRules（純粋版）の照合。
    /// 現行の MetaPointResolverSO から機械採取した挙動を固定する。
    ///
    /// このランナーは source が一致する JSON しか読まない。
    /// 無差別に golden_*.json を読むと、型の合わない JSON が既定値 0 として
    /// デシリアライズされ「0 == 0」で全件緑になる（実測で発生した事故）。
    /// </summary>
    public class MetaPointGoldenTests
    {
        private const string ExpectedSource = "MetaPointResolverSO";

        public sealed class Coeff
        {
            public int turnBonusPerTurn { get; set; }
            public int skillBonusMultiplier { get; set; }
            public int bossDefeatedBonus { get; set; }
            public int gameClearBonus { get; set; }
        }

        public sealed class Inputs
        {
            // calc
            public bool stateNull { get; set; }
            public int currentTurn { get; set; }
            public int stamina { get; set; }
            public int skill { get; set; }
            public int mental { get; set; }
            public bool isGameClear { get; set; }
            public int bossDefeatedCount { get; set; }

            // apply
            public bool profileNull { get; set; }
            public int availableMetaPoints { get; set; }
            public int totalEarnedMetaPoints { get; set; }
            public int totalRunsCompleted { get; set; }
            public int[] unlockedIds { get; set; } = Array.Empty<int>();
            public int earnedPoints { get; set; }
        }

        public sealed class Outputs
        {
            public int points { get; set; }
            public bool resultNull { get; set; }
            public int availableMetaPoints { get; set; }
            public int totalEarnedMetaPoints { get; set; }
            public int totalRunsCompleted { get; set; }
            public int[] unlockedIds { get; set; } = Array.Empty<int>();
        }

        public sealed class Case
        {
            public string id { get; set; } = "";
            public string control { get; set; } = "";
            public string method { get; set; } = "";
            public Coeff coeff { get; set; } = new Coeff();
            public Inputs @in { get; set; } = new Inputs();
            public Outputs @out { get; set; } = new Outputs();

            public override string ToString() { return id; }
        }

        private sealed class Golden
        {
            public string source { get; set; } = "";
            public List<Case> cases { get; set; } = new List<Case>();
        }

        private static List<TestCaseData> Load()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var list = new List<TestCaseData>();

            foreach (string path in Directory.GetFiles(dir, "golden_*.json"))
            {
                Golden g = JsonSerializer.Deserialize<Golden>(File.ReadAllText(path), opts);
                if (g?.cases == null) continue;
                if (!string.Equals(g.source, ExpectedSource, StringComparison.Ordinal)) continue;

                string tag = Path.GetFileNameWithoutExtension(path);
                foreach (Case c in g.cases)
                {
                    list.Add(new TestCaseData(c).SetName($"{tag}_{c.id}"));
                }
            }
            return list;
        }

        public static IEnumerable<TestCaseData> AllCases()
        {
            return Load();
        }

        /// <summary>「1 件も読めていない」を緑と呼ばせないための門。</summary>
        [Test]
        public void MetaPointGolden_SourceIsPresent()
        {
            Assert.Greater(Load().Count, 0,
                $"source = \"{ExpectedSource}\" のゴールデンを 1 件も読めていません");
        }

        [TestCaseSource(nameof(AllCases))]
        public void MatchesGoldenMaster(Case c)
        {
            var rules = new MetaPointRules(
                c.coeff.turnBonusPerTurn, c.coeff.skillBonusMultiplier,
                c.coeff.bossDefeatedBonus, c.coeff.gameClearBonus);

            if (c.method == "calc")
            {
                GameState state = c.@in.stateNull ? null : new GameState
                {
                    CurrentTurn = c.@in.currentTurn,
                    Stamina = c.@in.stamina,
                    Skill = c.@in.skill,
                    Mental = c.@in.mental
                };

                int actual = rules.CalculateEarnedPoints(state, c.@in.isGameClear, c.@in.bossDefeatedCount);
                Assert.AreEqual(c.@out.points, actual, "points");
                return;
            }

            if (c.method == "apply")
            {
                MetaProfileState profile = c.@in.profileNull ? null : new MetaProfileState
                {
                    AvailableMetaPoints = c.@in.availableMetaPoints,
                    TotalEarnedMetaPoints = c.@in.totalEarnedMetaPoints,
                    TotalRunsCompleted = c.@in.totalRunsCompleted,
                    UnlockedIds = c.@in.unlockedIds
                };

                MetaProfileState actual = rules.ApplyRunResult(profile, c.@in.earnedPoints);

                if (c.@out.resultNull)
                {
                    Assert.IsNull(actual, "resultNull");
                    return;
                }

                Assert.IsNotNull(actual, "resultNull");
                Assert.AreEqual(c.@out.availableMetaPoints, actual.AvailableMetaPoints, "AvailableMetaPoints");
                Assert.AreEqual(c.@out.totalEarnedMetaPoints, actual.TotalEarnedMetaPoints, "TotalEarnedMetaPoints");
                Assert.AreEqual(c.@out.totalRunsCompleted, actual.TotalRunsCompleted, "TotalRunsCompleted");
                CollectionAssert.AreEqual(c.@out.unlockedIds, actual.UnlockedIds, "UnlockedIds");
                return;
            }

            Assert.Fail($"未知の method: {c.method}");
        }
    }
}
