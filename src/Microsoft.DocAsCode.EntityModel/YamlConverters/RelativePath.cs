﻿namespace Microsoft.DocAsCode.EntityModel.YamlConverters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// relative path for case sensitive os.
    /// </summary>
    public sealed class RelativePath : IEquatable<RelativePath>
    {

        #region Consts/Fields
        private const string UpDirectory = "../";
        public static readonly RelativePath Empty = new RelativePath(0, new string[] { string.Empty });

        private readonly int _parentDirectoryCount;
        private readonly string[] _parts;
        #endregion

        #region Constructor

        private RelativePath(int parentDirectoryCount, string[] parts)
        {
            _parentDirectoryCount = parentDirectoryCount;
            _parts = parts;
        }

        #endregion

        #region Public Members

        public static RelativePath Parse(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (path.Length == 0)
            {
                return Empty;
            }
            if (Path.IsPathRooted(path))
            {
                throw new ArgumentException($"Rooted path({path}) is not supported", nameof(path));
            }
            var parts = path.Replace('\\', '/').Split('/');
            var stack = new Stack<string>();
            int parentCount = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                switch (parts[i])
                {
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
            return Create(parentCount, stack.Reverse());
        }

        public int ParentDirectoryCount => _parentDirectoryCount;

        public int SubdirectoryCount => _parts.Length - 1;

        public bool IsEmpty => ReferenceEquals(this, Empty);

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

        public override int GetHashCode()
        {
            var hash = _parentDirectoryCount;
            hash += _parts.Length << 16;
            for (int i = 0; i < _parts.Length; i++)
            {
                hash ^= _parts[i].GetHashCode() << (i % 10);
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
                if (_parts[i] != other._parts[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override string ToString() =>
            string.Concat(Enumerable.Repeat(UpDirectory, _parentDirectoryCount)) +
            string.Join("/", _parts);

        #endregion

        #region Private Members

        private static RelativePath Create(int parentDirectoryCount, IEnumerable<string> parts)
        {
            var partArray = parts.ToArray();
            if (parentDirectoryCount == 0 &&
                partArray.Length == 1 &&
                partArray[0].Length == 0)
            {
                return Empty;
            }
            return new RelativePath(parentDirectoryCount, partArray);
        }

        private IEnumerable<string> GetSubdirectories(int skip)
        {
            if (_parts.Length <= skip)
            {
                throw new ArgumentOutOfRangeException(nameof(skip));
            }
            return _parts.Take(_parts.Length - skip - 1);
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
            if (right.ParentDirectoryCount >= left.SubdirectoryCount)
            {
                return Create(left.ParentDirectoryCount - left.SubdirectoryCount + right.ParentDirectoryCount, right._parts);
            }
            else
            {
                return Create(left.ParentDirectoryCount, left.GetSubdirectories(right.ParentDirectoryCount).Concat(right._parts));
            }
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
            if (left._parentDirectoryCount < right._parentDirectoryCount)
            {
                throw new NotSupportedException("Right relative path has too many '../'.");
            }
            var parentCount = left._parentDirectoryCount - right._parentDirectoryCount;
            var leftParts = left._parts;
            var rightParts = right._parts;
            var commonCount = 0;
            for (int i = 0; i < rightParts.Length - 1; i++)
            {
                if (i >= leftParts.Length - 1)
                    break;
                if (leftParts[i] != rightParts[i])
                    break;
                commonCount++;
            }
            parentCount += rightParts.Length - 1 - commonCount;
            return Create(parentCount, leftParts.Skip(commonCount));
        }

        public static implicit operator string (RelativePath path)
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
