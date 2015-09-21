using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRMEntityBase
{
    #region example CRM classes
[CRMClassName("account")]
public class CRMAccount : CRMEntityBase
{
    [EntityID]
    [CRMAttributeData("accountid")]
    public string CRMAccountID { get; set; }

    [CRMAttributeData("name")]
    public string CRMAccountName { get; set; }
}

[CRMClassName("contact")]
public class CRMContact : CRMEntityBase
{
    [EntityID]
    [CRMAttributeData("contactid")]
    public string CRMContactID { get; set; }

    [CRMAttributeData("accountid")]
    [CRMReferenceObjectName("account")]
    public string CRMAccountID { get; set; }

    [IgnoreSave]
    [CRMAttributeData("email")]
    public string Email { get; set; }

    [CRMAttributeData("firstname")]
    public string FirstName { get; set; }

    [CRMAttributeData("lastname")]
    public string LastName { get; set; }

    [IgnoreLoad]
    [IgnoreSave]
    public string FullName
    {
        get
        {
            return this.FirstName + this.LastName;
        }
    }
}
    #endregion
}
