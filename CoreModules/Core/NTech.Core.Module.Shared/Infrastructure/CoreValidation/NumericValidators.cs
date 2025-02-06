namespace NTech.Core.Module.Shared.Infrastructure.CoreValidation
{
    public class PositiveNumberAttribute : NumbericAttribueBase
    {
        protected override string GetCustomErrorMessage(decimal value, string formattedValue) => "Value must be > 0";
        protected override bool IsValidNumber(decimal value) => value > 0m;
    }

    public class NonNegativeNumberAttribute : NumbericAttribueBase
    {
        protected override string GetCustomErrorMessage(decimal value, string formattedValue) => "Value must be >= 0";
        protected override bool IsValidNumber(decimal value) => value >= 0m;
    }
}
