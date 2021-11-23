// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Docs.Build;

[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "other code")]
[SuppressMessage("Layout", "MEN002:Line is too long", Justification = "other code")]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:Element should begin with upper-case letter", Justification = "other code")]
[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "other code")]
internal static class LibGit2
{
    private const string LibName = "git2-6777db8";

    static LibGit2()
    {
        if (git_libgit2_init() == 1)
        {
            git_openssl_set_locking();
        }

        // Disable hash verification to drastically speed up object lookup.
        git_libgit2_opts(22 /*GIT_OPT_ENABLE_STRICT_HASH_VERIFICATION*/, 0);
    }

    [DllImport(LibName)]
    public static extern unsafe void git_buf_free(git_buf* buffer);

    [DllImport(LibName)]
    public static extern unsafe int git_blob_lookup(out IntPtr blob, IntPtr repo, git_oid* obj);

    [DllImport(LibName)]
    public static extern unsafe IntPtr git_blob_rawcontent(IntPtr blob);

    [DllImport(LibName)]
    public static extern unsafe int git_blob_rawsize(IntPtr blob);

    [DllImport(LibName)]
    public static extern unsafe int git_libgit2_init();

    [DllImport(LibName)]
    public static extern unsafe int git_openssl_set_locking();

    [DllImport(LibName)]
    public static extern unsafe int git_libgit2_opts(int opt, int enabled);

    [DllImport(LibName)]
    public static extern unsafe int git_oid_fmt(sbyte* str, git_oid* oid);

    [DllImport(LibName)]
    public static extern unsafe int git_repository_init(out IntPtr repo, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int is_bare);

    [DllImport(LibName)]
    public static extern unsafe int git_repository_open(out IntPtr repo, [MarshalAs(UnmanagedType.LPUTF8Str)] string path);

    [DllImport(LibName)]
    public static extern unsafe int git_repository_head(out IntPtr reference, IntPtr repo);

    [DllImport(LibName)]
    public static extern unsafe void git_repository_free(IntPtr repo);

    [DllImport(LibName)]
    public static extern unsafe int git_remote_create(out IntPtr remote, IntPtr repo, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, [MarshalAs(UnmanagedType.LPUTF8Str)] string url);

    [DllImport(LibName)]
    public static extern unsafe int git_remote_lookup(out IntPtr remote, IntPtr repo, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(LibName)]
    public static extern unsafe int git_remote_lookup(out IntPtr remote, IntPtr repo, IntPtr name);

    [DllImport(LibName)]
    public static extern unsafe int git_remote_list(git_strarray* remotes, IntPtr repo);

    [DllImport(LibName)]
    public static extern unsafe IntPtr git_remote_url(IntPtr remote);

    [DllImport(LibName)]
    public static extern unsafe void git_remote_free(IntPtr remote);

    [DllImport(LibName)]
    public static extern unsafe int git_branch_lookup(out IntPtr reference, IntPtr repo, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, int type);

    [DllImport(LibName)]
    public static extern unsafe int git_branch_name(out IntPtr name, IntPtr reference);

    [DllImport(LibName)]
    public static extern unsafe int git_branch_upstream(out IntPtr reference, IntPtr branch);

    [DllImport(LibName)]
    public static extern unsafe int git_branch_remote_name(git_buf* buffer, IntPtr repo, IntPtr refname);

    [DllImport(LibName)]
    public static extern unsafe IntPtr git_reference_name(IntPtr reference);

    [DllImport(LibName)]
    public static extern unsafe git_oid* git_reference_target(IntPtr reference);

    [DllImport(LibName)]
    public static extern unsafe void git_reference_free(IntPtr reference);

    [DllImport(LibName)]
    public static extern unsafe int git_revparse_single(out IntPtr @out, IntPtr repo, [MarshalAs(UnmanagedType.LPUTF8Str)] string spec);

    [DllImport(LibName)]
    public static extern unsafe int git_object_lookup(out IntPtr obj, IntPtr repo, git_oid* id, int type);

    [DllImport(LibName)]
    public static extern unsafe git_oid* git_object_id(IntPtr obj);

    [DllImport(LibName)]
    public static extern unsafe void git_object_free(IntPtr obj);

    [DllImport(LibName)]
    public static extern unsafe IntPtr git_commit_message(IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe long git_commit_time(IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe int git_commit_time_offset(IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe git_signature* git_commit_author(IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe git_signature* git_commit_committer(IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe int git_commit_tree(out IntPtr tree, IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe void git_signature_free(git_signature* sig);

    [DllImport(LibName)]
    public static extern unsafe void git_strarray_free(git_strarray* strarray);

    [DllImport(LibName)]
    public static extern unsafe git_oid* git_commit_parent_id(IntPtr commit, int n);

    [DllImport(LibName)]
    public static extern unsafe uint git_commit_parentcount(IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe git_oid* git_commit_tree_id(IntPtr commit);

    [DllImport(LibName)]
    public static extern unsafe int git_tree_walk(IntPtr tree, int mode, IntPtr callback, void* payload);

    [DllImport(LibName)]
    public static extern unsafe IntPtr git_tree_entrycount(IntPtr tree);

    [DllImport(LibName)]
    public static extern unsafe IntPtr git_tree_entry_byindex(IntPtr tree, IntPtr i);

    [DllImport(LibName)]
    public static extern unsafe int git_tree_entry_bypath(out IntPtr tree, IntPtr root, [MarshalAs(UnmanagedType.LPUTF8Str)] string treeentry_path);

    [DllImport(LibName)]
    public static extern unsafe void git_tree_entry_free(IntPtr treeEntry);

    [DllImport(LibName)]
    public static extern unsafe git_oid* git_tree_entry_id(IntPtr entry);

    [DllImport(LibName)]
    public static extern unsafe byte* git_tree_entry_name(IntPtr entry);

    [DllImport(LibName)]
    public static extern unsafe int git_tree_entry_type(IntPtr entry);

    [DllImport(LibName)]
    public static extern unsafe int git_revwalk_new(out IntPtr walk, IntPtr repo);

    [DllImport(LibName)]
    public static extern unsafe int git_revwalk_push(IntPtr walk, git_oid* id);

    [DllImport(LibName)]
    public static extern unsafe int git_revwalk_push_head(IntPtr walk);

    [DllImport(LibName)]
    public static extern unsafe int git_revwalk_push_glob(IntPtr walk, [MarshalAs(UnmanagedType.LPUTF8Str)] string glob);

    [DllImport(LibName)]
    public static extern unsafe int git_revwalk_next(out git_oid oid, IntPtr walk);

    [DllImport(LibName)]
    public static extern unsafe void git_revwalk_sorting(IntPtr walk, int sort);

    [DllImport(LibName)]
    public static extern unsafe void git_revwalk_free(IntPtr walk);

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct git_buf
    {
        public IntPtr ptr;
        public int asize;
        public int size;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct git_strarray
    {
        public IntPtr* strings;
        public int count;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct git_time
    {
        private static readonly DateTimeOffset s_epoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public long time;
        public int offset;

        public DateTimeOffset ToDateTimeOffset()
        {
            var utcDateTime = s_epoch.AddSeconds(time);
            var timezone = TimeSpan.FromMinutes(offset);
            return new DateTimeOffset(utcDateTime.DateTime.Add(timezone), timezone);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public unsafe struct git_signature
    {
        public IntPtr name;
        public IntPtr email;
        public git_time when;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct git_oid
    {
        public long a;
        public long b;
        public int c;

        public override unsafe string ToString()
        {
            fixed (git_oid* p = &this)
            {
                var str = stackalloc sbyte[40];
                git_oid_fmt(str, p);
                return new string(str, 0, 40);
            }
        }
    }
}
