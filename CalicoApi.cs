namespace UCCXML;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Drawing.Printing;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

partial class CalicoApi
{
	static readonly string _baseUrl = @"https://calico.sos.ca.gov";
	static readonly string _apiUrl = "ucc/v1/api/";
	readonly HttpClient _client;
	public CalicoApi(string apiKey, string sosKey)
	{
		_client = new() { BaseAddress = new Uri(_baseUrl) };
		_client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
		_client.DefaultRequestHeaders.Add("SOS-Key", sosKey);
	}
	async Task<HttpResponseMessage> SendGET(string endpoint)
	{
		var response = await _client.GetAsync(_apiUrl + endpoint);
		return response;
	}
	public async static Task<(string Message, string Status)?> GetMessageAndStatusAsync(HttpResponseMessage response)
	{
		XElement? statusElement = await GetXElementAsync(response, "Status");
		XAttribute? statusAttribute = statusElement?.Attribute("value");
		if (statusElement is null || statusAttribute is null) return null;
		return (statusElement.Value, statusAttribute.Value);
	}
	public async static Task<XElement?> GetXElementAsync(HttpResponseMessage response, string elementName)
	{
		string xmlString = await response.Content.ReadAsStringAsync();
		XDocument document = XDocument.Parse(xmlString);
		return document.Descendants(elementName).FirstOrDefault();
	}
	public async Task<(HttpResponseMessage Response, string? ReceiptId)> SubmitFilingsAsync(string _xmlData)
	{
		var requestBody = new StringContent(_xmlData, Encoding.UTF8, new MediaTypeHeaderValue("applications/xml"));
		var response = await _client.PostAsync(_apiUrl + "FilingAsync", requestBody); 
		string responseContent = await response.Content.ReadAsStringAsync();

		string? receiptId = null;

		if (response.StatusCode == HttpStatusCode.Accepted)
		{
			XmlDocument contentDocument = new();
			contentDocument.LoadXml(responseContent);
			XmlNode? receiptNode = contentDocument?.DocumentElement?.SelectSingleNode("//DocumentReceiptID");
			receiptId = receiptNode?.InnerText;
		}
		return (response, receiptId);
	}

	/// <summary>
	/// Asynchronously retrieves the current balance.
	/// </summary>
	/// <returns>A tuple containing the HTTP status code and the balance as a string, or null if the balance could not be retrieved.</returns>
	public async Task<(HttpStatusCode Code, string? Balance)> GetBalanceAsync()
	{
		var response = await SendGET("Balance");
		string? balance = response.StatusCode == HttpStatusCode.OK ? (await GetXElementAsync(response, "Balance"))?.Value : null;
		return (response.StatusCode, balance);
	}

	public async Task<(HttpStatusCode Code, string Status)> GetServerStatusAsync()
	{
		var response = await SendGET("ServerStatus");
		string content = await response.Content.ReadAsStringAsync();
		Match apiKeyStatus = apiKeyStatusExp().Match(content);
		Match sosKeyStatus = sosKeyStatusExp().Match(content);
        Match profileStatus = profileStatusExp().Match(content);
		string status = $"Api Key: {apiKeyStatus.Value}\nSOS Key: {sosKeyStatus.Value}\nCustomer Profile: {profileStatus.Value}";
		return (response.StatusCode, status);
	}

	/// <summary>
	/// Gets status of document after being filed.
	/// </summary>
	/// <param name="documentId"></param>
	/// <returns>Http status code.<br/>
	/// <b>Accepted</b> -> Submission was successful and is in queue for processing.<br/>
	/// <b>OK</b> -> The filing has been processed and filed.<br/>
	/// <b>Processing</b> -> The xmlData is being processed.<br/>
	/// <b>Unauthorized</b> -> Invalid ApiKey or SosKey.<br/>
	/// <b>NotFound</b> -> Invalid Document Receipt ID provided.
	/// </returns>
	public async Task<HttpStatusCode> GetDocumentStatusAsync(string documentId)
	{
		var response = await SendGET("Status/?id=" + documentId);
		return response.StatusCode;
	}

	/// <summary>
	/// Tries to retrieve completed document from server.
	/// </summary>
	/// <param name="documentId"></param>
	/// <param name="fileName"></param>
	/// <returns>Document in a byte array, or null. Check Code for success:<br/>
	/// <b>Accepted</b> -> The xmlData was valid and is in queue for processing.<br/>
	/// <b>OK</b> -> byte[] of document will be returned.<br/>
	/// <b>Processing</b> -> The xmlData is being processed.<br/>
	/// <b>Unauthorized</b> -> Invalid ApiKey or SosKey.<br/>
	/// <b>NotFound</b> -> Invalid Document Receipt ID provided.
	/// </returns>
	public async Task<(HttpStatusCode Code, byte[]? Document)> GetDocumentAsync(string documentId)
	{
		var response = await SendGET("FilingAsync/?id=" + documentId);
		byte[] document = await response.Content.ReadAsByteArrayAsync();
		return response.StatusCode switch
		{
			HttpStatusCode.OK => (response.StatusCode, document),
			_ => (response.StatusCode, null)
		};
	}

    [GeneratedRegex(@"(?<=\(API-Key\):)([^<]*)")]
    private static partial Regex apiKeyStatusExp();
    [GeneratedRegex(@"(?<=SOS-Key:)([^<]*)")]
    private static partial Regex sosKeyStatusExp();
    [GeneratedRegex(@"(?<=CustomerProfile:)(.*)")]
    private static partial Regex profileStatusExp();
}