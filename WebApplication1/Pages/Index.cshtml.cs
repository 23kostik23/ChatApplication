using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        public readonly string _connectionString = @"Data Source=IDEAPAD3;Initial Catalog=Chat;Integrated Security=True;TrustServerCertificate=True";

        public List<string> Users { get; set; } = new List<string>();
        public int receiver_id = -1;
        public List<MessageModel> Messages { get; set; } = new List<MessageModel>();
        public class MessageModel
        {
            public string Text { get; set; }
            public DateTime Timestamp { get; set; }
            public bool IsSent { get; set; }
        }
        private readonly ILogger<IndexModel> _logger;

        public string ReceiverUsername { get; set; }

        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        private string GetConnectionString()
        {
            return _config.GetConnectionString("DefaultConnection");
        }

        public void OnGet()
        {
            LoadUsers();
            int? receiver_id = HttpContext.Session.GetInt32("ReceiverId");
            if (receiver_id == null || receiver_id == 0)
            {
                return;
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    "SELECT message, timestamp, sender_id FROM messages WHERE (sender_id = @user_id AND receiver_id = @receiver_id) OR (sender_id = @receiver_id AND receiver_id = @user_id) ORDER BY timestamp ASC",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", HttpContext.Session.GetInt32("UserId"));
                    cmd.Parameters.AddWithValue("@receiver_id", receiver_id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var message = new MessageModel
                            {
                                Text = reader.GetString(0),
                                Timestamp = reader.GetDateTime(1),
                                IsSent = reader.GetInt32(2) == HttpContext.Session.GetInt32("UserId")
                            };
                            Messages.Add(message);
                        }
                    }
                }
                using (SqlCommand cmd = new SqlCommand(@"SELECT u.username FROM users u
                                                        INNER JOIN messages m ON m.receiver_id = u.id
                                                        WHERE u.id = @Receiver_id", conn))
                {
                    cmd.Parameters.AddWithValue("@Receiver_id", HttpContext.Session.GetInt32("ReceiverId"));
                    ReceiverUsername = (string)cmd.ExecuteScalar();
                }
    //            using (SqlCommand cmd = new SqlCommand(
    //"SELECT message, timestamp FROM messages WHERE (sender_id = @user_id AND receiver_id = @receiver_id) OR (sender_id = @receiver_id AND receiver_id = @user_id) ORDER BY timestamp ASC",
    //conn))
    //            {

    //            }
            }
        }

        private void LoadUsers()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT username FROM users", conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Users.Add(reader.GetString(0));
                        }
                    }
                }
            }
        }

        [HttpGet]
        public IActionResult OnGetChatMessages()
        {
            List<string> messages = new List<string>();
            int? receiver_id = HttpContext.Session.GetInt32("ReceiverId");

            if (receiver_id == null)
            {
                receiver_id = 0; // По умолчанию нет собеседника
                HttpContext.Session.SetInt32("ReceiverId", 0);
            }

            if (receiver_id == 0)
            {
                messages.Add("Выберите собеседника, чтобы начать чат.");
                return new JsonResult(new { success = true, messages });
            }

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(
                    @"SELECT message FROM messages 
                    WHERE (sender_id = @user_id AND receiver_id = @receiver_id) 
                    OR (sender_id = @receiver_id AND receiver_id = @user_id) 
                     ORDER BY timestamp ASC",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@user_id", HttpContext.Session.GetInt32("UserId"));
                    cmd.Parameters.AddWithValue("@receiver_id", receiver_id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(reader.GetString(0));
                        }
                    }
                }
            }

            return new JsonResult(new { success = true, messages });
        }

        [HttpPost]
        public IActionResult OnPostSelectUser(string username)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT id FROM users WHERE username = @username", conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        int receiver_id = Convert.ToInt32(result);
                        HttpContext.Session.SetInt32("ReceiverId", receiver_id);
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Пользователь не найден!";
                    }
                }
            }

            return RedirectToPage(); // Перезагрузка страницы с новым собеседником
        }

        [BindProperty]
        public string Message { get; set; }

        public IActionResult OnPostSendMessage()
        {
            if (!string.IsNullOrWhiteSpace(Message))
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO messages (sender_id, receiver_id, message, timestamp)
                                                          VALUES (@userId, @receiverId, @message, GetDate());", conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", HttpContext.Session.GetInt32("UserId"));
                        cmd.Parameters.AddWithValue("@receiverId", HttpContext.Session.GetInt32("ReceiverId"));
                        cmd.Parameters.AddWithValue("@message", Message);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            return RedirectToPage();
        }
    }
}