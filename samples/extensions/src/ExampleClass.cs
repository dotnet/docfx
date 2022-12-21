namespace MyExample
{
    public class ExampleClass
    {
        public event Action<string> MyEvent;

        public string MyField;

        public string Myroperty { get; set; }

        public ExampleClass()
        {
            
        }

        public string MyMethod()
        {
            return "Hello World!";
        }
    }
}