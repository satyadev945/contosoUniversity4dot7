using ContosoUniversity.Models;

namespace ContosoUniversity.ViewModels
{
    public class InstructorIndexData
    {
        public IEnumerable<Instructor> Instructors { get; set; } = Enumerable.Empty<Instructor>();
        public IEnumerable<Course>? Courses { get; set; }
        public IEnumerable<Enrollment>? Enrollments { get; set; }
    }
}
