using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace RdtClient.Web.Controllers;

[AttributeUsage(AttributeTargets.Method)]
public class SabnzbdModeAttribute(String mode) : Attribute, IActionConstraint
{
    public Int32 Order => 0;

    public Boolean Accept(ActionConstraintContext context)
    {
        var request = context.RouteContext.HttpContext.Request;
        
        String? modeValue = request.Query["mode"];

        if (String.IsNullOrWhiteSpace(modeValue) && request.HasFormContentType)
        {
            modeValue = request.Form["mode"];
        }

        return String.Equals(modeValue, mode, StringComparison.OrdinalIgnoreCase);
    }
}
