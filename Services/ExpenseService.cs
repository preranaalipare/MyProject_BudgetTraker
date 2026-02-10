using InternalBudgetTracker.Data;
using InternalBudgetTracker.DTOs;
using InternalBudgetTracker.Enum;
using InternalBudgetTracker.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InternalBudgetTracker.Services
{
    public class ExpenseService
    {
        private readonly AppDbContext _context;
        private readonly  NotificationService _notificationService;

        public ExpenseService(AppDbContext context, NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }
        public string CreateExpense(ExpenseCreateDTO dto, ClaimsPrincipal user)
        {
            // 1️⃣ Token se user nikaalo
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = user.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || roleClaim == null)
                throw new Exception("Invalid token");

            // 2️⃣Sirf EMPLOYEE allow
            if (roleClaim.Value != "Employee")
                throw new Exception("Only employee can create expense");

            var employeeId = int.Parse(userIdClaim.Value);

            //// 3️⃣ Expense create
            // var expense = new Expense
            // {
            //     Description = dto.Description,
            //     Amount = dto.Amount,
            //     BudgetId = dto.BudgetId,
            //     EmployeeId = employeeId,   //  SAME as budget logic
            //     Status = ExpenseStatus.Pending,
            //     SubmittedDate = DateTime.UtcNow
            // };

            // _context.Expenses.Add(expense);
            // _context.SaveChanges();

            var expense= _context.Database.ExecuteSqlRaw(
     "EXEC dbo.sp_Expense " +
     "@Action, @Description, @Amount, @BudgetId, @EmployeeId, @ManagerId, @StartDate, @EndDate",
     new SqlParameter("@Action", "INSERT"),
     new SqlParameter("@Description", (object)dto.Description ?? DBNull.Value),
     new SqlParameter("@Amount", dto.Amount),
     new SqlParameter("@BudgetId", dto.BudgetId),
     new SqlParameter("@EmployeeId", employeeId),
     new SqlParameter("@ManagerId", dto.ManagerId),
     new SqlParameter("@StartDate", DBNull.Value),
     new SqlParameter("@EndDate", DBNull.Value)
     //new SqlParameter("@Status", DBNull.Value)
     
 );

            // 4️⃣ ExpenseApproval create

            //var approval = new ExpenseApproval
            //    {
            //    //ExpenseId = expense.ExpenseId,
            //           ExpenseId = expenseId,

            //    ManagerId = dto.ManagerId,
            //        Status = ExpenseStatus.Pending,
            //        StartDate = DateTime.UtcNow
            //    };

            //    _context.ExpenseApprovals.Add(approval);
            //    _context.SaveChanges();

            //Notification to manager when expense create

            _notificationService.CreateNotification(
        dto.ManagerId,
        NotificationType.ExpensePending,
        $"An expense request of ₹{dto.Amount} has been submitted and is awaiting your approval."
    );

            

            return "Expense created successfully";
        }

        //Service to get expenses
        public object GetExpenses(int? expenseId)
        {
            var query = _context.Expenses
                .Include(e => e.Employee)
                .Include(e => e.Budget)
                .AsQueryable();

            // GET BY ID
            if (expenseId.HasValue)
            {
                var expense = query
                    .FirstOrDefault(e => e.ExpenseId == expenseId.Value);

                if (expense == null)
                    throw new Exception("Expense not found");

                return expense; // single object
            }

            // GET ALL
            return _context.Expenses.ToList();
        }


        public string UpdateExpense(int expenseId, ExpenseUpdateDTO dto, ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            var userId = int.Parse(userIdClaim.Value);

            //Console.WriteLine("user" + user);


            //var expense = _context.Expenses.FirstOrDefault(e =>
            //    e.ExpenseId == expenseId &&
            //    e.EmployeeId == userId &&
            //    e.Status == ExpenseStatus.Pending &&
            //    e.EndDate == null
            //);

            //if (expense == null)
            //    throw new Exception("Expense not editable");

            //expense.Description = dto.Description ?? expense.Description;
            //expense.Amount = dto.Amount ?? expense.Amount;
            //expense.UpdatedDate = DateTime.UtcNow;

            //_context.SaveChanges();
            //return "Expense updated successfully";
            try {
                var rowsAffected = _context.Database.ExecuteSqlRaw(
                "EXEC dbo.sp_UpdateExpense @ExpenseId, @EmployeeId, @Description, @Amount",
                new SqlParameter("@ExpenseId", expenseId),
                new SqlParameter("@EmployeeId", userId),
                new SqlParameter("@Description", dto.Description ?? (object)DBNull.Value),
                new SqlParameter("@Amount", dto.Amount)
            );

                // 3Check if update happened
                if (rowsAffected == 0)
                    throw new Exception("Expense not editable. It may be approved or not yours.");
                return "Expense updated successfully";

            
            }
              catch (Exception ex)
              {
                     throw new Exception(ex.Message);
                 }
     }
        
        public string DeleteExpense(int expenseId,ClaimsPrincipal user)
        {
            try {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                var userId = int.Parse(userIdClaim.Value);
                var expenseIdParam = new SqlParameter("@ExpenseId", expenseId);
                var employeeIdParam = new SqlParameter("@EmployeeId", userId);

                _context.Database.ExecuteSqlRaw(
                    "EXEC sp_DeleteExpense @ExpenseId, @EmployeeId",
                    expenseIdParam,
                    employeeIdParam
                );

                return "Expense deleted successfully";
            }
            catch (Exception ex)
            {
                return ex.Message;   // 👈 user ko exact error milega
            }
        
 

            

            //var expense = _context.Expenses.FirstOrDefault(e =>
            //    e.ExpenseId == expenseId &&
            //    e.EmployeeId == employeeId &&
            //    e.Status == ExpenseStatus.Pending &&
            //    e.EndDate == null
            //);

            //if (expense == null)
            //    throw new Exception(
            //        "Expense not found OR not pending OR you are not the owner"
            //    );

            //// SOFT DELETE
            //expense.EndDate = DateTime.UtcNow;
            //expense.UpdatedDate = DateTime.UtcNow;

            //_context.SaveChanges();
            
        }

        //Service for Expense approval or Reject
        public string ApproveRejectExpense(int expenseId,ExpenseApprovalDTO dto, ClaimsPrincipal user)
        {
            //Token se manager id nikalo
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = user.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || roleClaim == null)
                throw new Exception("Invalid token");

            if (roleClaim.Value != "Manager")
                throw new Exception("Only manager can approve/reject");

            int managerId = int.Parse(userIdClaim.Value);

            //  Expense approval mapping check
            var approval = _context.ExpenseApprovals
                .FirstOrDefault(a =>
                    a.ExpenseId == expenseId &&
                    a.ManagerId == managerId);

            if (approval == null)
                throw new Exception("Expense not assigned to you ");

            if(approval.Status != ExpenseStatus.Pending ||approval.EndDate != null)
                throw new Exception("Expense already processed ");

            //  Expense fetch
            var expense = _context.Expenses
                .FirstOrDefault(e => e.ExpenseId == expenseId && e.EndDate == null);

            if (expense == null)
                throw new Exception("Invalid expense");

            // Approve / Reject
            //approval.IsApproved = dto.IsApproved;
            if (dto.Action == "Approve")
            {
                approval.Status = ExpenseStatus.Approved;
                expense.Status = ExpenseStatus.Approved;

                //Notification to employee for approval
                _notificationService.CreateNotification(expense.EmployeeId,NotificationType.ExpenseApproval,
        $"Your expense of amount {expense.Amount} has been approved");

            }
            else if (dto.Action == "Reject")
            {

                approval.Status = ExpenseStatus.Rejected;
                expense.Status = ExpenseStatus.Rejected;

                //Notification to employee for rejection
                _notificationService.CreateNotification(expense.EmployeeId,NotificationType.ExpenseRejected,
               $"Your expense was rejected. Reason: {dto.Comment}");

            }
            else
            {
                throw new Exception("Invalid action");
            }
            approval.Comment = dto.Comment;
            approval.EndDate = DateTime.UtcNow;
            expense.EndDate = DateTime.UtcNow;
            _context.SaveChanges();
            return $"Expense {approval.Status} successfully";

        }
    }
}
 


    
