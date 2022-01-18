using System;
using System.Collections.Generic;
using System.Linq;

namespace PersonalFinanceOCR.TNGeWallet
{
    class TNGeWalletTransaction : IEquatable<TNGeWalletTransaction>
    {
        public DateTime Date { get; set; }
        public double Amount { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public double Balance { get; set; }
        public string TransactionId { get; set; }

        // eWallet: Payment, Refund, Reload, Cashback, Balance Top Up, DuitNow QR, PayDirect Payment, eWallet Cash In, eWallet Cash Out, Transfer to Wallet, 
        // GO+ : GO+ Cash Out, GO+ Cash In, GO+ Daily Earnings
        public string Type { get; set; }

        private static Dictionary<string, string> specialHandlingMap = new Dictionary<string, string> 
        { 
            { "EarningsMMF", "Earnings MMF"},
            { "Receive from Wallet20", "Receive from Wallet 20"},
        };

        private static Dictionary<string, string> creditMap = new Dictionary<string, string> 
        { 
            { "Balance Top Up", "BALANCE_TOP_UP" },
            { "Reload", "RELOAD" },
            { "eWallet Cash In", "EWALLET_CASH_IN" },
            { "GO+ Cash In", "GO+_CASH_IN" },
            { "Receive from Wallet", "RECEIVE_FROM_WALLET" },
            { "GO+ Daily Earnings", "GO+_DAILY_EARNINGS" },
            { "Refund", "REFUND" },
            { "Cashback", "CASHBACK" },
            { "Cash Reward", "CASH_REWARD" },
        };

        private static Dictionary<string, string> debitMap = new Dictionary<string, string>
        {
            { "DuitNow QR TNGD", "DUITNOW_QR_TNGD" },
            { "DuitNow QR", "DUITNOW_QR" },
            { "PayDirect Payment", "PAYDIRECT_PAYMENT" },
            { "RFID Payment", "RFID_PAYMENT" },
            { "eWallet Cash Out", "EWALLET_CASH_OUT" },
            { "GO+ Cash Out", "GO+_CASH_OUT" },
            { "Transfer to Wallet", "TRANSFER_TO_WALLET" },
            { "Payment", "PAYMENT" },
        };

        public TNGeWalletTransaction()
        {
            Amount = 0;
        }

        public void Parse(string value)
        {
            string tmpValue = ReplaceType(value);

            string[] valueSplit = tmpValue.Split(' ');
            int valueSplitCount = valueSplit.Length;

            if(valueSplitCount >= 8 ||
               tmpValue.Contains(debitMap["Transfer to Wallet"]) && valueSplitCount >= 7)
            {
                string amountStr = valueSplit[valueSplitCount - 2];
                string balanceStr = valueSplit[valueSplitCount - 1];
                string dateStr = valueSplit[0];

                this.Type = valueSplit[2];
                this.Amount = ConvertDebitCredit(double.Parse(amountStr.Replace("RM", "")));
                this.Balance = double.Parse(balanceStr.Replace("RM", ""));
                this.TransactionId = valueSplit[valueSplitCount - 3];
                this.Date = ToDate(dateStr);
                this.Reference = valueSplit[3];

                string removedSpaceValue = string.Join(" ", valueSplit);

                this.Description = removedSpaceValue.Replace("Success", string.Empty)
                                                    .Replace(amountStr, string.Empty)
                                                    .Replace(balanceStr, string.Empty)
                                                    .Replace(dateStr, string.Empty)
                                                    .Replace(Type, string.Empty)
                                                    .Replace(Reference, string.Empty)
                                                    .Replace(TransactionId, string.Empty)
                                                    .Trim();
            }
            else
            {
                throw new Exception("Failed to parse TNG eWallet transaction history!");
            }
        }

        private DateTime ToDate(string value)
        {
            string[] valueSplit = value.Split('/');

            if(valueSplit.Length == 3)
            {
                int year = int.Parse(valueSplit[2]);
                int month = int.Parse(valueSplit[1]);
                int day = int.Parse(valueSplit[0]);
                return new DateTime(year, month, day);
            }
            else
            {
                throw new Exception("Invalid Date Format!");
            }
        }

        private double ConvertDebitCredit(double value)
        {
            if(creditMap.Values.Contains(Type))
            {
                return value;
            }
            else if(debitMap.Values.Contains(Type))
            {
                return -value;
            }
            else
            {
                throw new Exception("Found unexpected transaction type!");
            }
        }

        private string ReplaceType(string str)
        {
            string tmpStr = str;

            foreach(var type in specialHandlingMap)
            {
                string key = type.Key;
                string value = type.Value;

                tmpStr = tmpStr.Replace(key, value);
            }

            foreach (var type in debitMap)
            {
                string key = type.Key;
                string value = type.Value;

                tmpStr = tmpStr.Replace(key, value);
            }

            foreach (var type in creditMap)
            {
                string key = type.Key;
                string value = type.Value;

                tmpStr = tmpStr.Replace(key, value);
            }

            return tmpStr;
        }

        public static bool operator ==(TNGeWalletTransaction obj1, TNGeWalletTransaction obj2)
        {
            if (ReferenceEquals(obj1, obj2))
            {
                return true;
            }
            if (ReferenceEquals(obj1, null))
            {
                return false;
            }
            if (ReferenceEquals(obj2, null))
            {
                return false;
            }

            return obj1.Equals(obj2);
        }

        public static bool operator !=(TNGeWalletTransaction obj1, TNGeWalletTransaction obj2)
        {
            return !(obj1 == obj2);
        }

        public bool Equals(TNGeWalletTransaction other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Date.Equals(other.Date)
                   && Amount.Equals(other.Amount)
                   && Description.Equals(other.Description)
                   && Reference.Equals(other.Reference)
                   && Balance.Equals(other.Balance)
                   && TransactionId.Equals(other.TransactionId)
                   && Type.Equals(other.Type);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TNGeWalletTransaction);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Date.GetHashCode();
                hashCode = (hashCode * 397) ^ Amount.GetHashCode();
                hashCode = (hashCode * 397) ^ Description.GetHashCode();
                hashCode = (hashCode * 397) ^ Reference.GetHashCode();
                hashCode = (hashCode * 397) ^ Balance.GetHashCode();
                hashCode = (hashCode * 397) ^ TransactionId.GetHashCode();
                hashCode = (hashCode * 397) ^ Type.GetHashCode();

                return hashCode;
            }
        }
    }
}
