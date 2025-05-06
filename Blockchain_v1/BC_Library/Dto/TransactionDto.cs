using System;
using System.Collections.Generic;
using System.Text;

namespace BC_Library.Dto
{
    public class TransactionDto
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public decimal Amount { get; set; }

        public TransactionDto(string from, string to, decimal amount)
        {
            FromAddress = from;
            ToAddress = to;
            Amount = amount;
        }
    }

}
