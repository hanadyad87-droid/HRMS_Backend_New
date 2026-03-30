namespace HRMS_Backend.Models
{
    public class Qualification
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!; // بكالوريوس، ماجستير...

        public string Level { get; set; } = null!;
    }
}