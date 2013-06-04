using System;
namespace EFUtility.CodeGenerationTools
{
/// <summary>
/// Responsible for encapsulating the constants defined in Metadata
/// </summary>
public static class MetadataConstants
{
    public const string CSDL_EXTENSION = ".csdl";

    public const string CSDL_EDMX_SECTION_NAME = "ConceptualModels";
    public const string CSDL_ROOT_ELEMENT_NAME = "Schema";
    public const string EDM_ANNOTATION_09_02 = "http://schemas.microsoft.com/ado/2009/02/edm/annotation";

    public const string SSDL_EXTENSION = ".ssdl";

    public const string SSDL_EDMX_SECTION_NAME = "StorageModels";
    public const string SSDL_ROOT_ELEMENT_NAME = "Schema";

    public const string MSL_EXTENSION = ".msl";

    public const string MSL_EDMX_SECTION_NAME = "Mappings";
    public const string MSL_ROOT_ELEMENT_NAME = "Mapping";

    public const string TT_TEMPLATE_NAME = "TemplateName";
    public const string TT_TEMPLATE_VERSION = "TemplateVersion";
    public const string TT_MINIMUM_ENTITY_FRAMEWORK_VERSION = "MinimumEntityFrameworkVersion";

    public const string DEFAULT_TEMPLATE_VERSION = "4.0";

    public static readonly SchemaConstants V1_SCHEMA_CONSTANTS = new SchemaConstants(
        "http://schemas.microsoft.com/ado/2007/06/edmx",
        "http://schemas.microsoft.com/ado/2006/04/edm",
        "http://schemas.microsoft.com/ado/2006/04/edm/ssdl",
        "urn:schemas-microsoft-com:windows:storage:mapping:CS",
        new Version("3.5"));

    public static readonly SchemaConstants V2_SCHEMA_CONSTANTS = new SchemaConstants(
        "http://schemas.microsoft.com/ado/2008/10/edmx",
        "http://schemas.microsoft.com/ado/2008/09/edm",
        "http://schemas.microsoft.com/ado/2009/02/edm/ssdl",
        "http://schemas.microsoft.com/ado/2008/09/mapping/cs",
        new Version("4.0"));

    public static readonly SchemaConstants V3_SCHEMA_CONSTANTS = new SchemaConstants(
        "http://schemas.microsoft.com/ado/2009/11/edmx",
        "http://schemas.microsoft.com/ado/2009/11/edm",
        "http://schemas.microsoft.com/ado/2009/11/edm/ssdl",
        "http://schemas.microsoft.com/ado/2009/11/mapping/cs",
        new Version("4.5"));
}

public struct SchemaConstants
{
    public SchemaConstants(string edmxNamespace, string csdlNamespace, string ssdlNamespace, string mslNamespace, Version minimumTemplateVersion) : this()
    {
        EdmxNamespace = edmxNamespace;
        CsdlNamespace = csdlNamespace;
        SsdlNamespace = ssdlNamespace;
        MslNamespace = mslNamespace;
        MinimumTemplateVersion = minimumTemplateVersion;
    }

    public string EdmxNamespace { get; private set; }
    public string CsdlNamespace { get; private set; }
    public string SsdlNamespace { get; private set; }
    public string MslNamespace { get; private set; }
    public Version MinimumTemplateVersion { get; private set; }
}
}
