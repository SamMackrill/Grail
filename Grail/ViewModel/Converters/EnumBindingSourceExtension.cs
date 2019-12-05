using System;
using System.Windows.Markup;

namespace Grail.ViewModel.Converters
{
    public class EnumBindingSourceExtension : MarkupExtension
    {
        private readonly Type enumType;

        public EnumBindingSourceExtension(Type enumType)
        {
            if (enumType == this.enumType) return;

            if (enumType != null)
            {
                var type = Nullable.GetUnderlyingType(enumType) ?? enumType;
                if (!type.IsEnum) throw new ArgumentException("Type must be for an Enum.");
            }

            this.enumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (enumType == null)
                throw new InvalidOperationException("The EnumType must be specified.");

            var actualEnumType = Nullable.GetUnderlyingType(enumType) ?? enumType;
            var enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == enumType) return enumValues;

            var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }
    }
}