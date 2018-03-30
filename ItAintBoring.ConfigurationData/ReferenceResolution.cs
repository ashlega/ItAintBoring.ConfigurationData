using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace ItAintBoring.ConfigurationData
{
    public class ReferenceResolution
    {

        public static EntityMetadata GetMetadata(IPluginExecutionContext context, IOrganizationService service, string logicalName)
        {
            string keyName = logicalName + "_meta"; 
            if(!context.SharedVariables.Contains(keyName))
            {
                RetrieveEntityRequest request = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Entity | EntityFilters.Relationships,
                    LogicalName = logicalName
                };
                RetrieveEntityResponse response = (RetrieveEntityResponse)service.Execute(request);
                context.SharedVariables[keyName] = response.EntityMetadata;
            }
            return (EntityMetadata)context.SharedVariables[keyName];
        }

        public static bool RecordExists(IPluginExecutionContext context, IOrganizationService service, EntityReference er)
        {
            bool result = false;
            var metadata = GetMetadata(context, service, er.LogicalName);
            QueryExpression qe = new QueryExpression(er.LogicalName);
            qe.Criteria.AddCondition(new ConditionExpression(metadata.PrimaryIdAttribute, ConditionOperator.Equal, er.Id));
            result = service.RetrieveMultiple(qe).Entities.FirstOrDefault() != null;
            return result;
        }

        public static void ResolveReferences(IPluginExecutionContext context, IOrganizationService service, Entity entity)
        {
            var metadata = GetMetadata(context, service, entity.LogicalName);
            if (entity.Contains("businessunitid") && entity["businessunitid"] != null) //it's there and it's not being set to null
            {
                entity["businessunitid"] = new EntityReference("businessunit", context.BusinessUnitId);
            }

            List<string> removeAttributes = new List<string>();
            foreach(var a in entity.Attributes)
            {
                
                if (a.Key != "businessunitid" //can't update the attribute here since it's a for loop
                    && a.Value is EntityReference
                    && !RecordExists(context, service, (EntityReference)a.Value))
                {

                    removeAttributes.Add(a.Key);
                }
            }

            foreach(var key in removeAttributes)
            {
                entity.Attributes.Remove(key);
            }
        }
        
    }
}
