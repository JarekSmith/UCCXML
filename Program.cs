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

Details details = Details.BuildDetails("details.json");
(string ApiKey, string SosKey) = Details.GetKeys();
CalicoApi calicoApi = new(ApiKey, SosKey);

static List<Debtor> ReadDebtors(string pathToFile)
{
    IWorkbook workbook;
    using FileStream file = new(pathToFile, FileMode.Open, FileAccess.Read);
    workbook = WorkbookFactory.Create(file);
    var mapper = new Mapper(workbook);
    var items = mapper.Take<Debtor>("sheet0");
    List<Debtor> debtors = [];
    foreach(var item in items)
    {
        var debtor = item.Value;
        if (debtor is not null && debtor.DealName is not null) debtors.Add(debtor);
    }
    return debtors;
}

if (Environment.GetCommandLineArgs().Length < 2) throw new ArgumentException("usage: UCCXML path");
string pathToWorkbook = Environment.GetCommandLineArgs()[1];
var debtors = ReadDebtors(pathToWorkbook);

// DEBUG: Print credentials to log
Console.WriteLine($"""
    API key: {ApiKey},
    SOS key: {SosKey},
    Acc Num: {details.AccountNumber}
    """);

// Post XML to URL
string docId = "f8b9d09c-abed-47b0-abc5-bc43f6fd2575";

// DEBUG: Print each XML
Directory.CreateDirectory("Output/PreviewXML");
foreach (var debtor in debtors)
{
    string fileName = debtor.FileName + ".xml";
    File.WriteAllText("Output/PreviewXML/" + fileName, debtor.AsSubmissionXElement(details).ToString());
}
await debtors[0].SubmitAsync(calicoApi, details); // <-- Comment this out when setting the receiptId manually. Don't resubmit if you don't have to.
// debtors[0].ReceiptId = docId; // <-- Comment this out when getting the receiptId from the submission. Don't overwrite the receiptId or you'll lose it forever.
Console.WriteLine($"ReceiptId is {debtors[0].ReceiptId}");
Thread.Sleep(30000);
byte[]? document = await debtors[0].GetDocumentAsync(calicoApi);
Directory.CreateDirectory("Output/Documents");
if (document is not null) File.WriteAllBytes("Output/Documents/" + debtors[0].FileName + ".pdf", document);
else Console.WriteLine("Did not retrieve document...");