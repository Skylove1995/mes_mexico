using System.ComponentModel.DataAnnotations;

namespace MMES.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Employee ID is required.")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Employee ID is required.")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid department.")]
    public int DeptId { get; set; }

    [Required(ErrorMessage = "Company email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the usage policy.")]
    public bool Consent { get; set; }
}

public class AccountPageViewModel
{
    public LoginViewModel Login { get; set; } = new();
    public RegisterViewModel Register { get; set; } = new();
}
