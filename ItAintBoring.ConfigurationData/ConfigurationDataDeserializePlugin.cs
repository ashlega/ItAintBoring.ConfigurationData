using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Runtime.Serialization;
using System.Linq;
using Microsoft.Crm.Sdk.Messages;

namespace ItAintBoring.ConfigurationData
{
    public class ConfigurationDataDeserializePlugin : IPlugin
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
                    ImportAllSettings settings = null;
                    if(context.InputParameters.Contains("Settings"))
                    {
                        string settingsData = (string)context.InputParameters["Settings"];
                        if(!String.IsNullOrEmpty(settingsData)) settings = Common.DeSerializeSettings(settingsData);
                    }
                    Entity webResource = service.Retrieve(webResourceRef.LogicalName, webResourceRef.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
                    if (webResource.Contains("content") && webResource["content"] != null)
                    {
                        var dataSet = Common.ResourceFromString((string)webResource["description"]);
                        string content = Common.GetAttribute<string>(webResource, null, "content");
                        string fetchXml;
                        string data;
                        Common.ParseContent(content, out fetchXml, out data);
                        List<Entity> entities = Common.DeSerializeEntityList(data);
                        
                        if (entities != null)
                        {
                            string lookupField = null;
                            if (!String.IsNullOrWhiteSpace(dataSet.lookupfield))
                            {
                                lookupField = dataSet.lookupfield;
                            }

                            foreach (var e in entities)
                            {
                                var metadata = ReferenceResolution.GetMetadata(context, service, e.LogicalName);
                                if(lookupField == null)
                                {
                                    lookupField = metadata.PrimaryIdAttribute; 
                                }

                                ReferenceResolution.ResolveReferences(context, service, e);

                                if (metadata.IsIntersect == null || !metadata.IsIntersect.Value)
                                {

                                    if (lookupField != metadata.PrimaryIdAttribute && !e.Contains(lookupField))
                                    {
                                        throw new InvalidPluginExecutionException("Lookup error: The entity being imported does not have '" + lookupField + "' attribute");
                                    }
                                    QueryExpression qe = new QueryExpression(e.LogicalName);
                                    qe.Criteria.AddCondition(new ConditionExpression(lookupField, ConditionOperator.Equal,
                                        lookupField == metadata.PrimaryIdAttribute ? e.Id : e[lookupField]));

                                    var existing = service.RetrieveMultiple(qe).Entities.FirstOrDefault();
                                    if (existing != null)
                                    {
                                        if (!dataSet.createonly
                                            || settings != null && settings.alwaysUpdate)
                                        {
                                            e.Id = existing.Id;
                                            service.Update(e);
                                        }
                                    }
                                    else
                                    {
                                        service.Create(e);
                                    }
                                }
                                else
                                {
                                    if (e.LogicalName == "listmember")
                                    {
                                        if (e.Contains("entityid") && e.Contains("listid"))
                                        {
                                            QueryExpression qe = new QueryExpression("listmember");
                                            qe.Criteria.AddCondition(new ConditionExpression("entityid", ConditionOperator.Equal, ((EntityReference)e["entityid"]).Id));
                                            qe.Criteria.AddCondition(new ConditionExpression("listid", ConditionOperator.Equal, ((EntityReference)e["listid"]).Id));
                                            bool exists = service.RetrieveMultiple(qe).Entities.FirstOrDefault() != null;
                                            if (!exists)
                                            {
                                                AddMemberListRequest amlr = new AddMemberListRequest();
                                                amlr.EntityId = ((EntityReference)e["entityid"]).Id;
                                                amlr.ListId = ((EntityReference)e["listid"]).Id;
                                                service.Execute(amlr);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        foreach (var r in metadata.ManyToManyRelationships)
                                        {
                                            if (r.IntersectEntityName == e.LogicalName)
                                            {

                                                if (e.Contains(r.Entity1IntersectAttribute)
                                                    && e.Contains(r.Entity2IntersectAttribute)
                                                    )
                                                {
                                                    QueryExpression qe = new QueryExpression(r.IntersectEntityName);
                                                    qe.Criteria.AddCondition(new ConditionExpression(r.Entity1IntersectAttribute, ConditionOperator.Equal, (Guid)e[r.Entity1IntersectAttribute]));
                                                    qe.Criteria.AddCondition(new ConditionExpression(r.Entity2IntersectAttribute, ConditionOperator.Equal, (Guid)e[r.Entity2IntersectAttribute]));
                                                    bool exists = service.RetrieveMultiple(qe).Entities.FirstOrDefault() != null;
                                                    if (!exists)
                                                    {
                                                        Relationship rs = new Relationship(r.SchemaName);
                                                        EntityReferenceCollection collection = new EntityReferenceCollection();

                                                        collection.Add(new EntityReference(r.Entity2IntersectAttribute)
                                                        {
                                                            Id = (Guid)e[r.Entity2IntersectAttribute]
                                                        });

                                                        service.Associate(r.Entity1LogicalName,
                                                            (Guid)e[r.Entity1IntersectAttribute],
                                                            rs,
                                                            collection);
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        var updatedResource = new Entity(webResource.LogicalName);
                        updatedResource.Id = webResource.Id;
                        dataSet.appliedon = Common.CurrentTime();
                        dataSet.appliedin = context.OrganizationId.ToString().Replace("{", "").Replace("}", "");
                        updatedResource["description"] = Common.ResourceToString(dataSet);
                        service.Update(updatedResource);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException(ex.Message);
            }
        }
    }
}