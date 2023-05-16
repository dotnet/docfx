namespace BuildFromProject;

public class Class1 : IClass1
{
    public class Test<T> { }

    /// <summary>
    /// This method should do something...
    /// </summary>
    /// <include file='../../docs.xml' path='docs/members[@name="MyTests"]/Test/*'/>
    public void XmlCommentIncludeTag() { }
    
    /// <summary>
    /// Test
    /// </summary>
    /// <param name="args"></param>
    /// <seealso cref="Test{T}"/>
    /// <seealso cref="Class1"/>
    public void Issue896() { }

    /// <summary>
    /// Pricing models are used to calculate theoretical option values
    /// <list type="bullet">
    /// <item><term>1</term><description>Black Scholes</description></item>
    /// <item><term>2</term><description>Black76</description></item>
    /// <item><term>3</term><description>Black76Fut</description></item>
    /// <item><term>4</term><description>Equity Tree</description></item>
    /// <item><term>5</term><description>Variance Swap</description></item>
    /// <item><term>6</term><description>Dividend Forecast</description></item>
    /// </list>
    /// </summary>
    public void Issue1651() { }

    /// <remarks>
    /// There's really no reason to not believe that this class can test things.
    /// <list type="table">
    /// <listheader><term>Term</term><description>Description</description></listheader>
    /// <item><term>A Term</term><description>A Description</description></item>
    /// <item><term>Bee Term</term><description>Bee Description</description></item>
    /// </list>
    /// </remarks>
    public void Issue7484() { }

    /// <remarks>
    /// <code>
    ///         void Update()
    ///         {
    ///             myClass.Execute();
    ///         }
    /// </code>
    /// </remarks>
    /// <example>
    ///     <code source="../../../../common/Example.cs" region="MessageDeleted"></code>
    /// </example>
    public void Issue4017() { }

    /// <remarks>
    ///     For example:
    ///
    ///         MyClass myClass = new MyClass();
    ///
    ///         void Update()
    ///         {
    ///             myClass.Execute();
    ///         }
    /// </remarks>
    /// <example>
    /// ```csharp
    /// MyClass myClass = new MyClass();
    ///
    /// void Update()
    /// {
    ///     myClass.Execute();
    /// }
    /// ```
    /// </example>
    public void Issue2623() { }

    /// <remarks>
    /// > [!NOTE]
    /// > This is a &lt;note&gt;. &amp; &quot; &apos;
    ///
    /// Inline `&lt;angle brackets&gt;`.
    ///
    /// [link](https://www.github.com "title")
    ///
    /// ```csharp
    /// for (var i = 0; i > 10; i++) // &amp; &quot; &apos;
    /// var range = new Range&lt;int&gt; { Min = 0, Max = 10 };
    /// ```
    ///
    /// <code>
    /// var range = new Range&lt;int&gt; { Min = 0, Max = 10 };
    /// </code>
    /// </remarks>
    public void Issue2723() { }

    /// <remarks>
    /// <c>@"\\?\"</c> `@"\\?\"`
    /// </remarks>
    public void Issue4392() { }

    public void Issue8764<T>() where T: unmanaged { }

    public class Issue8665
    {
        public int Foo { get; }
        public char Bar { get; }
        public string Baz { get; }

        public Issue8665() : this(0, '\0', string.Empty) { }

        public Issue8665(int foo) : this(foo, '\0', string.Empty) { }

        public Issue8665(int foo, char bar) : this(foo, bar, string.Empty) { }

        public Issue8665(int foo, char bar, string baz)
        {
            Foo = foo;
            Bar = bar;
            Baz = baz;
        }
    }

    public class Issue8696Attribute : Attribute
    {
        [Issue8696Attribute("Changes the name of the server in the server list", 0, 0, null, false, null)]
        public Issue8696Attribute(string? description = null, int boundsMin = 0, int boundsMax = 0, string[]? validGameModes = null, bool hasMultipleSelections = false, Type? enumType = null)
        {
        }
    }
}
