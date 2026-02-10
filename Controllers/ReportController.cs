using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalBudgetTracker.Controllers
{
    [ApiController]
    [Route("api/report")]
   
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        // Department wise reports
        //[HttpGet("department")]
        //// [Authorize(Roles = "Admin")]
        //public IActionResult DepartmentReport([FromQuery] int? departmentId)
        //{
        //    return Ok(_reportService.GetDepartmentReport(departmentId));
        //}

        [HttpGet("department")]
        [Authorize(Roles = "Admin")]
        public IActionResult DepartmentReport([FromQuery] int? departmentId)
        {
            try
            {
                var data = _reportService.GetDepartmentReport(departmentId);

                return Ok(new
                {
                    success = true,
                    message = "Department report fetched successfully",
                    data = data
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Something went wrong",
                    error = ex.Message
                });
            }
        }

        // All budgets report
        [HttpGet("budget")]
        [Authorize(Roles = "Admin")]

        public IActionResult BudgetReport()
        {
            return Ok(_reportService.GetAllBudgetsReport());
        }

        // Overall summary report
        [HttpGet("summary")]
        [Authorize(Roles = "Admin")]
        public IActionResult SummaryReport()
        {
            return Ok(_reportService.GetSummaryReport());
        }
    }
}
