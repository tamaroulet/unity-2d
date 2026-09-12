// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Game.Core;
using Game.Features.Command;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// MS1 で採取したゴールデンマスタと GameRulesSO の出力が一致することを検査する。
    /// ラッパー化（CreateInitialState を純粋 GameRules へ委譲）で挙動が変わっていないことの証明。
    ///
    /// 既存の GameRulesSOTests はテスト1本・入力1組しかなく、等価性の証明には足りない。
    /// こちらは境界値込み 29 ケース（開示 18 / 非開示 11）を流す。
    ///
    /// 入力 JSON の場所は環境変数 MS2_GOLDEN_DIR で渡す。非開示分を
    /// リポジトリに置かないため。受入スクリプトが判定直前に投入し、直後に消す。
    /// </summary>
    public class GoldenMasterEquivalenceTests
    {
        // 必ず通る対照群。落ちたらテスト実行環境そのものの故障。
        [Test]
        public void AlwaysPasses_ControlGroup()
        {
            Assert.Pass();
        }

        // 「1件も実行しなかった」を緑と呼ばせないための門。
        // TestCaseSource が空でも NUnit は黙って 0 件を返すため、別テストで明示的に落とす。
        [Test]
        public void GoldenMasterEquivalence_SourceIsPresent()
        {
            string dir = GoldenDir();
            Assert.IsFalse(string.IsNullOrEmpty(dir), "MS2_GOLDEN_DIR が設定されていません");
            Assert.IsTrue(Directory.Exists(dir), "MS2_GOLDEN_DIR が存在しません: " + dir);

            string[] files = Directory.GetFiles(dir, "golden_*.json");
            Assert.Greater(files.Length, 0, "golden_*.json が1本もありません: " + dir);

            int total = 0;
            foreach (string f in files) { total += ParseFile(f).Count; }
            Assert.Greater(total, 0, "ケースが1件も読めませんでした");
        }

        public sealed class Case
        {
            public string Tag;
            public string Id;
            public string Control;
            public int PMin, PMax, Sta, Skl, Men, Turn, MaxTurn;
            public int OutTurn, OutSta, OutSkl, OutMen;
            public ulong OutMask;
            public int[] OutRelics;

            public override string ToString() { return Tag + "_" + Id; }
        }

        private static string GoldenDir()
        {
            return Environment.GetEnvironmentVariable("MS2_GOLDEN_DIR");
        }

        public static IEnumerable<TestCaseData> AllCases()
        {
            string dir = GoldenDir();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                yield break; // 空になった場合は SourceIsPresent が落ちる
            }

            foreach (string path in Directory.GetFiles(dir, "golden_*.json"))
            {
                foreach (Case c in ParseFile(path))
                {
                    yield return new TestCaseData(c).SetName(c.Tag + "_" + c.Id);
                }
            }
        }

        [TestCaseSource(nameof(AllCases))]
        public void MatchesGoldenMaster(Case c)
        {
            GameRulesSO so = GameRulesSOFactory.Create(
                paramMin: c.PMin, paramMax: c.PMax,
                initialStamina: c.Sta, initialSkill: c.Skl, initialMental: c.Men,
                startTurn: c.Turn, maxTurn: c.MaxTurn);
            try
            {
                GameState s = so.CreateInitialState();

                Assert.AreEqual(c.OutTurn, s.CurrentTurn, "CurrentTurn");
                Assert.AreEqual(c.OutSta, s.Stamina, "Stamina");
                Assert.AreEqual(c.OutSkl, s.Skill, "Skill");
                Assert.AreEqual(c.OutMen, s.Mental, "Mental");
                Assert.AreEqual(c.OutMask, s.FiredEventMask, "FiredEventMask");
                CollectionAssert.AreEqual(c.OutRelics, s.AcquiredRelicIds, "AcquiredRelicIds");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(so);
            }
        }

        // ---- 最小の読み取り。Newtonsoft はこのプロジェクトに入っておらず、
        //      JsonUtility は "in" という C# キーワード名のフィールドを扱えない。
        //      採取スクリプトが1ケース1行の固定書式で出すので、キー名で拾えば足りる。

        private static List<Case> ParseFile(string path)
        {
            string tag = Path.GetFileNameWithoutExtension(path);
            var list = new List<Case>();

            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.IndexOf("\"id\":", StringComparison.Ordinal) < 0) { continue; }

                var c = new Case
                {
                    Tag = tag,
                    Id = Str(line, "id"),
                    Control = Str(line, "control"),
                    PMin = Int(line, "paramMin"),
                    PMax = Int(line, "paramMax"),
                    Sta = Int(line, "initialStamina"),
                    Skl = Int(line, "initialSkill"),
                    Men = Int(line, "initialMental"),
                    Turn = Int(line, "startTurn"),
                    MaxTurn = Int(line, "maxTurn"),
                    OutTurn = Int(line, "CurrentTurn"),
                    OutSta = Int(line, "Stamina"),
                    OutSkl = Int(line, "Skill"),
                    OutMen = Int(line, "Mental"),
                    OutMask = ULong(line, "FiredEventMask"),
                    OutRelics = IntArray(line, "AcquiredRelicIds")
                };
                list.Add(c);
            }
            return list;
        }

        private static string Str(string line, string key)
        {
            Match m = Regex.Match(line, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
            Assert.IsTrue(m.Success, "キーが読めません: " + key);
            return m.Groups[1].Value;
        }

        private static int Int(string line, string key)
        {
            Match m = Regex.Match(line, "\"" + key + "\"\\s*:\\s*(-?\\d+)");
            Assert.IsTrue(m.Success, "キーが読めません: " + key);
            return int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static ulong ULong(string line, string key)
        {
            Match m = Regex.Match(line, "\"" + key + "\"\\s*:\\s*(\\d+)");
            Assert.IsTrue(m.Success, "キーが読めません: " + key);
            return ulong.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        private static int[] IntArray(string line, string key)
        {
            Match m = Regex.Match(line, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            Assert.IsTrue(m.Success, "キーが読めません: " + key);
            string body = m.Groups[1].Value.Trim();
            if (body.Length == 0) { return Array.Empty<int>(); }

            string[] parts = body.Split(',');
            var v = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                v[i] = int.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
            }
            return v;
        }
    }
}
