using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Services.Description;
using Pharmacy.Models;

namespace Pharmacy.Controllers
{
    public class ClientController : Controller
    {

        // GET: Client
        [Authorize]
        public ActionResult Clients()
        {
            var db = new CompanyEntities4();

            var us  = User.Identity.Name;  //Παίρνουμε τα στοιχεία του τρέχων συνδεδεμένου χρήστη(Πώλητη)
            Vendor vendor = db.Vendors.FirstOrDefault(c => c.Email == us); // τσεκάρουμε εαν υπάρχει στη λίστα μας
            List<Client> clients = vendor?.Clients.ToList(); //και πέρνουμε όλα τα στοιχεια των πελατών που αντιστοιχούν στον συγκεκριμένο πωλητή
            
            return View(clients); //view->List->Details
        }

        //καινούρια προσθήκη πελάτη
        [HttpPost]
        public ActionResult InsertClient(Client client) 
        {
            var db = new CompanyEntities4();
            var us = User.Identity.Name;  //Παίρνουμε τα στοιχεία του τρέχων συνδεδεμένου χρήστη(Πώλητη)
            Vendor vendor = db.Vendors.FirstOrDefault(c => c.Email == us); // τσεκάρουμε εαν υπάρχει στη λίστα μας

            if (vendor != null)
            {
                client.VendorId = vendor.Id_Vendor; // ->παιρνει το Id του Vendor και το αποδιδει στη στήλη του client για να τα αντιστοιχισει
                db.Clients.Add(client);
            }
            db.SaveChanges(); //αποθήκευσε τις αλλαγές

            return new EmptyResult();
        }

        //Update Client
        [HttpPost]
        public ActionResult UpdateClient(Client client) 
        {
           
                if (ModelState.IsValid)
                {
                    CompanyEntities4 db = new CompanyEntities4();

                    Client updateClient = (from c in db.Clients
                        where c.Id_Client == client.Id_Client
                        select c).FirstOrDefault(); //επέλεξε τον πελάτη απο την παράμετρο που θα σου δωθεί


                    if (updateClient != null)
                    {
                        updateClient.FirstName = client.FirstName;
                        updateClient.LastName = client.LastName;
                        updateClient.Address = client.Address;
                        updateClient.AFM = client.AFM;
                        updateClient.Telephone = client.Telephone;
                        updateClient.Brand_Name = client.Brand_Name;
                        updateClient.DOY = client.DOY;
                    }
                    db.SaveChanges();


                }

                return new EmptyResult();
        }

        //Delete client
        [HttpPost]
        public ActionResult DeleteClient(int clientId)
        {
            using (CompanyEntities4 db = new CompanyEntities4())
            {
                Client client = (from c in db.Clients
                                 where c.Id_Client == clientId
                                 select c).FirstOrDefault();
                if (client == null || clientId == 0) return new EmptyResult();
                db.Clients.Remove(client);
                db.SaveChanges();
            }
            return new EmptyResult();
        }


        //Εύρεση του τελευταίου Ιd_Client του πίνακα Clients
        [HttpPost]
        public ActionResult FindId()
        {
            var db = new CompanyEntities4();


            var query = (from c in db.Clients
                        orderby c.Id_Client
                        descending
                        select c.Id_Client).FirstOrDefault(); //το βρισκω
            query++; //το προσθετω κατα ενα
            var query2 = query.ToString(); //το μετατρεπω σε string για να το πάρει το ajax μετα

            return Content(query2); //και το επιστρεφω σε string
        }



        //------------------Search Result----------------

        
        [Authorize]
        public ActionResult SearchResult() 
        {
            return View();
        }


        //έυρεση πελατών
      
        public JsonResult Client(string id , string name)
        {
            var db = new CompanyEntities4();
            var us = User.Identity.Name;

            if (id == string.Empty && name == string.Empty) return new JsonResult();
            if (id != string.Empty && name == string.Empty)
            {
                int.TryParse(id, out var id1); //cast σε int

                var query = from v in db.Vendors  //inner join * προς *
                    from c in v.Clients
                    where v.Email == us && c.Id_Client == id1
                    select new
                    {
                        c.Id_Client,
                        c.FirstName,
                        c.LastName,
                        c.Address,
                        c.AFM,
                        c.Telephone,
                        c.Brand_Name,
                        c.DOY
                    };
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            if (id == string.Empty && name != string.Empty)
            {
                var query2 = from v in db.Vendors
                    from c in v.Clients
                    where v.Email == us && c.LastName == name
                    select new
                    {
                        c.Id_Client,
                        c.FirstName,
                        c.LastName,
                        c.Address,
                        c.AFM,
                        c.Telephone,
                        c.Brand_Name,
                        c.DOY
                    };
                return Json(query2, JsonRequestBehavior.AllowGet);
            }
            
            return new JsonResult();
        }


        //ευρεση παραγγελιων
        public JsonResult ClientOrders(string id, string name, string date)
        {
            var db = new CompanyEntities4();
            var us = User.Identity.Name;

            if (id == string.Empty && name == string.Empty && date == string.Empty) return new JsonResult();
            if (id != string.Empty && name == string.Empty && date == string.Empty)
            {
                int.TryParse(id, out var id1); //cast σε int
                var query = from v in db.Vendors  //inner join * προς *
                    from c in v.Clients
                    from o in c.Orders    //inner join 1 προς *
                    where v.Email == us && c.Id_Client == id1
                    select new
                    {
                        o.Id,
                        o.Date_order,
                        o.Medicine,
                        o.Quantity,
                        o.Vendor_Name,
                        o.Unit_Price,
                        o.Order_Cost
                    };
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            if (id == string.Empty && name != string.Empty && date == string.Empty)
            {
                var query2 = from v in db.Vendors  //inner join * προς *
                    from c in v.Clients
                    from o in c.Orders    //inner join 1 προς *
                    where v.Email == us && c.LastName == name
                    select new
                    {
                        o.Id,
                        o.Date_order,
                        o.Medicine,
                        o.Quantity,
                        o.Vendor_Name,
                        o.Unit_Price,
                        o.Order_Cost
                    };
                return Json(query2, JsonRequestBehavior.AllowGet);
            }
            if (id != string.Empty && name == string.Empty && date != string.Empty)
            {
                try
                {
                    var toparse = DateTime.ParseExact(date, "dd/MM/yyyy", null);//cast σε date
                    int.TryParse(id, out var id1); //cast σε int
                    var query = from v in db.Vendors
                        from c in v.Clients
                        from o in c.Orders
                        where v.Email == us && o.Date_order == toparse && c.Id_Client == id1
                        select new
                        {
                            o.Id,
                            o.Date_order,
                            o.Medicine,
                            o.Quantity,
                            o.Vendor_Name,
                            o.Unit_Price,
                            o.Order_Cost
                        };
                    return Json(query, JsonRequestBehavior.AllowGet);
                }
                catch (Exception e)
                {
                    //ThrowJsonError(e);
                    //return new JsonResult();
                    return Json(e, JsonRequestBehavior.AllowGet);
                }
            }
                return new JsonResult();
        }
      

        //ευρεση ιστορικου
        public JsonResult ClientHistory(string id, string name)
        {
            var db = new CompanyEntities4();
            var us = User.Identity.Name;

            if (id == string.Empty && name == string.Empty) return new JsonResult();
            if (id != string.Empty && name == string.Empty)
            {
                int.TryParse(id, out var id1); //cast σε int
                var query = from v in db.Vendors  //inner join * προς *
                    from c in v.Clients
                    from h in c.Histories
                    where v.Email == us && c.Id_Client == id1
                    select new
                    {
                        h.Id,
                        h.Record,
                        h.Complaints,
                        h.Absence,
                        h.Date,
                        h.Vendor_Name
                    };
                return Json(query, JsonRequestBehavior.AllowGet);
            }
            if (id == string.Empty && name != string.Empty)
            {
                var query2 = from v in db.Vendors  //inner join * προς *
                    from c in v.Clients
                    from h in c.Histories
                    where v.Email == us && c.LastName == name
                    select new
                    {
                        h.Id,
                        h.Record,
                        h.Complaints,
                        h.Absence,
                        h.Date,
                        h.Vendor_Name
                    };
                return Json(query2, JsonRequestBehavior.AllowGet);
            }
            return new JsonResult();
        }


        //Εύρεση του τελευταίου Ιd του πίνακα History
        [HttpPost]
        public ActionResult FindIdHistory()
        {
            var db = new CompanyEntities4();


            var query = (from c in db.Histories
                orderby c.Id
                    descending
                select c.Id).FirstOrDefault(); //το βρισκω
            query++; //το προσθετω κατα ενα
            var query2 = query.ToString(); //το μετατρεπω σε string για να το πάρει το ajax μετα

            return Content(query2); //και το επιστρεφω σε string
        }


        //Εύρεση του τελευταίου Ιd του πίνακα Orders
        [HttpPost]
        public ActionResult FindIdOrders()
        {
            var db = new CompanyEntities4();


            var query = (from c in db.Orders
                orderby c.Id
                    descending
                select c.Id).FirstOrDefault(); //το βρισκω
            query++; //το προσθετω κατα ενα
            var query2 = query.ToString(); //το μετατρεπω σε string για να το πάρει το ajax μετα

            return Content(query2); //και το επιστρεφω σε string
        }

        //Update orders
        [HttpPost]
        public ActionResult UpdateOrders(Order update)
        {

            if (ModelState.IsValid)
            {
                CompanyEntities4 db = new CompanyEntities4();

                Order updateOrder = (from c in db.Orders
                    where c.Id == update.Id
                    select c).FirstOrDefault(); //επέλεξε τον πελάτη απο την παράμετρο που θα σου δωθεί


                if (updateOrder != null)
                {
                    updateOrder.Date_order = update.Date_order;
                    updateOrder.Medicine = update.Medicine;
                    updateOrder.Quantity = update.Quantity;
                    updateOrder.Vendor_Name = update.Vendor_Name;
                    updateOrder.Unit_Price = update.Unit_Price;
                    updateOrder.Order_Cost = update.Order_Cost;

                }
                db.SaveChanges();
            }
            return new EmptyResult();
        }

        //Update history
        [HttpPost]
        public ActionResult UpdateHistory(History update)
        {

            if (ModelState.IsValid)
            {
                CompanyEntities4 db = new CompanyEntities4();

                History updateHistory = (from c in db.Histories
                    where c.Id == update.Id
                    select c).FirstOrDefault(); //επέλεξε τον πελάτη απο την παράμετρο που θα σου δωθεί


                if (updateHistory != null)
                {
                    updateHistory.Record = update.Record;
                    updateHistory.Complaints = update.Complaints;
                    updateHistory.Absence = update.Absence;
                    updateHistory.Date = update.Date;
                    updateHistory.Vendor_Name = update.Vendor_Name;

                }
                db.SaveChanges();
            }
            return new EmptyResult();
        }


        //καινούρια προσθήκη παραγγελίας
        [HttpPost]
        public ActionResult InsertOrder(Order order, string clientId)
        {
            if (ModelState.IsValid)
            {
                var db = new CompanyEntities4();
                int.TryParse(clientId, out var id1); //cast σε int

                order.Id_Client = id1; // ->παιρνει το Id του client και το αποδιδει στη στήλη του orders για να τα αντιστοιχισει
                db.Orders.Add(order);
                db.SaveChanges(); //αποθήκευσε τις αλλαγές

            }

            return new EmptyResult();
        }

        //καινούρια προσθήκη ιστορικού
        [HttpPost]
        public ActionResult InsertHistory(History history, string clientId)
        {
            if (ModelState.IsValid)
            {
                var db = new CompanyEntities4();
                int.TryParse(clientId, out var id1); //cast σε int

                history.Id_Client = id1; // ->παιρνει το Id του client και το αποδιδει στη στήλη του orders για να τα αντιστοιχισει
                db.Histories.Add(history);
                db.SaveChanges(); //αποθήκευσε τις αλλαγές

            }

            return new EmptyResult();
        }

        //Delete History
        [HttpPost]
        public ActionResult DeleteHistory(int historyId)
        {
            using (CompanyEntities4 db = new CompanyEntities4())
            {
                History history = (from h in db.Histories
                    where h.Id == historyId
                    select h).FirstOrDefault();
                if (history == null || historyId == 0) return new EmptyResult();
                db.Histories.Remove(history);
                db.SaveChanges();
            }
            return new EmptyResult();
        }

        //Delete Orders
        [HttpPost]
        public ActionResult DeleteOrders(int ordersId)
        {
            using (CompanyEntities4 db = new CompanyEntities4())
            {
                Order order = (from o in db.Orders
                    where o.Id == ordersId
                    select o).FirstOrDefault();
                if (order == null || ordersId == 0) return new EmptyResult();
                db.Orders.Remove(order);
                db.SaveChanges();
            }
            return new EmptyResult();
        }

      
        /*private JsonResult ThrowJsonError(Exception e)
        {
            Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            //Log your exception
            return Json(new {e.Message });
        }*/


    }
}