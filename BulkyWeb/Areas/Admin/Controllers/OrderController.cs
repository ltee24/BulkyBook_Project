using Bulky.DataAccess.Repository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
	public class OrderController : Controller
	{

		private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM _orderVM { get; set; }
        public OrderController(IUnitOfWork unitOfWork)
        {
			_unitOfWork = unitOfWork;
				
        }
        public IActionResult Index()
		{
			return View();
		}

        public IActionResult Details(int orderId)
        {
          _orderVM = new OrderVM()
            {
                OrderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == orderId, includeProperties: "Product")

            };
            return View(_orderVM);
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin +","+ SD.Role_Employee)]
        public IActionResult updateOrderDetail()
        {
            var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == _orderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = _orderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = _orderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = _orderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = _orderVM.OrderHeader.City;
            orderHeaderFromDb.State = _orderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = _orderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(_orderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = _orderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(_orderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = _orderVM.OrderHeader.TrackingNumber;
            }
            _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });

        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult startProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(_orderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), new { orderId = _orderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult shipOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u=>u.Id == _orderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = _orderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = _orderVM.OrderHeader.Carrier;
            orderHeader.TrackingNumber = _orderVM.OrderHeader.TrackingNumber;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }
            _unitOfWork.OrderHeader.Update(orderHeader);
            _unitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully";

            return RedirectToAction(nameof(Details), new { orderId = _orderVM.OrderHeader.Id });




           
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult cancelOrder()
        {
            var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == _orderVM.OrderHeader.Id);

            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            _unitOfWork.Save();
            TempData["Success"] = "Order Canceled Successfully";

            return RedirectToAction(nameof(Details), new { orderId = _orderVM.OrderHeader.Id });

        }

        #region API Calls
        [HttpGet]
		public IActionResult GetAll(string status)
		{
            IEnumerable<OrderHeader> objOrderHeadersList;

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee)){
                objOrderHeadersList = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identities;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                
                objOrderHeadersList = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId==userId,includeProperties:"ApplicationUser");

            }

            
            switch (status)
            {
                case "pending":
                    objOrderHeadersList = objOrderHeadersList.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    objOrderHeadersList = objOrderHeadersList.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    objOrderHeadersList = objOrderHeadersList.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    objOrderHeadersList = objOrderHeadersList.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                  
                    break;

            }






            return Json(new { data = objOrderHeadersList });



		}


		
		#endregion
	}
}
