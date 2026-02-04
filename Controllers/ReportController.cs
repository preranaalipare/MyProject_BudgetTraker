using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalBudgetTracker.Controllers
{
    [ApiController]
    [Route("api/report")]
    //[Authorize(Roles = "Admin")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        // Department wise report
        [HttpGet("department")]
        public IActionResult DepartmentReport([FromQuery] int? departmentId)
        {
            return Ok(_reportService.GetDepartmentReport(departmentId));
        }

        // All budgets report
        [HttpGet("budget")]
        public IActionResult BudgetReport()
        {
            return Ok(_reportService.GetAllBudgetsReport());
        }

        // Overall summary report
        [HttpGet("summary")]
        public IActionResult SummaryReport()
        {
            return Ok(_reportService.GetSummaryReport());
        }
    }
}
