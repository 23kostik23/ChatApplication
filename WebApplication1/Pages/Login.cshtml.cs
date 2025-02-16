using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;
using WebApplication1.Pages;

namespace WebApplication1.Pages
{
    public class LoginModel : PageModel
    {
        public readonly string _connectionString = @"Data Source=IDEAPAD3;Initial Catalog=Chat;Integrated Security=True;TrustServerCertificate=True";
        public SqlConnection _connection;
        public int UserId { get; set; }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
            UserId = 0;
        }
        public IActionResult OnPost()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = @"SELECT id FROM users 
                        WHERE username = @username 
                        AND password = @password";

                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", Username);
                command.Parameters.AddWithValue("@password", Password);

                var result = command.ExecuteScalar();
                if (result != null)
                {
                    UserId = Convert.ToInt32(result);
                    HttpContext.Session.SetInt32("UserId", UserId); // Устанавливаем глобальную переменную
                    return RedirectToPage("/Index");
                }
            }

            // Если аутентификация не удалась
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }
    }
}
