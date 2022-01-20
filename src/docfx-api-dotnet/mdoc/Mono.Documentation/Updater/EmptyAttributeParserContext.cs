namespace Mono.Documentation.Updater
{
    public class EmptyAttributeParserContext : IAttributeParserContext
    {
        private static readonly EmptyAttributeParserContext singletonInstance = new EmptyAttributeParserContext();

        private EmptyAttributeParserContext()
        {
        }

        public static IAttributeParserContext Empty()
        {
            return singletonInstance;
        }

        public void NextDynamicFlag()
        {
        }

        public bool IsDynamic()
        {
            return false;
        }

        public bool IsNullable()
        {
            return false;
        }
    }
}
