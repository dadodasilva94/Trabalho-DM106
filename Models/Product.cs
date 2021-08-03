using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Trabalho_DM106.Models
{
    public class Product
    {
        public int Id { get; set; }  

        [Required(ErrorMessage = "O campo é obrigatório!")]
        public string name { get; set; }

        public string description { get; set; }
        public string color { get; set; }

        [Required(ErrorMessage = "O campo é obrigatório!")]
        public string model { get; set; }

        [Required(ErrorMessage = "O campo é obrigatório!")]
        public string code { get; set; }

        public decimal price { get; set; }
        public decimal weight { get; set; }

        public decimal height { get; set; }
        public decimal widht { get; set; }
        public decimal lenght { get; set; }
        public decimal diameter { get; set; }
        public string imageURl { get; set; }





    }
}