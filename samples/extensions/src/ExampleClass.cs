using System;

namespace MyExample
{
    internal class ExampleClass
    {
        public event Action<string> MyEvent;

        public string MyField;

        public string MyProperty { get; internal set; }

        public ExampleClass()
        {
            
        }

        private string MyMethod()
        {
            return "Hello World!";
        }
    }
}