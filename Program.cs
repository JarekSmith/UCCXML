// See https://aka.ms/new-console-template for more information

using NPOI.SS.UserModel;
using Npoi.Mapper;
using System.Reflection.Metadata;
using UCCXML;
using System.Linq;
using System.Xml.Serialization;
using System.Net;
using NPOI.SS.Formula.Functions;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LogicExtensions;

var xmlData = File.ReadAllText("C:/Users/jarek/Downloads/sample.xml");

static (string ApiKey, string SosKey, string filerAccNum, string contactEmail) GetKeys()
{
    string fileText = File.ReadAllText(@"C:\Users\jarek\Documents\GitHub\beau-work\UCCXML\.env");
    string? ApiKey = ApiRegex().Match(fileText).Value.Trim();
    string? SosKey = SosRegex().Match(fileText).Value.Trim();
    string? FilerAccNum = Regex.Match(fileText, "(?<=CLIENT_ACCOUNT_NUMBER=).*").Value.Trim();
    string? ContactEmail = Regex.Match(fileText, "(?<=CONTACT_EMAIL=).*").Value.Trim();
    if (ApiKey is null || SosKey is null) throw new Exception("ApiKey and SosKey must be provided in .env");
    return (ApiKey, SosKey, FilerAccNum, ContactEmail);
}

(string ApiKey, string SosKey, string FilerAccNum, string ContactEmail) = GetKeys();
CalicoApi calicoApi = new(ApiKey, SosKey);

static List<Debtor> ReadDebtors(string pathToFile)
{
    IWorkbook workbook;
    using FileStream file = new(pathToFile, FileMode.Open, FileAccess.Read);
    workbook = WorkbookFactory.Create(file);
    var mapper = new Mapper(workbook);
    var items = mapper.Take<Debtor>("sheet1");
    List<Debtor> debtors = [];
    foreach(var item in items)
    {
        var debtor = item.Value;
        if (debtor is not null && debtor.FirstName is not null) debtors.Add(debtor);
    }
    return debtors;
}

void DoConsoleLoop(List<Debtor> debtors, DocumentInformation docInfo, string documentId)
{
    bool runLoop = true;
    while (runLoop)
    {
        Console.Write("(? for help) > ");
        string? input = Console.ReadLine();
        string response;
        switch (input)
        {
            case "1": // Submit data to be processed
                List<string> responses = [];
                foreach (Debtor debtor in debtors)
                {
                    var (Response, ReceiptId) = calicoApi.SubmitFilings(debtor.AsSubmissionXElement(docInfo).ToString());
                    responses.Add(Response.ToString());
                    if (Response.Code == HttpStatusCode.Accepted)
                    {
                        File.AppendAllText("Output/ReceiptIds.txt",
                            SpaceRegex().Replace($"{debtor.FirstName}-{debtor.LastName}-{debtor.StAddress}", "-")
                            + "," + (ReceiptId ?? "No receipt") + "\n");
                    }
                    else break;
                }
                response = string.Join("\n---\n", responses);
                break;
            case "2": // Check status of last submission
                response = calicoApi.GetFileStatus(documentId).ToString();
                break;
            case "3": // Check current balance
                response = calicoApi.GetBalance().ToString();
                break;
            case "4": // Check server status
                response = calicoApi.GetServerStatus().ToString();
                break;
            case "5": // Get document from last submission
                response = calicoApi.GetDocument(documentId, "test").ToString();
                break;
            case "6": // Preview XML documents
                Directory.CreateDirectory("Output/PreviewXML");
                foreach (Debtor debtor in debtors) 
                {
                    File.WriteAllText(SpaceRegex().Replace($"Output/PreviewXML/{debtor.FirstName}-{debtor.LastName}-{debtor.StAddress}.xml", "-"),
                        debtor.AsSubmissionXElement(docInfo).ToString()
                        );
                }
                response = "Done.";
                break;
            case "?": // Print help
                response = @"1. Submit data to be processed
2. Check status of last submission
3. Check current balance.
4. Check server status.
5. Get document from last submission.
6. Generate XML previews.
q. Quit
        ";
                break;
            case "q": // Quit
                runLoop = false;
                response = "Quitting...";
                break;
            default:  // Unrecognized
                response = "Invalid input. Enter '?' for options.";
                break;
        }
        //Console.Clear();
        Console.WriteLine(response);
    }
}

var debtors = ReadDebtors(@"C:\Users\jarek\Downloads\cleaned.xlsx");

UCCXML.Document document = new();
document.AddDebtor([.. debtors]);

Names filerInfo = new(new XElement("OrganizationName", "HDM Capital, LLC"),
    "42 Exchange Place", "Salt Lake City", "UT", "84111");
Names securedPartyInfo = new(new XElement("OrganizationName", "Fairtide Fund III Series 1 LLC"),
    "42 Exchange Place", "Salt Lake City", "UT", "84111");
string collateral = @"The solar energy system and all related equipment, fixtures, components, and improvements,
whether now installed or to be installed, located at or on the real property commonly known as [FULL PROPERTY ADDRESS],
together with all replacements, substitutions, additions, accessions, and proceeds thereof.";
string versionNumber = "06232003";
string phoneNumber = "8008365954";
string contactName = "Beau Smith";

DocumentInformation docInfo = new(versionNumber, FilerAccNum, contactName, ContactEmail, phoneNumber, filerInfo, securedPartyInfo, collateral);

// DEBUG: Print credentials to log
Console.WriteLine($"""
    API key: {ApiKey},
    SOS key: {SosKey},
    Acc Num: {FilerAccNum}
    """);

// Post XML to URL
string docId = "805062f7-3e7c-4ad7-804e-8ba958d9a10c";

// CLI Loop
DoConsoleLoop(debtors, docInfo, docId);

// Retrieve status
partial class Program
{
    [GeneratedRegex("(?<=API_KEY=).*")]
    private static partial Regex ApiRegex();
}
partial class Program
{
    [GeneratedRegex("(?<=SOS_KEY=).*")]
    private static partial Regex SosRegex();
}
partial class Program
{
    [GeneratedRegex(" ")]
    private static partial Regex SpaceRegex();
}