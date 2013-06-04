using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity.Design;
using System.Data.Mapping;
using System.Data.Metadata.Edm;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace EFUtility.CodeGenerationTools
{

    /// <summary>
    /// Responsible for loading an EdmItemCollection from a .edmx file or .csdl files
    /// </summary>
    public class MetadataLoader
    {
        private static Dictionary<string, string> TemplateMetadata = new Dictionary<string, string>();

        private readonly DynamicTextTransformation _textTransformation;

        private void DefineMetadata()
        {
            TemplateMetadata[MetadataConstants.TT_TEMPLATE_NAME] = "CSharpDbContext.Types";
            TemplateMetadata[MetadataConstants.TT_TEMPLATE_VERSION] = "5.0";
            TemplateMetadata[MetadataConstants.TT_MINIMUM_ENTITY_FRAMEWORK_VERSION] = "5.0";
        }


        /// <summary>
        /// Initializes an MetadataLoader Instance  with the
        /// TextTransformation (T4 generated class) that is currently running
        /// </summary>
        public MetadataLoader(object textTransformation)
        {
            DefineMetadata();

            if (textTransformation == null)
            {
                throw new ArgumentNullException("textTransformation");
            }

            _textTransformation = DynamicTextTransformation.Create(textTransformation);
        }

        /// <summary>
        /// Load the metadata for Edm, Store, and Mapping collections and register them
        /// with a new MetadataWorkspace, returns false if any of the parts can't be
        /// created, some of the ItemCollections may be registered and usable even if false is
        /// returned
        /// </summary>
        public bool TryLoadAllMetadata(string inputFile, out MetadataWorkspace metadataWorkspace)
        {
            metadataWorkspace = new MetadataWorkspace();

            EdmItemCollection edmItemCollection = CreateEdmItemCollection(inputFile);
            metadataWorkspace.RegisterItemCollection(edmItemCollection);

            StoreItemCollection storeItemCollection = null;
            if (TryCreateStoreItemCollection(inputFile, out storeItemCollection))
            {
                StorageMappingItemCollection storageMappingItemCollection = null;
                if (TryCreateStorageMappingItemCollection(inputFile, edmItemCollection, storeItemCollection, out storageMappingItemCollection))
                {
                    metadataWorkspace.RegisterItemCollection(storeItemCollection);
                    metadataWorkspace.RegisterItemCollection(storageMappingItemCollection);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Create an EdmItemCollection loaded with the metadata provided
        /// </summary>
        public EdmItemCollection CreateEdmItemCollection(string sourcePath, params string[] referenceSchemas)
        {
            EdmItemCollection edmItemCollection;
            if (TryCreateEdmItemCollection(sourcePath, referenceSchemas, out edmItemCollection))
            {
                return edmItemCollection;
            }

            return new EdmItemCollection();
        }

        /// <summary>
        /// Attempts to create a EdmItemCollection from the specified metadata file
        /// </summary>
        public bool TryCreateEdmItemCollection(string sourcePath, out EdmItemCollection edmItemCollection)
        {
            return TryCreateEdmItemCollection(sourcePath, null, out edmItemCollection);
        }

        /// <summary>
        /// Attempts to create a EdmItemCollection from the specified metadata file
        /// </summary>
        public bool TryCreateEdmItemCollection(string sourcePath, string[] referenceSchemas, out EdmItemCollection edmItemCollection)
        {
            edmItemCollection = null;

            if (!ValidateInputPath(sourcePath, _textTransformation))
            {
                return false;
            }

            if (referenceSchemas == null)
            {
                referenceSchemas = new string[0];
            }

            ItemCollection itemCollection = null;
            sourcePath = _textTransformation.Host.ResolvePath(sourcePath);
            EdmItemCollectionBuilder collectionBuilder = new EdmItemCollectionBuilder(_textTransformation, referenceSchemas.Select(s => _textTransformation.Host.ResolvePath(s)).Where(s => s != sourcePath));
            if (collectionBuilder.TryCreateItemCollection(sourcePath, out itemCollection))
            {
                edmItemCollection = (EdmItemCollection)itemCollection;
            }

            return edmItemCollection != null;
        }

        /// <summary>
        /// Attempts to create a StoreItemCollection from the specified metadata file
        /// </summary>
        public bool TryCreateStoreItemCollection(string sourcePath, out StoreItemCollection storeItemCollection)
        {
            storeItemCollection = null;

            if (!ValidateInputPath(sourcePath, _textTransformation))
            {
                return false;
            }

            ItemCollection itemCollection = null;
            StoreItemCollectionBuilder collectionBuilder = new StoreItemCollectionBuilder(_textTransformation);
            if (collectionBuilder.TryCreateItemCollection(_textTransformation.Host.ResolvePath(sourcePath), out itemCollection))
            {
                storeItemCollection = (StoreItemCollection)itemCollection;
            }
            return storeItemCollection != null;
        }

        /// <summary>
        /// Attempts to create a StorageMappingItemCollection from the specified metadata file, EdmItemCollection, and StoreItemCollection
        /// </summary>
        public bool TryCreateStorageMappingItemCollection(string sourcePath, EdmItemCollection edmItemCollection, StoreItemCollection storeItemCollection, out StorageMappingItemCollection storageMappingItemCollection)
        {
            storageMappingItemCollection = null;

            if (!ValidateInputPath(sourcePath, _textTransformation))
            {
                return false;
            }

            if (edmItemCollection == null)
            {
                throw new ArgumentNullException("edmItemCollection");
            }

            if (storeItemCollection == null)
            {
                throw new ArgumentNullException("storeItemCollection");
            }

            ItemCollection itemCollection = null;
            StorageMappingItemCollectionBuilder collectionBuilder = new StorageMappingItemCollectionBuilder(_textTransformation, edmItemCollection, storeItemCollection);
            if (collectionBuilder.TryCreateItemCollection(_textTransformation.Host.ResolvePath(sourcePath), out itemCollection))
            {
                storageMappingItemCollection = (StorageMappingItemCollection)itemCollection;
            }
            return storageMappingItemCollection != null;
        }

        /// <summary>
        /// Gets the Model Namespace from the provided schema file.
        /// </summary>
        public string GetModelNamespace(string sourcePath)
        {
            if (!ValidateInputPath(sourcePath, _textTransformation))
            {
                return String.Empty;
            }

            EdmItemCollectionBuilder builder = new EdmItemCollectionBuilder(_textTransformation);
            XElement model;
            if (builder.TryLoadRootElement(_textTransformation.Host.ResolvePath(sourcePath), out model))
            {
                XAttribute attribute = model.Attribute("Namespace");
                if (attribute != null)
                {
                    return attribute.Value;
                }
            }

            return String.Empty;
        }

        /// <summary>
        /// Returns true if the specified file path is valid
        /// </summary>
        private static bool ValidateInputPath(string sourcePath, DynamicTextTransformation textTransformation)
        {
            if (String.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("sourcePath");
            }

            if (sourcePath == "$edmxInputFile$")
            {
                textTransformation.Errors.Add(new CompilerError(textTransformation.Host.TemplateFile ?? CodeGenerationTools.GetResourceString("Template_CurrentlyRunningTemplate"), 0, 0, string.Empty,
                    CodeGenerationTools.GetResourceString("Template_ReplaceVsItemTemplateToken")));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Base class for ItemCollectionBuilder classes that
        /// loads the specific types of metadata
        /// </summary>
        private abstract class ItemCollectionBuilder
        {
            private readonly DynamicTextTransformation _textTransformation;
            private readonly string _fileExtension;
            private readonly string _edmxSectionName;
            private readonly string _rootElementName;

            /// <summary>
            /// FileExtension for individual (non-edmx) metadata file for this
            /// specific ItemCollection type
            /// </summary>
            public string FileExtension
            {
                get { return _fileExtension; }
            }

            /// <summary>
            /// The name of the XmlElement in the .edmx <Runtime> element
            /// to find this ItemCollection's metadata
            /// </summary>
            public string EdmxSectionName
            {
                get { return _edmxSectionName; }
            }

            /// <summary>
            /// The name of the root element of this ItemCollection's metadata
            /// </summary>
            public string RootElementName
            {
                get { return _rootElementName; }
            }

            /// <summary>
            /// Method to build the appropriate ItemCollection
            /// </summary>
            protected abstract ItemCollection CreateItemCollection(IEnumerable<XmlReader> readers, out IList<EdmSchemaError> errors);

            /// <summary>
            /// Ctor to setup the ItemCollectionBuilder members
            /// </summary>
            protected ItemCollectionBuilder(DynamicTextTransformation textTransformation, string fileExtension, string edmxSectionName, string rootElementName)
            {
                _textTransformation = textTransformation;
                _fileExtension = fileExtension;
                _edmxSectionName = edmxSectionName;
                _rootElementName = rootElementName;
            }

            /// <summary>
            /// Selects a namespace from the supplied constants.
            /// </summary>
            protected abstract string GetNamespace(SchemaConstants constants);

            /// <summary>
            /// Try to create an ItemCollection loaded with the metadata provided
            /// </summary>
            public bool TryCreateItemCollection(string sourcePath, out ItemCollection itemCollection)
            {
                itemCollection = null;

                if (!ValidateInputPath(sourcePath, _textTransformation))
                {
                    return false;
                }

                XElement schemaElement = null;
                if (TryLoadRootElement(sourcePath, out schemaElement))
                {
                    List<XmlReader> readers = new List<XmlReader>();
                    try
                    {
                        readers.Add(schemaElement.CreateReader());
                        IList<EdmSchemaError> errors = null;

                        ItemCollection tempItemCollection = CreateItemCollection(readers, out errors);
                        if (ProcessErrors(errors, sourcePath))
                        {
                            return false;
                        }

                        itemCollection = tempItemCollection;
                        return true;
                    }
                    finally
                    {
                        foreach (XmlReader reader in readers)
                        {
                            ((IDisposable)reader).Dispose();
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Tries to load the root element from the metadata file provided
            /// </summary>
            public bool TryLoadRootElement(string sourcePath, out XElement schemaElement)
            {
                schemaElement = null;
                string extension = Path.GetExtension(sourcePath);
                if (extension.Equals(".edmx", StringComparison.InvariantCultureIgnoreCase))
                {
                    return TryLoadRootElementFromEdmx(sourcePath, out schemaElement);
                }
                else if (extension.Equals(FileExtension, StringComparison.InvariantCultureIgnoreCase))
                {
                    // load from single metadata file (.csdl, .ssdl, or .msl)
                    schemaElement = XElement.Load(sourcePath, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Tries to load the root element from the provided edmxDocument
            /// </summary>
            private bool TryLoadRootElementFromEdmx(XElement edmxDocument, SchemaConstants schemaConstants, string sectionName, string rootElementName, out XElement rootElement)
            {
                rootElement = null;

                XNamespace edmxNs = schemaConstants.EdmxNamespace;
                XNamespace sectionNs = GetNamespace(schemaConstants);

                XElement runtime = edmxDocument.Element(edmxNs + "Runtime");
                if (runtime == null)
                {
                    return false;
                }

                XElement section = runtime.Element(edmxNs + sectionName);
                if (section == null)
                {
                    return false;
                }

                string templateVersion;

                if (!TemplateMetadata.TryGetValue(MetadataConstants.TT_TEMPLATE_VERSION, out templateVersion))
                {
                    templateVersion = MetadataConstants.DEFAULT_TEMPLATE_VERSION;
                }

                if (schemaConstants.MinimumTemplateVersion > new Version(templateVersion))
                {
                    _textTransformation.Errors.Add(new CompilerError(
                        _textTransformation.Host.TemplateFile ?? CodeGenerationTools.GetResourceString("Template_CurrentlyRunningTemplate"), 0, 0, string.Empty,
                            CodeGenerationTools.GetResourceString("Template_UnsupportedSchema")) { IsWarning = true });
                }

                rootElement = section.Element(sectionNs + rootElementName);
                return rootElement != null;
            }

            /// <summary>
            /// Tries to load the root element from the provided .edmx metadata file
            /// </summary>
            private bool TryLoadRootElementFromEdmx(string edmxPath, out XElement rootElement)
            {
                rootElement = null;

                XElement element = XElement.Load(edmxPath, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);

                return TryLoadRootElementFromEdmx(element, MetadataConstants.V3_SCHEMA_CONSTANTS, EdmxSectionName, RootElementName, out rootElement)
                    || TryLoadRootElementFromEdmx(element, MetadataConstants.V2_SCHEMA_CONSTANTS, EdmxSectionName, RootElementName, out rootElement)
                    || TryLoadRootElementFromEdmx(element, MetadataConstants.V1_SCHEMA_CONSTANTS, EdmxSectionName, RootElementName, out rootElement);
            }

            /// <summary>
            /// Takes an Enumerable of EdmSchemaErrors, and adds them
            /// to the errors collection of the template class
            /// </summary>
            private bool ProcessErrors(IEnumerable<EdmSchemaError> errors, string sourceFilePath)
            {
                bool foundErrors = false;
                foreach (EdmSchemaError error in errors)
                {
                    CompilerError newError = new CompilerError(error.SchemaLocation, error.Line, error.Column,
                                                     error.ErrorCode.ToString(CultureInfo.InvariantCulture),
                                                     error.Message);
                    newError.IsWarning = error.Severity == EdmSchemaErrorSeverity.Warning;
                    foundErrors |= error.Severity == EdmSchemaErrorSeverity.Error;
                    if (error.SchemaLocation == null)
                    {
                        newError.FileName = sourceFilePath;
                    }
                    _textTransformation.Errors.Add(newError);
                }

                return foundErrors;
            }
        }

        /// <summary>
        /// Builder class for creating a StorageMappingItemCollection
        /// </summary>
        private class StorageMappingItemCollectionBuilder : ItemCollectionBuilder
        {
            private readonly EdmItemCollection _edmItemCollection;
            private readonly StoreItemCollection _storeItemCollection;

            public StorageMappingItemCollectionBuilder(DynamicTextTransformation textTransformation, EdmItemCollection edmItemCollection, StoreItemCollection storeItemCollection)
                : base(textTransformation, MetadataConstants.MSL_EXTENSION, MetadataConstants.MSL_EDMX_SECTION_NAME, MetadataConstants.MSL_ROOT_ELEMENT_NAME)
            {
                _edmItemCollection = edmItemCollection;
                _storeItemCollection = storeItemCollection;
            }

            protected override ItemCollection CreateItemCollection(IEnumerable<XmlReader> readers, out IList<EdmSchemaError> errors)
            {
                return MetadataItemCollectionFactory.CreateStorageMappingItemCollection(_edmItemCollection, _storeItemCollection, readers, out errors);
            }

            /// <summary>
            /// Selects a namespace from the supplied constants.
            /// </summary>
            protected override string GetNamespace(SchemaConstants constants)
            {
                return constants.MslNamespace;
            }
        }

        /// <summary>
        /// Builder class for creating a StoreItemCollection
        /// </summary>
        private class StoreItemCollectionBuilder : ItemCollectionBuilder
        {
            public StoreItemCollectionBuilder(DynamicTextTransformation textTransformation)
                : base(textTransformation, MetadataConstants.SSDL_EXTENSION, MetadataConstants.SSDL_EDMX_SECTION_NAME, MetadataConstants.SSDL_ROOT_ELEMENT_NAME)
            {
            }

            protected override ItemCollection CreateItemCollection(IEnumerable<XmlReader> readers, out IList<EdmSchemaError> errors)
            {
                return MetadataItemCollectionFactory.CreateStoreItemCollection(readers, out errors);
            }

            /// <summary>
            /// Selects a namespace from the supplied constants.
            /// </summary>
            protected override string GetNamespace(SchemaConstants constants)
            {
                return constants.SsdlNamespace;
            }
        }

        /// <summary>
        /// Builder class for creating a EdmItemCollection
        /// </summary>
        private class EdmItemCollectionBuilder : ItemCollectionBuilder
        {
            private List<string> _referenceSchemas = new List<string>();

            public EdmItemCollectionBuilder(DynamicTextTransformation textTransformation)
                : base(textTransformation, MetadataConstants.CSDL_EXTENSION, MetadataConstants.CSDL_EDMX_SECTION_NAME, MetadataConstants.CSDL_ROOT_ELEMENT_NAME)
            {
            }

            public EdmItemCollectionBuilder(DynamicTextTransformation textTransformation, IEnumerable<string> referenceSchemas)
                : this(textTransformation)
            {
                _referenceSchemas.AddRange(referenceSchemas);
            }

            protected override ItemCollection CreateItemCollection(IEnumerable<XmlReader> readers, out IList<EdmSchemaError> errors)
            {
                List<XmlReader> ownedReaders = new List<XmlReader>();
                List<XmlReader> allReaders = new List<XmlReader>();
                try
                {
                    allReaders.AddRange(readers);
                    foreach (string path in _referenceSchemas.Distinct())
                    {
                        XElement reference;
                        if (TryLoadRootElement(path, out reference))
                        {
                            XmlReader reader = reference.CreateReader();
                            allReaders.Add(reader);
                            ownedReaders.Add(reader);
                        }
                    }

                    return MetadataItemCollectionFactory.CreateEdmItemCollection(allReaders, out errors);
                }
                finally
                {
                    foreach (XmlReader reader in ownedReaders)
                    {
                        ((IDisposable)reader).Dispose();
                    }
                }
            }

            /// <summary>
            /// Selects a namespace from the supplied constants.
            /// </summary>
            protected override string GetNamespace(SchemaConstants constants)
            {
                return constants.CsdlNamespace;
            }
        }
    }

}