namespace ContosoUniversity.Caching
{
    /// <summary>
    /// Lightweight, serializable value/text pair used to build drop-down lists from cached data.
    /// </summary>
    public class LookupItem
    {
        public int Value { get; set; }
        public string Text { get; set; }
    }
}
