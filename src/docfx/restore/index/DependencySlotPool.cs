// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Docs.Build
{
    internal static class DependencySlotPool<T> where T : DependencySlot, new()
    {
        private static int _defaultLockdownTimeInSecond = 10 * 60;

        public static (string path, T slot) TryGetSlot(string remote, Func<IReadOnlyList<T>, IReadOnlyList<T>> getOrderedFilteredSlots)
        {
            Debug.Assert(!string.IsNullOrEmpty(remote));
            Debug.Assert(getOrderedFilteredSlots != null);

            var restoreDir = AppData.GetGitDir(remote);

            string path = null;
            T slot = null;
            ProcessUtility.RunInsideMutex(
                remote + "/index.json",
                () =>
                {
                    var slots = GetSlots(restoreDir);

                    foreach (var i in getOrderedFilteredSlots(slots))
                    {
                        if (i.Restored /*restored successfully*/ &&
                        !ProcessUtility.IsExclusiveLockHeld(GetLockKey(remote, i.Id)) /*not being used for restoring*/)
                        {
                            slot = i;
                            break;
                        }
                    }

                    if (slot != null)
                    {
                        slot.LastAccessDate = DateTime.UtcNow;
                        path = $"{slot.Id}";
                    }

                    WriteSlots(restoreDir, slots.ToList());
                });

            return (path, slot);
        }

        public static bool ReleaseSlot(T slot, LockType lockType, bool successed = true)
        {
            Debug.Assert(slot != null);
            Debug.Assert(!string.IsNullOrEmpty(slot.Url));
            Debug.Assert(!string.IsNullOrEmpty(slot.Acquirer));

            var url = slot.Url;
            var restoreDir = AppData.GetGitDir(url);

            var released = false;
            ProcessUtility.RunInsideMutex(
                url + "/index.json",
                () =>
                {
                    var slots = GetSlots(restoreDir);
                    var slotToRelease = slots.Single(i => i.Id == slot.Id);

                    Debug.Assert(slotToRelease != null);

                    switch (lockType)
                    {
                        case LockType.Exclusive:
                            slotToRelease.LastAccessDate = DateTime.UtcNow;
                            slotToRelease.Restored = successed;
                            released = ProcessUtility.ReleaseExclusiveLock(GetLockKey(url, slot.Id), slot.Acquirer);
                            break;
                        case LockType.Shared:
                            released = ProcessUtility.ReleaseSharedLock(GetLockKey(url, slot.Id), slot.Acquirer);
                            break;
                    }

                    WriteSlots(restoreDir, slots);
                });

            Debug.Assert(released);

            return released;
        }

        public static (string path, T slot) AcquireSlot(string url, LockType type, Func<T, T> updateExistingSlot, Func<T, bool> matchExistingSlot)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            var restoreDir = AppData.GetGitDir(url);

            T slot = null;
            bool acquired = false;
            string acquirer = null;
            ProcessUtility.RunInsideMutex(
                url + "/index.json",
                () =>
                {
                    var slots = GetSlots(restoreDir);

                    switch (type)
                    {
                        case LockType.Exclusive: // find an available slot or create a new slot for restoring
                            var existed = false;
                            foreach (var i in slots)
                            {
                                if (DateTime.UtcNow - i.LastAccessDate > TimeSpan.FromSeconds(_defaultLockdownTimeInSecond))
                                {
                                    (acquired, acquirer) = ProcessUtility.AcquireExclusiveLock(GetLockKey(url, i.Id));
                                    if (acquired)
                                    {
                                        existed = true;
                                        slot = i;
                                        break;
                                    }
                                }
                            }

                            if (slot is null)
                            {
                                (acquired, acquirer) = ProcessUtility.AcquireExclusiveLock(GetLockKey(url, $"{slots.Count + 1}"));
                                if (acquired)
                                {
                                    slot = new T() { Id = $"{slots.Count + 1}" };
                                }
                            }

                            Debug.Assert(slot != null && acquired && !string.IsNullOrEmpty(acquirer));

                            // reset every property of rented slot
                            slot.Url = url;
                            slot.Restored = false;
                            slot.LastAccessDate = DateTime.MinValue;
                            slot.Acquirer = acquirer;

                            slot = updateExistingSlot(slot);
                            if (!existed)
                                slots.Add(slot);
                            break;
                        case LockType.Shared: // find an matched slot for building
                            foreach (var i in slots)
                            {
                                if (matchExistingSlot(i) && i.Restored)
                                {
                                    (acquired, acquirer) = ProcessUtility.AcquireSharedLock(GetLockKey(url, i.Id));
                                    if (acquired)
                                    {
                                        slot = i;
                                        slot.Url = url;
                                        slot.Acquirer = acquirer;
                                        break;
                                    }
                                }
                            }
                            break;
                        default:
                            throw new NotSupportedException($"{type} is not supported");
                    }

                    WriteSlots(restoreDir, slots);
                });

            if (slot != null)
            {
                Debug.Assert(!string.IsNullOrEmpty(slot.Acquirer));
            }

            return slot is null ? default : ($"{slot.Id}", slot);
        }

        public static List<T> GetSlots(string restoreDir)
        {
            Debug.Assert(!string.IsNullOrEmpty(restoreDir));

            var slotsFile = Path.Combine(restoreDir, "index.json");
            var content = File.Exists(slotsFile) ? File.ReadAllText(slotsFile) : string.Empty;

            return (JsonUtility.Deserialize<SlotInfo>(content) ?? new SlotInfo()).Slots;
        }

        public static void WriteSlots(string restoreDir, List<T> slots)
        {
            Debug.Assert(!string.IsNullOrEmpty(restoreDir));
            Debug.Assert(slots != null);

            Directory.CreateDirectory(restoreDir);

            var slotInfo = new SlotInfo { Slots = slots };
            var slotsFile = Path.Combine(restoreDir, "index.json");
            File.WriteAllText(slotsFile, JsonUtility.Serialize(slotInfo));
        }

        private static string GetLockKey(string remote, string id) => $"{remote}/{id}";

        // in case of any extened requirements
        private class SlotInfo
        {
            public List<T> Slots { get; set; } = new List<T>();
        }
    }

    public enum LockType
    {
        Shared,
        Exclusive,
    }
}
