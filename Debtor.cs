using System;
using Npoi.Mapper;
using NPOI;
using Npoi.Mapper.Attributes;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace UCCXML;

public partial class Debtor
{
	[Column("Customer First Name")]
	public string FirstName { get; set; }
	[Column("Customer Last Name")]
	public string LastName { get; set; }
	[Column("Customer City")]
	public string City { get; set; }
	[Column("Customer State")]
	public string State { get; set; }
	[Column("Customer St Address")]
	public string StAddress { get; set; }
	[Column("Customer Zip Code")]
	public string ZipCode { get; set; }
	[Column("Deal Name")]
	public string DealName { get; set; }
	[Column("Tranche")]
	public string Tranche { get; set; }
	[Column("Full Address")]
	public string FullAddress { get; set; }
	[Column("Deal Type")]
	public string DealType { get; set; }
	public string? ReceiptId { get; set; } = null;
	private string? _fileName = null;
	public string FileName { 
		get
		{
			_fileName ??= invalidCharsExp().Replace(DealName, "-");
			return _fileName;
		} } 
    public Debtor() { }

	public XElement AsXElement()
	{
		return new XElement("Names",
			GetName(),
			new XElement("MailAddress", StAddress),
			new XElement("City", City),
			new XElement("State", State),
			new XElement("PostalCode", ZipCode),
			new XElement("Country", "USA")
			);
	}

	bool IsCommercial() => DealType.Contains("Commercial");

	public XElement GetName()
	{
		if (IsCommercial())
		{
			string OrganizationName = Regex.Match(DealName, @$"(.*) (?:{StAddress})").Value;
			return new XElement("OrganizationName", OrganizationName);
		} else
		{
			return new XElement("IndividualName",
					new XElement("Surname", LastName),
					new XElement("FirstPersonalName", FirstName)
					);
		}
	}

	public XElement AsSubmissionXElement(Details details)
	{
		return Document.Make(details, this);
	}

	internal async Task SubmitAsync(CalicoApi calicoApi, Details details)
	{
		var result = await calicoApi.SubmitFilingsAsync(AsSubmissionXElement(details).ToString());
		if (result.ReceiptId is not null) ReceiptId = result.ReceiptId;
		else Console.WriteLine("Failed to retrieve receipt id.\nResponse below.\n--------------\n" + (await result.Response.Content.ReadAsStringAsync()));
	}

	internal async Task<byte[]?> GetDocumentAsync(CalicoApi calicoApi)
	{
		if (ReceiptId is null) throw new Exception("Attempting to retrieve document without submitting.");
		var result = await calicoApi.GetDocumentAsync(ReceiptId);
		if (result.Code == System.Net.HttpStatusCode.OK) return result.Document;
		Console.WriteLine($"Document not ready: {result.Code}");
		return null;
	}

    [GeneratedRegex(@"[ <>:""/\\|?*]")]
    private static partial Regex invalidCharsExp();
}
