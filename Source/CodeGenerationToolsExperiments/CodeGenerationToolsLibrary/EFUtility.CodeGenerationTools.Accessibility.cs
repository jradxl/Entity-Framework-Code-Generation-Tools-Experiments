using System;
using System.Collections.Generic;
using System.Data.Metadata.Edm;
using System.Linq;

namespace EFUtility.CodeGenerationTools
{
    /// <summary>
    /// Responsible for encapsulating the retrieval and translation of the CodeGeneration
    /// annotations in the EntityFramework Metadata to a form that is useful in code generation.
    /// </summary>
    public static class Accessibility
    {
        private const string GETTER_ACCESS = "http://schemas.microsoft.com/ado/2006/04/codegeneration:GetterAccess";
        private const string SETTER_ACCESS = "http://schemas.microsoft.com/ado/2006/04/codegeneration:SetterAccess";
        private const string TYPE_ACCESS = "http://schemas.microsoft.com/ado/2006/04/codegeneration:TypeAccess";
        private const string METHOD_ACCESS = "http://schemas.microsoft.com/ado/2006/04/codegeneration:MethodAccess";
        private const string ACCESS_PROTECTED = "Protected";
        private const string ACCESS_INTERNAL = "Internal";
        private const string ACCESS_PRIVATE = "Private";
        private static readonly Dictionary<string, int> AccessibilityRankIdLookup = new Dictionary<string, int>
        {
            { "private", 1},
            { "internal", 2},
            { "protected", 3},
            { "public", 4},
        };

        /// <summary>
        /// Gets the accessibility that should be applied to a type being generated from the provided GlobalItem.
        ///
        /// defaults to public if no annotation is found.
        /// </summary>
        public static string ForType(GlobalItem item)
        {
            if (item == null)
            {
                return null;
            }

            return GetAccessibility(item, TYPE_ACCESS);
        }

        /// <summary>
        /// Gets the accessibility that should be applied at the property level for a property being
        /// generated from the provided EdmMember.
        ///
        /// defaults to public if no annotation is found.
        /// </summary>
        public static string ForProperty(EdmMember member)
        {
            if (member == null)
            {
                return null;
            }

            string getterAccess, setterAccess, propertyAccess;
            CalculatePropertyAccessibility(member, out propertyAccess, out getterAccess, out setterAccess);
            return propertyAccess;
        }

        /// <summary>
        /// Gets the accessibility that should be applied at the property level for a Read-Only property being
        /// generated from the provided EdmMember.
        ///
        /// defaults to public if no annotation is found.
        /// </summary>
        public static string ForReadOnlyProperty(EdmMember member)
        {
            if (member == null)
            {
                return null;
            }

            return GetAccessibility(member, GETTER_ACCESS);
        }

        /// <summary>
        /// Gets the accessibility that should be applied at the property level for a property being
        /// generated from the provided EntitySet.
        ///
        /// defaults to public if no annotation is found.
        /// </summary>
        public static string ForReadOnlyProperty(EntitySet set)
        {
            if (set == null)
            {
                return null;
            }

            return GetAccessibility(set, GETTER_ACCESS);
        }

        /// <summary>
        /// Gets the accessibility that should be applied at the property level for a Write-Only property being
        /// generated from the provided EdmMember.
        ///
        /// defaults to public if no annotation is found.
        /// </summary>
        public static string ForWriteOnlyProperty(EdmMember member)
        {
            if (member == null)
            {
                return null;
            }

            return GetAccessibility(member, SETTER_ACCESS);
        }


        /// <summary>
        /// Gets the accessibility that should be applied at the get level for a property being
        /// generated from the provided EdmMember.
        ///
        /// defaults to empty if no annotation is found or the accessibility is the same as the property level.
        /// </summary>
        public static string ForGetter(EdmMember member)
        {
            if (member == null)
            {
                return null;
            }

            string getterAccess, setterAccess, propertyAccess;
            CalculatePropertyAccessibility(member, out propertyAccess, out getterAccess, out setterAccess);
            return getterAccess;
        }

        /// <summary>
        /// Gets the accessibility that should be applied at the set level for a property being
        /// generated from the provided EdmMember.
        ///
        /// defaults to empty if no annotation is found or the accessibility is the same as the property level.
        /// </summary>
        public static string ForSetter(EdmMember member)
        {
            if (member == null)
            {
                return null;
            }

            string getterAccess, setterAccess, propertyAccess;
            CalculatePropertyAccessibility(member, out propertyAccess, out getterAccess, out setterAccess);
            return setterAccess;
        }

        /// <summary>
        /// Gets the accessibility that should be applied to a method being generated from the provided EdmFunction.
        ///
        /// defaults to public if no annotation is found.
        /// </summary>
        public static string ForMethod(EdmFunction function)
        {
            if (function == null)
            {
                return null;
            }

            return GetAccessibility(function, METHOD_ACCESS);
        }

        //JsrSoft     private static void CalculatePropertyAccessibility(MetadataItem item,
        public static void CalculatePropertyAccessibility(MetadataItem item,
            out string propertyAccessibility,
            out string getterAccessibility,
            out string setterAccessibility)
        {
            getterAccessibility = GetAccessibility(item, GETTER_ACCESS);
            int getterRank = AccessibilityRankIdLookup[getterAccessibility];

            setterAccessibility = GetAccessibility(item, SETTER_ACCESS);
            int setterRank = AccessibilityRankIdLookup[setterAccessibility];

            int propertyRank = Math.Max(getterRank, setterRank);
            if (setterRank == propertyRank)
            {
                setterAccessibility = String.Empty;
            }

            if (getterRank == propertyRank)
            {
                getterAccessibility = String.Empty;
            }

            propertyAccessibility = AccessibilityRankIdLookup.Where(v => v.Value == propertyRank).Select(v => v.Key).Single();
        }

        private static string GetAccessibility(MetadataItem item, string name)
        {
            string accessibility;
            if (MetadataTools.TryGetStringMetadataPropertySetting(item, name, out accessibility))
            {
                return TranslateUserAccessibilityToCSharpAccessibility(accessibility);
            }

            return "public";
        }

        private static string TranslateUserAccessibilityToCSharpAccessibility(string userAccessibility)
        {
            if (userAccessibility == ACCESS_PROTECTED)
            {
                return "protected";
            }
            else if (userAccessibility == ACCESS_INTERNAL)
            {
                return "internal";
            }
            else if (userAccessibility == ACCESS_PRIVATE)
            {
                return "private";
            }
            else
            {
                // default to public
                return "public";
            }
        }
    }
}
