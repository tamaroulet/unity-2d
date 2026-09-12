// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Features.MetaProgression
{
    /// <summary>
    /// PlayerPrefs + JsonUtility を用いて MetaProfileState を永続化・復元する静的ストア。
    /// WebGL 環境での IndexedDB 同期を保証するため、保存時は必ず PlayerPrefs.Save() を呼ぶ。
    /// 読み込み失敗時や未知のバージョン時は例外を投げず初期状態の MetaProfileState を返す。
    /// </summary>
    public static class MetaProfileStore
    {
        public const string PrefsKey = "Game.MetaProfile";
        public const int CurrentVersion = 1;

        /// <summary>
        /// PlayerPrefs から MetaProfileState を復元する。
        /// キーが存在しない、JSON 解析失敗、またはバージョン不一致の場合は新規 MetaProfileState を返す。
        /// </summary>
        public static MetaProfileState Load()
        {
            if (!PlayerPrefs.HasKey(PrefsKey))
            {
                return new MetaProfileState();
            }

            string json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return new MetaProfileState();
            }

            try
            {
                MetaProfileDto dto = JsonUtility.FromJson<MetaProfileDto>(json);
                if (dto == null || dto.Version != CurrentVersion)
                {
                    return new MetaProfileState();
                }

                return new MetaProfileState
                {
                    AvailableMetaPoints = dto.AvailableMetaPoints,
                    TotalEarnedMetaPoints = dto.TotalEarnedMetaPoints,
                    TotalRunsCompleted = dto.TotalRunsCompleted,
                    UnlockedIds = dto.UnlockedIds != null
                        ? (IReadOnlyList<int>)dto.UnlockedIds
                        : Array.Empty<int>()
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MetaProfileStore] Failed to parse profile JSON. Returning fresh profile. Error: {ex.Message}");
                return new MetaProfileState();
            }
        }

        /// <summary>
        /// MetaProfileState を JSON 化して PlayerPrefs に永続化する。
        /// </summary>
        public static void Save(MetaProfileState profile)
        {
            if (profile == null)
            {
                profile = new MetaProfileState();
            }

            int[] unlockedArray = profile.UnlockedIds != null
                ? new List<int>(profile.UnlockedIds).ToArray()
                : Array.Empty<int>();

            MetaProfileDto dto = new MetaProfileDto
            {
                Version = CurrentVersion,
                AvailableMetaPoints = profile.AvailableMetaPoints,
                TotalEarnedMetaPoints = profile.TotalEarnedMetaPoints,
                TotalRunsCompleted = profile.TotalRunsCompleted,
                UnlockedIds = unlockedArray
            };

            string json = JsonUtility.ToJson(dto);
            PlayerPrefs.SetString(PrefsKey, json);
            PlayerPrefs.Save();
        }
    }
}
