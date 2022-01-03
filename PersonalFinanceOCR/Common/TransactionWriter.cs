using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalFinanceOCR.Common
{
    interface TransactionWriter
    {
        void Write(List<dynamic> items, string fileName);
    }
}
