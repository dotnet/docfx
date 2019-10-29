// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// relative path
    /// </summary>
    public sealed class RelativePath : IEquatable<RelativePath>
    {

        #region Consts/Fields
        private const string ParentDirectory = "../";
        public const char WorkingFolderChar = '~';
        public const string WorkingFolderString = "~";
        public static readonly string NormalizedWorkingFolder = "~/";
        public static readonly string AltWorkingFolder = "~\\";
        public static readonly RelativePath Empty = new RelativePath(false, 0, new string[] { string.Empty });
        public static readonly RelativePath WorkingFolder = new RelativePath(true, 0, new string[] { string.Empty });
        public static readonly char[] InvalidPartChars = PathUtility.InvalidPathChars.Concat(@"\/?").ToArray();
        private static readonly string[] EncodedInvalidPartChars = Array.ConvertAll(InvalidPartChars, ch => Uri.EscapeDataString(ch.ToString()));
        private static readonly char[] UnsafeInvalidPartChars = { '/' };
        private static readonly string[] EncodedUnsafeInvalidPartChars = Array.ConvertAll(UnsafeInvalidPartChars, ch => Uri.EscapeDataString(ch.ToString()));

        private readonly bool _isFromWorkingFolder;
        private readonly int _parentDirectoryCount;
        private readonly string[] _parts;
        #endregion

        #region Constructor

        private RelativePath(bool isFromWorkingFolder, int parentDirectoryCount, string[] parts)
        {
            _isFromWorkingFolder = isFromWorkingFolder;
            _parentDirectoryCount = parentDirectoryCount;
            _parts = parts;
        }

        #endregion

        #region Public Members
        public static RelativePath FromUrl(string path)
        {
            return Parse(path).UrlDecode();
        }

        public static bool IsRelativePath(string path)
        {
            // TODO : to merge with the PathUtility one
            return path != null &&
                path.Length > 0 &&
                path[0] != '/' &&
                path[0] != '\\' &&
                path.IndexOfAny(PathUtility.InvalidPathChars) == -1;
        }

        public static RelativePath Parse(string path) => TryParseCore(path, true);

        public static RelativePath TryParse(string path) => TryParseCore(path, false);

        public static bool IsPathFromWorkingFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return path.StartsWith(NormalizedWorkingFolder, StringComparison.Ordinal)
                || path.StartsWith(AltWorkingFolder, StringComparison.Ordinal);
        }

        public static string GetPathWithoutWorkingFolderChar(string path)
        {
            TryGetPathWithoutWorkingFolderChar(path, out string pathWithoutWorkingDirectory);
            return pathWithoutWorkingDirectory;
        }

        public static bool TryGetPathWithoutWorkingFolderChar(string path, out string pathFromWorkingFolder)
        {
            if (IsPathFromWorkingFolder(path))
            {
                pathFromWorkingFolder = path.Substring(2);
                return true;
            }
            pathFromWorkingFolder = path;
            return false;
        }

        public int ParentDirectoryCount => _parentDirectoryCount;

        public int SubdirectoryCount => _parts.Length - 1;

        public bool IsEmpty => ReferenceEquals(this, Empty);

        /// <summary>
        /// Concat two relative path
        /// e.g.:
        ///     {d/e.txt}.BasedOn({a/b/c/}) = {a/b/c/d/e.txt}
        ///     {../d/e.txt}.BasedOn({a/b/c/}) = {a/b/d/e.txt}
        ///     {d/e.txt}.BasedOn({a/b/c.txt}) = {a/b/d/e.txt}
        ///     {../e.txt}.BasedOn({a/b/c.txt}) = {a/e.txt}
        ///     {../e.txt}.BasedOn({../c.txt}) = {../../e.txt}
        /// </summary>
        public RelativePath BasedOn(RelativePath path)
        {
            if (_isFromWorkingFolder)
            {
                return this;
            }
            if (ParentDirectoryCount >= path.SubdirectoryCount)
            {
                return Create(path._isFromWorkingFolder, path.ParentDirectoryCount - path.SubdirectoryCount + this.ParentDirectoryCount, this._parts);
            }
            else
            {
                return Create(path._isFromWorkingFolder, path.ParentDirectoryCount, path.GetSubdirectories(this.ParentDirectoryCount).Concat(this._parts));
            }
        }

        /// <summary>
        /// Get relative path from right relative path to left relative path
        /// e.g.:
        ///     {a/b/c.txt}.MakeRelativeTo({d/e.txt}) = {../a/b/c.txt}
        ///     {a/b/c.txt}.MakeRelativeTo({a/d.txt}) = {b/c.txt}
        ///     {../../a.txt}.MakeRelativeTo({../b.txt}) = {../a.txt}
        ///     {../../a.txt}.MakeRelativeTo({../b/c.txt}) = {../../a.txt}
        ///     {a.txt}.MakeRelativeTo({../b.txt}) = Oop...
        /// </summary>
        public RelativePath MakeRelativeTo(RelativePath relativeTo)
        {
            if (_isFromWorkingFolder != relativeTo._isFromWorkingFolder)
            {
                if (_isFromWorkingFolder)
                {
                    return this;
                }
                throw new NotSupportedException("From working folder must be same.");
            }
            if (_parentDirectoryCount < relativeTo._parentDirectoryCount)
            {
                throw new NotSupportedException("Relative to path has too many '../'.");
            }
            var parentCount = _parentDirectoryCount - relativeTo._parentDirectoryCount;
            var leftParts = _parts;
            var rightParts = relativeTo._parts;
            var commonCount = 0;
            for (int i = 0; i < rightParts.Length - 1; i++)
            {
                if (i >= leftParts.Length - 1)
                    break;
                if (!FilePathComparer.OSPlatformSensitiveStringComparer.Equals(leftParts[i], rightParts[i]))
                    break;
                commonCount++;
            }
            parentCount += rightParts.Length - 1 - commonCount;
            return Create(false, parentCount, leftParts.Skip(commonCount));
        }

        /// <summary>
        /// Rebase the relative path
        /// </summary>
        /// <param name="from">original base path</param>
        /// <param name="to">new base path</param>
        /// <returns>rebased relative path</returns>
        public RelativePath Rebase(RelativePath from, RelativePath to)
        {
            return (from + this) - to;
        }

        public string FileName => _parts[_parts.Length - 1];

        public bool IsFromWorkingFolder()
        {
            return _isFromWorkingFolder;
        }

        public string GetFileNameWithoutExtension()
        {
            return Path.GetFileNameWithoutExtension(FileName);
        }

        public RelativePath ChangeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (fileName.Contains('\\') || fileName.Contains('/') || fileName == ".." || fileName == ".")
            {
                throw new ArgumentException($"{fileName} is not a valid file name.");
            }

            return ChangeFileNameWithNoCheck(fileName);
        }

        public RelativePath GetDirectoryPath()
        {
            if (_parts.Length == 0)
            {
                throw new InvalidOperationException($"Unable to get directory path for {this.ToString()}");
            }

            return ChangeFileNameWithNoCheck(string.Empty);
        }

        public RelativePath GetPathFromWorkingFolder()
        {
            if (_isFromWorkingFolder)
            {
                return this;
            }
            return new RelativePath(true, _parentDirectoryCount, _parts);
        }

        public RelativePath RemoveWorkingFolder()
        {
            if (_isFromWorkingFolder)
            {
                return new RelativePath(false, _parentDirectoryCount, _parts);
            }
            return this;
        }

        public RelativePath UrlEncode()
        {
            var parts = new string[_parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Uri.EscapeDataString(_parts[i]);
            }
            return new RelativePath(_isFromWorkingFolder, _parentDirectoryCount, parts);
        }

        public RelativePath UrlDecode()
        {
            string[] parts = UrlDecodeCore(true);

            if (_parts.Length > 0 && parts[0] == WorkingFolderString)
            {
                return new RelativePath(true, _parentDirectoryCount, parts.Skip(1).ToArray());
            }

            return new RelativePath(_isFromWorkingFolder, _parentDirectoryCount, parts);
        }

        public RelativePath UrlDecodeUnsafe()
        {
            string[] parts = UrlDecodeCore(false);

            if (_parts.Length > 0 && parts[0] == WorkingFolderString)
            {
                return new RelativePath(true, _parentDirectoryCount, parts.Skip(1).ToArray());
            }

            return new RelativePath(_isFromWorkingFolder, _parentDirectoryCount, parts);
        }

        public override int GetHashCode()
        {
            var hash = _parentDirectoryCount;
            hash += _parts.Length << 16;
            for (int i = 0; i < _parts.Length; i++)
            {
                hash ^= FilePathComparer.OSPlatformSensitiveStringComparer.GetHashCode(_parts[i]) << (i % 10);
            }
            return hash;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RelativePath);
        }

        public bool Equals(RelativePath other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            if (_parentDirectoryCount != other._parentDirectoryCount)
            {
                return false;
            }
            if (_parts.Length != other._parts.Length)
            {
                return false;
            }
            for (int i = 0; i < _parts.Length; i++)
            {
                if (!FilePathComparer.OSPlatformSensitiveStringComparer.Equals(_parts[i], other._parts[i]))
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString() =>
            (_isFromWorkingFolder ? NormalizedWorkingFolder : "") +
            string.Concat(Enumerable.Repeat(ParentDirectory, _parentDirectoryCount)) +
            string.Join("/", _parts);

        /// <summary>
        /// Test whether a relative path starts with another folder relative path
        /// Return false if either path starts with "../"
        /// </summary>
        public bool InDirectory(RelativePath value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (value._parts[value._parts.Length - 1] != "")
            {
                return false;
            }
            if (_isFromWorkingFolder ^ value._isFromWorkingFolder)
            {
                return false;
            }
            if (_parentDirectoryCount > 0 || value._parentDirectoryCount > 0)
            {
                return false;
            }
            if (_parts.Length < value._parts.Length)
            {
                return false;
            }

            int i;
            for (i = 0; i < value._parts.Length; i++)
            {
                if (value._parts[i] == string.Empty)
                {
                    return true;
                }
                if (!FilePathComparer.OSPlatformSensitiveStringComparer.Equals(_parts[i], value._parts[i]))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Private Members

        private static RelativePath TryParseCore(string path, bool throwOnError)
        {
            if (path == null)
            {
                if (throwOnError)
                {
                    throw new ArgumentNullException(nameof(path));
                }
                return null;
            }
            if (path.Length == 0)
            {
                return Empty;
            }
            if (path.IndexOfAny(PathUtility.InvalidPathChars) != -1)
            {
                if (throwOnError)
                {
                    throw new ArgumentException($"Path({path}) contains invalid char.", nameof(path));
                }
                return null;
            }
            if (Path.IsPathRooted(path))
            {
                if (throwOnError)
                {
                    throw new ArgumentException($"Rooted path({path}) is not supported", nameof(path));
                }
                return null;
            }
            bool isFromWorkingFolder = false;
            var parts = path.Split('/', '\\');
            var stack = new Stack<string>();
            int parentCount = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                switch (parts[i])
                {
                    case "~":
                    case "%7E":
                        if (parentCount > 0 || stack.Count > 0 || isFromWorkingFolder)
                        {
                            throw new InvalidOperationException($"Invalid path: {path}");
                        }
                        isFromWorkingFolder = true;
                        break;
                    case "..":
                        if (stack.Count > 0)
                        {
                            stack.Pop();
                        }
                        else
                        {
                            parentCount++;
                        }
                        break;
                    case ".":
                    case "":
                        break;
                    default:
                        stack.Push(parts[i]);
                        break;
                }
            }
            if (parts[parts.Length - 1].Length == 0)
            {
                // if end with "/", treat it as folder
                stack.Push(string.Empty);
            }
            return Create(isFromWorkingFolder, parentCount, stack.Reverse());
        }

        private static RelativePath Create(bool isFromWorkingFolder, int parentDirectoryCount, IEnumerable<string> parts)
        {
            var partArray = parts.ToArray();
            if (parentDirectoryCount == 0 &&
                (partArray.Length == 0 ||
                 (partArray.Length == 1 &&
                  partArray[0].Length == 0)))
            {
                if (isFromWorkingFolder)
                {
                    return WorkingFolder;
                }
                else
                {
                    return Empty;
                }
            }
            return new RelativePath(isFromWorkingFolder, parentDirectoryCount, partArray);
        }

        private IEnumerable<string> GetSubdirectories(int skip)
        {
            if (_parts.Length <= skip)
            {
                throw new ArgumentOutOfRangeException(nameof(skip));
            }
            return _parts.Take(_parts.Length - skip - 1);
        }

        private RelativePath ChangeFileNameWithNoCheck(string fileName)
        {
            var parts = (string[])_parts.Clone();
            parts[parts.Length - 1] = fileName;
            return new RelativePath(_isFromWorkingFolder, _parentDirectoryCount, parts);
        }

        private string[] UrlDecodeCore(bool safe)
        {
            StringBuilder sb = null;
            var parts = new string[_parts.Length];
            var invalidPartChars = safe ? InvalidPartChars : UnsafeInvalidPartChars;
            for (int i = 0; i < parts.Length; i++)
            {
                var origin = _parts[i];
                var value = Uri.UnescapeDataString(origin);
                bool modified = false;
                int index;
                int lastIndex = value.Length - 1;
                int originLastIndex = origin.Length - 1;
                while (lastIndex != -1 && (index = value.LastIndexOfAny(invalidPartChars, lastIndex)) != -1)
                {
                    modified = true;
                    lastIndex = index - 1;
                    if (sb == null)
                    {
                        sb = new StringBuilder(value, Math.Max(value.Length + 18, 64));
                    }
                    else if (sb.Length == 0)
                    {
                        sb.Append(value);
                    }
                    var chIndex = Array.IndexOf(invalidPartChars, value[index]);
                    sb.Remove(index, 1);
                    var text = safe ? EncodedInvalidPartChars[chIndex] : EncodedUnsafeInvalidPartChars[chIndex];
                    var originIndex = origin.LastIndexOf(text, originLastIndex, StringComparison.OrdinalIgnoreCase);
                    originLastIndex = originIndex - 1;
                    sb.Insert(index, origin.Substring(originIndex, text.Length));
                }
                parts[i] = modified ? sb.ToString() : value;
                if (sb != null)
                {
                    sb.Length = 0;
                }
            }

            return parts;
        }

        #endregion

        #region Operators

        /// <summary>
        /// Concat two relative path
        /// e.g.:
        ///     a/b/c/ + d/e.txt = a/b/c/d/e.txt
        ///     a/b/c/ + ../d/e.txt = a/b/d/e.txt
        ///     a/b/c.txt + d/e.txt = a/b/d/e.txt
        ///     a/b/c.txt + ../e.txt = a/e.txt
        ///     ../c.txt + ../e.txt = ../../e.txt
        /// </summary>
        public static RelativePath operator +(RelativePath left, RelativePath right)
        {
            return (right ?? Empty).BasedOn(left ?? Empty);
        }

        /// <summary>
        /// Get relative path from right relative path to left relative path
        /// e.g.:
        ///     a/b/c.txt - d/e.txt = ../a/b/c.txt
        ///     a/b/c.txt - a/d.txt = b/c.txt
        ///     ../../a.txt - ../b.txt = ../a.txt
        ///     ../../a.txt - ../b/c.txt = ../../a.txt
        ///     a.txt - ../b.txt = Oop...
        /// </summary>
        public static RelativePath operator -(RelativePath left, RelativePath right)
        {
            return (left ?? Empty).MakeRelativeTo(right ?? Empty);
        }

        public static bool operator ==(RelativePath left, RelativePath right) =>
            object.Equals(left, right);

        public static bool operator !=(RelativePath left, RelativePath right) =>
            !object.Equals(left, right);

        public static implicit operator string(RelativePath path)
        {
            if (path == null)
            {
                return null;
            }
            return path.ToString();
        }

        public static explicit operator RelativePath(string path)
        {
            if (path == null)
            {
                return null;
            }
            return Parse(path);
        }

        #endregion

    }
}
