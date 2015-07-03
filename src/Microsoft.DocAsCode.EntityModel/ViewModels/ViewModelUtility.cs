namespace Microsoft.DocAsCode.EntityModel.ViewModels
{
    using System.Collections.Generic;

    public static class ViewModelUtility
    {
        public static TValue GetLanguageProperty<TValue>(this SortedList<SyntaxLanguage, TValue> dict, SyntaxLanguage language, TValue defaultValue = null)
            where TValue : class
        {
            TValue result;
            if (dict.TryGetValue(language, out result))
            {
                return result;
            }
            if (language == SyntaxLanguage.Default && dict.Count > 0)
            {
                return dict.Values[0];
            }
            return defaultValue;
        }
    }
}
