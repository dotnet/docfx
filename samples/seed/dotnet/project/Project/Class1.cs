﻿namespace BuildFromProject;

public class Class1
{
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

    public class Test<T> { }
}