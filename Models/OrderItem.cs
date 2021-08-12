using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Trabalho_DM106.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int amount { get; set; }

        public int productId { get; set; }

        public int orderId { get; set; }

        public Product productInfo { get; set; }
    }
}