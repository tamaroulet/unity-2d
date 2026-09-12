// SPDX-AI-Disclosure: ai-generated
using System.Collections.Generic;
using Game.Core;
using Game.Features.Boss;
using Game.Features.Command;
using Game.Features.Ending;
using Game.Features.Event;
using Game.Features.MetaProgression;
using Game.Features.Relic;
using UnityEngine;

namespace Game.Features.GameFlow
{
    /// <summary>
    /// ターン進行・状態遷移・Resolver と EventChannelSO のオーケストレーションを担う
    /// MonoBehaviour。自身はパラメータの計算・判定を行わず、GameRulesSO,
    /// CommandResolverSO, EventResolverSO, EndingResolverSO, RelicResolverSO,
    /// AutoBattleResolverSO, MetaPointResolverSO および TurnRules への
    /// 処理の委譲のみを行う。
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] private GameRulesSO _gameRules;
        [SerializeField] private CommandResolverSO _commandResolver;
        [SerializeField] private GameEventCatalogSO _eventCatalog;
        [SerializeField] private EventResolverSO _eventResolver;
        [SerializeField] private EndingRulesSO _endingRules;
        [SerializeField] private EndingResolverSO _endingResolver;
        [SerializeField] private RelicCatalogSO _relicCatalog;
        [SerializeField] private RelicResolverSO _relicResolver;
        [SerializeField] private GameStateEventChannelSO _gameStateChannel;
        [SerializeField] private GameEventFiredChannelSO _eventFiredChannel;
        [SerializeField] private EndingDecidedChannelSO _endingDecidedChannel;
        [SerializeField] private RelicAcquiredChannelSO _relicAcquiredChannel;
        [SerializeField] private BossCatalogSO _bossCatalog;
        [SerializeField] private AutoBattleResolverSO _autoBattleResolver;

        /// <summary>
        /// ボスバトルを起動するターン番号のリスト（Act 順）。
        /// リストの i 番目の要素で戦うボスは BossCatalogSO 内の BossId == i + 1 のもの
        /// （例: 既定値 {6, 12, 18, 24} は Act 1〜4 が Boss_Act1_01〜Boss_Act4_01 に対応する）。
        /// </summary>
        [SerializeField] private List<int> _bossBattleTurns = new List<int> { 6, 12, 18, 24 };
        [SerializeField] private MetaPointResolverSO _metaPointResolver;
        [SerializeField] private MetaUnlockCatalogSO _metaUnlockCatalog;

        private GamePhase _currentPhase = GamePhase.Initializing;
        private GameState _currentState;
        private readonly List<RelicSO> _activeRelics = new List<RelicSO>();
        private MetaProfileState _metaProfile = new MetaProfileState();
        private int _bossDefeatedCount = 0;
        private bool _runFinalized;
        [SerializeField] private bool _autoStartOnPlay = true;

        public GamePhase CurrentPhase => _currentPhase;

        public GameState CurrentState => _currentState;

        public IReadOnlyList<RelicSO> ActiveRelics => _activeRelics;

        public MetaProfileState MetaProfile => _metaProfile;

        public int BossDefeatedCount => _bossDefeatedCount;

        /// <summary>
        /// ボス戦発生時にUIへ通知するイベント (Boss, Result, OnDismissCallback)。
        /// </summary>
        public event System.Action<BossSO, FullBattleResult, System.Action> OnBossBattleOccurred;

        /// <summary>
        /// ボス撃破後のレリックドラフト提示イベント (DraftCandidates)。
        /// </summary>
        public event System.Action<IReadOnlyList<RelicSO>> OnRelicDraftRequested;

        /// <summary>メタプロフィールが変化したときの通知（HUD 更新用）。</summary>
        public event System.Action<MetaProfileState> OnMetaProfileChanged;

        /// <summary>周回終了後のショップ提示要求 (Profile, OnShopClosed)。購読者が居なければ即座に次周回へ。</summary>
        public event System.Action<MetaProfileState, System.Action> OnMetaShopRequested;

        public IReadOnlyList<int> BossBattleTurns => _bossBattleTurns;

        private void Awake()
        {
            _metaProfile = MetaProfileStore.Load();
            _bossBattleTurns = new List<int> { 6, 12, 18, 24 };
        }

        private void Start()
        {
            _bossBattleTurns = new List<int> { 6, 12, 18, 24 };
            if (_autoStartOnPlay)
            {
                StartGame();
            }
        }



        private void OnEnable()
        {
            if (_relicAcquiredChannel != null)
            {
                _relicAcquiredChannel.OnEventRaised += OnRelicAcquired;
            }
        }

        private void OnDisable()
        {
            if (_relicAcquiredChannel != null)
            {
                _relicAcquiredChannel.OnEventRaised -= OnRelicAcquired;
            }
        }

        private void NotifyStateChanged()
        {
            _gameStateChannel?.Raise(_currentState);
        }

        public void RequestRestart()
        {
            if (OnMetaShopRequested != null)
            {
                OnMetaShopRequested.Invoke(_metaProfile, StartGame);
                return;
            }
            StartGame();
        }

        /// <summary>
        /// GameRulesSO から初期状態を生成して通知し、最初のターンを開始する。
        /// アンロック済みの初期ステータス底上げがあれば適用する。
        /// </summary>
        public void StartGame()
        {
            _runFinalized = false;
            LastEncounteredBoss = null;
            LastBossBattleResult = null;

            _activeRelics.Clear();
            _bossDefeatedCount = 0;
            _currentPhase = GamePhase.Initializing;

            if (_gameRules == null)
            {
                Debug.LogError("[GameFlowController] _gameRules is not assigned on GameFlowController.");
                return;
            }

            GameState baseState = _gameRules.CreateInitialState();
            _currentState = _metaPointResolver != null && _metaUnlockCatalog != null
                ? _metaPointResolver.ApplyUnlockedStatBonuses(baseState, _metaProfile, _metaUnlockCatalog, _gameRules)
                : baseState;

            NotifyStateChanged();

            Debug.Log($"[GameFlowController] Game Started! Initial State: Turn={_currentState.CurrentTurn}, Stamina={_currentState.Stamina}, Skill={_currentState.Skill}, Mental={_currentState.Mental}");

            OnMetaProfileChanged?.Invoke(_metaProfile);

            BeginTurn();
        }

        /// <summary>
        /// コマンド選択時に呼ばれる。WaitingInput 中でなければ何もしない。
        /// 所持レリックによる効果修飾を適用したうえで CommandResolverSO へ委譲する。
        /// </summary>
        public void ExecuteCommand(CommandDataSO command)
        {
            if (_currentPhase != GamePhase.WaitingInput)
            {
                Debug.LogWarning($"[GameFlowController] Cannot execute command {command?.name}: Not in WaitingInput phase (Current phase: {_currentPhase})");
                return;
            }

            if (_commandResolver == null || _gameRules == null)
            {
                Debug.LogError("[GameFlowController] _commandResolver or _gameRules is missing!");
                return;
            }

            _currentPhase = GamePhase.ExecutingCommand;

            CommandEffect effectiveEffect = _relicResolver != null
                ? _relicResolver.ModifyCommandEffect(command.Effect, _activeRelics)
                : command.Effect;

            CommandResult result = _commandResolver.Resolve(_currentState, effectiveEffect, _gameRules);

            if (!result.IsExecutable)
            {
                Debug.LogWarning($"[GameFlowController] Command {command?.name} not executable with current stamina {_currentState.Stamina} (Cost: {effectiveEffect.StaminaCost})");
                _currentPhase = GamePhase.WaitingInput;
                return;
            }

            _currentState = result.State;
            NotifyStateChanged();

            if (_currentPhase != GamePhase.TurnEnd)
            {
                _currentPhase = GamePhase.TurnEnd;
            }

            AdvanceTurn();
        }

        /// <summary>
        /// ターン終了時パッシブ効果を適用し、TurnRules の終了判定に従って遷移先を決定する。
        /// </summary>
        public void AdvanceTurn()
        {
            if (_relicResolver != null && _activeRelics.Count > 0)
            {
                _currentState = _relicResolver.ApplyTurnEndRelics(_currentState, _activeRelics, _gameRules);
                NotifyStateChanged();
            }

            TerminationKind termination = TurnRules.EvaluateTermination(_currentState, _gameRules.MaxTurn);

            switch (termination)
            {
                case TerminationKind.GameOver:
                    _endingDecidedChannel?.Raise(EndingKind.Defeat);
                    _currentPhase = GamePhase.GameOver;
                    FinalizeRun(isGameClear: false);
                    break;
                case TerminationKind.NormalEnd:
                    EndingKind ending = _endingResolver != null && _endingRules != null
                        ? _endingResolver.Resolve(_currentState, _endingRules)
                        : EndingKind.Stamina;
                    _endingDecidedChannel?.Raise(ending);
                    _currentPhase = GamePhase.GameClear;
                    FinalizeRun(isGameClear: true);
                    break;
                default:
                    BeginTurn();
                    break;
            }
        }

        /// <summary>
        /// ターン開始時にパッシブ効果適用およびイベント発生を判定する。
        /// </summary>
        private void BeginTurn()
        {
            _currentPhase = GamePhase.TurnStart;

            if (_relicResolver != null && _activeRelics.Count > 0)
            {
                _currentState = _relicResolver.ApplyTurnStartRelics(_currentState, _activeRelics, _gameRules);
                NotifyStateChanged();
            }

            int actIndex = _autoBattleResolver != null && _bossCatalog != null && _bossBattleTurns != null && _currentState.CurrentTurn >= 6
                ? _bossBattleTurns.IndexOf(_currentState.CurrentTurn)
                : -1;

            if (actIndex >= 0)
            {
                int bossId = actIndex + 1;
                BossSO boss = _bossCatalog.FindById(bossId);
                if (boss != null && _autoBattleResolver != null)
                {
                    _currentPhase = GamePhase.BossBattle;
                    FullBattleResult battleResult = _autoBattleResolver.ResolveFullBattle(
                        _currentState, boss, _activeRelics, _gameRules);

                    _currentState = battleResult.FinalPlayerState;
                    NotifyStateChanged();

                    LastEncounteredBoss = boss;
                    LastBossBattleResult = battleResult;

                    if (battleResult.Outcome == BattleOutcomeKind.Victory)
                    {
                        _bossDefeatedCount++;
                        bool isFinalBoss = (actIndex >= _bossBattleTurns.Count - 1);

                        if (isFinalBoss)
                        {
                            _currentPhase = GamePhase.GameClear;
                            System.Action triggerEnding = () =>
                            {
                                EndingKind ending = _endingResolver != null && _endingRules != null
                                    ? _endingResolver.Resolve(_currentState, _endingRules)
                                    : EndingKind.Stamina;
                                _endingDecidedChannel?.Raise(ending);
                                FinalizeRun(isGameClear: true);
                            };

                            if (OnBossBattleOccurred != null)
                            {
                                OnBossBattleOccurred.Invoke(boss, battleResult, triggerEnding);
                            }
                            else
                            {
                                triggerEnding();
                            }
                            return;
                        }

                        // Act 1〜3 ボス勝利: レリックドラフト画面へ
                        _currentPhase = GamePhase.ShowingRelicDraft;
                        System.Action proceedToDraft = () =>
                        {
                            List<RelicSO> draftOptions = GetDraftCandidates();
                            if (OnRelicDraftRequested != null)
                            {
                                OnRelicDraftRequested.Invoke(draftOptions);
                            }
                            else
                            {
                                // UI未バインド（テスト実行時など）の自動フォールバック
                                int fallbackRelicId = draftOptions.Count > 0 ? draftOptions[0].RelicId : bossId;
                                OnRelicAcquired(fallbackRelicId);
                            }
                        };

                        if (OnBossBattleOccurred != null)
                        {
                            OnBossBattleOccurred.Invoke(boss, battleResult, proceedToDraft);
                        }
                        else
                        {
                            proceedToDraft();
                        }
                        return;
                    }
                    else
                    {
                        _currentPhase = GamePhase.GameOver;
                        if (OnBossBattleOccurred != null)
                        {
                            OnBossBattleOccurred.Invoke(boss, battleResult, () =>
                            {
                                _endingDecidedChannel?.Raise(EndingKind.Defeat);
                                FinalizeRun(isGameClear: false);
                            });
                        }
                        else
                        {
                            _endingDecidedChannel?.Raise(EndingKind.Defeat);
                            FinalizeRun(isGameClear: false);
                        }
                        return;
                    }
                }
            }

            if (_eventResolver != null && _eventCatalog != null)
            {
                EventResult eventResult = _eventResolver.Resolve(_currentState, _eventCatalog);

                if (eventResult.HasFired)
                {
                    _currentState = eventResult.State;
                    _currentPhase = GamePhase.ShowingEvent;
                    NotifyStateChanged();
                    _eventFiredChannel?.Raise(eventResult.FiredEvent.EventId);
                    return;
                }
            }

            _currentPhase = GamePhase.WaitingInput;
        }

        /// <summary>
        /// 直近のボス戦情報（UI表示用）。
        /// </summary>
        public BossSO LastEncounteredBoss { get; private set; }
        public FullBattleResult LastBossBattleResult { get; private set; }

        private void FinalizeRun(bool isGameClear)
        {
            if (_runFinalized) return;

            if (_metaPointResolver != null)
            {
                int earnedPoints = _metaPointResolver.CalculateEarnedPoints(_currentState, isGameClear, _bossDefeatedCount);
                _metaProfile = _metaPointResolver.ApplyRunResult(_metaProfile, earnedPoints);
                MetaProfileStore.Save(_metaProfile);
            }

            _runFinalized = true;
            OnMetaProfileChanged?.Invoke(_metaProfile);
        }

        /// <summary>
        /// アンロックの購入を試みる。成功時はプロフィールを更新して true を返す。
        /// </summary>
        public bool TryPurchaseMetaUnlock(MetaUnlockSO unlock)
        {
            if (_metaPointResolver == null || unlock == null)
            {
                return false;
            }

            (MetaProfileState newProfile, bool success) = _metaPointResolver.ApplyUnlock(_metaProfile, unlock);
            if (success)
            {
                _metaProfile = newProfile;
                MetaProfileStore.Save(_metaProfile);
                OnMetaProfileChanged?.Invoke(_metaProfile);
            }
            return success;
        }

        /// <summary>
        /// イベントダイアログが閉じられたときに呼ばれる。
        /// </summary>
        public void OnEventDismissed()
        {
            _currentPhase = GamePhase.WaitingInput;
        }

        /// <summary>
        /// レリック獲得チャンネルから呼ばれるハンドラ。所持レリック一覧を更新して入力待ちへ復帰する。
        /// </summary>
        public void OnRelicAcquired(int relicId)
        {
            if (_relicCatalog != null)
            {
                RelicSO relic = _relicCatalog.FindById(relicId);
                if (relic != null && !_activeRelics.Contains(relic))
                {
                    _activeRelics.Add(relic);
                }
            }

            List<int> newIds = new List<int>(_currentState.AcquiredRelicIds);
            if (!newIds.Contains(relicId))
            {
                newIds.Add(relicId);
            }

            _currentState = _currentState with { AcquiredRelicIds = newIds };
            _gameStateChannel?.Raise(_currentState);
            _currentPhase = GamePhase.WaitingInput;
        }

        /// <summary>
        /// カタログから現在未所持のレリックを最大3件抽出する。
        /// </summary>
        public List<RelicSO> GetDraftCandidates()
        {
            List<RelicSO> candidates = new List<RelicSO>();
            if (_relicCatalog != null && _relicCatalog.Relics != null)
            {
                foreach (RelicSO relic in _relicCatalog.Relics)
                {
                    if (relic == null) continue;

                    bool alreadyAcquired = false;
                    if (_currentState != null && _currentState.AcquiredRelicIds != null)
                    {
                        for (int i = 0; i < _currentState.AcquiredRelicIds.Count; i++)
                        {
                            if (_currentState.AcquiredRelicIds[i] == relic.RelicId)
                            {
                                alreadyAcquired = true;
                                break;
                            }
                        }
                    }

                    if (!alreadyAcquired)
                    {
                        candidates.Add(relic);
                        if (candidates.Count >= 3) break;
                    }
                }
            }
            return candidates;
        }
    }
}
