using System.Web.Mvc;

namespace nCredit.Code
{
    public class CommaAndDotDecimalModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == null)
                return base.BindModel(controllerContext, bindingContext);

            if (bindingContext.ModelType == typeof(decimal?) && string.IsNullOrWhiteSpace(valueProviderResult?.AttemptedValue))
                return new decimal?();

            decimal d;
            if (!decimal.TryParse(valueProviderResult.AttemptedValue?.Replace(",", ".")?.Trim(), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out d))
            {
                return base.BindModel(controllerContext, bindingContext);
            }
            return d;
        }
    }
}