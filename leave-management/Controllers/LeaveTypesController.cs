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
using Microsoft.AspNetCore.Mvc;

namespace leave_management.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class LeaveTypesController : Controller
    {
        private readonly ILeaveTypeRepository _repo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public LeaveTypesController(ILeaveTypeRepository repo, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repo = repo;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        
        
        // GET: LeaveTypesController
        public async Task<ActionResult> Index()
        {
            //var leaveTypes =await _repo.FindAll();
            var leaveTypes =await _unitOfWork.LeaveTypes.FindAll();
            var model = _mapper.Map<List<LeaveType>, List<LeaveTypeVM>>(leaveTypes.ToList());
            return View(model);
        }

        // GET: LeaveTypesController/Details/5
        public async Task<ActionResult>  Details(int id)
        {
            //bool isExitsts =await _repo.isExitsts(id);
            bool isExitsts =await _unitOfWork.LeaveTypes.isExitsts(q => q.Id == id);
            if (!isExitsts)
            {
                return NotFound();
            }

            //var leavetype = await _repo.FindById(id);
            var leavetype = await _unitOfWork.LeaveTypes.Find(q => q.Id == id);
            var model = _mapper.Map<LeaveTypeVM>(leavetype);
            return View(model);
        }

        // GET: LeaveTypesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LeaveTypesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>  Create(LeaveTypeVM model)
        {
            try
            {
                //TO: Add insert logic here

                if(!ModelState.IsValid)
                {
                    return View(model);
                }

                var leaveType = _mapper.Map<LeaveType>(model);
                leaveType.DateCreated = DateTime.Now;

                //var isSuccess =await _repo.Create(leaveType);
                //if(!isSuccess)
                //{
                //    ModelState.AddModelError("", "Something Went Wrong ...");
                //    return View(model);
                //}

                await _unitOfWork.LeaveTypes.Create(leaveType);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
                
            }
            catch
            {
                ModelState.AddModelError("", "Something Went Wrong ...");
                return View(model);
            }
        }

        // GET: LeaveTypesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            bool isExists = await _unitOfWork.LeaveTypes.isExitsts(q => q.Id == id);

            //bool isExitsts = await _repo.isExitsts(id);
            if (!isExists)
            {
                return NotFound();
            }

            // var leavetype =await _repo.FindById(id);
            var leavetype = await _unitOfWork.LeaveTypes.Find(q => q.Id == id);
            var model = _mapper.Map<LeaveTypeVM>(leavetype);
            return View(model);
        }

        // POST: LeaveTypesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>  Edit(LeaveTypeVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var leaveType = _mapper.Map<LeaveType>(model);

                _unitOfWork.LeaveTypes.Update(leaveType);
                await _unitOfWork.Save();
                //var isSuccess =await _repo.Update(leaveType);

                //if(!isSuccess)
                //{
                //    ModelState.AddModelError("", "Something Went Wrong ...");
                //    return View(model);
                //}

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Something Went Wrong ...");
                return View(model);
            }
        }

        // GET: LeaveTypesController/Delete/5
        public async Task<ActionResult>  Delete(int id)
        {
            //var leaveType =await _repo.FindById(id);

            var leavetype = await _unitOfWork.LeaveTypes.Find(expression : q => q.Id == id);

            // var isSuccess = await _repo.Delete(leaveType);

            //if (leaveType == null)
            //{
            //    return NotFound();
            //}

            //if (!isSuccess)
            //{
            //    return BadRequest();
            //}

            _unitOfWork.LeaveTypes.Delete(leavetype);
            await _unitOfWork.Save();

            return RedirectToAction(nameof(Index));

        }

        // POST: LeaveTypesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult>  Delete(int id, LeaveTypeVM model)
        { 
            try
            {
                //var leaveType =await _repo.FindById(id);

                var leavetype = await _unitOfWork.LeaveTypes.Find(expression: q => q.Id == id);

                //var isSuccess =await _repo.Delete(leaveType);

                //if(leaveType == null)
                //{
                //    return NotFound();
                //}

                //if (!isSuccess)
                //{
                //    return View(model);
                //}

                _unitOfWork.LeaveTypes.Delete(leavetype);
                await _unitOfWork.Save();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(model);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
