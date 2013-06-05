using EF5Fluent.Utility;
using EFUtility.CodeGenerationTools;
using Microsoft.VisualStudio.TextTemplating;
using System;
using System.Collections.Generic;
using System.Data.Mapping;
using System.Data.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CodeGenerationToolsExperiments
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Entity Frameworks CodeGenerationTools Demo Program.\n");

            //I had problems with accessing Dictionary by [] index, so quickly used these
            //to check in normal C# usage.
            //var tc = new TestCollections();
            //tc.TestCollections2();

            try
            {
                //As this program only reads and interprets the XML within the EDMX,
                //there is no need for a ConnectionString in the App.Config and
                //no connection is made to a database.

                //Since this is a test program, let's assume you are executing from .\bin\Debug
                var currentdirectory = Environment.CurrentDirectory;
                var inputFile = @"..\..\Model1.edmx";
                inputFile = Path.Combine(currentdirectory, inputFile);

                //Demo 1
                //EFFluentUtility is from
                //http://visualstudiogallery.msdn.microsoft.com/5d663b99-ed3b-481d-b7bc-b947d2457e3c
                //By opening the VSIX (DbContextFluentTemplate_V50, which is a ZIP) I've taken a 
                //copy of "CSharpDbContextFluent.Mapping.tt" from the ItemTemplate.
                //Then copied the supporting classes from the template and
                //entered here in the CodeGenerationToolsLibrary project as a .Net Class here.
                //I've used the Mapping.tt because it is the most complex,
                //and shows how to obtain the Mapping info used to
                //create the EntityConfiguration from the EDMX.
                //
                // See PropertyToColumnMapping and ManyToManyMappings from
                // MetadataLoadResult
                //
                EF5FluentUtility efu = new EF5FluentUtility();
                MetadataLoadResult mlr = efu.LoadMetadata(inputFile);

                var a = mlr.EdmItems;
                var b = mlr.ManyToManyMappings;
                var c = mlr.PropertyToColumnMapping;
                var d = mlr.TphMappings;

                //Obtain the EntityContainer from the ItemsCollection
                var container = mlr.EdmItems.GetItems<EntityContainer>()[0];

                //Iterate all the EntitySets and Properties
                //EntitySet are as in "People", where the EntityType is "Person"
                foreach (var entityset in container.BaseEntitySets.OfType<EntitySet>().OrderBy(e => e.Name))
                {
                    var entitysetname = entityset.Name;
                    var entityname = entityset.ElementType.Name;
                    var entitytype = entityset.ElementType;
                    var declaringtype = entityset.ElementType.NavigationProperties[0].DeclaringType;

                    //Navigation Properties for a particular Entity
                    var collectionNavigations1 = entityset.ElementType.NavigationProperties.Where(np => np.DeclaringType == entityset.ElementType);

                    // Find m:m relationshipsto configure for a particular Entity
                    var manyManyRelationships = entityset.ElementType.NavigationProperties
                        .Where(np =>
                            //np.DeclaringType is EntityType &&
                            np.ToEndMember.RelationshipMultiplicity == RelationshipMultiplicity.Many
                            && np.FromEndMember.RelationshipMultiplicity == RelationshipMultiplicity.Many
                                // Ensures we only configure from one end.
                                // Convention is to have source on the left, as it would be diagrammatically.
                            && np.RelationshipType.RelationshipEndMembers.First() == np.FromEndMember)
                        .ToArray();

                    //Now process the M:Ms
                    foreach (var navProperty in manyManyRelationships)
                    {
                        var otherNavProperty = navProperty.ToEndMember.GetEntityType().NavigationProperties.Where(n => n.RelationshipType == navProperty.RelationshipType && n != navProperty).Single();
                        var association = (AssociationType)navProperty.RelationshipType;

                        //This did not work in the T4 Template.
                        var mapping1 = mlr.ManyToManyMappings[association];

                        var mapping2 = mlr.ManyToManyMappings.Where(m => m.Key.Name == association.Name).FirstOrDefault().Value;
                        var item1 = mapping1.Item1;

                        //This did not work in the T4 Template.
                        var leftKeyMappings1 = mapping1.Item2[navProperty.ToEndMember];
                        var leftKeyMappings2 = mapping1.Item2.Where(np => np.Key.Name == navProperty.ToEndMember.Name).FirstOrDefault().Value;

                        // Need to ensure that FKs are declared in the same order as the PK properties on each principal type
                        var leftType = (EntityType)navProperty.DeclaringType;
                        var rightType = (EntityType)otherNavProperty.DeclaringType;

                        //Access using Index [navProperty.FromEndMember] did not work within template.
                        var leftKeyMappings = mapping1.Item2.Where(np => np.Key.Name == navProperty.FromEndMember.Name).FirstOrDefault().Value; //[navProperty.FromEndMember];
                        var rightKeyMappings = mapping1.Item2.Where(np => np.Key.Name == otherNavProperty.FromEndMember.Name).FirstOrDefault().Value; //[otherNavProperty.FromEndMember];

                        var left = leftKeyMappings.Where(km => km.Key.Name == "PersonID");
                        var right = rightType.KeyMembers.Select(m => "\"" + rightKeyMappings.Where(km => km.Key.Name == m.Name).FirstOrDefault().Value + "\"").FirstOrDefault();
                    }

                    //Dual OrderBy to bring Keys to top of list
                    //Iterate all properties for this particular Entity.
                    var properties = entityset.ElementType.Properties.OrderBy(s => s.Name).OrderBy(p => entityset.ElementType.KeyMembers.Contains(p) == false).ToArray();
                    foreach (var property in properties)
                    {
                        var propertyname = property.Name;

                        //This did not work in the T4 Template.
                        var mapping1 = mlr.PropertyToColumnMapping[entityset.ElementType];

                        //PropertyToColumnMapping is a collection for ALL Entities, so here the particular Entity needs to be filtered out.
                        //Mapping returned is a complex generic Dictionary.
                        var mapping2 = mlr.PropertyToColumnMapping.Where(k => k.Key == entityset.ElementType).FirstOrDefault().Value;
                        var mapping3 = mlr.PropertyToColumnMapping.Where(k => k.Key.FullName == entityset.ElementType.FullName).FirstOrDefault().Value;

                        var mapping4 = mapping3.Item2.Where(p => p.Key.Name == property.Name).FirstOrDefault().Value;

                        var i1 = mapping1.Item1;
                        var i2 = mapping1.Item2;
                        var mapright = i2[property];
                    }

                }

                //********************************************************************************

                //Demo 2
                //This takes EF.Utility.CS.ttinclude and receates as
                //.net Classes.
                //
                MyTextTransformation tt = new MyTextTransformation();
                CodeGenerationTools code = new CodeGenerationTools(tt);

                CodeRegion cregion = new CodeRegion(tt, 1);
                MetadataTools mtool = new MetadataTools(tt);
                MetadataLoader mloader = new MetadataLoader(tt);

                var ItemCollection = mloader.CreateEdmItemCollection(inputFile);

                var Container = ItemCollection.GetItems<EntityContainer>()[0];
                Console.WriteLine(String.Format("Container Name: {0}", Container.Name));
                Console.WriteLine(String.Format("Model Namespace: {0}", mloader.GetModelNamespace(inputFile)));
                Console.WriteLine(String.Format("No Of Items in ItemsCollection: {0}\n", ItemCollection.Count));
                Console.WriteLine("Press any key to continue.\n"); Console.ReadKey();

                foreach (var i in ItemCollection) { Console.WriteLine(String.Format("Item: {0}, {1}", i.ToString(), i.BuiltInTypeKind.ToString())); }
                Console.WriteLine("Press any key to continue.\n"); Console.ReadKey();

                EdmItemCollection edmItemCollection = null; ;
                var ret = mloader.TryCreateEdmItemCollection(inputFile, out edmItemCollection);

                StoreItemCollection storeItemCollection = null;
                ret = mloader.TryCreateStoreItemCollection(inputFile, out storeItemCollection);

                StorageMappingItemCollection storageMappingItemCollection = null;
                ret = mloader.TryCreateStorageMappingItemCollection(inputFile, edmItemCollection, storeItemCollection, out storageMappingItemCollection);
                foreach (var i in storageMappingItemCollection) { Console.WriteLine(String.Format("Item: {0}, {1}", i.ToString(), i.BuiltInTypeKind.ToString())); }
                DataSpace ds = storageMappingItemCollection.DataSpace;

                MetadataWorkspace metadataWorkspace = null;
                ret = mloader.TryLoadAllMetadata(inputFile, out metadataWorkspace);

                //Get the schema "dbo" from a particular Entity.
                EntityContainer ec = storeItemCollection.GetItems<EntityContainer>().First();
                EntitySet eset = ec.GetEntitySetByName(code.Escape("People"), true);
                string schemaName1 = eset.MetadataProperties["Schema"].Value.ToString();

                //Get the schema "dbo" from any Entity. I guess the schema will be same for all?!
                EntitySetBase fes = ec.BaseEntitySets.FirstOrDefault();
                string schemaName2 = fes.MetadataProperties["Schema"].Value.ToString();

                var edmxSchema = EDMXchema.GetSchemaElement(inputFile);

                Console.WriteLine("Press any key to continue.\n"); Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("Press any key to quit.");
            Console.ReadKey();

            // var loadResult = LoadMetadata(inputFile);
        }
    }


    public class MyTextTransformation : TextTransformation
    {

        public override string TransformText()
        {
            // throw new System.NotImplementedException();
            return string.Empty;
        }
    }

    public static class EDMXchema
    {
        /// <summary>
        /// It returns root element of conceptual model (edmx file).
        /// Not present in EF.Utility.CS.ttinclude, so recreated here. There is probably a better way!
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        public static XElement GetSchemaElement(string sourcePath)
        {

            if (String.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("sourcePath");
            }

            //string edmxNS = @"http://schemas.microsoft.com/ado/2008/10/edmx";
            string edmxNS = @"http://schemas.microsoft.com/ado/2009/11/edmx";
            XElement root = XElement.Load(sourcePath);
            XElement csdl = root.Descendants(XName.Get("ConceptualModels", edmxNS)).FirstOrDefault();

            return csdl;
        }
    }

    public class TestCollections
    {
        public void TestCollections1()
        {
            List<string> dinosaurs = new List<string>();

            Console.WriteLine("\nCapacity: {0}", dinosaurs.Capacity);

            dinosaurs.Add("Tyrannosaurus");
            dinosaurs.Add("Amargasaurus");
            dinosaurs.Add("Mamenchisaurus");
            dinosaurs.Add("Deinonychus");
            dinosaurs.Add("Compsognathus");

            Console.WriteLine();
            foreach (string dinosaur in dinosaurs)
            {
                Console.WriteLine(dinosaur);
            }

            Console.WriteLine("\nCapacity: {0}", dinosaurs.Capacity);
            Console.WriteLine("Count: {0}", dinosaurs.Count);

            var third = dinosaurs[2];

            Console.WriteLine("\nContains(\"Deinonychus\"): {0}",
                dinosaurs.Contains("Deinonychus"));

            Console.WriteLine("\nInsert(2, \"Compsognathus\")");
            dinosaurs.Insert(2, "Compsognathus");

            Console.WriteLine();
            foreach (string dinosaur in dinosaurs)
            {
                Console.WriteLine(dinosaur);
            }

            Console.WriteLine("\ndinosaurs[3]: {0}", dinosaurs[3]);

            Console.WriteLine("\nRemove(\"Compsognathus\")");
            dinosaurs.Remove("Compsognathus");

            Console.WriteLine();
            foreach (string dinosaur in dinosaurs)
            {
                Console.WriteLine(dinosaur);
            }

            dinosaurs.TrimExcess();
            Console.WriteLine("\nTrimExcess()");
            Console.WriteLine("Capacity: {0}", dinosaurs.Capacity);
            Console.WriteLine("Count: {0}", dinosaurs.Count);

            dinosaurs.Clear();
            Console.WriteLine("\nClear()");
            Console.WriteLine("Capacity: {0}", dinosaurs.Capacity);
            Console.WriteLine("Count: {0}", dinosaurs.Count);
        }

        public void TestCollections2()
        {
            // Create a new dictionary of strings, with string keys. 
            //
            Dictionary<string, string> openWith = new Dictionary<string, string>();

            // Add some elements to the dictionary. There are no  
            // duplicate keys, but some of the values are duplicates.
            openWith.Add("txt", "notepad.exe");
            openWith.Add("bmp", "paint.exe");
            openWith.Add("dib", "paint.exe");
            openWith.Add("rtf", "wordpad.exe");

            var key = "xrtf";
            //var third = openWith[key];
            var third2 = openWith.Where(k => k.Key == key).FirstOrDefault().Value;

            // The Add method throws an exception if the new key is  
            // already in the dictionary. 
            try
            {
                openWith.Add("txt", "winword.exe");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("An element with Key = \"txt\" already exists.");
            }

            // The Item property is another name for the indexer, so you  
            // can omit its name when accessing elements. 
            Console.WriteLine("For key = \"rtf\", value = {0}.",
                openWith["rtf"]);

            // The indexer can be used to change the value associated 
            // with a key.
            openWith["rtf"] = "winword.exe";
            Console.WriteLine("For key = \"rtf\", value = {0}.",
                openWith["rtf"]);

            // If a key does not exist, setting the indexer for that key 
            // adds a new key/value pair.
            openWith["doc"] = "winword.exe";

            // The indexer throws an exception if the requested key is 
            // not in the dictionary. 
            try
            {
                Console.WriteLine("For key = \"tif\", value = {0}.",
                    openWith["tif"]);
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine("Key = \"tif\" is not found.");
            }

            // When a program often has to try keys that turn out not to 
            // be in the dictionary, TryGetValue can be a more efficient  
            // way to retrieve values. 
            string value = "";
            if (openWith.TryGetValue("tif", out value))
            {
                Console.WriteLine("For key = \"tif\", value = {0}.", value);
            }
            else
            {
                Console.WriteLine("Key = \"tif\" is not found.");
            }

            // ContainsKey can be used to test keys before inserting  
            // them. 
            if (!openWith.ContainsKey("ht"))
            {
                openWith.Add("ht", "hypertrm.exe");
                Console.WriteLine("Value added for key = \"ht\": {0}",
                    openWith["ht"]);
            }

            // When you use foreach to enumerate dictionary elements, 
            // the elements are retrieved as KeyValuePair objects.
            Console.WriteLine();
            foreach (KeyValuePair<string, string> kvp in openWith)
            {
                Console.WriteLine("Key = {0}, Value = {1}",
                    kvp.Key, kvp.Value);
            }

            // To get the values alone, use the Values property.
            Dictionary<string, string>.ValueCollection valueColl =
                openWith.Values;

            // The elements of the ValueCollection are strongly typed 
            // with the type that was specified for dictionary values.
            Console.WriteLine();
            foreach (string s in valueColl)
            {
                Console.WriteLine("Value = {0}", s);
            }

            // To get the keys alone, use the Keys property.
            Dictionary<string, string>.KeyCollection keyColl =
                openWith.Keys;

            // The elements of the KeyCollection are strongly typed 
            // with the type that was specified for dictionary keys.
            Console.WriteLine();
            foreach (string s in keyColl)
            {
                Console.WriteLine("Key = {0}", s);
            }

            // Use the Remove method to remove a key/value pair.
            Console.WriteLine("\nRemove(\"doc\")");
            openWith.Remove("doc");

            if (!openWith.ContainsKey("doc"))
            {
                Console.WriteLine("Key \"doc\" is not found.");
            }
        }
    }
}
