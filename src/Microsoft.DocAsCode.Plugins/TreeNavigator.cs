// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;
    using System.Collections.Generic;

    public class TreeNavigator
    {
        private NavigatorTreeItem _tree;
        private NavigatorTreeItem _current;

        public TreeNavigator(TreeItem tree)
        {
            _tree = Init(tree, null);
            _current = _tree;
        }

        public bool MoveToParent()
        {
            if (_current.Parent != null)
            {
                _current = _current.Parent;
                return true;
            }

            return false;
        }

        public TreeItem Current
        {
            get
            {
                return _current.Current;
            }
        }

        public bool MoveToChild(Func<TreeItem, bool> comparer)
        {
            if (_current.Items == null || _current.Items.Count == 0)
            {
                return false;
            }

            foreach(var item in _current.Items)
            {
                if (comparer(item.Current))
                {
                    _current = item;
                    return true;
                }
            }

            return true;
        }

        public bool MoveToFirstChild()
        {
            if (_current.Items == null || _current.Items.Count == 0)
            {
                return false;
            }
            _current = _current.Items[0];
            return true;
        }

        public bool MoveTo(Func<TreeItem, bool> comparer)
        {
            return MoveTo(_tree, comparer);
        }

        public bool AppendChild(TreeItem child)
        {
            var items = new List<TreeItem>(_current.Current.Items ?? new List<TreeItem>());
            items.Add(child);
            _current.Current.Items = items;
            _current.Items.Add(new NavigatorTreeItem { Current = child, Parent = _current });
            return true;
        }

        public bool RemoveChild(Func<TreeItem, bool> comparer)
        {
            if (_current.Items == null || _current.Items.Count == 0)
            {
                return false;
            }

            foreach (var item in _current.Items.ToArray())
            {
                if (comparer(item.Current))
                {
                    _current.Current.Items.Remove(item.Current);
                    _current.Items.Remove(item);
                    return true;
                }
            }

            return false;
        }

        private NavigatorTreeItem Init(TreeItem current, NavigatorTreeItem parent)
        {
            var tree = new NavigatorTreeItem
            {
                Current = current,
                Parent = parent,
            };

            if (current.Items != null)
            {
                foreach (var item in current.Items)
                {
                    tree.Items.Add(Init(item, tree));
                }
            }
            return tree;
        }

        private bool MoveTo(NavigatorTreeItem node, Func<TreeItem, bool> comparer)
        {
            if (comparer(node.Current))
            {
                _current = node;
                return true;
            }
            foreach (var item in node.Items)
            {
                if (MoveTo(item, comparer))
                {
                    return true;
                }
            }
            return false;
        }
        
        private class NavigatorTreeItem
        {
            public NavigatorTreeItem Parent { get; set; }
            public TreeItem Current { get; set; }
            public List<NavigatorTreeItem> Items { get; set; } = new List<NavigatorTreeItem>();
        }
    }
}
