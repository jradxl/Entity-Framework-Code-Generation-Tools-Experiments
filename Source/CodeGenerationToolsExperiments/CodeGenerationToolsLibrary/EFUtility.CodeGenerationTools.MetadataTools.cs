using System;
using System.Collections.Generic;
using System.Data.Metadata.Edm;
using System.Linq;
using System.Xml.Linq;

namespace EFUtility.CodeGenerationTools
{
    /// <summary>
    /// Responsible for making the Entity Framework Metadata more
    /// accessible for code generation.
    /// </summary>
    public class MetadataTools
    {
        private readonly DynamicTextTransformation _textTransformation;

        /// <summary>
        /// Initializes an MetadataTools Instance  with the
        /// TextTransformation (T4 generated class) that is currently running
        /// </summary>
        public MetadataTools(object textTransformation)
        {
            if (textTransformation == null)
            {
                throw new ArgumentNullException("textTransformation");
            }

            _textTransformation = DynamicTextTransformation.Create(textTransformation);
        }

        /// <summary>
        /// This method returns the underlying CLR type of the o-space type corresponding to the supplied <paramref name="typeUsage"/>
        /// Note that for an enum type this means that the type backing the enum will be returned, not the enum type itself.
        /// </summary>
        public Type ClrType(TypeUsage typeUsage)
        {
            return UnderlyingClrType(typeUsage.EdmType);
        }

        /// <summary>
        /// This method returns the underlying CLR type given the c-space type.
        /// Note that for an enum type this means that the type backing the enum will be returned, not the enum type itself.
        /// </summary>
        public Type UnderlyingClrType(EdmType edmType)
        {
            var primitiveType = edmType as PrimitiveType;
            if (primitiveType != null)
            {
                return primitiveType.ClrEquivalentType;
            }

            var enumType = edmType as EnumType;
            if (enumType != null)
            {
                return enumType.UnderlyingType.ClrEquivalentType;
            }

            return typeof(object);
        }

        /// <summary>
        /// True if the EdmProperty is a key of its DeclaringType, False otherwise.
        /// </summary>
        public bool IsKey(EdmProperty property)
        {
            if (property != null && property.DeclaringType.BuiltInTypeKind == BuiltInTypeKind.EntityType)
            {
                return ((EntityType)property.DeclaringType).KeyMembers.Contains(property);
            }

            return false;
        }

        /// <summary>
        /// True if the EdmProperty TypeUsage is Nullable, False otherwise.
        /// </summary>
        public bool IsNullable(EdmProperty property)
        {
            return property != null && IsNullable(property.TypeUsage);
        }

        /// <summary>
        /// True if the TypeUsage is Nullable, False otherwise.
        /// </summary>
        public bool IsNullable(TypeUsage typeUsage)
        {
            Facet nullableFacet = null;
            if (typeUsage != null &&
                typeUsage.Facets.TryGetValue("Nullable", true, out nullableFacet))
            {
                return (bool)nullableFacet.Value;
            }

            return false;
        }

        /// <summary>
        /// If the passed in TypeUsage represents a collection this method returns final element
        /// type of the collection, otherwise it returns the value passed in.
        /// </summary>
        public TypeUsage GetElementType(TypeUsage typeUsage)
        {
            if (typeUsage == null)
            {
                return null;
            }

            if (typeUsage.EdmType is CollectionType)
            {
                return GetElementType(((CollectionType)typeUsage.EdmType).TypeUsage);
            }
            else
            {
                return typeUsage;
            }
        }

        /// <summary>
        /// Returns the NavigationProperty that is the other end of the same association set if it is
        /// available, otherwise it returns null.
        /// </summary>
        public NavigationProperty Inverse(NavigationProperty navProperty)
        {
            if (navProperty == null)
            {
                return null;
            }

            EntityType toEntity = navProperty.ToEndMember.GetEntityType();
            return toEntity.NavigationProperties
                .SingleOrDefault(n => Object.ReferenceEquals(n.RelationshipType, navProperty.RelationshipType) && !Object.ReferenceEquals(n, navProperty));
        }

        /// <summary>
        /// Given a property on the dependent end of a referential constraint, returns the corresponding property on the principal end.
        /// Requires: The association has a referential constraint, and the specified dependentProperty is one of the properties on the dependent end.
        /// </summary>
        public EdmProperty GetCorrespondingPrincipalProperty(NavigationProperty navProperty, EdmProperty dependentProperty)
        {
            if (navProperty == null)
            {
                throw new ArgumentNullException("navProperty");
            }

            if (dependentProperty == null)
            {
                throw new ArgumentNullException("dependentProperty");
            }

            ReadOnlyMetadataCollection<EdmProperty> fromProperties = GetPrincipalProperties(navProperty);
            ReadOnlyMetadataCollection<EdmProperty> toProperties = GetDependentProperties(navProperty);
            return fromProperties[toProperties.IndexOf(dependentProperty)];
        }

        /// <summary>
        /// Given a property on the principal end of a referential constraint, returns the corresponding property on the dependent end.
        /// Requires: The association has a referential constraint, and the specified principalProperty is one of the properties on the principal end.
        /// </summary>
        public EdmProperty GetCorrespondingDependentProperty(NavigationProperty navProperty, EdmProperty principalProperty)
        {
            if (navProperty == null)
            {
                throw new ArgumentNullException("navProperty");
            }

            if (principalProperty == null)
            {
                throw new ArgumentNullException("principalProperty");
            }

            ReadOnlyMetadataCollection<EdmProperty> fromProperties = GetPrincipalProperties(navProperty);
            ReadOnlyMetadataCollection<EdmProperty> toProperties = GetDependentProperties(navProperty);
            return toProperties[fromProperties.IndexOf(principalProperty)];
        }

        /// <summary>
        /// Gets the collection of properties that are on the principal end of a referential constraint for the specified navigation property.
        /// Requires: The association has a referential constraint.
        /// </summary>
        public ReadOnlyMetadataCollection<EdmProperty> GetPrincipalProperties(NavigationProperty navProperty)
        {
            if (navProperty == null)
            {
                throw new ArgumentNullException("navProperty");
            }

            return ((AssociationType)navProperty.RelationshipType).ReferentialConstraints[0].FromProperties;
        }

        /// <summary>
        /// Gets the collection of properties that are on the dependent end of a referential constraint for the specified navigation property.
        /// Requires: The association has a referential constraint.
        /// </summary>
        public ReadOnlyMetadataCollection<EdmProperty> GetDependentProperties(NavigationProperty navProperty)
        {
            if (navProperty == null)
            {
                throw new ArgumentNullException("navProperty");
            }

            return ((AssociationType)navProperty.RelationshipType).ReferentialConstraints[0].ToProperties;
        }

        /// <summary>
        /// True if this entity type requires the HandleCascadeDelete method defined and the method has
        /// not been defined on any base type
        /// </summary>
        public bool NeedsHandleCascadeDeleteMethod(ItemCollection itemCollection, EntityType entity)
        {
            bool needsMethod = ContainsCascadeDeleteAssociation(itemCollection, entity);
            // Check to make sure no base types have already declared this method
            EntityType baseType = entity.BaseType as EntityType;
            while (needsMethod && baseType != null)
            {
                needsMethod = !ContainsCascadeDeleteAssociation(itemCollection, baseType);
                baseType = baseType.BaseType as EntityType;
            }
            return needsMethod;
        }

        /// <summary>
        /// True if this entity type participates in any relationships where the other end has an OnDelete
        /// cascade delete defined, or if it is the dependent in any identifying relationships
        /// </summary>
        private bool ContainsCascadeDeleteAssociation(ItemCollection itemCollection, EntityType entity)
        {
            return itemCollection.GetItems<AssociationType>().Where(a =>
                    ((RefType)a.AssociationEndMembers[0].TypeUsage.EdmType).ElementType == entity && IsCascadeDeletePrincipal(a.AssociationEndMembers[1]) ||
                    ((RefType)a.AssociationEndMembers[1].TypeUsage.EdmType).ElementType == entity && IsCascadeDeletePrincipal(a.AssociationEndMembers[0])).Any();
        }

        /// <summary>
        /// True if the source end of the specified navigation property is the principal in an identifying relationship.
        /// or if the source end has cascade delete defined.
        /// </summary>
        public bool IsCascadeDeletePrincipal(NavigationProperty navProperty)
        {
            if (navProperty == null)
            {
                throw new ArgumentNullException("navProperty");
            }

            return IsCascadeDeletePrincipal((AssociationEndMember)navProperty.FromEndMember);
        }

        /// <summary>
        /// True if the specified association end is the principal in an identifying relationship.
        /// or if the association end has cascade delete defined.
        /// </summary>
        public bool IsCascadeDeletePrincipal(AssociationEndMember associationEnd)
        {
            if (associationEnd == null)
            {
                throw new ArgumentNullException("associationEnd");
            }

            return associationEnd.DeleteBehavior == OperationAction.Cascade || IsPrincipalEndOfIdentifyingRelationship(associationEnd);
        }

        /// <summary>
        /// True if the specified association end is the principal end in an identifying relationship.
        /// In order to be an identifying relationship, the association must have a referential constraint where all of the dependent properties are part of the dependent type's primary key.
        /// </summary>
        public bool IsPrincipalEndOfIdentifyingRelationship(AssociationEndMember associationEnd)
        {
            if (associationEnd == null)
            {
                throw new ArgumentNullException("associationEnd");
            }

            ReferentialConstraint refConstraint = ((AssociationType)associationEnd.DeclaringType).ReferentialConstraints.Where(rc => rc.FromRole == associationEnd).SingleOrDefault();
            if (refConstraint != null)
            {
                EntityType entity = refConstraint.ToRole.GetEntityType();
                return !refConstraint.ToProperties.Where(tp => !entity.KeyMembers.Contains(tp)).Any();
            }
            return false;
        }

        /// <summary>
        /// True if the specified association type is an identifying relationship.
        /// In order to be an identifying relationship, the association must have a referential constraint where all of the dependent properties are part of the dependent type's primary key.
        /// </summary>
        public bool IsIdentifyingRelationship(AssociationType association)
        {
            if (association == null)
            {
                throw new ArgumentNullException("association");
            }

            return IsPrincipalEndOfIdentifyingRelationship(association.AssociationEndMembers[0]) || IsPrincipalEndOfIdentifyingRelationship(association.AssociationEndMembers[1]);
        }

        /// <summary>
        /// requires: firstType is not null
        /// effects: if secondType is among the base types of the firstType, return true,
        /// otherwise returns false.
        /// when firstType is same as the secondType, return false.
        /// </summary>
        public bool IsSubtypeOf(EdmType firstType, EdmType secondType)
        {
            if (secondType == null)
            {
                return false;
            }

            // walk up firstType hierarchy list
            for (EdmType t = firstType.BaseType; t != null; t = t.BaseType)
            {
                if (t == secondType)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the subtype of the EntityType in the current itemCollection
        /// </summary>
        public IEnumerable<EntityType> GetSubtypesOf(EntityType type, ItemCollection itemCollection, bool includeAbstractTypes)
        {
            if (type != null)
            {
                IEnumerable<EntityType> typesInCollection = itemCollection.GetItems<EntityType>();
                foreach (EntityType typeInCollection in typesInCollection)
                {
                    if (type.Equals(typeInCollection) == false && this.IsSubtypeOf(typeInCollection, type))
                    {
                        if (includeAbstractTypes || !typeInCollection.Abstract)
                        {
                            yield return typeInCollection;
                        }
                    }
                }
            }
        }

        public static bool TryGetStringMetadataPropertySetting(MetadataItem item, string propertyName, out string value)
        {
            value = null;
            MetadataProperty property = item.MetadataProperties.FirstOrDefault(p => p.Name == propertyName);
            if (property != null)
            {
                value = (string)property.Value;
            }
            return value != null;
        }
    }
}
