namespace HRMS_Backend.Models
{
    public class FAQ
    {
        public int Id { get; set; }

        // السؤال
        public string Question { get; set; }

        // الإجابة
        public string Answer { get; set; }

        // التصنيف (مثلاً: إجازات، بيانات، طباعة)
        public string Category { get; set; }

        // هل السؤال مفعل ويظهر للموظفين أم لا
        public bool IsActive { get; set; } = true;
    }
}