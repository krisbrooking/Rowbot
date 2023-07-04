namespace Rowbot.Common.Extensions
{
    internal static class TypeExtensions
    {
        internal static bool ImplementsGenericInterface(this Type type, Type genericInterface)
        {
            if (genericInterface is null)
            {
                return false;
            }

            return type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterface);
        }
    }
}
