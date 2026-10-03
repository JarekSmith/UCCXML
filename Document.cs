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
    public Document() { }

    static internal XElement Make(Details details, Debtor debtor)
    {
        string transType = "Initial";
        string collateralText = details.CollateralText[debtor.DealType];
        string collateralDesignationType = "NODesignation"; // NODesignation, Trust, or PersonalRepresentative

        return new XElement("Document",
            new XElement("XMLVersion",
                new XAttribute("Version", details.VersionNumber)
            ),
            new XElement("Header",
                new XElement("Filer", 
                    MakeOrganizationNames(details.Filer), 
                    new XElement("ClientAccountNum", details.AccountNumber),
                    new XElement("ContactName", details.ContactInfo.Name),
                    new XElement("ContactEmail", details.ContactInfo.Email),
                    new XElement("ContactPhone", details.ContactInfo.PhoneNumber)
                )
            ),
            new XElement("Record",
                new XElement("TransType", new XAttribute("Type", transType)),
                new XElement("Debtors", new XElement("DebtorName", debtor.AsXElement())),
                new XElement("SecuredParties",
                    new XElement("SecuredName", MakeOrganizationNames(
                        GetSecuredParty(debtor, details)
                        ))
                ),
                new XElement("Collateral", new XElement("ColText", collateralText)),
                new XElement("CollateralDesignation", new XAttribute("Type", collateralDesignationType))
            )
        );
    }

    static XElement MakeOrganizationNames(OrganizationNames organization) {
        return new XElement("Names",
                new XElement("OrganizationName", organization.OrganizationName),
                new XElement("MailAddress", organization.StAddress),
                new XElement("City", organization.City),
                new XElement("State", organization.State),
                new XElement("PostalCode", organization.ZipCode),
                new XElement("Country", "USA")
        );
    }

    static OrganizationNames GetSecuredParty(Debtor debtor, Details details)
    {
        return new(debtor.Tranche, details.Filer.StAddress, details.Filer.City, details.Filer.State, details.Filer.ZipCode);
    }
}

