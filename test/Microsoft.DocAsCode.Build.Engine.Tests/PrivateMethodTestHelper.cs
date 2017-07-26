namespace UnitTestUtilities
{
    using System.Reflection;
    using System;

    public class PrivateMethodTestHelper
    {
        public static object RunStaticMethod(System.Type t, string strMethod, object[] aobjParams)
        {
            BindingFlags eFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            return RunMethod(t, strMethod, null, aobjParams, eFlags);
        } //end of method

        public static object RunInstanceMethod(System.Type t, string strMethod, object objInstance, object[] aobjParams)
        {
            BindingFlags eFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            return RunMethod(t, strMethod, objInstance, aobjParams, eFlags);
        } //end of method

        private static object RunMethod(System.Type t, string strMethod, object objInstance, object[] aobjParams, BindingFlags eFlags)
        {
            MethodInfo m;
            try
            {
                m = t.GetMethod(strMethod, eFlags);
                if (m == null)
                {
                    throw new ArgumentException($"There is no method '{strMethod}' for type '{t.ToString()}'.");
                }

                object objRet = m.Invoke(objInstance, aobjParams);
                return objRet;
            }
            catch
            {
                throw;
            }
        } //end of method
    }
}
