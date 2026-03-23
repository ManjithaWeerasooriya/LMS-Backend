namespace LMS_Backend.Models.DTOs.Public
{
    public class PublicCourseListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public double DurationHours { get; set; }
        public string? TeacherName { get; set; }
    }
}