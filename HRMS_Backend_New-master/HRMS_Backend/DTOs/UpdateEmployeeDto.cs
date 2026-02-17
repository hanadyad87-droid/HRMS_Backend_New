namespace HRMS_Backend.DTOs
{
    public class UpdateEmployeeDto
    {
        public string EmployeeNumber { get; set; }
        public string FullName { get; set; }
        public string Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? MotherName { get; set; }
        public string? NationalId { get; set; }
        public DateTime BirthDate { get; set; }

        public string? Gender { get; set; }
        public int MaritalStatusId { get; set; }


        public IFormFile? Photo { get; set; }
    }

}
