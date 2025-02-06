namespace System.Web.Mvc
{
    public static class UrlHelperExtensions
    {
        public static string ActionStrict(this UrlHelper source, string actionName, string controllerName, object routeValues = null)
        {
            string result;

            if (routeValues == null)
                result = source.Action(actionName, controllerName);
            else
                result = source.Action(actionName, controllerName, routeValues);

            if (result == null)
                throw new Exception($"Route {controllerName}.{actionName} does not exist");

            return result;
        }
    }
}
