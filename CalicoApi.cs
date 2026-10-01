namespace UCCXML;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Drawing.Printing;
using System.Text.RegularExpressions;
using System.Xml;

class CalicoApi
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
	HttpResponseMessage SendGET(string endpoint)
	{
		var response = _client.GetAsync(_apiUrl + endpoint).Result;
		return response;
	}
	static HttpResponse GetResponseStringAndCode(HttpResponseMessage message)
	{
		string value = Encoding.ASCII.GetString(message.Content.ReadAsByteArrayAsync().Result);
		HttpStatusCode code = message.StatusCode;
		return new(value, code);
	}
	static (string Message, string Status)? GetMessageAndStatus(HttpResponseMessage response)
	{
		string? message = null;
		string? status = null;
		if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
		{
			XmlDocument doc = new();
			doc.LoadXml(response.Content.ReadAsStringAsync().Result);
			XmlNode? statusNode = doc?.DocumentElement?.SelectSingleNode("//Status");
			message = statusNode?.InnerText;
			status = statusNode?.Attributes?["value"]?.Value;
		}
		return message is null || status is null ? null : (message, status);

	}
	public (HttpResponse Response, string? ReceiptId) SubmitFilings(string _xmlData)
	{
		var requestBody = new StringContent(_xmlData, Encoding.UTF8, new MediaTypeHeaderValue("applications/xml"));
		var response = _client.PostAsync(_apiUrl + "FilingAsync", requestBody).Result; 
		byte[] responseContent = response.Content.ReadAsByteArrayAsync().Result;
		string contentString = Encoding.ASCII.GetString(responseContent);

		string? receiptId = null;

		if (response.StatusCode == HttpStatusCode.Accepted)
		{
			XmlDocument contentDocument = new();
			contentDocument.LoadXml(contentString);
			XmlNode? receiptNode = contentDocument?.DocumentElement?.SelectSingleNode("//DocumentReceiptID");
			receiptId = receiptNode?.InnerText;
		}
		var status = GetMessageAndStatus(response);
		string fullMessage = $"Status: {status?.Status ?? "No status found."}\n{status?.Message ?? "No message found."}";
		return (new(contentString + "parsedMessage: " + fullMessage, response.StatusCode), receiptId);
	}

	public HttpResponse GetBalance()
	{
		return GetResponseStringAndCode(
            SendGET("balance")
        );
	}

	public HttpResponse GetServerStatus()
	{
		return GetResponseStringAndCode(
			SendGET("ServerStatus")
			);
	}

	public HttpResponse GetFileStatus(string document_id)
	{
		return GetResponseStringAndCode(
			SendGET("Status/?id=" + document_id)
		);
	}

	public HttpStatusCode GetDocument(string documentId, string fileName)
	{
		var response = SendGET("FilingAsync/?id=" + documentId);
		byte[] document = response.Content.ReadAsByteArrayAsync().Result;
		if (response.StatusCode == HttpStatusCode.OK)
		{
			string path = Path.Combine(
				Environment.CurrentDirectory,
				$"Output/Documents"
				);
			Directory.CreateDirectory(path);
			File.WriteAllBytes(path + $"/{fileName}.pdf", document);
		}
		return response.StatusCode;
	}
}

public class HttpResponse(string value, HttpStatusCode code)
{
	public string Message { get; set; } = value;
	public HttpStatusCode Code { get; set; } = code;
	override public string ToString()
	{
		return $"""
			Content: {Message},
			Code: {Code}
			""";
	}
}