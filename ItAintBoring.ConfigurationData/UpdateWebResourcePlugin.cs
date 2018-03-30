using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace ItAintBoring.ConfigurationData
{
    
    public class UpdateWebResourcePlugin: IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (context.InputParameters.Contains("description") && context.InputParameters.Contains("id"))
            {
                IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
                try
                {
                    string description = (string)context.InputParameters["description"];
                    Guid id = Guid.Parse((string)context.InputParameters["id"]);
                    Entity entity = service.Retrieve("webresource", id, new Microsoft.Xrm.Sdk.Query.ColumnSet("webresourceid", "content", "description"));
                    if (description.Contains("fetchxml") && description.Contains("ita_configuration_data"))
                    {
                        var resource = Common.ResourceFromString(description);
                        string content = Common.GetAttribute<string>(entity, null, "content");
                        string fetchXml;
                        string data;
                        Common.ParseContent(content, out fetchXml, out data);
                        string updatedContent = Common.PackContent(resource.fetchxml, data);
                        entity["content"] = updatedContent;
                        resource.fetchxml = "";
                        //resource.modifiedon = Common.CurrentTime();
                        entity["description"] = Common.ResourceToString(resource);
                        service.Update(entity);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException(ex.Message);
                }
            }
        }
    }
    
}

