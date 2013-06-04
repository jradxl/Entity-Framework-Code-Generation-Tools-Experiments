
/// ****************************************************************************
/// <summary>
/// From http://visualstudiogallery.msdn.microsoft.com/5d663b99-ed3b-481d-b7bc-b947d2457e3c
/// with many thanks.
/// The supporting classes from CSharpDbContextFluent.Mapping.tt template
/// have been entered here with very little modification.
/// Jsrsoft, June 2013
/// </summary>
///  ****************************************************************************

using EFUtility.CodeGenerationTools;
using Microsoft.Data.Entity.Design.DatabaseGeneration;
using System;
using System.Collections.Generic;
using System.Data.Mapping;
using System.Data.Metadata.Edm;
using System.Linq;
using System.Xml;

namespace EF5Fluent.Utility
{
    public class EF5FluentUtility
    {
        public MetadataLoadResult LoadMetadata(string inputFile)
        {
            var loader = new MetadataLoader(this);
            bool loaded = false;
            EdmItemCollection edmItemCollection = loader.CreateEdmItemCollection(inputFile);
            StoreItemCollection storeItemCollection = null;
            if (loader.TryCreateStoreItemCollection(inputFile, out storeItemCollection))
            {
                StorageMappingItemCollection storageMappingItemCollection;
                if (loader.TryCreateStorageMappingItemCollection(inputFile, edmItemCollection, storeItemCollection, out storageMappingItemCollection))
                    loaded = true;
            }

            if (loaded == false)
                throw new Exception("Cannot load a metadata from the file " + inputFile);

            var mappingMetadata = LoadMappingMetadata(inputFile);
            var mappingNode = mappingMetadata.Item1;
            var nsmgr = mappingMetadata.Item2;

            var allEntitySets = storeItemCollection.GetAllEntitySets();

            return new MetadataLoadResult
                      {
                          EdmItems = edmItemCollection,
                          PropertyToColumnMapping = BuildEntityMappings(mappingNode, nsmgr, edmItemCollection.GetItems<EntityType>(), edmItemCollection.GetAllEntitySets(), allEntitySets),
                          ManyToManyMappings = BuildManyToManyMappings(mappingNode, nsmgr, edmItemCollection.GetAllAssociationSets(), allEntitySets),
                          TphMappings = BuildTPHMappings(mappingNode, nsmgr, edmItemCollection.GetItems<EntityType>(), edmItemCollection.GetAllEntitySets(), allEntitySets)
                      };
        }

        public string ToTable(EntitySet entitySet)
        {
            var toTable = entitySet.Name;
            string schema = entitySet.GetSchemaName();
            if (!string.IsNullOrWhiteSpace(schema) && schema != "dbo")
                toTable += "\", \"" + schema;
            return toTable;
        }

        public string GetGenerationOption(EdmProperty property, EntityType entity)
        {
            string result = "";
            bool isPk = entity.KeyMembers.Contains(property);
            MetadataProperty storeGeneratedPatternProperty = null;
            string storeGeneratedPatternPropertyValue = "None";

            if (property.MetadataProperties.TryGetValue(MetadataConstants.EDM_ANNOTATION_09_02 + ":StoreGeneratedPattern", false, out storeGeneratedPatternProperty))
                storeGeneratedPatternPropertyValue = storeGeneratedPatternProperty.Value.ToString();

            PrimitiveType edmType = (PrimitiveType)property.TypeUsage.EdmType;
            if (edmType == null && property.TypeUsage.EdmType is EnumType)
            {
                EnumType enumType = property.TypeUsage.EdmType as EnumType;
                edmType = enumType.UnderlyingType;
            }
            if (storeGeneratedPatternPropertyValue == "Computed")
            {
                result = ".HasDatabaseGeneratedOption(new Nullable<DatabaseGeneratedOption>(DatabaseGeneratedOption.Computed))";
            }
            else if ((edmType.ClrEquivalentType == typeof(int)) || (edmType.ClrEquivalentType == typeof(short)) || (edmType.ClrEquivalentType == typeof(long)))
            {
                if (isPk && (storeGeneratedPatternPropertyValue != "Identity"))
                    result = ".HasDatabaseGeneratedOption(new Nullable<DatabaseGeneratedOption>(DatabaseGeneratedOption.None))";
                else if ((!isPk || (entity.KeyMembers.Count > 1)) && (storeGeneratedPatternPropertyValue == "Identity"))
                    result = ".HasDatabaseGeneratedOption(new Nullable<DatabaseGeneratedOption>(DatabaseGeneratedOption.Identity))";
            }
            return result;
        }

        private Tuple<XmlNode, XmlNamespaceManager> LoadMappingMetadata(string inputFile)
        {
            var xmlDoc = new XmlDocument();
            //xmlDoc.Load(Host.ResolvePath(inputFile));
            xmlDoc.Load(inputFile);

            var schemaConstantsList = new SchemaConstants[]
                            {
                                MetadataConstants.V3_SCHEMA_CONSTANTS,
                                MetadataConstants.V2_SCHEMA_CONSTANTS,
                                MetadataConstants.V1_SCHEMA_CONSTANTS,
                            };
            foreach (var schemaConstants in schemaConstantsList)
            {
                var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("ef", schemaConstants.MslNamespace);
                nsmgr.AddNamespace("edmx", schemaConstants.EdmxNamespace);
                var mappingNode = xmlDoc.DocumentElement.SelectSingleNode("./*/edmx:Mappings", nsmgr);

                if (mappingNode != null)
                    return Tuple.Create(mappingNode, nsmgr);
            }

            throw new Exception(CodeGenerationTools.GetResourceString("Template_UnsupportedSchema"));
        }

        private Dictionary<EntityType, Dictionary<EntityType, Dictionary<EdmProperty, string>>> BuildTPHMappings(XmlNode mappingNode, XmlNamespaceManager nsmgr, IEnumerable<EntityType> entityTypes, IEnumerable<EntitySet> entitySets, IEnumerable<EntitySet> tableSets)
        {
            var dictionary = new Dictionary<EntityType, Dictionary<EntityType, Dictionary<EdmProperty, string>>>();

            foreach (EntitySet set in entitySets)
            {
                XmlNodeList nodes = mappingNode.SelectNodes(string.Format(".//ef:EntitySetMapping[@Name=\"{0}\"]/ef:EntityTypeMapping/ef:MappingFragment", set.Name), nsmgr);
                foreach (XmlNode node in nodes)
                {
                    string typeName = node.ParentNode.Attributes["TypeName"].Value;
                    if (typeName.StartsWith("IsTypeOf("))
                        typeName = typeName.Substring("IsTypeOf(".Length, typeName.Length - "IsTypeOf()".Length);
                    EntityType type = entityTypes.Single(z => z.FullName == typeName);
                    string tableName = node.Attributes["StoreEntitySet"].Value;
                    EntitySet set2 = tableSets.Single(entitySet => entitySet.Name == tableName);
                    var entityMap = new Dictionary<EdmProperty, string>();

                    XmlNodeList propertyNodes = node.SelectNodes("./ef:Condition", nsmgr);
                    if (propertyNodes.Count == 0) continue;
                    foreach (XmlNode propertyNode in propertyNodes)
                    {
                        string str = propertyNode.Attributes["ColumnName"].Value;
                        EdmProperty property2 = set2.ElementType.Properties[str];
                        string val = propertyNode.Attributes["Value"].Value;
                        entityMap.Add(property2, val);
                    }
                    EntityType baseType = (EntityType)(type.BaseType ?? type);
                    if (!dictionary.Keys.Contains(baseType))
                    {
                        var entityMappings = new Dictionary<EntityType, Dictionary<EdmProperty, string>>();
                        //entityMappings.Add(type,entityMap);
                        dictionary.Add(baseType, entityMappings);
                    }
                    dictionary[baseType].Add(type, entityMap);
                }
            }
            return dictionary;
        }

        private Dictionary<EntityType, Tuple<EntitySet, Dictionary<EdmProperty, EdmProperty>>> BuildEntityMappings(XmlNode mappingNode, XmlNamespaceManager nsmgr, IEnumerable<EntityType> entityTypes, IEnumerable<EntitySet> entitySets, IEnumerable<EntitySet> tableSets)
        {
            var dictionary = new Dictionary<EntityType, Tuple<EntitySet, Dictionary<EdmProperty, EdmProperty>>>();

            foreach (EntitySet set in entitySets)
            {
                XmlNodeList nodes = mappingNode.SelectNodes(string.Format(".//ef:EntitySetMapping[@Name=\"{0}\"]/ef:EntityTypeMapping/ef:MappingFragment", set.Name), nsmgr);
                foreach (XmlNode node in nodes)
                {
                    string typeName = node.ParentNode.Attributes["TypeName"].Value;
                    if (typeName.StartsWith("IsTypeOf("))
                        typeName = typeName.Substring("IsTypeOf(".Length, typeName.Length - "IsTypeOf()".Length);
                    EntityType type = entityTypes.Single(z => z.FullName == typeName);
                    string tableName = node.Attributes["StoreEntitySet"].Value;
                    EntitySet set2 = tableSets.Single(entitySet => entitySet.Name == tableName);
                    var entityMap = new Dictionary<EdmProperty, EdmProperty>();
                    foreach (EdmProperty property in type.Properties)
                    {
                        XmlNode propertyNode = node.SelectSingleNode(string.Format("./ef:ScalarProperty[@Name=\"{0}\"]", property.Name), nsmgr);
                        if (propertyNode == null) continue;
                        string str = propertyNode.Attributes["ColumnName"].Value;
                        EdmProperty property2 = set2.ElementType.Properties[str];
                        entityMap.Add(property, property2);
                    }
                    dictionary.Add(type, Tuple.Create(set2, entityMap));
                }
            }
            return dictionary;
        }

        private Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>> BuildManyToManyMappings(XmlNode mappingNode, XmlNamespaceManager nsmgr, IEnumerable<AssociationSet> associationSets, IEnumerable<EntitySet> tableSets)
        {
            var dictionary = new Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>>();
            foreach (AssociationSet associationSet in associationSets.Where(set => set.ElementType.IsManyToMany()))
            {

                XmlNode node = mappingNode.SelectSingleNode(string.Format("//ef:AssociationSetMapping[@Name=\"{0}\"]", associationSet.Name), nsmgr);
                string tableName = node.Attributes["StoreEntitySet"].Value;
                EntitySet entitySet = tableSets.Single(s => s.Name == tableName);

                var relationEndMap = new Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>();
                foreach (AssociationSetEnd end in associationSet.AssociationSetEnds)
                {
                    var map = new Dictionary<EdmMember, string>();
                    foreach (XmlNode endProperty in node.SelectSingleNode(string.Format("./ef:EndProperty[@Name=\"{0}\"]", end.Name), nsmgr).ChildNodes)
                    {
                        string str = endProperty.Attributes["Name"].Value;
                        EdmProperty key = end.EntitySet.ElementType.Properties[str];
                        string str2 = endProperty.Attributes["ColumnName"].Value;
                        map.Add(key, str2);
                    }
                    relationEndMap.Add(end.CorrespondingAssociationEndMember, map);
                }
                dictionary.Add(associationSet.ElementType, Tuple.Create(entitySet, relationEndMap));
            }
            return dictionary;
        }
    }

    public class MetadataLoadResult
    {
        public EdmItemCollection EdmItems { get; set; }
        public Dictionary<EntityType, Tuple<EntitySet, Dictionary<EdmProperty, EdmProperty>>> PropertyToColumnMapping { get; set; }
        public Dictionary<AssociationType, Tuple<EntitySet, Dictionary<RelationshipEndMember, Dictionary<EdmMember, string>>>> ManyToManyMappings { get; set; }
        public Dictionary<EntityType, Dictionary<EntityType, Dictionary<EdmProperty, string>>> TphMappings { get; set; }
    }
}
