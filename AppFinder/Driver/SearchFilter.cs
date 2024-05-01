namespace AppFinder.Driver
{
    public class SearchFilter
    {
        public string State { get; set; }
        public string City { get; set; }
        public int MaxPages { get; set; } = 3;
        public OperationType OperationType { get; set; }
    }
}
