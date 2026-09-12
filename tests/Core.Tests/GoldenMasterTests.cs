using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Game.Features.Command;
using NUnit.Framework;

namespace StandaloneCore.Tests
{
    public class GoldenMasterTests
    {
        /// <summary>このランナーが受け付けるゴールデンの出所。JSON の source と一致しないものは読まない。</summary>
        private const string ExpectedSource = "GameRulesSO.CreateInitialState";

        // 対照群（必ず通る）。落ちたらテスト実行環境そのものの故障。
        [Test]
        public void AlwaysPasses_ControlGroup()
        {
            Assert.Pass();
        }

        // NUnit のテストメソッドは public 必須。引数型を private にすると CS0051 になる。
        public sealed class Inputs
        {
            public int paramMin { get; set; }
            public int paramMax { get; set; }
            public int initialStamina { get; set; }
            public int initialSkill { get; set; }
            public int initialMental { get; set; }
            public int startTurn { get; set; }
            public int maxTurn { get; set; }
        }

        public sealed class Expected
        {
            public int CurrentTurn { get; set; }
            public int Stamina { get; set; }
            public int Skill { get; set; }
            public int Mental { get; set; }
            public ulong FiredEventMask { get; set; }
            public int[] AcquiredRelicIds { get; set; } = Array.Empty<int>();
        }

        public sealed class Case
        {
            public string id { get; set; } = "";
            public string control { get; set; } = "";
            public Inputs @in { get; set; } = new Inputs();
            public Expected @out { get; set; } = new Expected();
        }

        public sealed class Golden
        {
            public string source { get; set; } = "";
            public List<Case> cases { get; set; } = new List<Case>();
        }

        public static IEnumerable<TestCaseData> AllCases()
        {
            string dir = TestContext.CurrentContext.TestDirectory;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (string path in Directory.GetFiles(dir, "golden_*.json"))
            {
                Golden g = JsonSerializer.Deserialize<Golden>(File.ReadAllText(path), opts);
                if (g?.cases == null) continue;

                // 他機能のゴールデンを誤読しない。
                // これが無いと golden_metapoint_*.json も読み込まれ、型が合わないため
                // 入力も期待値もすべて既定値 0 になり、new GameRules(0,...) の出力 0 と
                // 「0 == 0」で一致して全件が偽陽性で緑になる（実測で発生した事故）。
                if (!string.Equals(g.source, ExpectedSource, StringComparison.Ordinal)) continue;

                string tag = Path.GetFileNameWithoutExtension(path);
                foreach (Case c in g.cases)
                {
                    yield return new TestCaseData(c).SetName($"{tag}_{c.id}");
                }
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void MatchesGoldenMaster(Case c)
        {
            var rules = new GameRules(
                c.@in.paramMin, c.@in.paramMax,
                c.@in.initialStamina, c.@in.initialSkill, c.@in.initialMental,
                c.@in.startTurn, c.@in.maxTurn);

            var state = rules.CreateInitialState();

            Assert.AreEqual(c.@out.CurrentTurn,    state.CurrentTurn,    "CurrentTurn");
            Assert.AreEqual(c.@out.Stamina,        state.Stamina,        "Stamina");
            Assert.AreEqual(c.@out.Skill,          state.Skill,          "Skill");
            Assert.AreEqual(c.@out.Mental,         state.Mental,         "Mental");
            Assert.AreEqual(c.@out.FiredEventMask, state.FiredEventMask, "FiredEventMask");
            CollectionAssert.AreEqual(c.@out.AcquiredRelicIds, state.AcquiredRelicIds, "AcquiredRelicIds");
        }
    }
}