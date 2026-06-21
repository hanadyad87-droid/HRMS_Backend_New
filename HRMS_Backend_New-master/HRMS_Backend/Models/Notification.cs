namespace HRMS_Backend.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }   
        public User User { get; set; }

        public string Title { get; set; }
        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>نوع الإجراء: assignment, claimed, verification, rejected, task, general</summary>
        public string? Type { get; set; }

        /// <summary>معرّف الكيان المرتبط (طلب، مهمة، ...)</summary>
        public int? RequestId { get; set; }

        /// <summary>نوع الكيان: maintenance, salary_certificate, data_update, task, leave, ...</summary>
        public string? EntityType { get; set; }
    }
}
