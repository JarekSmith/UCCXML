using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace UCCXML;

public class Document
{
    readonly List<Debtor> _debtors = [];
    public Document() { }

    public XElement Make(DocumentInformation documentInformation)
    {
        string transType = "Initial";
        string collateralDesignationType = "NODesignation"; // NODesignation, Trust, or PersonalRepresentative

        return new XElement("Document",
            new XElement("XMLVersion",
                new XAttribute("Version", documentInformation.Version)
            ),
            new XElement("Header",
                new XElement("Filer", 
                    MakeNames(documentInformation.Filer), 
                    new XElement("ClientAccountNum", documentInformation.AccountNumber),
                    new XElement("ContactName", documentInformation.ContactName),
                    new XElement("ContactEmail", documentInformation.ContactEmail),
                    new XElement("ContactPhone", documentInformation.ContactPhone)
                )
            ),
            new XElement("Record",
                new XElement("TransType", new XAttribute("Type", transType)),
                new XElement("Debtors", _debtors.Select(d => new XElement("DebtorName", d.AsXElement()))),
                new XElement("SecuredParties",
                    new XElement("SecuredName", MakeNames(documentInformation.SecuredParty))
                ),
                new XElement("Collateral", new XElement("ColText", documentInformation.CollateralText)),
                new XElement("CollateralDesignation", new XAttribute("Type", collateralDesignationType))
            )
        );
    }

    public void AddDebtor(params Debtor[] debtors)
    {
        _debtors.AddRange(debtors);
    }

    static XElement MakeNames(Names names) {
        return new XElement("Names",
                names.NameElement,
                new XElement("MailAddress", names.MailAddress),
                new XElement("City", names.City),
                new XElement("State", names.State),
                new XElement("PostalCode", names.PostalCode),
                new XElement("Country", "USA")
        );
    }
}

/// <summary>
/// An object that represents the name and details of an individual or organization.
/// </summary>
/// <param name="nameElement">An XElement "OrganizationName" with the name of an organization, or an XElement "IndividualName"
/// with child XElements for "Surname" and "FirstPersonalName"</param>
/// <param name="mailAddress">Mailing Address of the designated party.</param>
/// <param name="city">City of the designated party.</param>
/// <param name="state">2 character US postal identification code.<br/><b>Values:</b><br/>See Appendix A – State Codes.</param>
/// <param name="postalCode">The postal code for the party.</param>
public class Names(XElement nameElement, string mailAddress, string city, string state, string postalCode)
{
    public XElement NameElement = nameElement;
    public string MailAddress = mailAddress;
    public string City = city;
    public string State = state;
    public string PostalCode = postalCode;
    public string Country = "USA";
}

public class DocumentInformation(string version, string accountNumber, string contactName, string contactEmail,
        string contactPhone, Names filer, Names securedParty, string collateralText)
{
    public readonly string Version = version;
    public readonly string AccountNumber = accountNumber;
    public readonly string ContactName = contactName;
    public readonly string ContactEmail = contactEmail;
    public readonly string ContactPhone = contactPhone;
    public readonly Names Filer = filer;
    public readonly Names SecuredParty = securedParty;
    public readonly string CollateralText = collateralText;
}