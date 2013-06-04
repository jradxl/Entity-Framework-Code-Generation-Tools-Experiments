using EF5Fluent.Utility;
using EFUtility.CodeGenerationTools;
using Microsoft.VisualStudio.TextTemplating;
using System;
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
}
