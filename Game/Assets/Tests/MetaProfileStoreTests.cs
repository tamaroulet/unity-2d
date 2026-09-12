// SPDX-AI-Disclosure: ai-generated
using System;
using System.Collections.Generic;
using Game.Features.MetaProgression;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class MetaProfileStoreTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(MetaProfileStore.PrefsKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void MetaProfileStore_RoundTrip_PreservesAllProperties()
        {
            var original = new MetaProfileState
            {
                AvailableMetaPoints = 150,
                TotalEarnedMetaPoints = 300,
                TotalRunsCompleted = 4,
                UnlockedIds = new List<int> { 10, 20, 30 }
            };

            MetaProfileStore.Save(original);
            MetaProfileState loaded = MetaProfileStore.Load();

            Assert.IsNotNull(loaded);
            Assert.AreEqual(original.AvailableMetaPoints, loaded.AvailableMetaPoints);
            Assert.AreEqual(original.TotalEarnedMetaPoints, loaded.TotalEarnedMetaPoints);
            Assert.AreEqual(original.TotalRunsCompleted, loaded.TotalRunsCompleted);
            Assert.IsNotNull(loaded.UnlockedIds);
            Assert.AreEqual(3, loaded.UnlockedIds.Count);
            Assert.AreEqual(10, loaded.UnlockedIds[0]);
            Assert.AreEqual(20, loaded.UnlockedIds[1]);
            Assert.AreEqual(30, loaded.UnlockedIds[2]);
        }

        [Test]
        public void MetaProfileStore_CorruptedJsonOrUnknownVersion_ReturnsFreshProfileWithoutException()
        {
            // Case 1: Corrupted JSON
            PlayerPrefs.SetString(MetaProfileStore.PrefsKey, "{ not valid json ... }}");
            PlayerPrefs.Save();

            MetaProfileState fromCorrupt = null;
            Assert.DoesNotThrow(() =>
            {
                fromCorrupt = MetaProfileStore.Load();
            });

            Assert.IsNotNull(fromCorrupt);
            Assert.AreEqual(0, fromCorrupt.AvailableMetaPoints);
            Assert.AreEqual(0, fromCorrupt.TotalEarnedMetaPoints);
            Assert.AreEqual(0, fromCorrupt.TotalRunsCompleted);
            Assert.AreEqual(0, fromCorrupt.UnlockedIds.Count);

            // Case 2: Unknown future version
            string futureVersionJson = "{\"Version\":999,\"AvailableMetaPoints\":9999,\"TotalEarnedMetaPoints\":9999,\"TotalRunsCompleted\":99,\"UnlockedIds\":[1]}";
            PlayerPrefs.SetString(MetaProfileStore.PrefsKey, futureVersionJson);
            PlayerPrefs.Save();

            MetaProfileState fromFuture = null;
            Assert.DoesNotThrow(() =>
            {
                fromFuture = MetaProfileStore.Load();
            });

            Assert.IsNotNull(fromFuture);
            Assert.AreEqual(0, fromFuture.AvailableMetaPoints, "Unknown version must be ignored, returning fresh profile");
            Assert.AreEqual(0, fromFuture.TotalEarnedMetaPoints);
            Assert.AreEqual(0, fromFuture.TotalRunsCompleted);
            Assert.AreEqual(0, fromFuture.UnlockedIds.Count);
        }
    }
}
