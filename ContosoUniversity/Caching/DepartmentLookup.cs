namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Lightweight, serializable projection of Department used for dropdown lists cached in ElastiCache.
    /// </summary>
    public class DepartmentLookup
    {
        public int DepartmentID { get; set; }
        public string Name { get; set; }
    }
}
