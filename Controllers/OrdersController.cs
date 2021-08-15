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

namespace Trabalho_DM106.Controllers
{
    [RoutePrefix("api/orders")]
    public class OrdersController : ApiController
    {
        private Trabalho_DM106Context db = new Trabalho_DM106Context();

        // GET: api/Orders
        [Authorize (Roles = "ADMIN")]
        public IQueryable<Order> GetOrders()
        {
            return db.Orders;
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

        // PUT: api/Orders/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutOrder(int id, Order order)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != order.Id)
            {
                return BadRequest();
            }

            db.Entry(order).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
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