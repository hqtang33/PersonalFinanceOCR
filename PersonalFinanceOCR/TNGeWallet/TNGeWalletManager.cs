using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Windows;

namespace PersonalFinanceOCR.TNGeWallet
{
    class TNGeWalletManager
    {
        private static string TNG_E_WALLET_PATH = "TNGeWallet";
        private static string TNG_DIRECTORY_FORMAT = "yyyy-MM";
        private static string TNG_RESULT_FORMAT = "yyyy-MM-dd";

        private static string APP_DATA_FOLDER = "DATA";
        private static string STATEMENT_FOLDER = "STATEMENT";
        private static string RESULT_FOLDER = "RESULT";
        private static string HISTORY_FOLDER = "HISTORY";

        public void ProcessStatement(string filePath)
        {
            PdfReader reader = new PdfReader(filePath);
            int pagenumber = reader.NumberOfPages;
            List<string> allFilteredTransaction = new List<string>();
            List<TNGeWalletTransaction> allFilteredTransactionObj = new List<TNGeWalletTransaction>();
            for (int page = 1; page <= pagenumber; page++)
            {
                ITextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                string rawText = PdfTextExtractor.GetTextFromPage(reader, page, strategy);
                string encodedRawText = Encoding.UTF8.GetString(ASCIIEncoding.Convert(Encoding.Default, Encoding.UTF8, Encoding.Default.GetBytes(rawText)));

                string[] encodedRawTextSplit = encodedRawText.Split(new[] { "\n", "\r", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                allFilteredTransaction.AddRange(FilterProperFormatTransaction(encodedRawTextSplit));
            }

            foreach (var transaction in allFilteredTransaction)
            {
                TNGeWalletTransaction transactionObj = new TNGeWalletTransaction();
                transactionObj.Parse(transaction);
                allFilteredTransactionObj.Add(transactionObj);
            }

            if (allFilteredTransactionObj.Count > 0)
            {
                var transactionsByMonth = allFilteredTransactionObj.GroupBy(obj => $"{obj.Date.ToString(TNG_DIRECTORY_FORMAT)}");
                foreach (var month in transactionsByMonth)
                {
                    string yearMonth = month.Key;
                    string directoryPath = System.IO.Path.Combine(APP_DATA_FOLDER, TNG_E_WALLET_PATH, RESULT_FOLDER, yearMonth);

                    if (Directory.Exists(directoryPath) == false)
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    var transactionsByDay = month.GroupBy(obj => obj.Date.ToString("dd"));
                    foreach (var day in transactionsByDay)
                    {
                        var allTransaction = day.ToList();
                        string jsonContent = JsonConvert.SerializeObject(allTransaction, Formatting.Indented);
                        string jsonFileName = System.IO.Path.Combine(directoryPath, $"{yearMonth}-{day.Key}.json");
                        File.WriteAllText(jsonFileName, jsonContent);
                    }
                }

                // COPY STATEMENT TO BACKUP FOLDER
                var orderedTransactions = allFilteredTransactionObj.OrderBy(obj => obj.Date.Year)
                                         .ThenBy(obj => obj.Date.Month)
                                         .ThenBy(obj => obj.Date.Day);

                string fileName;
                if (allFilteredTransactionObj.Count > 1)
                {
                    fileName = $"{orderedTransactions.First().Date.ToString(TNG_RESULT_FORMAT)} to {orderedTransactions.Last().Date.ToString(TNG_RESULT_FORMAT)}.pdf";
                }
                else
                {
                    fileName = $"{orderedTransactions.First().Date.ToString(TNG_RESULT_FORMAT)}.pdf";
                }

                string statementBackupDirectory = System.IO.Path.Combine(APP_DATA_FOLDER, TNG_E_WALLET_PATH, STATEMENT_FOLDER);
                string statementBackupFullPath = System.IO.Path.Combine(statementBackupDirectory, fileName);

                if (Directory.Exists(statementBackupDirectory) == false)
                {
                    Directory.CreateDirectory(statementBackupDirectory);
                }

                if (File.Exists(statementBackupFullPath))
                {
                    File.Delete(statementBackupFullPath);
                }

                File.Copy(filePath, statementBackupFullPath);
            }
        }

        private List<string> FilterProperFormatTransaction(IEnumerable<string> lines)
        {
            List<string> filteredTransaction = new List<string>();
            Regex regex = new Regex(@"^\s*\b\d{1,2}\/\d{1,2}\/\d{4}\b\s*Success\s*(.*?)RM\d*\.?\d*\s*RM\d*\.?\d*");

            foreach (var line in lines)
            {
                if (regex.IsMatch(line))
                {
                    if(line.Contains("Via FPX to GO+") == false)
                    {
                        if (line.Contains("Payment (via GO+")) continue; // SPECIAL HANDLING
                        if (line.Contains("Cash In")) continue; // SPECIAL HANDLING
                        if (line.Contains("Cash Out")) continue; // SPECIAL HANDLING
                    }

                    filteredTransaction.Add(line);
                }
            }

            return filteredTransaction;
        }
    
        public void ExportResult(List<string> selectedDates, string filePath)
        {
            Trace.Assert(selectedDates.Count > 0);

            List<TNGeWalletTransaction> tNGeWalletTransactions = new List<TNGeWalletTransaction>();
            foreach(var tmpDate in selectedDates)
            {
                DateTime date = DateTime.ParseExact(tmpDate, TNG_RESULT_FORMAT, null);

                string directory = date.ToString(TNG_DIRECTORY_FORMAT);
                string resultFileName = date.ToString(TNG_RESULT_FORMAT);
                string resultFileNameWithExt = $"{resultFileName}.json";
                string fullPath = System.IO.Path.Combine(APP_DATA_FOLDER, TNG_E_WALLET_PATH, RESULT_FOLDER, directory, resultFileNameWithExt);

                if(File.Exists(fullPath))
                {
                    string jsonStr = File.ReadAllText(fullPath);
                    var transactionObjList = JsonConvert.DeserializeObject<List<TNGeWalletTransaction>>(jsonStr);
                    tNGeWalletTransactions.AddRange(transactionObjList);
                }
            }

            // HISTORY PATH
            string historyDirPath = System.IO.Path.Combine(APP_DATA_FOLDER, TNG_E_WALLET_PATH, HISTORY_FOLDER);
            string historyFilePath = System.IO.Path.Combine(historyDirPath, "history.json");

            // FIND DUPLICATE HISTORY
            List<TNGeWalletTransaction> historyTransactions = new List<TNGeWalletTransaction>();

            if(File.Exists(historyFilePath))
            {
                string historyJson = File.ReadAllText(historyFilePath);
                historyTransactions = JsonConvert.DeserializeObject<List<TNGeWalletTransaction>>(historyJson);
            }

            var newTransactions = tNGeWalletTransactions.Except(historyTransactions).ToList();
            if(newTransactions.Count != 0)
            {
                if (newTransactions.Count != tNGeWalletTransactions.Count)
                {
                    MessageBox.Show("Found duplicate result! Will remove all duplicated result!", "DUPLICATE", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                TNGeWalletCSVWriter writer = new TNGeWalletCSVWriter();
                writer.Write(newTransactions, filePath);

                // WRITE HISTORY
                if (Directory.Exists(historyDirPath) == false)
                {
                    Directory.CreateDirectory(historyDirPath);
                }

                // COMBINE ALL TRANSACTIONS
                var allTransactions = new List<TNGeWalletTransaction>(historyTransactions);
                allTransactions.AddRange(newTransactions);

                string newAllTransactionJson = JsonConvert.SerializeObject(allTransactions, Formatting.Indented);
                File.WriteAllText(historyFilePath, newAllTransactionJson);
            }
            else
            {
                throw new Exception("No new transaction found!");
            }
        }

        public Dictionary<string, string> SearchResult(DateTime startDate, DateTime endDate)
        {
            var allSelectedDates = Enumerable.Range(0, 1 + endDate.Subtract(startDate).Days)
                                             .Select(offset => startDate.AddDays(offset))
                                             .OrderByDescending(date => date.Year)
                                             .ThenByDescending(date => date.Month)
                                             .ThenByDescending(date => date.Day)
                                             .ToList();

            Dictionary<string, string> allAvailableResult = new Dictionary<string, string>(); // Key = date, Value = fullPath

            foreach (var date in allSelectedDates)
            {
                string directory = date.ToString(TNG_DIRECTORY_FORMAT);
                string resultFileName = date.ToString(TNG_RESULT_FORMAT);
                string resultFileNameWithExt = $"{resultFileName}.json";
                string fullPath = System.IO.Path.Combine(APP_DATA_FOLDER, TNG_E_WALLET_PATH, RESULT_FOLDER, directory, resultFileNameWithExt);

                if (File.Exists(fullPath))
                {
                    allAvailableResult.Add(resultFileName, fullPath);
                }
            }

            return allAvailableResult;
        }
    }
}
