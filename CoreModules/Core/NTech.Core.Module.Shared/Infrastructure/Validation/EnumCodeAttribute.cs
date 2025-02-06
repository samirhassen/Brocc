using System;

namespace NTech.Services.Infrastructure.NTechWs
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EnumCodeAttribute : NTechWsStringValidationAttributeBase
    {
        public Type EnumType { get; set; }

        protected override bool IsValidString(string value)
        {
            if (EnumType == null)
                throw new Exception("Missing EnumType");
            return Enum.IsDefined(EnumType, value);
        }

        public override bool RequiresValidationContext => true;
    }
}
