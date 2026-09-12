// SPDX-AI-Disclosure: ai-generated
using System.Collections;
using System.Reflection;
using Game.Core;
using Game.Features.Boss;
using Game.Features.GameFlow;
using Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// u-0058: 表示が「繋がっている」ではなく「追随している」ことを見る liveness テスト。
    /// 正の側（状態追随）と負の側（参照を外した対照群）を 2 本 1 組で書く。
    /// </summary>
    public class DisplayLivenessTest
    {
        private const string SceneName = "MainGame";

        private GameFlowController _flow;
        private StatusView _statusView;
        private BossBattleDialogView _bossDialog;
        private RunDriver _driver;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("Game.MetaProfile");
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("Game.MetaProfile");
            PlayerPrefs.Save();
        }

        private static T GetField<T>(Component target, string fieldName)
        {
            Assert.IsNotNull(target, $"Target component is null for field '{fieldName}'.");
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} field not found.");
            return (T)field.GetValue(target);
        }

        private static void SetField(Component target, string fieldName, object value)
        {
            Assert.IsNotNull(target, $"Target component is null for field '{fieldName}'.");
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} field not found.");
            field.SetValue(target, value);
        }

        private IEnumerator LoadSceneAndInit()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.IsTrue(load != null, $"Failed to load scene '{SceneName}'.");
            while (!load.isDone)
            {
                yield return null;
            }
            yield return null;

            _flow = Object.FindFirstObjectByType<GameFlowController>();
            Assert.IsTrue(_flow != null, "GameFlowController not found in scene.");

            _statusView = Object.FindFirstObjectByType<StatusView>();
            Assert.IsTrue(_statusView != null, "StatusView not found in scene.");

            _bossDialog = Object.FindFirstObjectByType<BossBattleDialogView>(FindObjectsInactive.Include);
            Assert.IsTrue(_bossDialog != null, "BossBattleDialogView not found in scene.");

            _driver = RunDriver.FromScene();

            float deadline = Time.realtimeSinceStartup + 10f;
            while (_flow.CurrentPhase != GamePhase.WaitingInput && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.AreEqual(GamePhase.WaitingInput, _flow.CurrentPhase, "Game did not reach WaitingInput phase.");
            Assert.IsNotNull(_statusView.LastDisplayedState, "StatusView should have received initial GameState.");
        }

        // ==========================================
        // 1. StaminaGauge
        // ==========================================

        [UnityTest]
        public IEnumerator StaminaGauge_TracksState()
        {
            yield return LoadSceneAndInit();

            // ① 対象の参照が null でないことを先に主張する
            Slider staminaGauge = GetField<Slider>(_statusView, "_staminaGauge");
            Assert.IsNotNull(staminaGauge, "StatusView._staminaGauge should not be null.");

            float beforeValue = staminaGauge.value;
            int initialStamina = _statusView.LastDisplayedState.Stamina;
            Assert.AreEqual((float)initialStamina, beforeValue);

            // ② 状態を動かす（RunDriver で数ターン進める）
            _driver.CommandPolicy = _ => _driver.TrainButton;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_statusView.LastDisplayedState.Stamina == initialStamina && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            // ③ 表示側の値が状態に一致することを見る
            Assert.AreEqual((float)_statusView.LastDisplayedState.Stamina, staminaGauge.value);

            // ④ 動かす前と後で値が変わったことを見る
            Assert.AreNotEqual(beforeValue, staminaGauge.value);
        }

        [UnityTest]
        public IEnumerator StaminaGauge_FailsToTrack_WhenReferenceCleared()
        {
            yield return LoadSceneAndInit();

            Slider staminaGauge = GetField<Slider>(_statusView, "_staminaGauge");
            Assert.IsNotNull(staminaGauge, "StatusView._staminaGauge should not be null.");

            float beforeValue = staminaGauge.value;
            int initialStamina = _statusView.LastDisplayedState.Stamina;

            // 反射で当該の private フィールドに null を入れてから同じ操作をし、
            SetField(_statusView, "_staminaGauge", null);

            _driver.CommandPolicy = _ => _driver.TrainButton;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_statusView.LastDisplayedState.Stamina == initialStamina && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            Assert.AreNotEqual(initialStamina, _statusView.LastDisplayedState.Stamina, "Stamina should have changed.");

            // 表示が動かないことを見る
            Assert.AreEqual(beforeValue, staminaGauge.value, "Stamina gauge value should remain unchanged when reference is cleared.");
            Assert.AreNotEqual((float)_statusView.LastDisplayedState.Stamina, staminaGauge.value);
        }

        // ==========================================
        // 2. SkillGauge
        // ==========================================

        [UnityTest]
        public IEnumerator SkillGauge_TracksState()
        {
            yield return LoadSceneAndInit();

            // ① 対象の参照が null でないことを先に主張する
            Slider skillGauge = GetField<Slider>(_statusView, "_skillGauge");
            Assert.IsNotNull(skillGauge, "StatusView._skillGauge should not be null.");

            float beforeValue = skillGauge.value;
            int initialSkill = _statusView.LastDisplayedState.Skill;
            Assert.AreEqual(Mathf.Clamp(initialSkill, 0, 100), (int)beforeValue);

            // ② 状態を動かす（RunDriver で数ターン進める）
            _driver.CommandPolicy = _ => _driver.TrainButton;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_statusView.LastDisplayedState.Skill == initialSkill && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            // ③ 表示側の値が状態に一致することを見る
            float expectedSkill = Mathf.Clamp(_statusView.LastDisplayedState.Skill, 0, 100);
            Assert.AreEqual(expectedSkill, skillGauge.value);

            // ④ 動かす前と後で値が変わったことを見る
            Assert.AreNotEqual(beforeValue, skillGauge.value);
        }

        [UnityTest]
        public IEnumerator SkillGauge_FailsToTrack_WhenReferenceCleared()
        {
            yield return LoadSceneAndInit();

            Slider skillGauge = GetField<Slider>(_statusView, "_skillGauge");
            Assert.IsNotNull(skillGauge, "StatusView._skillGauge should not be null.");

            float beforeValue = skillGauge.value;
            int initialSkill = _statusView.LastDisplayedState.Skill;

            // 反射で当該の private フィールドに null を入れてから同じ操作をし、
            SetField(_statusView, "_skillGauge", null);

            _driver.CommandPolicy = _ => _driver.TrainButton;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_statusView.LastDisplayedState.Skill == initialSkill && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            Assert.AreNotEqual(initialSkill, _statusView.LastDisplayedState.Skill, "Skill should have changed.");

            // 表示が動かないことを見る
            Assert.AreEqual(beforeValue, skillGauge.value, "Skill gauge value should remain unchanged when reference is cleared.");
            Assert.AreNotEqual(Mathf.Clamp(_statusView.LastDisplayedState.Skill, 0, 100), (int)skillGauge.value);
        }

        // ==========================================
        // 3. MentalGauge
        // ==========================================

        [UnityTest]
        public IEnumerator MentalGauge_TracksState()
        {
            yield return LoadSceneAndInit();

            // ① 対象の参照が null でないことを先に主張する
            Slider mentalGauge = GetField<Slider>(_statusView, "_mentalGauge");
            Assert.IsNotNull(mentalGauge, "StatusView._mentalGauge should not be null.");

            float beforeValue = mentalGauge.value;
            int initialMental = _statusView.LastDisplayedState.Mental;
            Assert.AreEqual((float)initialMental, beforeValue);

            // ② 状態を動かす（RunDriver で数ターン進める）
            _driver.CommandPolicy = _ => _driver.TrainButton;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_statusView.LastDisplayedState.Mental == initialMental && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            // ③ 表示側の値が状態に一致することを見る
            Assert.AreEqual((float)_statusView.LastDisplayedState.Mental, mentalGauge.value);

            // ④ 動かす前と後で値が変わったことを見る
            Assert.AreNotEqual(beforeValue, mentalGauge.value);
        }

        [UnityTest]
        public IEnumerator MentalGauge_FailsToTrack_WhenReferenceCleared()
        {
            yield return LoadSceneAndInit();

            Slider mentalGauge = GetField<Slider>(_statusView, "_mentalGauge");
            Assert.IsNotNull(mentalGauge, "StatusView._mentalGauge should not be null.");

            float beforeValue = mentalGauge.value;
            int initialMental = _statusView.LastDisplayedState.Mental;

            // 反射で当該の private フィールドに null を入れてから同じ操作をし、
            SetField(_statusView, "_mentalGauge", null);

            _driver.CommandPolicy = _ => _driver.TrainButton;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (_statusView.LastDisplayedState.Mental == initialMental && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            Assert.AreNotEqual(initialMental, _statusView.LastDisplayedState.Mental, "Mental should have changed.");

            // 表示が動かないことを見る
            Assert.AreEqual(beforeValue, mentalGauge.value, "Mental gauge value should remain unchanged when reference is cleared.");
            Assert.AreNotEqual((float)_statusView.LastDisplayedState.Mental, mentalGauge.value);
        }

        // ==========================================
        // 4. BossHpSlider
        // ==========================================

        [UnityTest]
        public IEnumerator BossHpSlider_TracksState()
        {
            yield return LoadSceneAndInit();

            // ① 対象の参照が null でないことを先に主張する
            Slider bossHpSlider = GetField<Slider>(_bossDialog, "_bossHpSlider");
            Assert.IsNotNull(bossHpSlider, "BossBattleDialogView._bossHpSlider should not be null.");

            float beforeValue = bossHpSlider.value;
            float beforeMaxValue = bossHpSlider.maxValue;

            // ② 状態を動かす（RunDriver で数ターン進める）
            float deadline = Time.realtimeSinceStartup + 25f;
            while (!_bossDialog.IsVisible && _driver.Peek(_flow) != RunDriver.Action.DismissBoss && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            Assert.IsTrue(_bossDialog.IsVisible || _driver.Peek(_flow) == RunDriver.Action.DismissBoss,
                $"Boss battle dialog did not appear within timeout. {_driver.Describe(_flow)}");
            Assert.IsNotNull(_flow.LastBossBattleResult, "LastBossBattleResult should not be null.");

            BossState bossState = _flow.LastBossBattleResult.FinalBossState;
            Assert.IsNotNull(bossState, "FinalBossState should not be null.");

            // ③ 表示側の値が状態に一致することを見る
            // 指示書: BossBattleDialogView _bossHpSlider value == 現在 HP かつ maxValue == 最大 HP
            Assert.AreEqual((float)bossState.CurrentHp, bossHpSlider.value, "Slider value should match CurrentHp.");
            Assert.AreEqual((float)bossState.MaxHp, bossHpSlider.maxValue, "Slider maxValue should match MaxHp.");

            // ④ 動かす前と後で値が変わったことを見る
            Assert.AreNotEqual(beforeMaxValue, bossHpSlider.maxValue, "Slider maxValue should have changed from initial value to boss MaxHp.");
        }

        [UnityTest]
        public IEnumerator BossHpSlider_FailsToTrack_WhenReferenceCleared()
        {
            yield return LoadSceneAndInit();

            Slider bossHpSlider = GetField<Slider>(_bossDialog, "_bossHpSlider");
            Assert.IsNotNull(bossHpSlider, "BossBattleDialogView._bossHpSlider should not be null.");

            float beforeValue = bossHpSlider.value;
            float beforeMaxValue = bossHpSlider.maxValue;

            // 反射で当該の private フィールドに null を入れてから同じ操作をし、
            SetField(_bossDialog, "_bossHpSlider", null);

            float deadline = Time.realtimeSinceStartup + 25f;
            while (!_bossDialog.IsVisible && _driver.Peek(_flow) != RunDriver.Action.DismissBoss && Time.realtimeSinceStartup < deadline)
            {
                _driver.Step(_flow);
                yield return null;
            }

            Assert.IsTrue(_bossDialog.IsVisible || _driver.Peek(_flow) == RunDriver.Action.DismissBoss,
                $"Boss battle dialog did not appear within timeout. {_driver.Describe(_flow)}");
            Assert.IsNotNull(_flow.LastBossBattleResult, "LastBossBattleResult should not be null.");

            // 表示が動かないことを見る
            Assert.AreEqual(beforeValue, bossHpSlider.value, "Boss HP slider value should not change when reference is cleared.");
            Assert.AreEqual(beforeMaxValue, bossHpSlider.maxValue, "Boss HP slider maxValue should not change when reference is cleared.");
        }
    }
}
