using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.Data;
using System.Collections;
using System.ServiceModel.Description;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System.Net.Security;
using Microsoft.Xrm.Sdk;

namespace CRMEntityBase
{
    [Serializable]
    public abstract class CRMEntityBase
    {
        #region static constructor - sets up the service.
        public static IOrganizationService service;
        public static void InitCRMService()
        {
            if (service == null)
            {
                Uri organizationUriIFD = new Uri(Config.GetValue("CRMUrl"));

                ClientCredentials credentials = new ClientCredentials();
                credentials.UserName.UserName = Config.GetValue("CRMUsername");
                credentials.UserName.Password = Config.GetValue("CRMPassword");

                IServiceConfiguration<IOrganizationService> config = ServiceConfigurationFactory.CreateConfiguration<IOrganizationService>(organizationUriIFD);

                using (OrganizationServiceProxy serviceProxy = new OrganizationServiceProxy(config, credentials))
                {
                    serviceProxy.ServiceConfiguration.CurrentServiceEndpoint.Behaviors.Add(new ProxyTypesBehavior());
                    service = serviceProxy;
                }
            }
        }
        #endregion

        #region constructors
        public CRMEntityBase()
        { }
        #endregion

        #region EntityBase methods
        public virtual string GetEntityIDFieldName()
        {
            PropertyInfo pi = this.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttributes(typeof(EntityID), false).Any());

            if (pi != null)
                return pi.Name;
            else
                throw new Exception("Class does not have a designated entity ID field");
        }

        public virtual string GetEntityIDFieldValue()
        {
            PropertyInfo pi = this.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttributes(typeof(EntityID), false).Any());

            if (pi != null)
                return pi.GetValue(this, null) as string;
            else
                throw new Exception("Class does not have a designated entity ID field");
        }

        public virtual void SetEntityIDFieldValue(string valueIn)
        {
            PropertyInfo pi = this.GetType().GetProperties().FirstOrDefault(x => x.GetCustomAttributes(typeof(EntityID), false).Any());

            if (pi != null)
                pi.SetValue(this, valueIn, null);
            else
                throw new Exception("Class does not have a designated entity ID field");
        }
        #endregion

        #region CRUD methods
        public virtual bool Load()
        {
            CRMEntityBase.InitCRMService();

            CRMClassName className = (CRMClassName)this.GetType().GetCustomAttributes(typeof(CRMClassName), true)[0];

            Guid _tmpGuid;

            if (!Guid.TryParse(this.GetEntityIDFieldValue(), out _tmpGuid))
                return false;

            ColumnSet entityFields = new ColumnSet(this.GetCRMFieldsFromObject().Select(x => x.PropName).ToArray());

            try
            {
                Entity crmBaseEntity = service.Retrieve(className.Name, _tmpGuid, entityFields);

                if (crmBaseEntity != null)
                {
                    this.SetObjectFieldsFromCRMObject(crmBaseEntity);
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public virtual string Save()
        {
            CRMEntityBase.InitCRMService();
            Guid _tmpGuid;

            CRMClassName className = (CRMClassName)this.GetType().GetCustomAttributes(typeof(CRMClassName), true)[0];

            Entity _crmEntity = new Entity(className.Name);

            foreach (CRMPropertyValue _crmProp in this.GetCRMFieldsFromObject())
            {
                if (_crmProp.IgnoreSave)
                    continue;

                if (_crmProp.IsEntityID)
                {
                    if (_crmProp.Value != null && Guid.TryParse(_crmProp.Value.ToString(), out _tmpGuid))
                    {
                        _crmEntity.Attributes.Add(_crmProp.PropName, _tmpGuid);
                        _crmEntity.Id = _tmpGuid;
                    }
                }
                else
                {
                    if (_crmProp.EntityReferenceName == null)
                    {
                        if (_crmProp.IsIntEnum)
                            _crmEntity.Attributes.Add(_crmProp.PropName, new OptionSetValue((int)_crmProp.Value));
                        else if (_crmProp.IsMoney)
                            _crmEntity.Attributes.Add(_crmProp.PropName, new Money((decimal)_crmProp.Value));
                        else
                            _crmEntity.Attributes.Add(_crmProp.PropName, _crmProp.Value);
                    }
                    else
                    {
                        if (_crmProp.Value != null && Guid.TryParse(_crmProp.Value.ToString(), out _tmpGuid))
                            _crmEntity.Attributes.Add(_crmProp.PropName, new EntityReference(_crmProp.EntityReferenceName, _tmpGuid));
                    }
                }
            }

            if (!Guid.TryParse(this.GetEntityIDFieldValue(), out _tmpGuid))
                this.SetEntityIDFieldValue(service.Create(_crmEntity).ToString());
            else
                service.Update(_crmEntity);

            return this.GetEntityIDFieldValue();
        }

        public virtual void Delete()
        {
            CRMEntityBase.InitCRMService();

            CRMClassName className = (CRMClassName)this.GetType().GetCustomAttributes(typeof(CRMClassName), true)[0];

            Guid _tmpGuid;

            if (!Guid.TryParse(this.GetEntityIDFieldValue(), out _tmpGuid))
                return;

            service.Delete(className.Name, _tmpGuid);
        }

        public override bool Equals(object obj)
        {
            return this.ObjectEquals(obj);
        }

        public void SetObjectFieldsFromCRMObject(Entity crmBaseEntity)
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();

            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                object[] customAttributes = pi.GetCustomAttributes(typeof(CRMAttributeData), false);

                if (customAttributes.Length <= 0)
                    continue;

                CRMAttributeData crmAttribute = (CRMAttributeData)customAttributes[0];
                object baseAttribute = crmBaseEntity.Attributes.FirstOrDefault(x => x.Key == crmAttribute.Name).Value;

                if (baseAttribute == null)
                    continue;

                if (pi.CanWrite && !pi.GetCustomAttributes(typeof(IgnoreLoad), false).Any())
                {
                    if (pi.PropertyType.IsEnum)
                    {
                        if (baseAttribute is OptionSetValue)
                            pi.SetValue(this, Convert.ChangeType(((OptionSetValue)baseAttribute).Value, Enum.GetUnderlyingType(pi.PropertyType)), null);
                        else
                            pi.SetValue(this, Convert.ChangeType(baseAttribute, Enum.GetUnderlyingType(pi.PropertyType)), null);
                    }
                    else
                    {
                        object safeValue = null;

                        if (baseAttribute.GetType() == pi.PropertyType)
                        {
                            safeValue = baseAttribute;
                        }
                        else
                        {
                            if ((crmAttribute.IsMoney || (baseAttribute is Money)) && pi.PropertyType == typeof(decimal))
                                safeValue = ((Money)baseAttribute).Value;
                            else if (pi.PropertyType == typeof(string))
                            {
                                if (baseAttribute is EntityReference)
                                    safeValue = ((EntityReference)baseAttribute).Id.ToString();
                                else
                                    safeValue = baseAttribute.ToString();
                            }
                            else
                            {
                                Type t = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
                                safeValue = (baseAttribute == null) ? Data.GetDefaultValue(t) : Convert.ChangeType(baseAttribute, t);
                            }
                        }

                        pi.SetValue(this, safeValue, null);
                    }
                }
            }
        }

        public virtual List<CRMPropertyValue> GetCRMFieldsFromObject()
        {
            List<CRMPropertyValue> fields = new List<CRMPropertyValue>();

            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                bool _ignoreSave = pi.GetCustomAttributes(typeof(IgnoreSave), false).Any();
                object[] customAttributes = pi.GetCustomAttributes(typeof(CRMAttributeData), false);
                object[] customAttributesRef = pi.GetCustomAttributes(typeof(CRMReferenceObjectName), false);
                bool _isEntityID = pi.GetCustomAttributes(typeof(EntityID), false).Any();

                if (customAttributes.Length <= 0)
                    continue;

                CRMAttributeData crmAttribute = (CRMAttributeData)customAttributes[0];
                CRMReferenceObjectName crmRefName = null;

                if (customAttributesRef.Length > 0)
                    crmRefName = (CRMReferenceObjectName)customAttributesRef[0];

                object value = pi.GetValue(this, null);

                bool isIntEnum = false;

                if (pi.PropertyType.IsEnum)
                {
                    isIntEnum = (Enum.GetUnderlyingType(pi.PropertyType) == typeof(int));
                    value = Convert.ChangeType(value, Enum.GetUnderlyingType(pi.PropertyType));
                }

                fields.Add(new CRMPropertyValue { PropName = crmAttribute.Name, IsMoney = crmAttribute.IsMoney, Value = value, EntityReferenceName = ((crmRefName == null) ? null : crmRefName.Name), IgnoreSave = _ignoreSave, IsIntEnum = isIntEnum, IsEntityID = _isEntityID });
            }

            return fields;
        }
        #endregion

        #region some good useful find methods
        public static List<T> FindAllByCRMEntityConditions<T>(params CRMCondition[] crmFieldNameToSearchValue) where T : CRMEntityBase
        {
            List<T> retVal = new List<T>();

            T _crmEntityBase = Activator.CreateInstance<T>();

            CRMEntityBase.InitCRMService();
            CRMClassName className = (CRMClassName)_crmEntityBase.GetType().GetCustomAttributes(typeof(CRMClassName), true)[0];

            EntityCollection crmEntities = CRMEntityBase.FindAllCRMEntitiesByConditions(className.Name, new ColumnSet(_crmEntityBase.GetCRMFieldsFromObject().Select(x => x.PropName).ToArray()), crmFieldNameToSearchValue);

            foreach (Entity _entity in crmEntities.Entities)
            {
                _crmEntityBase = Activator.CreateInstance<T>();
                _crmEntityBase.SetObjectFieldsFromCRMObject(_entity);

                retVal.Add(_crmEntityBase);
            }

            return retVal;
        }

        public static List<T> FindAllByFilter<T>(FilterExpression _expression) where T : CRMEntityBase
        {
            List<T> retVal = new List<T>();

            T _crmEntityBase = Activator.CreateInstance<T>();

            CRMEntityBase.InitCRMService();
            CRMClassName className = (CRMClassName)_crmEntityBase.GetType().GetCustomAttributes(typeof(CRMClassName), true)[0];

            EntityCollection crmEntities = CRMEntityBase.FindAllCRMEntitiesByConditions(className.Name, new ColumnSet(_crmEntityBase.GetCRMFieldsFromObject().Select(x => x.PropName).ToArray()), _expression);

            foreach (Entity _entity in crmEntities.Entities)
            {
                _crmEntityBase = Activator.CreateInstance<T>();
                _crmEntityBase.SetObjectFieldsFromCRMObject(_entity);

                retVal.Add(_crmEntityBase);
            }

            return retVal;
        }

        public bool FindByCRMEntityConditions(params CRMCondition[] crmFieldNameToSearchValue)
        {
            CRMEntityBase.InitCRMService();
            CRMClassName className = (CRMClassName)this.GetType().GetCustomAttributes(typeof(CRMClassName), true)[0];

            EntityCollection crmEntities = CRMEntityBase.FindAllCRMEntitiesByConditions(className.Name, new ColumnSet(this.GetCRMFieldsFromObject().Select(x => x.PropName).ToArray()), crmFieldNameToSearchValue);

            if (crmEntities.Entities.Count <= 0)
                return false;

            this.SetObjectFieldsFromCRMObject(crmEntities.Entities[0]);

            return true;
        }

        public bool FindByFilter(FilterExpression _expression)
        {
            CRMEntityBase.InitCRMService();
            CRMClassName className = (CRMClassName)this.GetType().GetCustomAttributes(typeof(CRMClassName), true)[0];

            EntityCollection crmEntities = CRMEntityBase.FindAllCRMEntitiesByConditions(className.Name, new ColumnSet(this.GetCRMFieldsFromObject().Select(x => x.PropName).ToArray()), _expression);

            if (crmEntities.Entities.Count <= 0)
                return false;

            this.SetObjectFieldsFromCRMObject(crmEntities.Entities[0]);

            return true;
        }

        public static EntityCollection FindAllCRMEntitiesByConditions(string _crmEntityName, ColumnSet _crmColumnSet, FilterExpression _expression)
        {
            CRMEntityBase.InitCRMService();

            List<CRMEntityBase> _entities = new List<CRMEntityBase>();

            QueryExpression query = new QueryExpression();
            query.EntityName = _crmEntityName;
            query.Criteria = _expression;
            query.ColumnSet = _crmColumnSet;

            EntityCollection crmEntities = service.RetrieveMultiple(query);

            return crmEntities;
        }

        public static EntityCollection FindAllCRMEntitiesByConditions(string _crmEntityName, ColumnSet _crmColumnSet, params CRMCondition[] crmFieldNameToSearchValue)
        {
            CRMEntityBase.InitCRMService();

            List<CRMEntityBase> _entities = new List<CRMEntityBase>();

            FilterExpression filter = new FilterExpression();

            foreach (CRMCondition kvp in crmFieldNameToSearchValue)
                filter.AddCondition(new ConditionExpression(kvp.FieldName, ConditionOperator.Equal, kvp.Value));

            QueryExpression query = new QueryExpression();
            query.EntityName = _crmEntityName;
            query.Criteria = filter;
            query.ColumnSet = _crmColumnSet;

            EntityCollection crmEntities = service.RetrieveMultiple(query);

            return crmEntities;
        }
        #endregion
    }


    #region attributes
    public class EntityID : Attribute { }
    public class EntityName : Attribute { }
    public class IgnoreLoad : Attribute { }
    public class IgnoreSave : Attribute { }
    public class IgnoreEquals : Attribute { }

    public class CRMAttributeData : Attribute
    {
        public string Name { get; set; }
        public bool IsMoney { get; set; }

        public CRMAttributeData(string _name, bool _isMoney = false)
        {
            this.Name = _name;
            this.IsMoney = _isMoney;
        }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class CRMClassName : Attribute
    {
        public string Name { get; set; }

        public CRMClassName(string _name)
        {
            this.Name = _name;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class CRMReferenceObjectName : Attribute
    {
        public string Name { get; set; }

        public CRMReferenceObjectName(string _name)
        {
            this.Name = _name;
        }
    }

    public class CRMCondition
    {
        public string FieldName { get; set; }
        public virtual string Value { get; set; }

        public CRMCondition() { }

        public CRMCondition(string fieldName, string value)
        {
            this.FieldName = fieldName;
            this.Value = value;
        }
    }
    #endregion

    #region a couple of helper classes
    public class CRMDateTimeCondition : CRMCondition
    {
        public DateTime DateTimeValue { get; set; }

        public override string Value
        {
            get
            {
                string crmDateTimeFormatted =
                    this.DateTimeValue.ToString("yyyy-MM-dd")
                    + "T"
                    + this.DateTimeValue.ToString("HH:mm:ss");

                return crmDateTimeFormatted;
            }
        }

        public CRMDateTimeCondition(string fieldName, DateTime value)
        {
            this.FieldName = fieldName;
            this.DateTimeValue = value;
        }
    }

    public class CRMPropertyValue
    {
        public string PropName { get; set; }
        public string EntityReferenceName { get; set; }
        public object Value { get; set; }
        public bool IgnoreSave { get; set; }
        public bool IsIntEnum { get; set; }
        public bool IsEntityID { get; set; }
        public bool IsMoney { get; set; }
    }
    #endregion
}

