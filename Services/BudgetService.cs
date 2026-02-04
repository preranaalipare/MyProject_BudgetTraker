using InternalBudgetTracker.Data;
using InternalBudgetTracker.DTOs;
using InternalBudgetTracker.Enum;
using InternalBudgetTracker.Models;
using System.Security.Claims;

namespace InternalBudgetTracker.Services
{
     public class BudgetService
    {
        private readonly AppDbContext _context;

        public BudgetService(AppDbContext context)
        {
            _context = context;
        }

        public string CreateBudget(BudgetCreateDTO dto, ClaimsPrincipal user)
        {
            //get data from token
            //Console.WriteLine(user);
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
             var roleClaim = user.FindFirst(ClaimTypes.Role);
            var emailClaim = user.FindFirst(ClaimTypes.Email);
           
            if ( userIdClaim==null ||roleClaim == null)
                throw new Exception("Invalid token");

            if (roleClaim.Value != "Manager")
                throw new Exception("Invalid permission");

            var userId = int.Parse(userIdClaim.Value);

            //Budget create
            var budget = new Budget
            {
                Title = dto.Title,
                AmountAllocated = dto.AmountAllocated,
                StartDate = DateTime.UtcNow,
                EndDate = null,
                CreatedByUserId = userId,
                Status = BudgetStatus.Active,
                DepartmentId = dto.DepartmentId

            };

            _context.Budgets.Add(budget);
            _context.SaveChanges();

            return "success";
        }

        //get budget by id or all
        public List<Budget> GetBudgets(int? budgetId)
        {
            if (budgetId != null)
            {
                // Single budget by ID
                return _context.Budgets
                    .Where(b =>
                        b.BudgetId == budgetId &&
                        b.EndDate == null &&
                        b.Status == BudgetStatus.Active
                    )
                    .ToList();
            }

            // All active budgets
            return _context.Budgets
                .Where(b =>
                    b.EndDate == null &&
                    b.Status == BudgetStatus.Active
                )
                .ToList();
        }

        //Service to Update Budget
        public string UpdateBudget(int budgetId,BudgetUpdateDTO dto,ClaimsPrincipal user )
        {
            // Token se data
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            var emailClaim = user.FindFirst(ClaimTypes.Email);

            if (userIdClaim == null || roleClaim == null)
                throw new Exception("Invalid token");

            if (roleClaim.Value != "Manager")
                throw new Exception("Invalid permission");

            int userId = int.Parse(userIdClaim.Value);

            //  Budget fetch (same user)
            var budget = _context.Budgets.FirstOrDefault(b =>
                b.BudgetId == budgetId &&
                b.CreatedByUserId == userId &&
                b.EndDate == null &&
                b.Status == BudgetStatus.Active
            );

            if (budget == null)
                throw new Exception("Invalid Budget ID or you did not create this budget");

            //  Partial update
            if (dto.Title != null)
                budget.Title = dto.Title;

            if (dto.AmountAllocated.HasValue)
                budget.AmountAllocated = dto.AmountAllocated.Value;

            if (dto.DepartmentId.HasValue)
                budget.DepartmentId = dto.DepartmentId.Value;

            _context.SaveChanges();

            return "success";
        }


        //Service to delete Budget
        public string DeleteBudget(int budgetId, ClaimsPrincipal user)
        {
            // 1️⃣ Token se data nikalna
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            var roleClaim = user.FindFirst(ClaimTypes.Role);
            var emailClaim = user.FindFirst(ClaimTypes.Email);

            if (userIdClaim == null || roleClaim == null)
                throw new Exception("Invalid token");

            if (roleClaim.Value != "Manager")
                throw new Exception("Invalid permission");

            int userId = int.Parse(userIdClaim.Value);

            // 2️⃣ Budget find karo
            var budget = _context.Budgets
                .FirstOrDefault(b => b.BudgetId == budgetId && b.EndDate == null);
            if (budget == null)
                throw new Exception("Invalid budget id");

            // 3️Check: same user ne create kiya?
            if (budget.CreatedByUserId != userId)
                throw new Exception("You did not create this budget");

            // 4️ Soft delete
            budget.Status = BudgetStatus.Closed;
            budget.EndDate = DateTime.UtcNow;

            _context.SaveChanges();

            return "Budget deleted successfully";
        }


    }



}



