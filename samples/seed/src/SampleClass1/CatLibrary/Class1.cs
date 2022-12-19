using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatLibrary
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    //interface
    /// <summary>
    /// This is <b>basic</b> interface of all animal.
    /// </summary>
    public interface IAnimal
    {
        //property
        /// <summary>
        /// Name of Animal.
        /// </summary>
        string Name { get; }
        //index
        /// <summary>
        /// Return specific number animal's name.
        /// </summary>
        /// <param name="index">Animal number.</param>
        /// <returns>Animal name.</returns>
        string this[int index] { get; }
        //method
        /// <summary>
        /// Animal's eat method.
        /// </summary>
        void Eat();
        //template method
        /// <summary>
        /// Overload method of eat. This define the animal eat by which tool.
        /// </summary>
        /// <typeparam name="Tool">It's a class type.</typeparam>
        /// <param name="tool">Tool name.</param>
        void Eat<Tool>(Tool tool)
            where Tool : class;

        /// <summary>
        /// Feed the animal with some food
        /// </summary>
        /// <param name="food">Food to eat</param>
        void Eat(string food);
    }

    /// <summary>
    /// Cat's interface
    /// </summary>
    public interface ICat : IAnimal
    {
        //event
        /// <summary>
        /// eat event of cat. Every cat must implement this event.
        /// </summary>
        event EventHandler eat;
    }

    /// <summary>
    /// <para>Here's main class of this <i>Demo</i>.</para>
    /// <para>You can see mostly type of article within this class and you for more detail, please see the remarks.</para>
    /// <para></para>
    /// <para>this class is a template class. It has two Generic parameter. they are: <typeparamref name="T"/> and <typeparamref name="K"/>.</para>
    /// <para>The extension method of this class can refer to <see cref="ICatExtension"/> class</para>
    /// </summary>
    /// <example>
    /// <para>Here's example of how to create an instance of this class. As T is limited with <c>class</c> and K is limited with <c>struct</c>.</para>
    /// <code language="c#">
    ///     var a = new Cat(object, int)();
    ///     int catNumber = new int();
    ///     unsafe
    ///     {
    ///         a.GetFeetLength(catNumber);
    ///     }
    /// </code>
    /// <para>As you see, here we bring in <b>pointer</b> so we need to add <languageKeyword>unsafe</languageKeyword> keyword.</para>
    /// </example>
    /// <typeparam name="T">This type should be class and can new instance.</typeparam>
    /// <typeparam name="K">This type is a struct type, class type can't be used for this parameter.</typeparam>
    /// <remarks>
    /// <para>Here's all the content you can see in this class.</para>
    /// <list type="ordered">
    /// <listItem>Constructors. With different input parameters.</listItem>
    /// <listItem>
    /// <b>Methods</b>. Including:
    /// <list>
    /// <listItem>
    /// Template method.
    /// </listItem>
    /// <listItem>
    /// Normal method wit generic parameter.
    /// </listItem>
    /// <listItem>
    /// Override method.
    /// </listItem>
    /// <listItem>
    /// unsafe method with pointer.
    /// </listItem>
    /// </list>
    /// </listItem>
    /// <listItem><b>Operators</b>. You can also see explicit operator here.</listItem>
    /// <listItem><b>Properties</b>. Include normal property and index.</listItem>
    /// <listItem><b>Events</b>.</listItem>
    /// <listItem><b>Fields</b>.</listItem>
    /// <listItem><b>EII</b>. ExplicitImplementInterface. including eii property, eii method, eii event.</listItem>
    /// <listItem><b>Extension Methods</b>. The extension methods not definition in this class, but we can find it!</listItem>
    /// </list>
    /// </remarks>
    [Serializable]
    public class Cat<T, K> : ICat
        where T : class, new()
        where K : struct
    {
        //Constructors: normal with parameter
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Cat() { }

        /// <summary>
        /// Constructor with one generic parameter.
        /// </summary>
        /// <param name="ownType">This parameter type defined by class.</param>
        public Cat(T ownType) { }

        /// <summary>
        /// It's a complex constructor. The parameter will have some attributes.
        /// </summary>
        /// <param name="nickName">it's string type.</param>
        /// <param name="age">It's an out and ref parameter.</param>
        /// <param name="realName">It's an out paramter.</param>
        /// <param name="isHealthy">It's an in parameter.</param>
        public Cat(string nickName, out int age, [Out] string realName, [In] bool isHealthy) { age = 1; }

        //Methods: template + normal with generic type + pointer method
        /// <summary>
        /// It's a method with complex return type.
        /// </summary>
        /// <param name="date">Date time to now.</param>
        /// <returns>It's a relationship map of different kind food.</returns>
        public Dictionary<string, List<int>> CalculateFood(DateTime date) { return null; }

        /// <summary>
        /// This method have attribute above it.
        /// </summary>
        /// <param name="ownType">Type come from class define.</param>
        /// <param name="anotherOwnType">Type come from class define.</param>
        /// <param name="cheat">Hint whether this cat has cheat mode.</param>
        /// <exception cref="ArgumentException">This is an argument exception</exception>
        [Conditional("Debug")]
        public void Jump(T ownType, K anotherOwnType, ref bool cheat)
        {
            EventHandler handler = ownEat;
        }

        /// <summary>
        /// Override the method of <c>Object.Equals(object obj).</c>
        /// </summary>
        /// <param name="obj">Can pass any class type.</param>
        /// <returns>The return value tell you whehter the compare operation is successful.</returns>
        public override bool Equals(object obj) { return false; }

        /// <summary>
        /// It's an <c>unsafe</c> method.
        /// As you see, <paramref name="catName"/> is a <b>pointer</b>, so we need to add <languageKeyword>unsafe</languageKeyword> keyword.
        /// </summary>
        /// <param name="catName">Thie represent for cat name length.</param>
        /// <param name="parameters">Optional parameters.</param>
        /// <returns>Return cat tail's length.</returns>
        public unsafe long GetTailLength(int* catName, params object[] parameters) { return 1; }

        //operator
        /// <summary>
        /// Addition operator of this class.
        /// </summary>
        /// <param name="lsr">...</param>
        /// <param name="rsr">~~~</param>
        /// <returns>Result with <i>int</i> type.</returns>
        public static int operator +(Cat<T, K> lsr, int rsr) { return 1; }

        /// <summary>
        /// Similar with operaotr +, refer to that topic.
        /// </summary>
        public static int operator -(Cat<T, K> lsr, int rsr) { return 1; }

        /// <summary>
        /// Expilicit operator of this class.
        /// <para>It means this cat can evolve to change to Tom. Tom and Jerry.</para>
        /// </summary>
        /// <param name="src">Instance of this class.</param>
        /// <returns>Advanced class type of cat.</returns>
        public static explicit operator Tom(Cat<T, K> src) { return null; }

        //Property: index + normal
        /// <summary>
        /// This is index property of Cat. You can see that the visibility is different between <c>get</c> and <c>set</c> method.
        /// </summary>
        /// <param name="a">Cat's name.</param>
        /// <returns>Cat's number.</returns>
        public int this[string a]
        {
            protected get { return 1; }
            set { }
        }

        /// <summary>
        /// Hint cat's age.
        /// </summary>
        protected int Age
        {
            get { return 1; }
            set { }
        }

        //event
        /// <summary>
        /// Eat event of this cat
        /// </summary>
        public event EventHandler ownEat;

        //Field: with attribute
        /// <summary>
        /// Field with attribute.
        /// </summary>
        [ContextStatic]
        [NonSerialized]
        public bool isHealthy;

        //EII Method
        /// <summary>
        /// EII method.
        /// </summary>
        void IAnimal.Eat() { }
        /// <summary>
        /// EII template method.
        /// </summary>
        /// <typeparam name="Tool">Tool for eat.</typeparam>
        /// <param name="a">Tool name.</param>
        void IAnimal.Eat<Tool>(Tool a) { }

        /// <summary>
        /// Implementation of Eat(food)
        /// </summary>
        /// <param name="food">Food to eat</param>
        void IAnimal.Eat(string food) { }

        //EII Property
        /// <summary>
        /// EII property.
        /// </summary>
        public string Name { get { return "Pig"; } }

        /// <summary>
        /// EII index.
        /// </summary>
        /// <param name="a">Cat's number.</param>
        /// <returns>Cat's name.</returns>
        string IAnimal.this[int a] { get { return "Animal"; } }

        //EII Event
        /// <summary>
        /// EII event.
        /// </summary>
        event EventHandler ICat.eat
        {
            add { ownEat += value; }
            remove { ownEat -= value; }
        }
    }

    public class Complex<T, J>
    {

    }

    /// <summary>
    /// Tom class is only inherit from Object. Not any member inside itself.
    /// </summary>
    public class Tom
    {
        /// <summary>
        /// This is a Tom Method with complex type as return
        /// </summary>
        /// <param name="a">A complex input</param>
        /// <param name="b">Another complex input</param>
        /// <returns>Complex @CatLibrary.TomFromBaseClass</returns>
        /// <exception cref="NotImplementedException">This is not implemented</exception>
        /// <exception cref="ArgumentException">This is the exception to be thrown when implemented</exception>
        /// <exception cref="CatException{T}">This is the exception in current documentation</exception>
        public Complex<string, TomFromBaseClass> TomMethod(Complex<TomFromBaseClass, TomFromBaseClass> a, Tuple<string, Tom> b)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// *TomFromBaseClass* inherits from @
    /// </summary>
    public class TomFromBaseClass : Tom
    {

        /// <summary>
        /// This is a #ctor with parameter
        /// </summary>
        /// <param name="k"></param>
        public TomFromBaseClass(int k)
        {

        }

    }

    public class CatException<T> : Exception
    {

    }

    /// <summary>
    /// It's the class that contains ICat interface's extension method.
    /// <para>This class must be <b>public</b> and <b>static</b>.</para>
    /// <para>Also it shouldn't be a geneic class</para>
    /// </summary>
    public static class ICatExtension
    {
        /// <summary>
        /// Extension method hint that how long the cat can sleep.
        /// </summary>
        /// <param name="icat">The type will be extended.</param>
        /// <param name="hours">The length of sleep.</param>
        public static void Sleep(this ICat icat, long hours) { }

        /// <summary>
        /// Extension method to let cat play
        /// </summary>
        /// <param name="icat">Cat</param>
        /// <param name="toy">Something to play</param>
        public static void Play(this ICat icat, string toy) { }
    }

    //delegate
    /// <summary>
    /// Delegate in the namespace
    /// </summary>
    /// <param name="pics">a name list of pictures.</param>
    /// <param name="name">give out the needed name.</param>
    public delegate void MRefNormalDelegate(List<string> pics, out string name);

    /// <summary>
    /// Generic delegate with many constrains.
    /// </summary>
    /// <typeparam name="K">Generic K.</typeparam>
    /// <typeparam name="T">Generic T.</typeparam>
    /// <typeparam name="L">Generic L.</typeparam>
    /// <param name="k">Type K.</param>
    /// <param name="t">Type T.</param>
    /// <param name="l">Type L.</param>
    public delegate void MRefDelegate<K, T, L>(K k, T t, L l)
        where K : class, IComparable
        where T : struct
        where L : Tom, IEnumerable<long>;

    /// <summary>
    /// Fake delegate
    /// </summary>
    /// <typeparam name="T">Fake para</typeparam>
    /// <param name="num">Fake para</param>
    /// <param name="name">Fake para</param>
    /// <param name="scores">Optional Parameter.</param>
    /// <returns>Return a fake number to confuse you.</returns>
    public delegate int FakeDelegate<T>(long num, string name, params object[] scores);
}

namespace MRef.Demo.Enumeration
{
    // Enumeration
    /// <summary>
    /// Enumeration ColorType
    /// </summary>
    /// <remarks>
    /// <para>
    /// Red/Blue/Yellow can become all color you want.
    /// </para>
    /// <list type="bullet">
    /// <listItem>
    /// Orange = Red + Yellow
    /// </listItem>
    /// <listItem>
    /// Purple = Red + Blue
    /// </listItem>
    /// <listItem>
    /// Green = Blue + Yellow
    /// </listItem>
    /// </list>
    /// </remarks>
    /// <seealso cref="T:System.Object"/>
    public enum ColorType
    {
        /// <summary>
        /// this color is red
        /// </summary>
        Red,
        /// <summary>
        /// blue like river
        /// </summary>
        Blue,
        /// <summary>
        /// yellow comes from desert
        /// </summary>
        Yellow
    }
}
