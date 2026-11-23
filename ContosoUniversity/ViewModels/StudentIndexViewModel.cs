using ContosoUniversity.Models;
using X.PagedList;

namespace ContosoUniversity.ViewModels
{
    public class StudentIndexViewModel
    {
        public IPagedList<Student> Students { get; set; }
        public string CurrentSort { get; set; }
        public string CurrentFilter { get; set; }
        public string NameSortParm { get; set; }
        public string DateSortParm { get; set; }
    }
}
