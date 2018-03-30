using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using System.Runtime.Serialization;

namespace ItAintBoring.ConfigurationData
{
    /*
    public class DataSerializerPlugin : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);


            Entity target = (Entity)context.InputParameters["Target"];
            Entity entity = service.Retrieve(target.LogicalName, target.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
            string data = Common.SerializeEntity(entity);
            target["new_serializeddata"] = Common.CompressString(data);
            return;

            Entity deserialized = Common.DeSerializeEntity(data);
            Entity copy = new Entity(deserialized.LogicalName);
            foreach (var a in deserialized.Attributes)
            {
                if (a.Key != "state" && a.Key != "statuscode" && !(a.Value is Guid))
                {
                    copy.Attributes.Add(a);
                }
            }
            
            service.Create(copy);
                
        }
    }
    */
}
