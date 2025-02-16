using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.Common;
using System.Data.SqlClient;

namespace WebApplication1.Pages
{
    public class RegisterModel : PageModel
    {
        public readonly string _connectionString = @"Data Source=IDEAPAD3;Initial Catalog=Chat;Integrated Security=True;TrustServerCertificate=True";
        public SqlConnection _connection;

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string ConfirmPassword { get; set; }
        public void OnGet()
        {
        }
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid || Password != ConfirmPassword)
            {
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var query = @"INSERT INTO users(username, password)
                            VALUES (@username, @password)";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", Username);
                        command.Parameters.AddWithValue("@password", Password);
                        command.ExecuteNonQuery();
                    }
                }

                TempData["SuccessMessage"] = "Account created successfully!";
                return RedirectToPage("/Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error creating account");
                return Page();
            }
        }
    }
}
