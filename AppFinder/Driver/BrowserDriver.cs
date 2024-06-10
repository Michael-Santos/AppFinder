using Microsoft.Playwright;

namespace AppFinder.Driver
{
    public class BrowserDriver : IBrowserDriver
    {
        private IBrowserContext _browser;

        private IBrowserContext Browser
        {
            get
            {
                if (_browser == null)
                    _browser = Initialize().Result;

                return _browser;
            }
        }

        public BrowserDriver() { }

        #region Initialize
        private static async Task<IBrowserContext> Initialize()
        {
            const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 OPR/107.0.0.0";

            var browserOptions = new BrowserTypeLaunchOptions()
            {
                Headless = false
            };

            var broserNewContext = new BrowserNewContextOptions()
            {
                UserAgent = userAgent
            };

            var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(browserOptions);
            return await browser.NewContextAsync(broserNewContext);
        }
        #endregion

        public async Task<IPage> OpenPropertyPage(string url)
        {
            var page = await Browser.NewPageAsync();
            await page.GotoAsync(url);
            return page;
        }

        private async Task<Coordinates> GetPosition(IPage page)
        {
            var coordinates = new Coordinates();
            await page.RunAndWaitForRequestAsync(async () =>
            {
                await page.Locator(".map__navigate").ClickAsync();
            }, request => {
                if (request.Url.Contains("map"))
                {
                    Console.WriteLine(request.Url);
                    var locationQueryString = request.Url.Split("&q=")[1];
                    var coordidatesSplited = locationQueryString.Split(",");
                    coordinates.Latitude = coordidatesSplited[0];
                    coordinates.Longitude = coordidatesSplited[1];
                    return true;
                }
                return false;
            });

            return coordinates;
        }

        public async Task<IEnumerable<PropertyInfo>> FetchAllProperties(SearchFilter filter)
        {
            var properties = new List<PropertyInfo>();
            var page = await Browser.NewPageAsync();
            await page.GotoAsync($"https://www.vivareal.com.br/{filter.OperationType.GetDescription()}/{filter.State}/{filter.City}");
            var numberPropertiesTotal = await GetTotalNumberProperties(page);
            var pageSize = await GetPageSize(page);
            var totalPages = (numberPropertiesTotal / pageSize);
            if (numberPropertiesTotal % pageSize > 0) totalPages++;

            var pagesToFetch = filter.MaxPages < totalPages ? filter.MaxPages : totalPages;

            for (var pageNumber = 1; pageNumber <= pagesToFetch; pageNumber++)
            {
                if (pageNumber != 1) await GoToPage(pageNumber, page, filter);
                var propertiesPageResult = await FetchPropertiesOnPage(page);
                properties.AddRange(propertiesPageResult);
            }

            return properties;
        }

        private async Task GoToPage(int pageNumber, IPage page, SearchFilter filter)
        {
            var url = $"https://www.vivareal.com.br/{filter.OperationType.GetDescription()}/{filter.State}/{filter.City}?pagina={pageNumber}";
            await page.GotoAsync($"https://www.vivareal.com.br/{filter.OperationType.GetDescription()}/{filter.State}/{filter.City}?pagina={pageNumber}");
            await page.ScreenshotAsync(new() { Path = "screenshot.png" });
        }

        private async Task<IList<PropertyInfo>> FetchPropertiesOnPage(IPage page)
        {
            var properties = new List<PropertyInfo>();
            var resultList = page.Locator("[data-type=\"property\"]");
            var propertiesSections = await resultList.AllAsync();
            foreach ( var propertiesSection in propertiesSections)
            {
                var property = await FetchPropertyInfos(propertiesSection);
                //await GetPosition(property.Url);
                properties.Add(property);
            }

            return properties;
        }

        private async Task<PropertyInfo> FetchPropertyInfos(ILocator locator)
        {
            var property = new PropertyInfo();
            property.Url = await GetUrl(locator);
            property.Dimension= await GetDimension(locator);
            property.Bathrooms = await GetBathrooms(locator);
            property.Bedrooms = await GetBedrooms(locator);
            property.Garage = await GetGarage(locator);
            property.Price = await GetPrice(locator);
            property.Address = await GetAddress(locator);
            
            var propertyPage = await OpenPropertyPage(property.Url);
            var coordinates = await GetPosition(propertyPage);
            property.Latitude = coordinates.Latitude;
            property.Longitude = coordinates.Longitude;
            property.Publisher = await GetPublisher(propertyPage);
            await propertyPage.CloseAsync();
            return property;
        }

        private async Task<string> GetPublisher(IPage page)
        {
            var publisherSection = page.Locator(".publisher__name").First;
            var text = await publisherSection.TextContentAsync();
            return text.Trim();
        }

        private static async Task<string> GetAddress(ILocator locator)
        {
            var addressSection = locator.Locator(".property-card__address").First;
            var text = await addressSection.TextContentAsync();
            return text.Trim();
        }

        private static async Task<string> GetPrice(ILocator locator)
        {
            var valueCardSection = locator.Locator(".property-card__values").First;
            var priceSection = valueCardSection.Locator(".property-card__price").First;
            var text = await priceSection.TextContentAsync();
            return text.Replace("Preço abaixo do mercado", "").Replace("A partir de", "").Trim();
        }

        private static async Task<string> GetGarage(ILocator locator)
        {
            var cardSection = locator.Locator("li.property-card__detail-garage").First;
            var garageSection = cardSection.Locator("span.js-property-card-value").First;
            var text = await garageSection.TextContentAsync();
            return text.Trim();
        }

        private static async Task<string> GetBedrooms(ILocator locator)
        {
            var cardSection = locator.Locator("li.property-card__detail-room").First;
            var bedroomSection = cardSection.Locator("span.js-property-card-value").First;
            return await bedroomSection.TextContentAsync();
        }

        private static async Task<string> GetBathrooms(ILocator locator)
        {
            var cardSection = locator.Locator("li.property-card__detail-bathroom").First;
            var bathroomSection = cardSection.Locator("span.js-property-card-value").First;
            var text = await bathroomSection.TextContentAsync();
            return text.Trim();
        }

        private static async Task<string> GetDimension(ILocator locator)
        {
            var dimentionSection = locator.Locator("span.js-property-card-detail-area").First;
            var text = await dimentionSection.TextContentAsync();
            return text.Trim();
        }

        private static async Task<string> GetUrl(ILocator locator)
        {
            var baseUrl = "https://www.vivareal.com.br";
            var link = await locator.Locator("a").First.GetAttributeAsync("href");
            return baseUrl + link;
        }

        private static async Task<int> GetPageSize(IPage page)
        {
            var resultList = page.Locator("[data-type=\"property\"]");
            var result = await resultList.AllAsync();
            return result.Count;
        }

        private static async Task<int> GetTotalNumberProperties(IPage page)
        {
            var text = page.Locator(".js-total-records");
            var value = text.TextContentAsync().Result.Replace(".", "");
            if (int.TryParse(value, out int result))
            {
                return result;
            }

            return 0;
        }

        public Task<IEnumerable<string>> SearchCitiesAvailable(string city)
        {
            throw new NotImplementedException();
        }
    }
}
