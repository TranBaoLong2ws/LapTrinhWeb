using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020656.Models.Cart
{
    public class cartItem
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Photo { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        // Tính tổng tiền cho từng dòng
        public decimal TotalPrice => Quantity * Price;
    }
}
