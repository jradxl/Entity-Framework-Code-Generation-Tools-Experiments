// Copyright (c) Microsoft Corporation.  All rights reserved.

/// ****************************************************************************
/// <summary>
/// From the supporting EF ttinclude, "EF.Utility.CS.ttinclude"
/// with many thanks.
/// The supporting classes from the template
/// have been entered here with very little modification.
/// Jsrsoft, June 2013
/// </summary>
///  ****************************************************************************

using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Metadata.Edm;
using System.Globalization;
using System.IO;
using System.Linq;


namespace EFUtility.CodeGenerationTools
{

    /// <summary>
    /// Responsible for helping to create source code that is
    /// correctly formated and functional
    /// </summary>
    public class CodeGenerationTools
    {
        private readonly DynamicTextTransformation _textTransformation;
        private readonly CSharpCodeProvider _code;
        private readonly MetadataTools _ef;

        /// <summary>
        /// Initializes a new CodeGenerationTools object with the TextTransformation (T4 generated class)
        /// that is currently running
        /// </summary>
        public CodeGenerationTools(object textTransformation)
        {
            if (textTransformation == null)
            {
                throw new ArgumentNullException("textTransformation");
            }

            _textTransformation = DynamicTextTransformation.Create(textTransformation);
            _code = new CSharpCodeProvider();
            _ef = new MetadataTools(_textTransformation);
            FullyQualifySystemTypes = false;
            CamelCaseFields = true;
        }

        /// <summary>
        /// When true, all types that are not being generated
        /// are fully qualified to keep them from conflicting with
        /// types that are being generated. Useful when you have
        /// something like a type being generated named System.
        ///
        /// Default is false.
        /// </summary>
        public bool FullyQualifySystemTypes { get; set; }

        /// <summary>
        /// When true, the field names are Camel Cased,
        /// otherwise they will preserve the case they
        /// start with.
        ///
        /// Default is true.
        /// </summary>
        public bool CamelCaseFields { get; set; }

        /// <summary>
        /// Returns the NamespaceName suggested by VS if running inside VS.  Otherwise, returns
        /// null.
        /// </summary>
        public string VsNamespaceSuggestion()
        {
            string suggestion = _textTransformation.Host.ResolveParameterValue("directiveId", "namespaceDirectiveProcessor", "namespaceHint");
            if (String.IsNullOrEmpty(suggestion))
            {
                return null;
            }

            return suggestion;
        }

        /// <summary>
        /// Returns a string that is safe for use as an identifier in C#.
        /// Keywords are escaped.
        /// </summary>
        public string Escape(string name)
        {
            if (name == null)
            {
                return null;
            }

            return _code.CreateEscapedIdentifier(name);
        }

        /// <summary>
        /// Returns the name of the TypeUsage's EdmType that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(TypeUsage typeUsage)
        {
            if (typeUsage == null)
            {
                return null;
            }

            if (typeUsage.EdmType is ComplexType ||
                typeUsage.EdmType is EntityType)
            {
                return Escape(typeUsage.EdmType.Name);
            }
            else if (typeUsage.EdmType is SimpleType)
            {
                Type clrType = _ef.UnderlyingClrType(typeUsage.EdmType);
                string typeName = typeUsage.EdmType is EnumType ? Escape(typeUsage.EdmType.Name) : Escape(clrType);
                if (clrType.IsValueType && _ef.IsNullable(typeUsage))
                {
                    return String.Format(CultureInfo.InvariantCulture, "Nullable<{0}>", typeName);
                }

                return typeName;
            }
            else if (typeUsage.EdmType is CollectionType)
            {
                return String.Format(CultureInfo.InvariantCulture, "ICollection<{0}>", Escape(((CollectionType)typeUsage.EdmType).TypeUsage));
            }

            throw new ArgumentException("typeUsage");
        }

        /// <summary>
        /// Returns the name of the EdmMember that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(EdmMember member)
        {
            if (member == null)
            {
                return null;
            }

            return Escape(member.Name);
        }

        /// <summary>
        /// Returns the name of the EdmType that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(EdmType type)
        {
            if (type == null)
            {
                return null;
            }

            return Escape(type.Name);
        }

        /// <summary>
        /// Returns the name of the EdmFunction that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(EdmFunction function)
        {
            if (function == null)
            {
                return null;
            }

            return Escape(function.Name);
        }

        /// <summary>
        /// Returns the name of the EnumMember that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(EnumMember member)
        {
            if (member == null)
            {
                return null;
            }

            return Escape(member.Name);
        }

        /// <summary>
        /// Returns the name of the EntityContainer that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(EntityContainer container)
        {
            if (container == null)
            {
                return null;
            }

            return Escape(container.Name);
        }

        /// <summary>
        /// Returns the name of the EntitySet that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(EntitySet set)
        {
            if (set == null)
            {
                return null;
            }

            return Escape(set.Name);
        }

        /// <summary>
        /// Returns the name of the StructuralType that is safe for
        /// use as an identifier.
        /// </summary>
        public string Escape(StructuralType type)
        {
            if (type == null)
            {
                return null;
            }

            return Escape(type.Name);
        }

        /// <summary>
        /// Returns the NamespaceName with each segment safe to
        /// use as an identifier.
        /// </summary>
        public string EscapeNamespace(string namespaceName)
        {
            if (String.IsNullOrEmpty(namespaceName))
            {
                return namespaceName;
            }

            string[] parts = namespaceName.Split('.');
            namespaceName = String.Empty;
            foreach (string part in parts)
            {
                if (namespaceName != String.Empty)
                {
                    namespaceName += ".";
                }

                namespaceName += Escape(part);
            }

            return namespaceName;
        }

        /// <summary>
        /// Returns the name of the EdmMember formatted for
        /// use as a field identifier.
        ///
        /// This method changes behavior based on the CamelCaseFields
        /// setting.
        /// </summary>
        public string FieldName(EdmMember member)
        {
            if (member == null)
            {
                return null;
            }

            return FieldName(member.Name);
        }

        /// <summary>
        /// Returns the name of the EntitySet formatted for
        /// use as a field identifier.
        ///
        /// This method changes behavior based on the CamelCaseFields
        /// setting.
        /// </summary>
        public string FieldName(EntitySet set)
        {
            if (set == null)
            {
                return null;
            }

            return FieldName(set.Name);

        }

        private string FieldName(string name)
        {
            if (CamelCaseFields)
            {
                return "_" + CamelCase(name);
            }
            else
            {
                return "_" + name;
            }
        }

        /// <summary>
        /// Returns the name of the Type object formatted for
        /// use in source code.
        ///
        /// This method changes behavior based on the FullyQualifySystemTypes
        /// setting.
        /// </summary>
        public string Escape(Type clrType)
        {
            return Escape(clrType, FullyQualifySystemTypes);
        }

        /// <summary>
        /// Returns the name of the Type object formatted for
        /// use in source code.
        /// </summary>
        public string Escape(Type clrType, bool fullyQualifySystemTypes)
        {
            if (clrType == null)
            {
                return null;
            }

            string typeName;
            if (fullyQualifySystemTypes)
            {
                typeName = "global::" + clrType.FullName;
            }
            else
            {
                typeName = _code.GetTypeOutput(new CodeTypeReference(clrType));
            }
            return typeName;
        }

        /// <summary>
        /// Returns the abstract option if the entity is Abstract, otherwise returns String.Empty
        /// </summary>
        public string AbstractOption(EntityType entity)
        {
            if (entity.Abstract)
            {
                return "abstract";
            }
            return String.Empty;
        }

        /// <summary>
        /// Returns the passed in identifier with the first letter changed to lowercase
        /// </summary>
        public string CamelCase(string identifier)
        {
            if (String.IsNullOrEmpty(identifier))
            {
                return identifier;
            }

            if (identifier.Length == 1)
            {
                return identifier[0].ToString(CultureInfo.InvariantCulture).ToLowerInvariant();
            }

            return identifier[0].ToString(CultureInfo.InvariantCulture).ToLowerInvariant() + identifier.Substring(1);
        }

        /// <summary>
        /// If the value parameter is null or empty an empty string is returned,
        /// otherwise it retuns value with a single space concatenated on the end.
        /// </summary>
        public string SpaceAfter(string value)
        {
            return StringAfter(value, " ");
        }

        /// <summary>
        /// If the value parameter is null or empty an empty string is returned,
        /// otherwise it retuns value with a single space concatenated on the end.
        /// </summary>
        public string SpaceBefore(string value)
        {
            return StringBefore(" ", value);
        }

        /// <summary>
        /// If the value parameter is null or empty an empty string is returned,
        /// otherwise it retuns value with append concatenated on the end.
        /// </summary>
        public string StringAfter(string value, string append)
        {
            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }

            return value + append;
        }

        /// <summary>
        /// If the value parameter is null or empty an empty string is returned,
        /// otherwise it retuns value with prepend concatenated on the front.
        /// </summary>
        public string StringBefore(string prepend, string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return String.Empty;
            }

            return prepend + value;
        }

        /// <summary>
        /// Returns false and shows an error if the supplied type names aren't case-insensitively unique,
        /// otherwise returns true.
        /// </summary>
        public bool VerifyCaseInsensitiveTypeUniqueness(IEnumerable<string> types, string sourceFile)
        {
            return VerifyCaseInsensitiveUniqueness(types, t => string.Format(CultureInfo.CurrentCulture, GetResourceString("Template_CaseInsensitiveTypeConflict"), t), sourceFile);
        }

        /// <summary>
        /// Returns false and shows an error if the supplied strings aren't case-insensitively unique,
        /// otherwise returns true.
        /// </summary>
        private bool VerifyCaseInsensitiveUniqueness(IEnumerable<string> items, Func<string, string> formatMessage, string sourceFile)
        {
            HashSet<string> hash = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (string item in items)
            {
                if (!hash.Add(item))
                {
                    _textTransformation.Errors.Add(new System.CodeDom.Compiler.CompilerError(sourceFile, -1, -1, "6023", formatMessage(item)));
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns the names of the items in the supplied collection that correspond to O-Space types.
        /// </summary>
        public IEnumerable<string> GetAllGlobalItems(EdmItemCollection itemCollection)
        {
            return itemCollection.GetItems<GlobalItem>().Where(i => i is EntityType || i is ComplexType || i is EnumType || i is EntityContainer).Select(g => GetGlobalItemName(g));
        }

        /// <summary>
        /// Returns the name of the supplied GlobalItem.
        /// </summary>
        public string GetGlobalItemName(GlobalItem item)
        {
            if (item is EdmType)
            {
                return ((EdmType)item).Name;
            }
            else
            {
                return ((EntityContainer)item).Name;
            }
        }

        /// <summary>
        /// Retuns as full of a name as possible, if a namespace is provided
        /// the namespace and name are combined with a period, otherwise just
        /// the name is returned.
        /// </summary>
        public string CreateFullName(string namespaceName, string name)
        {
            if (String.IsNullOrEmpty(namespaceName))
            {
                return name;
            }

            return namespaceName + "." + name;
        }

        /// <summary>
        /// Retuns a literal representing the supplied value.
        /// </summary>
        public string CreateLiteral(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            Type type = value.GetType();
            if (type.IsEnum)
            {
                return type.FullName + "." + value.ToString();
            }
            if (type == typeof(Guid))
            {
                return string.Format(CultureInfo.InvariantCulture, "new Guid(\"{0}\")",
                                     ((Guid)value).ToString("D", CultureInfo.InvariantCulture));
            }
            else if (type == typeof(DateTime))
            {
                return string.Format(CultureInfo.InvariantCulture, "new DateTime({0}, DateTimeKind.Unspecified)",
                                     ((DateTime)value).Ticks);
            }
            else if (type == typeof(byte[]))
            {
                var arrayInit = string.Join(", ", ((byte[])value).Select(b => b.ToString(CultureInfo.InvariantCulture)).ToArray());
                return string.Format(CultureInfo.InvariantCulture, "new Byte[] {{{0}}}", arrayInit);
            }
            else if (type == typeof(DateTimeOffset))
            {
                var dto = (DateTimeOffset)value;
                return string.Format(CultureInfo.InvariantCulture, "new DateTimeOffset({0}, new TimeSpan({1}))",
                                     dto.Ticks, dto.Offset.Ticks);
            }
            else if (type == typeof(TimeSpan))
            {
                return string.Format(CultureInfo.InvariantCulture, "new TimeSpan({0})",
                                     ((TimeSpan)value).Ticks);
            }

            var expression = new CodePrimitiveExpression(value);
            var writer = new StringWriter();
            CSharpCodeProvider code = new CSharpCodeProvider();
            code.GenerateCodeFromExpression(expression, writer, new CodeGeneratorOptions());
            return writer.ToString();
        }

        /// <summary>
        /// Returns a resource string from the System.Data.Entity.Design assembly.
        /// </summary>
        public static string GetResourceString(string resourceName, CultureInfo culture = null)
        {
            if (_resourceManager == null)
            {
                _resourceManager = new System.Resources.ResourceManager("System.Data.Entity.Design",
                    typeof(System.Data.Entity.Design.MetadataItemCollectionFactory).Assembly);
            }

            return _resourceManager.GetString(resourceName, culture);
        }
        static System.Resources.ResourceManager _resourceManager;

        private const string ExternalTypeNameAttributeName = @"http://schemas.microsoft.com/ado/2006/04/codegeneration:ExternalTypeName";

        /// <summary>
        /// Gets the entity, complex, or enum types for which code should be generated from the given item collection.
        /// Any types for which an ExternalTypeName annotation has been applied in the conceptual model
        /// metadata (CSDL) are filtered out of the returned list.
        /// </summary>
        /// <typeparam name="T">The type of item to return.</typeparam>
        /// <param name="itemCollection">The item collection to look in.</param>
        /// <returns>The items to generate.</returns>
        public IEnumerable<T> GetItemsToGenerate<T>(ItemCollection itemCollection) where T : GlobalItem
        {
            return itemCollection.GetItems<T>().Where(i => !i.MetadataProperties.Any(p => p.Name == ExternalTypeNameAttributeName));
        }

        /// <summary>
        /// Returns the escaped type name to use for the given usage of a c-space type in o-space. This might be
        /// an external type name if the ExternalTypeName annotation has been specified in the
        /// conceptual model metadata (CSDL).
        /// </summary>
        /// <param name="typeUsage">The c-space type usage to get a name for.</param>
        /// <returns>The type name to use.</returns>
        public string GetTypeName(TypeUsage typeUsage)
        {
            return typeUsage == null ? null : GetTypeName(typeUsage.EdmType, _ef.IsNullable(typeUsage), modelNamespace: null);
        }

        /// <summary>
        /// Returns the escaped type name to use for the given c-space type in o-space. This might be
        /// an external type name if the ExternalTypeName annotation has been specified in the
        /// conceptual model metadata (CSDL).
        /// </summary>
        /// <param name="edmType">The c-space type to get a name for.</param>
        /// <returns>The type name to use.</returns>
        public string GetTypeName(EdmType edmType)
        {
            return GetTypeName(edmType, isNullable: null, modelNamespace: null);
        }

        /// <summary>
        /// Returns the escaped type name to use for the given usage of an c-space type in o-space. This might be
        /// an external type name if the ExternalTypeName annotation has been specified in the
        /// conceptual model metadata (CSDL).
        /// </summary>
        /// <param name="typeUsage">The c-space type usage to get a name for.</param>
        /// <param name="modelNamespace">If not null and the type's namespace does not match this namespace, then a
        /// fully qualified name will be returned.</param>
        /// <returns>The type name to use.</returns>
        public string GetTypeName(TypeUsage typeUsage, string modelNamespace)
        {
            return typeUsage == null ? null : GetTypeName(typeUsage.EdmType, _ef.IsNullable(typeUsage), modelNamespace);
        }

        /// <summary>
        /// Returns the escaped type name to use for the given c-space type in o-space. This might be
        /// an external type name if the ExternalTypeName annotation has been specified in the
        /// conceptual model metadata (CSDL).
        /// </summary>
        /// <param name="edmType">The c-space type to get a name for.</param>
        /// <param name="modelNamespace">If not null and the type's namespace does not match this namespace, then a
        /// fully qualified name will be returned.</param>
        /// <returns>The type name to use.</returns>
        public string GetTypeName(EdmType edmType, string modelNamespace)
        {
            return GetTypeName(edmType, isNullable: null, modelNamespace: modelNamespace);
        }

        /// <summary>
        /// Returns the escaped type name to use for the given c-space type in o-space. This might be
        /// an external type name if the ExternalTypeName annotation has been specified in the
        /// conceptual model metadata (CSDL).
        /// </summary>
        /// <param name="edmType">The c-space type to get a name for.</param>
        /// <param name="isNullable">Set this to true for nullable usage of this type.</param>
        /// <param name="modelNamespace">If not null and the type's namespace does not match this namespace, then a
        /// fully qualified name will be returned.</param>
        /// <returns>The type name to use.</returns>
        private string GetTypeName(EdmType edmType, bool? isNullable, string modelNamespace)
        {
            if (edmType == null)
            {
                return null;
            }

            var collectionType = edmType as CollectionType;
            if (collectionType != null)
            {
                return String.Format(CultureInfo.InvariantCulture, "ICollection<{0}>", GetTypeName(collectionType.TypeUsage, modelNamespace));
            }

            // Try to get an external type name, and if that is null, then try to get escape the name from metadata,
            // possibly namespace-qualifying it.
            var typeName = Escape(edmType.MetadataProperties
                                  .Where(p => p.Name == ExternalTypeNameAttributeName)
                                  .Select(p => (string)p.Value)
                                  .FirstOrDefault())
                ??
                (modelNamespace != null && edmType.NamespaceName != modelNamespace ?
                 CreateFullName(EscapeNamespace(edmType.NamespaceName), Escape(edmType)) :
                 Escape(edmType));

            if (edmType is StructuralType)
            {
                return typeName;
            }

            if (edmType is SimpleType)
            {
                var clrType = _ef.UnderlyingClrType(edmType);
                if (!(edmType is EnumType))
                {
                    typeName = Escape(clrType);
                }

                return clrType.IsValueType && isNullable == true ?
                    String.Format(CultureInfo.InvariantCulture, "Nullable<{0}>", typeName) :
                    typeName;
            }

            throw new ArgumentException("typeUsage");
        }
    }
}
