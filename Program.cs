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
using System.Runtime.InteropServices;
using System.Text;

string detailsPath = Path.GetDirectoryName(Environment.ProcessPath) ?? throw new Exception("No details.json file found.");
Details details = Details.BuildDetails(Path.Combine(detailsPath, "details.json"));
bool verboseMode = details.Verbose ?? false;
string outputPath = Path.Combine(
    details.OutputPath ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
    "Output"
   );
Directory.CreateDirectory(outputPath);
(string ApiKey, string SosKey) = Details.GetKeys();
CalicoApi calicoApi = new(ApiKey, SosKey);

void EndProgram(string? endMessage = null)
{
    Console.WriteLine(endMessage ?? $"Done! Documents should be saved at {outputPath}/Documents");
    Console.WriteLine("Press ENTER to close...");
    Console.ReadLine();
    Environment.Exit(0);
}

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

async Task HandleReceiptInputAsync(string path, string outDirectory) 
{
    Console.WriteLine("Receipt list detected.");
    List<Debtor.Receipt> receipts = [];
    PrintIfVerbose("Getting receipts...");
    using StreamReader file = new(path);
    string? headerLine = await file.ReadLineAsync();
    string? line;
    while ((line = await file.ReadLineAsync()) is not null)
    {
        string[] lineItems = line.Split(",");
        string dealName = lineItems[0];
        string receiptId = lineItems[1];
        if (receiptId != "NORECEIPT") receipts.Add(new(dealName, receiptId));
        else Console.WriteLine($"Deal: {dealName} has no receipt.");
    }
    PrintIfVerbose("Saving documents...");
    Directory.CreateDirectory(outDirectory);
    foreach (var receipt in receipts)
    {
        var result = await calicoApi.GetDocumentAsync(receipt.ReceiptId);
        if (result.Code == HttpStatusCode.OK && result.Document is not null)
        {
            await File.WriteAllBytesAsync(
                Path.Combine(outDirectory, receipt.FileName + ".pdf"), 
                result.Document
                );
        }
        else
        {
            Console.WriteLine($"Could not retrieve document for {receipt.DealName}.\nDocument retrieval code: {result.Code}");
        }
    }
    EndProgram();
};

static async Task SaveReceiptIds(List<Debtor> debtors, string path)
{
    using StreamWriter writer = new(path, false, new UTF8Encoding(true));
    await writer.WriteLineAsync("DealName,ReceiptId");
    foreach (Debtor debtor in debtors)
    {
        await writer.WriteLineAsync(string.Join(",", [debtor.DealName, debtor.ReceiptId ?? "NORECEIPT"]));
    }
}

void PrintIfVerbose(string text)
{
    if (verboseMode) Console.WriteLine(text);
}

if (Environment.GetCommandLineArgs().Length < 2)
{
    string endMessage = $@"Details:
    Balance: ${(await calicoApi.GetBalanceAsync()).Balance}

You may have meant to submit one or more documents. Either use 'UCCXML path', or drag a file into the executable.";
    EndProgram(endMessage);
}

string firstArg = Environment.GetCommandLineArgs()[1];
if (firstArg.EndsWith(".csv"))
{
    if (await new StreamReader(firstArg).ReadLineAsync() == "DealName,ReceiptId")
    {
        await HandleReceiptInputAsync(firstArg, Path.Combine(outputPath, "Documents"));
    }
}

string pathToWorkbook = Environment.GetCommandLineArgs()[1];
var debtors = ReadDebtors(pathToWorkbook);
Console.WriteLine("Workbook detected.");

PrintIfVerbose("Creating submissions...");
string previewXmlDir = Path.Combine(outputPath, "PreviewXML");
Directory.CreateDirectory(previewXmlDir);
foreach (var debtor in debtors)
{
    string fileName = debtor.FileName + ".xml";
    File.WriteAllText(Path.Combine(previewXmlDir, fileName), debtor.AsSubmissionXElement(details).ToString());
}
PrintIfVerbose("Submitting files...");
foreach (Debtor debtor in debtors)
{
    await debtor.SubmitAsync(calicoApi, details);
}
PrintIfVerbose("Saving receipts...");
string receiptIdsDir = Path.Combine(outputPath, "ReceiptIds");
Directory.CreateDirectory(receiptIdsDir);
int receiptIdLogNumber = 1;
while (Path.Exists(Path.Combine(receiptIdsDir, $"receipt-ids-{receiptIdLogNumber}.csv")))
{
    receiptIdLogNumber++;
}
await SaveReceiptIds(debtors, Path.Combine(receiptIdsDir, $"receipt-ids-{receiptIdLogNumber}.csv"));
Console.WriteLine("Waiting for files to be processed... (Default 1 hour)");
await details.WaitAndDisplayTimeRemainingAsync();
Console.WriteLine("Ready to retrieve documents. Ensure you have internet connection, then press ENTER.");
Console.Write("> ");
Console.ReadLine();
PrintIfVerbose("Retrieving documents...");
string documentsDir = Path.Combine(outputPath, "Documents");
Directory.CreateDirectory(documentsDir);
foreach (Debtor debtor in debtors) 
{ 
    byte[]? document = await debtor.GetDocumentAsync(calicoApi);
    if (document is not null) File.WriteAllBytes(Path.Combine(documentsDir, debtor.FileName + ".pdf"), document);
    else Console.WriteLine($"Did not retrieve document for {debtor.DealName}...");
}
EndProgram();