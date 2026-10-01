using System;
using Npoi.Mapper;
using NPOI;
using Npoi.Mapper.Attributes;
using System.Xml.Linq;

namespace UCCXML;

public class Debtor
{
	[Column("Customer First Name")]
	public string? FirstName { get; set; }

	[Column("Customer Last Name")]
	public string? LastName { get; set; }

	[Column("Customer St Address")]
	public string? StAddress { get; set; }

	[Column("Customer City")]
	public string? City { get; set; }

	[Column("Customer State")]
	public string? State { get; set; }
	[Column("Customer Zip Code")]
	public string? ZipCode { get; set; }

	public Debtor() { }

	public XElement AsXElement()
	{
		return new XElement("Names",
			new XElement("IndividualName",
				new XElement("Surname", LastName),
				new XElement("FirstPersonalName", FirstName)
			),
			new XElement("MailAddress", StAddress),
			new XElement("City", City),
			new XElement("State", State),
			new XElement("PostalCode", ZipCode),
			new XElement("Country", "USA")
			);
	}

	public XElement AsSubmissionXElement(DocumentInformation documentInformation)
	{
		Document doc = new();
		doc.AddDebtor(this);
		return doc.Make(documentInformation);
	}
}
