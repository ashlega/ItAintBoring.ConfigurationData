using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Runtime.Serialization;
using System.Linq;

namespace ItAintBoring.ConfigurationData
{
    public class WebResourcePlugin : IPlugin
    {


        public void ProcessEntity(IOrganizationService service, Entity entity)
        {
            if (entity.Contains("description"))
            {

                string description = (string)entity["description"];
                if (description.Contains("fetchxml") && description.Contains("ita_configuration_data"))
                {
                    var dataSet = Common.ResourceFromString(description);
                    Entity resource = service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet("content"));
                    string content = Common.GetAttribute<string>(resource, null, "content");
                    string fetchXml;
                    string data;
                    Common.ParseContent(content, out fetchXml, out data);
                    if (fetchXml != null && fetchXml != "")
                    {
                        dataSet.fetchxml = fetchXml;
                        entity["description"] = Common.ResourceToString(dataSet);
                    }

                }
            }
        }
        
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.Depth > 1) return;

            try
            {
                if(context.MessageName == "Retrieve")
                {

                    Entity entity = (Entity)context.OutputParameters["BusinessEntity"];
                    ProcessEntity(service, entity);

                }
                else if (context.MessageName == "RetrieveMultiple")
                {

                    EntityCollection entityCollection = (EntityCollection)context.OutputParameters["BusinessEntityCollection"];
                    var badEntityList = new List<Entity>();
                    foreach (var entity in entityCollection.Entities)
                    {
                        try
                        {
                            ProcessEntity(service, entity);
                        }
                        catch(Exception ex)
                        {
                            badEntityList.Add(entity);
                        }
                    }
                    badEntityList.ForEach(e => entityCollection.Entities.Remove(e));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
  
    }
    
    
}
