// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Docs.Build
{
    internal static class NativeMethods
    {
        private const string LibName = "git2-b0d9952";
        private static readonly DateTimeOffset s_epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        static NativeMethods()
        {
            if (GitLibGit2Init() == 1)
                GitOpensslSetLocking();

            // Disable hash verification to drastically speed up object lookup.
            GitLibgit2Opts(22 /*GIT_OPT_ENABLE_STRICT_HASH_VERIFICATION*/, 0);
        }

        public enum GitObjectType
        {
            Any = -2,
            Bad,
            Ext1,
            Commit,
            Tree,
            Blob,
            Tag,
            Ext2,
            OfsDelta,
            RefDelta,
        }

        public static byte[] ToUtf8Native(string str)
        {
            var buf = new byte[Encoding.UTF8.GetByteCount(str) + 1];
            Encoding.UTF8.GetBytes(str, 0, str.Length, buf, 0);
            return buf;
        }

        public static unsafe string FromUtf8Native(byte* str)
        {
            var n = 0;
            var p = str;
            while (*p++ != 0)
            {
                n++;
            }

            return Encoding.UTF8.GetString(str, n);
        }

        public static DateTimeOffset ToDateTimeOffset(long time, int offset)
        {
            DateTimeOffset utcDateTime = s_epoch.AddSeconds(time);
            TimeSpan timezone = TimeSpan.FromMinutes(offset);
            return new DateTimeOffset(utcDateTime.DateTime.Add(timezone), timezone);
        }

        [DllImport(LibName, EntryPoint = "git_libgit2_init")]
        public static unsafe extern int GitLibGit2Init();

        [DllImport(LibName, EntryPoint = "git_openssl_set_locking")]
        public static unsafe extern int GitOpensslSetLocking();

        [DllImport(LibName, EntryPoint = "git_libgit2_opts")]
        public static unsafe extern int GitLibgit2Opts(int opt, int enabled);

        [DllImport(LibName, EntryPoint = "git_oid_fmt")]
        public static unsafe extern int GitOidFmt(sbyte* str, GitOid* oid);

        [DllImport(LibName, EntryPoint = "git_repository_open")]
        public static unsafe extern int GitRepositoryOpen(out IntPtr repo, byte* path);

        [DllImport(LibName, EntryPoint = "git_repository_head")]
        public static unsafe extern int GitRepositoryHead(out IntPtr reference, IntPtr repo);

        [DllImport(LibName, EntryPoint = "git_repository_free")]
        public static unsafe extern void GitRepositoryFree(IntPtr repo);

        [DllImport(LibName, EntryPoint = "git_remote_lookup")]
        public static unsafe extern int GitRemoteLookup(out IntPtr remote, IntPtr repo, byte* name);

        [DllImport(LibName, EntryPoint = "git_remote_url")]
        public static unsafe extern byte* GitRemoteUrl(IntPtr remote);

        [DllImport(LibName, EntryPoint = "git_remote_free")]
        public static unsafe extern void GitRemoteFree(IntPtr remote);

        [DllImport(LibName, EntryPoint = "git_branch_lookup")]
        public static unsafe extern int GitBranchLookup(out IntPtr reference, IntPtr repo, byte* name, int type);

        [DllImport(LibName, EntryPoint = "git_branch_name")]
        public static unsafe extern int GitBranchName(out byte* name, IntPtr reference);

        [DllImport(LibName, EntryPoint = "git_reference_target")]
        public static unsafe extern GitOid* GitReferenceTarget(IntPtr reference);

        [DllImport(LibName, EntryPoint = "git_reference_free")]
        public static unsafe extern void GitReferenceFree(IntPtr reference);

        [DllImport(LibName, EntryPoint = "git_object_lookup")]
        public static unsafe extern int GitObjectLookup(out IntPtr obj, IntPtr repo, GitOid* id, GitObjectType type);

        [DllImport(LibName, EntryPoint = "git_object_free")]
        public static unsafe extern void GitObjectFree(IntPtr obj);

        [DllImport(LibName, EntryPoint = "git_commit_message")]
        public static unsafe extern byte* GitCommitMessage(IntPtr commit);

        [DllImport(LibName, EntryPoint = "git_commit_time")]
        public static unsafe extern long GitCommitTime(IntPtr commit);

        [DllImport(LibName, EntryPoint = "git_commit_time_offset")]
        public static unsafe extern int GitCommitTimeOffset(IntPtr commit);

        [DllImport(LibName, EntryPoint = "git_commit_author")]
        public static unsafe extern GitSignature* GitCommitAuthor(IntPtr commit);

        [DllImport(LibName, EntryPoint = "git_commit_committer")]
        public static unsafe extern GitSignature* GitCommitCommitter(IntPtr commit);

        [DllImport(LibName, EntryPoint = "git_signature_free")]
        public static unsafe extern void GitSignatureFree(GitSignature* sig);

        [DllImport(LibName, EntryPoint = "git_commit_parent_id")]
        public static unsafe extern GitOid* GitCommitParentId(IntPtr commit, int n);

        [DllImport(LibName, EntryPoint = "git_commit_parentcount")]
        public static unsafe extern uint GitCommitParentcount(IntPtr commit);

        [DllImport(LibName, EntryPoint = "git_commit_tree_id")]
        public static unsafe extern GitOid* GitCommitTreeId(IntPtr commit);

        [DllImport(LibName, EntryPoint = "git_tree_walk")]
        public static unsafe extern int GitTreeWalk(IntPtr tree, int mode, IntPtr callback, void* payload);

        [DllImport(LibName, EntryPoint = "git_tree_entrycount")]
        public static unsafe extern IntPtr GitTreeEntrycount(IntPtr tree);

        [DllImport(LibName, EntryPoint = "git_tree_entry_byindex")]
        public static unsafe extern IntPtr GitTreeEntryByindex(IntPtr tree, IntPtr i);

        [DllImport(LibName, EntryPoint = "git_tree_entry_bypath")]
        public static unsafe extern int GitTreeEntryBypath(out IntPtr tree, IntPtr root, byte* treeentry_path);

        [DllImport(LibName, EntryPoint = "git_tree_entry_free")]
        public static unsafe extern void GitTreeEntryFree(IntPtr treeEntry);

        [DllImport(LibName, EntryPoint = "git_tree_entry_id")]
        public static unsafe extern GitOid* GitTreeEntryId(IntPtr entry);

        [DllImport(LibName, EntryPoint = "git_tree_entry_name")]
        public static unsafe extern byte* GitTreeEntryName(IntPtr entry);

        [DllImport(LibName, EntryPoint = "git_tree_entry_type")]
        public static unsafe extern int GitTreeEntryType(IntPtr entry);

        [DllImport(LibName, EntryPoint = "git_revwalk_new")]
        public static unsafe extern int GitRevwalkNew(out IntPtr walk, IntPtr repo);

        [DllImport(LibName, EntryPoint = "git_revwalk_push")]
        public static unsafe extern int GitRevwalkPush(IntPtr walk, GitOid* id);

        [DllImport(LibName, EntryPoint = "git_revwalk_push_head")]
        public static unsafe extern int GitRevwalkPushHead(IntPtr walk);

        [DllImport(LibName, EntryPoint = "git_revwalk_push_glob")]
        public static unsafe extern int GitRevwalkPushGlob(IntPtr walk, byte* glob);

        [DllImport(LibName, EntryPoint = "git_revwalk_next")]
        public static unsafe extern int GitRevwalkNext(out GitOid oid, IntPtr walk);

        [DllImport(LibName, EntryPoint = "git_revwalk_sorting")]
        public static unsafe extern void GitRevwalkSorting(IntPtr walk, int sort);

        [DllImport(LibName, EntryPoint = "git_revwalk_free")]
        public static unsafe extern void GitRevwalkFree(IntPtr walk);

        [StructLayout(LayoutKind.Sequential)]
        public struct GitTime
        {
            public long Time;
            public int Offset;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct GitSignature
        {
            public byte* Name;
            public byte* Email;
            public GitTime When;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GitOid : IEquatable<GitOid>
        {
            public const int Size = 20;

            public long A;
            public long B;
            public int C;

            public static bool Equals(ref GitOid a, ref GitOid b) => a.A == b.A && a.B == b.B && a.C == b.C;

            public bool Equals(GitOid other) => A == other.A && B == other.B && C == other.C;

            public override bool Equals(object obj) => obj is GitOid && Equals((GitOid)obj);

            public unsafe override string ToString()
            {
                fixed (GitOid* p = &this)
                {
                    sbyte* str = stackalloc sbyte[40];
                    GitOidFmt(str, p);
                    return new string(str, 0, 40);
                }
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }
    }
}
