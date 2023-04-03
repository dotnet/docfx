namespace BuildFromProject;

public interface IInheritdoc
{
    /// <summary>
    /// This method should do something...
    /// </summary>
    void Issue7629();
}

public class Inheritdoc : IInheritdoc, IDisposable
{
    public void Dispose() { }

    public void Issue7629() { }

    /// <inheritdoc cref="IInheritdoc.Issue7629" />
    public void Issue7628() { }

    public class Issue8101
    {
        /// <summary>
        /// Create a new tween.
        /// </summary>
        /// <param name="from">The starting value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="duration">Total tween duration in seconds.</param>
        /// <param name="onChange">A callback that will be invoked every time the tween value changes.</param>
        /// <returns>The newly created tween instance.</returns>
        public static object Tween(float from, float to, float duration, Action<float> onChange) => null;

        /// <inheritdoc cref="Tween" />
        public static object Tween(int from, int to, float duration, Action<int> onChange) => null;
    }

    public struct Issue8129
    {
        /// <inheritdoc/>
        public Issue8129(string foo) { }
    }

    /// <summary>
    /// This is a test class to have something for DocFX to document.
    /// </summary>
    /// <remarks>
    /// We're going to talk about things now.
    /// <list type="table">
    /// <listheader>Things for the header</listheader>
    /// <item>
    /// <term><see cref="BoolReturningMethod(bool)"/></term>
    /// <description><inheritdoc cref="BoolReturningMethod(bool)" path="/summary"/></description>
    /// </item>
    /// <item>
    /// <term><see cref="DoDad"/></term>
    /// <description><inheritdoc cref="DoDad" path="/summary"/></description>
    /// </item>
    /// </list>
    /// </remarks>
    public class Issue7484
    {
        /// <summary>
        /// This is a constructor to document.
        /// </summary>
        public Issue7484() { }

        /// <summary>
        /// A string that could have something.
        /// </summary>
        public string DoDad { get; }

        /// <summary>
        /// Simple method to generate docs for.
        /// </summary>
        /// <remarks>
        /// I'd like to take a moment to thank all of those who helped me get to
        /// a place where I can write documentation like this.
        /// </remarks>
        /// <param name="source">A meaningless boolean value, much like most questions in the world.</param>
        /// <returns>An exactly equivalently meaningless boolean value, much like most answers in the world.</returns>
        public bool BoolReturningMethod(bool source) => source;
    }

    public class Issue7035
    {
        /// <inheritdoc cref="B"/>
        public void A() { }

        /// <inheritdoc cref="A"/>
        public void B() { }
    }

    public class Issue6366
    {
        public abstract class Class1<T>
        {
            /// <summary>
            /// This text inherited.
            /// </summary>
            /// <param name="parm1">This text NOT inherited.</param>
            /// <param name="parm2">This text inherited.</param>
            /// <returns>This text inherited.</returns>
            public abstract T TestMethod1(T parm1, int parm2);
        }

        public class Class2 : Class1<bool>
        {
            /// <inheritdoc/>
            public override bool TestMethod1(bool parm1, int parm2) => false;
        }
    }
}