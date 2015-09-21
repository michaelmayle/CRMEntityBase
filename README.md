# CRMEntityBase
An MS CRM EntityBase base class that allows simple data objects to be built using the CRM as a data store.


I was looking for a way to use CRM as my back-end data store for a new project on which I was working. I created some new objects in CRM that mapped one-to-many to contacts and accounts, and some supporting objects that linked to them. I needed to use these classes in a variety of places, and I did not want to be burdened with a ton of extra fields, poorly named with crazy prefixes that came from CRM.
To battle the CRM Bloat, I created some trim classes that were basically wrappers around the CRM entity classes. As I programmed, I fanaticized about having a CRM Entity Base class where I could map the CRM Entity Properties to my classes’ properties and then just call normal CRUD (load/save/delete) methods and hide the fact that these were CRM classes at all.
So, I decided to put in the time and do it. It took a while, but I have what I consider to be a very nice, compact, and easy to use CRMEntityBaseClass. I simply inherit from the class to link to any CRM object.

(By the way, also included in the project is the Config class I mentioned in a post on my blog that allows your config file to be aware of where it is deployed… http://michaelmayle.com/2015/06/02/custom-configuration-that-knows-when-it-has-been-deployed/)

In the following example I have created tiny wrapper classes for the Account and Contact entities in CRM.

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

    public string FullName
    {
        get
        {
            return this.FirstName + this.LastName;
        }
    }
}

Now, using these classes is as easy as instantiating one, calling save, loading, deleting, whatever, and they actually save to their corresponding CRM entities. A couple of notes:
•	The classes are tied to their corresponding CRM entity via the "CRMClassName" attribute.
•	The class properties are tied to the CRM attributes via the "CRMAttributeData" attribute.
•	If the property is actually a CRM "Relationship" generated field, you also need to include the "CRMReferenceObjectName" attribute to tell it which object to which it links.
•	In the example above I chose (at random) to demonstrate a property that would not be written to the database on the "Save()" call, by simply decorating that property with the "IgnoreSave" attribute.
•	I did not have to do that for the "FullName" property, since it is not even decorated with the "CRMAttributeData" attribute.

This is a fluid project; it is a work in progress. Please comment or contribute to the project on the GitHub site if you have any input.


