using System;
using System.Linq;
using System.Collections.Generic;
namespace Mono.Documentation.Updater.Frameworks
{
    public static class FXUtils
    {
        public static string AddFXToList (string existingValue, string newFX)
        {
            var cachedValue = GetCache (existingValue, newFX, CacheAction.Add);
            if (!string.IsNullOrWhiteSpace (cachedValue))
                return cachedValue;

            var splitValue = SplitList (existingValue);
            if (!splitValue.Contains (newFX)) splitValue.Add (newFX);
            var returnVal = JoinList (splitValue);

            SetCache (existingValue, newFX, returnVal, CacheAction.Add);

            return returnVal;
        }

        public static string RemoveFXFromList (string existingValue, string FXToRemove)
        {
            var cachedValue = GetCache (existingValue, FXToRemove, CacheAction.Remove);
            if (!string.IsNullOrWhiteSpace (cachedValue))
                return cachedValue;

            var splitValue = SplitList (existingValue);
            splitValue.Remove (FXToRemove);
            var returnVal = JoinList (splitValue);

            SetCache (existingValue, FXToRemove, returnVal, CacheAction.Remove);

            return returnVal;
        }

        /// <summary>Returns a list of all previously processed frameworks (not including the current)</summary>
        internal static string PreviouslyProcessedFXString (FrameworkTypeEntry typeEntry)
        {
            if (typeEntry == null)
                return string.Empty;

            return string.Join (";", typeEntry
                .PreviouslyProcessedFrameworkTypes
                .Select (previous => previous?.Framework?.Name)
                .Where (n => !string.IsNullOrWhiteSpace (n))
                .ToArray ());
        }


        static List<string> SplitList (string existingValue)
        {
            existingValue = existingValue ?? string.Empty;

            return existingValue.Split (new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList ();
        }

        static string JoinList (List<string> splitValue)
        {
            return string.Join (";", splitValue.ToArray ());
        }

        #region Framework String Cache Stuff

        /// <summary>Cache for modified framework strings</summary>
        static Dictionary<string, ValueTuple<Dictionary<string, string>, Dictionary<string, string>>> map = new Dictionary<string, ValueTuple<Dictionary<string, string>, Dictionary<string, string>>> ();
        enum CacheAction { Add, Remove }

        static string GetCache (string currentValue, string value, CacheAction action)
        {
            ValueTuple<Dictionary<string, string>, Dictionary<string, string>> cacheKey;
            if (map.TryGetValue (currentValue, out cacheKey))
            {
                string cachedValue = string.Empty;
                switch (action)
                {
                    case CacheAction.Add:
                        cacheKey.Item1.TryGetValue (value, out cachedValue);
                        break;
                    case CacheAction.Remove:
                        cacheKey.Item2.TryGetValue (value, out cachedValue);
                        break;
                }
                return cachedValue;
            }
            return string.Empty;
        }
        static void SetCache (string currentValue, string value, string newValue, CacheAction action)
        {
            ValueTuple<Dictionary<string, string>, Dictionary<string, string>> outerCacheValue;
            if (!map.TryGetValue (currentValue, out outerCacheValue))
            {
                outerCacheValue = new ValueTuple<Dictionary<string, string>, Dictionary<string, string>> (new Dictionary<string, string> (), new Dictionary<string, string> ());
                map.Add (currentValue, outerCacheValue);
            }

            Dictionary<string, string> innerCacheContainer = null;
            switch (action)
            {
                case CacheAction.Add:
                    innerCacheContainer = outerCacheValue.Item1;
                    break;
                case CacheAction.Remove:
                    innerCacheContainer = outerCacheValue.Item2;
                    break;
            }

            if (!innerCacheContainer.ContainsKey (value))
            {
                innerCacheContainer.Add (value, newValue);
            }

        }

        #endregion
    }
}
