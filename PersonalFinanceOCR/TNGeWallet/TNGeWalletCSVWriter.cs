using PersonalFinanceOCR.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PersonalFinanceOCR.TNGeWallet
{
    class TNGeWalletCSVWriter
    {
        public void Write(List<TNGeWalletTransaction> items, string fileName)
        {
            var transactions = items;

            using(StreamWriter file = new StreamWriter(fileName))
            {
                file.WriteLine("Date,Payee,Note,Expense,Income");
                foreach (var transaction in transactions)
                {
                    string date = transaction.Date.ToString("dd/MM/yyyy");
                    string payee = transaction.Description;
                    string debit = transaction.Amount < 0 ? Math.Abs(transaction.Amount).ToString() : string.Empty;
                    string credit = transaction.Amount > 0 ? Math.Abs(transaction.Amount).ToString() : string.Empty;
                    string type = transaction.Type;
                    string note = $"Type: {type} | Ref: {transaction.Reference} | Id: {transaction.TransactionId}";
                    file.WriteLine($"{date},{payee},{note},{debit},{credit}");
                }
            }
        }
    }
}
