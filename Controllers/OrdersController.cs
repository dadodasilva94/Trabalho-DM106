using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Web.Http;
using System.Web.Http.Description;
using Trabalho_DM106.Data;
using Trabalho_DM106.Models;
using Trabalho_DM106.br.com.correios.ws;
using TraballhoDM106.CRMClient;

namespace Trabalho_DM106.Controllers
{
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private Trabalho_DM106Context db = new Trabalho_DM106Context();

        // GET: api/Orders
        [Authorize (Roles = "ADMIN")]
        public List<Order> GetOrders()
        {
            return db.Orders.Include(order => order.OrderItems).ToList();
        }

        // GET: api/Orders/5
        [Authorize(Roles = "ADMIN, USER")]
        [ResponseType(typeof(Order))]
        public IHttpActionResult GetOrder(int id)
        {
            Order order = db.Orders.Find(id);

            if (order == null)
            {
                return NotFound();
            }
            if ((order.email == null) || order.email.Length == 0)
            {
                return BadRequest();
            }
            if (checkOrderOwnerByOrder(User, order))
            {
                return Ok(order);
            }
            else
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
        }

        [ResponseType(typeof(Order))]
        [Authorize]
        [HttpGet]
        [Route("byemail")]
        public IHttpActionResult GetOrderEmail(string email)
        {
            var orders = db.Orders.Where(o => o.email == email);

            if (orders == null)
            {
                return NotFound();
            }
            if (!orders.Any())
            {
                return BadRequest();
            }
            if (checkOrderOwnerByEmail(User, email))
            {
                return Ok(orders);
            }
            else
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
        }

        [Authorize]
        [HttpPut]
        [Route("caulafrete")]
        public IHttpActionResult CalculaFrete(int id)
        {
            Order order = db.Orders.Find(id);

            /*Váriaveis de controle*/
            string cepOrigem = "37584000";
            string frete;
            string cepDest;
            decimal peso = 0;
            int forma = 1;
            decimal comprimento = 0;
            decimal altura = 0;
            decimal largura = 0;
            decimal diamentro = 0;
            string entregaMaoPropria = "N";
            string avisoRecebimento = "S";
            decimal shipping;

            /*Valida se o pedido existe*/
            if (order == null)
            {
                return BadRequest();
            }

            /*Válida se o usuário autenticado tem permissão para acessar o pedido*/
            if(!checkOrderOwnerByOrder(User, order))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            /*Válida se o pedido não está vazio*/
            if(order.OrderItems.Count == 0)
            {
                return BadRequest("Pedido sem itens!");
            }

            ICollection<OrderItem> produtos = order.OrderItems;
            CRMRestClient clienteCrm = new CRMRestClient();
            Customer customer = clienteCrm.GetCustomerByEmail(User.Identity.Name);

            /*Valida email do usuário, utilizando CRM*/
            if(customer != null)
            {
                cepDest = customer.zip;
            }
            else
            {
                return BadRequest("Falha ao consultar CRM");
            }

            /*Válida se é um pedido novo*/
            if(!order.status.Equals("Novo"))
            {
                BadRequest("Pedido com STATUS diferente de Novo");
            }


            foreach(OrderItem item in produtos)
            {
                Product product = db.Products.Find(item.ProductId);

                peso = (item.amount * product.weight) + peso;
                largura = (item.amount * product.widht) + largura;
                comprimento = (item.amount * product.lenght) + comprimento;
                altura = (item.amount * product.height) + altura;
                diamentro = (item.amount + product.diameter) + diamentro;
                order.totalPrice = (item.amount * order.totalPrice) + product.price;
            }
            order.totalWeight = peso;

            CalcPrecoPrazoWS correiosServ = new CalcPrecoPrazoWS();

            /*Calculo do frete*/
            cResultado resultado = correiosServ.CalcPrecoPrazo("", "", "40010", cepOrigem, cepDest, Convert.ToString(peso), forma, Decimal.ToInt32(comprimento), 
                Decimal.ToInt32(altura), Decimal.ToInt32(largura), Decimal.ToInt32(diamentro), entregaMaoPropria, Decimal.ToInt32(order.totalPrice), avisoRecebimento);

            if(resultado.Servicos[0].Erro.Equals("0"))
            {
                frete = "Valor do frente: " + resultado.Servicos[0].Valor + " - Prazo de entrega: " + resultado.Servicos[0].PrazoEntrega + " dia(s)";
                shipping = Convert.ToDecimal(resultado.Servicos[0].Valor);
                order.deliveryData = order.orderData.AddDays(Int32.Parse(resultado.Servicos[0].PrazoEntrega));
            }
            else
            {
                return BadRequest("Erro dos correios: " + resultado.Servicos[0].Erro + " - Mensagem de erro: " + resultado.Servicos[0].MsgErro);
            }

            if(id != order.Id)
            {
                return BadRequest();
            }

            order.shippingPrice = shipping;
            db.Entry(order).State = EntityState.Modified;

            try 
            {
                db.SaveChanges();
            }
            catch(DbUpdateConcurrencyException)
            {
                if(!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        [Authorize]
        [HttpPut]
        [Route("closeorder")]
        public IHttpActionResult closeOrder(int id)
        {
            Order order = db.Orders.Find(id);

            /*Valida se existe o Pedido*/
            if(order == null)
            {
                return NotFound();
            }

            /*Valida se o usuário tem permissão para acessar o pedido*/
            if(!checkOrderOwnerByOrder(User, order))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
            if(!ModelState.IsValid)
            {
                return BadRequest();
            }

            /*Valida se já foi calculado o frete*/
            if(order.shippingPrice == 0)
            {
                return BadRequest("É necessário calcular o frete");
            }
            order.status = "Fechado";

            db.Entry(order).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch(DbUpdateConcurrencyException)
            {
                if(!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Orders
        [Authorize]
        [ResponseType(typeof(Order))]
        public IHttpActionResult PostOrder(Order order)
        {
            order.status = "Novo";
            order.totalWeight = 0;
            order.shippingPrice = 0;
            order.totalPrice = 0;
            order.orderData = DateTime.Now;
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            db.Orders.Add(order);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = order.Id }, order);
        }

        // DELETE: api/Orders/5
        [ResponseType(typeof(Order))]
        public IHttpActionResult DeleteOrder(int id)
        {
            Order order = db.Orders.Find(id);

            if (order == null)
            {
                return NotFound();
            }
            if ((order.email == null) || order.email.Length == 0)
            {
                return BadRequest();
            }
            if (checkOrderOwnerByOrder(User, order))
            {
                db.Orders.Remove(order);
                db.SaveChanges();

                return Ok(order);
            }
            else
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool OrderExists(int id)
        {
            return db.Orders.Count(e => e.Id == id) > 0;
        }

        private bool checkOrderOwnerByEmail(IPrincipal user, string email)
        {
            return ((user.Identity.Name.Equals(email)) || (user.IsInRole("ADMIN")));
        }

        private bool checkOrderOwnerByOrder(IPrincipal user, Order order)
        {
            return ((user.Identity.Name.Equals(order.email)) || (user.IsInRole("ADMIN")));
        }
    }
}