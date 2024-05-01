namespace AppFinder.Driver
{
    public interface IBrowserDriver
    {
        Task<IEnumerable<PropertyInfo>> FetchAllProperties(SearchFilter filter);
        Task<IEnumerable<string>> SearchCitiesAvailable(string city);
    }
}