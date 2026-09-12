// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Game.Core;
using Game.Features.MetaProgression;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// MS3 のゴールデンマスタと MetaPointResolverSO（委譲後）の照合。
    ///
    /// Pure C# 側（tests/Core.Tests/MetaPointGoldenTests.cs）は MetaPointRules を直接呼ぶ。
    /// こちらは SO 経由で呼ぶ。両方が要る:
    ///   純粋クラス直だけ → 委譲の配線ミスを見逃す
    ///   SO 経由だけ      → 委譲が未配線でも旧実装が答えて緑になる
    ///
    /// source ガードは必須。無差別に golden_*.json を読むと、型の合わないデータが
    /// 既定値としてデシリアライズされ「0 == 0」で全件緑になる（実測で発生した事故）。
    /// </summary>
    public class MetaPointEquivalenceTests
    {
        private const string ExpectedSource = "MetaPointResolverSO";

        // 必ず通る対照群。落ちたらテスト実行環境そのものの故障。
        [Test]
        public void AlwaysPasses_ControlGroup()
        {
            Assert.Pass();
        }

        // 「1 件も読めていない」を緑と呼ばせないための門。
        [Test]
        public void MetaPointEquivalence_SourceIsPresent()
        {
            string dir = GoldenDir();
            Assert.IsFalse(string.IsNullOrEmpty(dir), "MS3_GOLDEN_DIR が設定されていません");
            Assert.IsTrue(Directory.Exists(dir), "MS3_GOLDEN_DIR が存在しません: " + dir);
            Assert.Greater(LoadAll(dir).Count, 0,
                "source = \"" + ExpectedSource + "\" のゴールデンを 1 件も読めていません");
        }

        public sealed class Case
        {
            public string Tag = "";
            public string Id = "";
            public string Method = "";
            public int CTurn, CSkill, CBoss, CClear;

            public bool StateNull;
            public int Turn, Sta, Skl, Men, BossCount;
            public bool IsClear;

            public bool ProfileNull;
            public int Avail, TotalEarned, Runs, Earned;
            public int[] Unlocked = Array.Empty<int>();

            public int OutPoints;
            public bool OutResultNull;
            public int OutAvail, OutTotalEarned, OutRuns;
            public int[] OutUnlocked = Array.Empty<int>();

            public override string ToString() { return Tag + "_" + Id; }
        }

        private static string GoldenDir()
        {
            return Environment.GetEnvironmentVariable("MS3_GOLDEN_DIR");
        }

        public static IEnumerable<TestCaseData> AllCases()
        {
            string dir = GoldenDir();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) { yield break; }

            foreach (Case c in LoadAll(dir))
            {
                yield return new TestCaseData(c).SetName(c.Tag + "_" + c.Id);
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void MatchesGoldenMaster(Case c)
        {
            MetaPointResolverSO so = MetaPointResolverSOFactory.Create(c.CTurn, c.CSkill, c.CBoss, c.CClear);
            try
            {
                if (c.Method == "calc")
                {
                    GameState state = c.StateNull ? null : new GameState
                    {
                        CurrentTurn = c.Turn, Stamina = c.Sta, Skill = c.Skl, Mental = c.Men
                    };
                    int actual = so.CalculateEarnedPoints(state, c.IsClear, c.BossCount);
                    Assert.AreEqual(c.OutPoints, actual, "points");
                    return;
                }

                if (c.Method == "apply")
                {
                    MetaProfileState profile = c.ProfileNull ? null : new MetaProfileState
                    {
                        AvailableMetaPoints = c.Avail,
                        TotalEarnedMetaPoints = c.TotalEarned,
                        TotalRunsCompleted = c.Runs,
                        UnlockedIds = c.Unlocked
                    };
                    MetaProfileState actual = so.ApplyRunResult(profile, c.Earned);

                    if (c.OutResultNull) { Assert.IsNull(actual, "resultNull"); return; }

                    Assert.IsNotNull(actual, "resultNull");
                    Assert.AreEqual(c.OutAvail, actual.AvailableMetaPoints, "AvailableMetaPoints");
                    Assert.AreEqual(c.OutTotalEarned, actual.TotalEarnedMetaPoints, "TotalEarnedMetaPoints");
                    Assert.AreEqual(c.OutRuns, actual.TotalRunsCompleted, "TotalRunsCompleted");
                    CollectionAssert.AreEqual(c.OutUnlocked, actual.UnlockedIds, "UnlockedIds");
                    return;
                }

                Assert.Fail("未知の method: " + c.Method);
            }
            finally { UnityEngine.Object.DestroyImmediate(so); }
        }

        // ---- 読み取り。Newtonsoft は入っておらず、JsonUtility は "in" という
        //      C# キーワード名のフィールドを扱えないため、キー名で拾う。

        private static List<Case> LoadAll(string dir)
        {
            var list = new List<Case>();
            foreach (string path in Directory.GetFiles(dir, "golden_*.json"))
            {
                string text = File.ReadAllText(path);
                Match src = Regex.Match(text, "\"source\"\\s*:\\s*\"([^\"]*)\"");
                if (!src.Success || src.Groups[1].Value != ExpectedSource) { continue; }

                // 採取スクリプトが 1 ケース 1 行で出す固定書式。入れ子を正規表現で
                // 切り出そうとすると整形出力で壊れるため、行で分ける。
                string tag = Path.GetFileNameWithoutExtension(path);
                foreach (string raw in text.Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.IndexOf("\"method\"", StringComparison.Ordinal) < 0) { continue; }
                    list.Add(Parse(tag, line));
                }
            }
            return list;
        }

        private static Case Parse(string tag, string blob)
        {
            var c = new Case
            {
                Tag = tag,
                Id = Str(blob, "id"),
                Method = Str(blob, "method"),
                CTurn = Int(blob, "turnBonusPerTurn"),
                CSkill = Int(blob, "skillBonusMultiplier"),
                CBoss = Int(blob, "bossDefeatedBonus"),
                CClear = Int(blob, "gameClearBonus")
            };

            if (c.Method == "calc")
            {
                c.StateNull = Bool(blob, "stateNull");
                c.Turn = Int(blob, "currentTurn");
                c.Sta = Int(blob, "stamina");
                c.Skl = Int(blob, "skill");
                c.Men = Int(blob, "mental");
                c.IsClear = Bool(blob, "isGameClear");
                c.BossCount = Int(blob, "bossDefeatedCount");
                c.OutPoints = Int(blob, "points");
                return c;
            }

            // in と out に同名キーが出る。出現順に頼ると書式の変化で静かに壊れるため、
            // "out" の位置で文字列を割り、それぞれの区間だけを見る。
            int outIdx = blob.IndexOf("\"out\"", StringComparison.Ordinal);
            Assert.Greater(outIdx, 0, "out が見つかりません: " + c.Id);
            string inPart = blob.Substring(0, outIdx);
            string outPart = blob.Substring(outIdx);

            c.ProfileNull = Bool(inPart, "profileNull");
            c.Earned = Int(inPart, "earnedPoints");
            c.Avail = Int(inPart, "availableMetaPoints");
            c.TotalEarned = Int(inPart, "totalEarnedMetaPoints");
            c.Runs = Int(inPart, "totalRunsCompleted");
            c.Unlocked = IntArray(inPart, "unlockedIds");

            c.OutResultNull = Bool(outPart, "resultNull");
            if (!c.OutResultNull)
            {
                c.OutAvail = Int(outPart, "availableMetaPoints");
                c.OutTotalEarned = Int(outPart, "totalEarnedMetaPoints");
                c.OutRuns = Int(outPart, "totalRunsCompleted");
                c.OutUnlocked = IntArray(outPart, "unlockedIds");
            }
            return c;
        }

        private static string Str(string s, string key)
        {
            Match m = Regex.Match(s, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
            Assert.IsTrue(m.Success, "キーが読めません: " + key);
            return m.Groups[1].Value;
        }

        private static int Int(string s, string key)
        {
            Match m = Regex.Match(s, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            Assert.IsTrue(m.Success, "キーが読めません: " + key);
            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static bool Bool(string s, string key)
        {
            Match m = Regex.Match(s, "\"" + key + "\"\\s*:\\s*(true|false)");
            return m.Success && m.Groups[1].Value == "true";
        }

        private static int[] IntArray(string s, string key)
        {
            Match m = Regex.Match(s, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            Assert.IsTrue(m.Success, "キーが読めません: " + key);
            string body = m.Groups[1].Value.Trim();
            if (body.Length == 0) { return Array.Empty<int>(); }

            string[] parts = body.Split(',');
            var a = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                a[i] = int.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
            }
            return a;
        }
    }
}
