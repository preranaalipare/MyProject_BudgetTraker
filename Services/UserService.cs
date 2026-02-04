
using InternalBudgetTracker.Data;
using InternalBudgetTracker.DTOs;
using InternalBudgetTracker.Enum;
using InternalBudgetTracker.Models;
using InternalBudgetTracker.Services;
using Microsoft.EntityFrameworkCore;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly HelperService _helperService;


    public UserService(AppDbContext context, HelperService helperService)
    {
        _context = context;
        _helperService = helperService;
    }

    
    // EMPLOYEE REGISTRATION
    public string RegisterEmployee(UserRegisterDTO dto)
    {
        if (_context.Users.Any(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = _helperService.GenerateHashPassword(dto.Password),
            Role = UserRole.Employee,
            Status = UserStatus.Active,
            IsVerified = false,
            DepartmentId = dto.DepartmentId
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        string token = _helperService.GenerateToken(
            user.UserId,
            user.Email,
            user.Role.ToString()
        );

        EmailService.SendVerificationMail(user.Email, token);

        return "A verification email is sent to your address";
    }

    
    // MANAGER REGISTRATION

    public string RegisterManager(UserRegisterDTO dto)
    {
        if (_context.Users.Any(u => u.Email == dto.Email))
            throw new Exception("Email already exists");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = _helperService.GenerateHashPassword(dto.Password),
            Role = UserRole.Manager,
            Status = UserStatus.Active,
            IsVerified = false,
            DepartmentId = dto.DepartmentId
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        string token = _helperService.GenerateToken(
            user.UserId,
            user.Email,
            user.Role.ToString()
        );

        EmailService.SendVerificationMail(user.Email, token);

        return "A verification email is sent to your address";
    }

    
    // VERIFY USER (EMAIL)
 
    public string VerifyUser(string token)
    {
        dynamic result = _helperService.CheckValidToken(token);

        if (!result.valid)
            throw new Exception(result.error);

        string email = result.data.email;

        if (string.IsNullOrWhiteSpace(email)) throw new Exception("Email not found in token");

        var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
        if (user == null)
            throw new Exception("Invalid User");

        user.IsVerified = true;
        _context.SaveChanges();

        return "Successfully verified";
    }

   
    // LOGIN USER
    
    public Dictionary<string, string> Login(UserLoginDTO dto)
    {
        string hashPassword =
            _helperService.GenerateHashPassword(dto.Password);

    var user = _context.Users.FirstOrDefault(
        u => u.Email.ToLower() == dto.Email.ToLower() && u.Password == hashPassword
    );

        if (user == null)
            throw new Exception("Invalid username or password");

        if (!user.IsVerified)
            throw new Exception("Please verify your email first");

    var token = _helperService.GenerateToken(
        user.UserId,
        user.Email,
        user.Role.ToString()
        
    );
        
         return new Dictionary<string, string>
         {
             {"token",token.ToString() },
             {"role",user.Role.ToString()}

         };
    }
}

