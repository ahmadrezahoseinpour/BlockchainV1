using System;
using System.Collections.Generic;
using System.Text;

namespace BC_Library.Dto
{
    public class SmartContractDto
    {
        public string Code { get; set; } 
        public string State { get; set; }

        public SmartContractDto(string code, string state = "{}")
        {
            Code = code;
            State = state;
        }

        public bool Execute(decimal balance, decimal amount)
        {
            if (Code.StartsWith("TRANSFER_IF_BALANCE_ABOVE:"))
            {
                if (decimal.TryParse(Code.Split(':')[1], out decimal threshold))
                {
                    return balance >= threshold && amount > 0;
                }
            }
            return true;
        }
    }
}
