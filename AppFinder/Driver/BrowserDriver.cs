using Microsoft.Playwright;
using System.Text.RegularExpressions;

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
                Headless = true
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

        public async Task GoToHome()
        {
            var page = await Browser.NewPageAsync();
            var response = await page.GotoAsync($"https://www.vivareal.com.br/venda/sp/sorocaba/?pagina=2");
            var headers = response.Request.Headers;
            await page.ScreenshotAsync(new() { Path = "screenshot.png" });
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

        private static async Task<IList<PropertyInfo>> FetchPropertiesOnPage(IPage page)
        {
            var properties = new List<PropertyInfo>();
            var resultList = page.Locator("[data-type=\"property\"]");
            var propertiesSections = await resultList.AllAsync();
            foreach ( var propertiesSection in propertiesSections)
            {
                properties.Add(await FetchPropertyInfos(propertiesSection));
            }

            return properties;
        }

        private static async Task<PropertyInfo> FetchPropertyInfos(ILocator locator)
        {
            var property = new PropertyInfo();
            property.Link = await GetLink(locator);
            property.Dimension= await GetDimension(locator);
            property.Bathrooms = await GetBathrooms(locator);
            property.Bedrooms = await GetBedrooms(locator);
            property.Garage = await GetGarage(locator);
            property.Price = await GetPrice(locator);
            return property;
        }

        private static async Task<string> GetPrice(ILocator locator)
        {
            var valueCardSection = locator.Locator(".property-card__values").First;
            var priceSection = valueCardSection.Locator(".property-card__price").First;
            var text = await priceSection.TextContentAsync();
            return text.Replace("Preço abaixo do mercado", "").Replace("A partir de", "");
        }

        private static async Task<string> GetGarage(ILocator locator)
        {
            var cardSection = locator.Locator("li.property-card__detail-garage").First;
            var garageSection = cardSection.Locator("span.js-property-card-value").First;
            return await garageSection.TextContentAsync();
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
            return await bathroomSection.TextContentAsync();
        }

        private static async Task<string> GetDimension(ILocator locator)
        {
            var dimentionSection = locator.Locator("span.js-property-card-detail-area").First;
            return await dimentionSection.TextContentAsync();
        }
        private static async Task<string> GetLink(ILocator locator)
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
