using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Linq;
using System.Runtime.Serialization;

namespace ItAintBoring.ConfigurationData
{
    public class ConfigurationDataSerializePlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            try
            {
                
                if (context.InputParameters.Contains("WebResource"))
                {
                    EntityReference webResourceRef = (EntityReference)context.InputParameters["WebResource"];
                    Entity webResource = service.Retrieve(webResourceRef.LogicalName, webResourceRef.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                    
                    if (webResource.Contains("description") && webResource["description"] != null)
                    {
                        string description = (string)webResource["description"];
                        var resource = Common.ResourceFromString(description);

                        string content = Common.GetAttribute<string>(webResource, null, "content");
                        string fetchXml;
                        string data;
                        Common.ParseContent(content, out fetchXml, out data);
                        if (String.IsNullOrEmpty(fetchXml))
                        {
                            fetchXml = resource.fetchxml;
                        }

                        var fe = new FetchExpression(fetchXml);
                        var result = service.RetrieveMultiple(fe).Entities.ToList();
                        var updatedResource = new Entity(webResource.LogicalName);
                        updatedResource.Id = webResource.Id;
                        updatedResource["content"] = Common.PackContent(fetchXml, Common.SerializeEntityList(result));
                        resource.modifiedon = Common.CurrentTime();
                        updatedResource["description"] = Common.ResourceToString(resource);
                        service.Update(updatedResource);
                        
                    }
                    /*
                    string fetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'>
  <entity name='account'>
    <all-attributes/>
    
    <order attribute='name' descending='false' />
    <filter type='and'>
      <condition attribute='accountid' operator='in'>
        <value uiname='Client 1' uitype='account'>{0DE2A620-15F4-E711-93FE-00155D00C101}</value>
        <value uiname='Client 2' uitype='account'>{12E2A620-15F4-E711-93FE-00155D00C101}</value>
      </condition>
    </filter>
  </entity>
</fetch>";
*/

                    

                }
            }
            catch(Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
                
        }
    }
}