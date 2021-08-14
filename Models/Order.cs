using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Trabalho_DM106.Models
{
    public class Order
    {
        public Order()
        {
            this.OrderItems = new HashSet<OrderItem>();
        }
        public int Id { get; set; }
        public String email { get; set; }

        public DateTime orderData { get; set; }
        public DateTime deliveryData { get; set; }

        public string status { get; set; }

        public decimal totalPrice { get; set; }

        public decimal totalWeight { get; set; }

        public decimal shippingPrice { get; set; }

        public virtual ICollection<OrderItem> OrderItems{ get; set; }



    }
}