namespace LMS_Backend.Models.DTOs.Public
{
    public class PublicCourseDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public double DurationHours { get; set; }
        public string? DifficultyLevel { get; set; }
        public string? Prerequisites { get; set; }
        public double? AverageRating { get; set; }
        public string? TeacherName { get; set; }
    }
}