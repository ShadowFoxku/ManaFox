using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ManaFox.Hosting.Middleware.Conventions
{
    public class ApplyFromBodyConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            foreach (var parameter in action.Parameters.Where(p => p.BindingInfo == null))
            {
                parameter.BindingInfo ??= new BindingInfo();
                parameter.BindingInfo.BindingSource = BindingSource.Body;
            }
        }
    }
}
