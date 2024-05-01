using System.ComponentModel;
using System.Reflection;

namespace AppFinder.Driver
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());

            var attribute = (DescriptionAttribute)fieldInfo.GetCustomAttribute(typeof(DescriptionAttribute), false);

            return attribute != null ? attribute.Description : value.ToString();
        }
    }
}