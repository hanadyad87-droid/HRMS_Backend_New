namespace HRMS_Backend.DTOs
{
    public class CreateEmployeeDto
    {
       
        public string FullName { get; set; }
        public int UserId { get; set; }

        public Guid PublicId { get; set; }



        public string MotherName { get; set; }
        public string NationalId { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public string Nationality { get; set; }
       

        public int MaritalStatusId { get; set; }
        // 🔹 صورة الموظف
        public IFormFile? Photo { get; set; }
    }
}
