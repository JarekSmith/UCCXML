using System;
using Npoi.Mapper;
using NPOI;
using Npoi.Mapper.Attributes;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace UCCXML;

public class Debtor
{
	[Column("Deal Name")]
	public string DealName { get; set; }
	[Column("Tranche")]
	public string Tranche { get; set; }
	[Column("Full Address")]
	public string FullAddress { get; set; }
	[Column("Deal Type")]
	public string DealType { get; set; }

    public Debtor() { }

	private (string firstName, string lastName) GetName()
	{
		Match result = Regex.Match(DealName, @$"(?<firstName>\S*) (?<lastName>.*) (?<stAddress>{GetStAddress()})");
		if (!result.Success) throw new Exception($"DealName should have the form 'Firstname Surname[ additional names] StAddress'. Got '{DealName}'");
		string firstName = result.Groups["firstName"].Value;
		string lastName = result.Groups["lastName"].Value;
		return (firstName, lastName);
	}

	public string GetFirstName() => GetName().firstName;

	public string GetLastName() => GetName().lastName;

    private List<string> AddressParts()
    {
		List<string> addressParts = [..FullAddress.Split(",")];
		if (addressParts.Count != 3) throw new Exception($"Full address should have the form 'StAddress, City, State Zip'. Got '{FullAddress}'");
		return addressParts;
	}
	public string GetStAddress() => AddressParts()[0].Trim();

	public string GetCity() => AddressParts()[1].Trim();

	private List<string> StateAndZip() 
	{
		List<string> stateAndZip = [.. AddressParts()[2].Trim().Split(" ")];
		if (stateAndZip.Count != 2) throw new Exception($"Full address should have the form 'StAddress, City, State Zip'. Got '{FullAddress}'");
		return stateAndZip;
	}
	public string GetState() => StateAndZip()[0];

	public string GetZipCode() => StateAndZip()[1];

	public XElement AsXElement()
	{
		return new XElement("Names",
			new XElement("IndividualName",
				new XElement("Surname", GetLastName()),
				new XElement("FirstPersonalName", GetFirstName())
			),
			new XElement("MailAddress", GetStAddress()),
			new XElement("City", GetCity()),
			new XElement("State", GetState()),
			new XElement("PostalCode", GetZipCode()),
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
