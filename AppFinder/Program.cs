// See https://aka.ms/new-console-template for more information
using AppFinder.Driver;

var broser = new BrowserDriver();
var seachFilter = new SearchFilter()
{
    City = "sorocaba",
    State = "sp",
    OperationType = OperationType.Sell
};
var properties = await broser.FetchAllProperties(seachFilter);