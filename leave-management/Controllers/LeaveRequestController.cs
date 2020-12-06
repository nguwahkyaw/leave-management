using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using leave_management.Contracts;
using leave_management.Data;
using leave_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace leave_management.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ILeaveRequestRepository _leaveRequestRepo;
        private readonly ILeaveTypeRepository _leaveTyperepo;
        private readonly ILeaveAllocationRepository _leaveAllocationrepo;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveRequestController(ILeaveRequestRepository leaveRequestRepo, ILeaveTypeRepository leaveTyperepo, ILeaveAllocationRepository leaveAllocationrepo, IMapper mapper, UserManager<Employee> userManager)
        {
            _leaveRequestRepo = leaveRequestRepo;
            _leaveTyperepo = leaveTyperepo;
            _leaveAllocationrepo = leaveAllocationrepo;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Roles = "Administrator")]
        // GET: LeaveRequestController
        public async Task<ActionResult>  Index()
        {
            var leaveRequests =await _leaveRequestRepo.FindAll();
            var leaveRequestsModel = _mapper.Map<List<LeaveRequestVM>>(leaveRequests);
            var model = new AdminLeaveRequestVM
            {
                TotalRequests = leaveRequestsModel.Count,
                ApprovedRequests = leaveRequestsModel.Count(q => q.Approved == true),
                PendingRequests = leaveRequestsModel.Count(q => q.Approved == null),
                RejectRequests = leaveRequestsModel.Count(q => q.Approved == false),
                LeaveRequests = leaveRequestsModel
            };

            return View(model);
        }

        public async Task<ActionResult>  MyLeave()
        {
            var employee = await _userManager.GetUserAsync(User);
            var employeeid = employee.Id;
            var employeeLeaveAllocations =await _leaveAllocationrepo.GetLeaveAllocationsByEmployee(employeeid);
            var employeeLeaveRequests = await _leaveRequestRepo.GetLeaveRequestsByEmployee(employeeid);

            var employeeAllocationModel = _mapper.Map<List<LeaveAllocationVM>>(employeeLeaveAllocations);
            var employeeRequestModel = _mapper.Map<List<LeaveRequestVM>>(employeeLeaveRequests);

            var model = new EmployeeLeaveRequestVM
            {
                LeaveAllocations = employeeAllocationModel,
                LeaveRequests = employeeRequestModel
            };

            return View(model);
        }

        // GET: LeaveRequestController/Details/5
        public async Task<ActionResult>  Details(int id)
        {
            var leaveRequest =await _leaveRequestRepo.FindById(id);
            var model = _mapper.Map<LeaveRequestVM>(leaveRequest);
            return View(model);
        }

        public async Task<ActionResult>  ApproveRequest(int id)
        {
            try
            {
                var user = _userManager.GetUserAsync(User).Result;
                var leaveRequest =await _leaveRequestRepo.FindById(id);

                var employeeid = leaveRequest.RequestingEmployeeId;
                var leaveTypeId = leaveRequest.LeaveTypeId;                
                var leaveAllocation =await _leaveAllocationrepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leaveTypeId);

                int daysRequested = (int)(leaveRequest.EndDate - leaveRequest.StartDate).TotalDays;
                leaveAllocation.NumberOfDays = leaveAllocation.NumberOfDays - daysRequested;


                leaveRequest.Approved = true;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                await _leaveRequestRepo.Update(leaveRequest);
                await _leaveAllocationrepo.Update(leaveAllocation);


                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<ActionResult>  RejectRequest(int id)
        {
            try
            {
                var user =await _userManager.GetUserAsync(User);
                var leaveRequest =await _leaveRequestRepo.FindById(id);
                leaveRequest.Approved = false;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                await _leaveRequestRepo.Update(leaveRequest);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: LeaveRequestController/Create
        public async Task<ActionResult>  Create()
        {
            var leaveTypes =await _leaveTyperepo.FindAll();
            var leaveTypesItems = leaveTypes.Select(q => new SelectListItem
            { 
                Text = q.Name,
                Value = q.Id.ToString()

            });

            var model = new CreateLeaveRequestVM
            {
                LeaveTypes = leaveTypesItems
            };

            return View(model);

        }

        // POST: LeaveRequestController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>  Create(CreateLeaveRequestVM model)
        {
            try
            {
                var startDate = model.StartDate;
                var endDate = model.EndDate;

                var leaveTypes =await _leaveTyperepo.FindAll();
                var leaveTypesItems = leaveTypes.Select(q => new SelectListItem
                {
                    Text = q.Name,
                    Value = q.Id.ToString()

                });
                model.LeaveTypes = leaveTypesItems;

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                

                //check startdate is larger than enddate 
                // if 0 => just one day
                // if -1 => not large
                //if 1 => large

                if(DateTime.Compare(startDate,endDate)>1)
                {
                    ModelState.AddModelError("", "Start Date cannot be further in the future than the End Date!");
                    return View(model);
                }

                var employee =await _userManager.GetUserAsync(User);
                var allocation =await _leaveAllocationrepo.GetLeaveAllocationsByEmployeeAndType(employee.Id, model.LeaveTypeId);
                int dateRequests = (int)(endDate - startDate).TotalDays;

                if (dateRequests>allocation.NumberOfDays)
                {
                    ModelState.AddModelError("", "You have no Sufficient Days for this request!");
                    return View(model);
                }

                var leaveRequestModel = new LeaveRequestVM
                {
                    LeaveTypeId = model.LeaveTypeId,
                    RequestingEmployeeId = employee.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Approved = null,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    RequestCommments = model.RequestComments
                    
                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaveRequestModel);
                var isSuccess =await _leaveRequestRepo.Create(leaveRequest);
                if (!isSuccess)
                {
                    ModelState.AddModelError("", "Something Went Wrong ...");
                    return View(model);
                }

                return RedirectToAction("MyLeave");
            }
            catch
            {
                return View(model);
            }
        }

        public async Task<ActionResult>  CancelRequest(int id)
        {
            var leaveRequest =await _leaveRequestRepo.FindById(id);
            leaveRequest.Cancelled = true;
            await _leaveRequestRepo.Update(leaveRequest);
            return RedirectToAction("MyLeave");
        }

        // GET: LeaveRequestController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: LeaveRequestController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public  ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveRequestController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveRequestController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
