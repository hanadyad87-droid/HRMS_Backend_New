namespace HRMS_Backend.DTOs
{
    public class FAQDTO
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Category { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
