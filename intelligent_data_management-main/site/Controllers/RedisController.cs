using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Linq.Expressions;
using Site.Data;
using Site.Models;
using Site.ViewModels;
using System.Reflection;

namespace Site.Controllers
{
    
    public static class QueryableExtensionss
    {
        public static IQueryable<T> OrderByDynamicRedis<T>(this IQueryable<T> query, string sortField, string sortOrder)
        {
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, sortField);
            var lambda = Expression.Lambda(property, param);

            var methodCallExpression = Expression.Call(typeof(Queryable),
                sortOrder == "asc" ? "OrderBy" : "OrderByDescending",
                new Type[] { typeof(T), property.Type },
                query.Expression,
                lambda);

            return query.Provider.CreateQuery<T>(methodCallExpression);
        }
    }

    
    
    public class RedisController : Controller
    {
        private readonly ILogger<RedisController> _logger;
        private readonly IConnectionMultiplexer _redis;

        public RedisController(ILogger<RedisController> logger, IConnectionMultiplexer redis)
        {
            _logger = logger;
            _redis = redis;
        }

        public IActionResult QueryOptions()
        {
            // This method might return view options that allow users to perform
            // simple key-value operations against Redis data.
            return View("QueryOptions");
        }
        
        
        [HttpGet("check-data-existence")]
        public async Task<IActionResult> CheckDataExistence()
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var allKeys = server.Keys(database: db.Database, pageSize: 10).Any(); // Check if any keys exist

            if (!allKeys)
            {
                _logger.LogInformation("No data found in the database.");
                return NotFound("No data found in the database.");
            }

            _logger.LogInformation("Data exists in the database.");
            return Ok("Data exists in the database.");
        }

        
        public void LogViewModelProperties(object viewModel)
        {
            Type type = viewModel.GetType(); // Get the type of the object
            PropertyInfo[] properties = type.GetProperties(); // Get properties of the type

            foreach (PropertyInfo property in properties)
            {
                // Get the value of the property for the given object instance
                var value = property.GetValue(viewModel, null);
                // Log the name of the property and its value
                _logger.LogInformation($"{property.Name}: {value}");
            }
        }

        [HttpGet("get-value/{key}")]
        public async Task<IActionResult> GetValue(string key)
        {
            // Fetch a value by key from Redis.
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (!value.HasValue)
            {
                return NotFound();
            }
            return Content(value);
        }

        [HttpPost("set-value")]
        public async Task<IActionResult> SetValue(string key, string value)
        {
            // Set a key-value pair in Redis.
            var db = _redis.GetDatabase();
            bool isSuccess = await db.StringSetAsync(key, value);
            if (!isSuccess)
            {
                return BadRequest("Failed to set value.");
            }
            return Ok();
        }
[HttpGet]
public async Task<IActionResult> TotalSalesByAllCountries(int pageNumber = 1, int pageSize = 10, string sortField = "Country", string sortOrder = "asc")
{
    try
    {
        var cacheKey = "TotalSalesByAllCountries:Data";
        var db = _redis.GetDatabase();

        string cachedData = await db.StringGetAsync(cacheKey);
        List<TotalSalesByCountryViewModel> totalsByCountryList;

        if (!string.IsNullOrEmpty(cachedData))
        {
            totalsByCountryList = JsonConvert.DeserializeObject<List<TotalSalesByCountryViewModel>>(cachedData);
            _logger.LogInformation("Retrieved total sales by country from cache.");
        }
        else
        {
            var totalsByCountry = await GetTotalSalesByAllCountries();
            totalsByCountryList = totalsByCountry.Select(kvp => new TotalSalesByCountryViewModel
            {
                Country = kvp.Key,
                TotalSales = kvp.Value
            }).ToList();
            await db.StringSetAsync(cacheKey, JsonConvert.SerializeObject(totalsByCountryList), TimeSpan.FromMinutes(1));
            _logger.LogInformation("Cached total sales by country data.");
        }

        // Apply sorting each time, regardless of data source (cache or database)
        var queryableData = totalsByCountryList.AsQueryable();
        var sortedData = queryableData.OrderByDynamicRedis(sortField, sortOrder);

        // Pagination
        var pagedData = sortedData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = (int)Math.Ceiling(queryableData.Count() / (double)pageSize);
            
        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        
        
        var model = new SortViewModel<TotalSalesByCountryViewModel>
        {
            Data = pagedData,
            SortCriteria = new List<SortCriterion> { new SortCriterion { Field = sortField, Direction = sortOrder } }
        };

        return View("ViewForQuery2", model);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "An error occurred while retrieving total sales by all countries.");
        return View("Error");
    }
}



        public async Task<Dictionary<string, double>> GetTotalSalesByAllCountries()
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var pattern = "totalSalesByCountry:*";
            var totalsByCountry = new Dictionary<string, double>();

            var keys = server.Keys(database: db.Database, pattern: pattern);
            foreach (var key in keys)
            {
                var country = key.ToString().Split(':')[1]; // Assuming key format is "totalSalesByCountry:{Country}"
                var value = await db.StringGetAsync(key);
                if (!value.IsNullOrEmpty)
                {
                    totalsByCountry[country] = double.Parse(value, CultureInfo.InvariantCulture);
                }
            }

            return totalsByCountry;
        }


        [HttpGet]
        public async Task<IActionResult> SalesByAllProducts(string sortField = "ProductStockCode", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var cacheKey = $"SalesByAllProducts:Data"; // Cache key for unsorted data
                var db = _redis.GetDatabase();
                string cachedData = await db.StringGetAsync(cacheKey);

                List<ProductSalesViewModel> allProductSales;
                if (!string.IsNullOrEmpty(cachedData))
                {
                    allProductSales = JsonConvert.DeserializeObject<List<ProductSalesViewModel>>(cachedData);
                    _logger.LogInformation("Retrieved product sales from cache.");
                }
                else
                {
                    allProductSales = await GetAllProductSales();
                    if (!allProductSales.Any())
                    {
                        _logger.LogInformation("No product sales data found.");
                        return View("ViewForQuery1", new SortViewModel<ProductSalesViewModel>()); // Return empty model if no data
                    }
                    // Cache the raw data
                    await db.StringSetAsync(cacheKey, JsonConvert.SerializeObject(allProductSales), TimeSpan.FromMinutes(1));
                    _logger.LogInformation("Cached product sales data.");
                }

                // Apply sorting to the data after retrieval from cache or database
                
                var sortedData = allProductSales.AsQueryable().OrderByDynamicRedis(sortField, sortOrder).ToList();
                var pagedData = sortedData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
                
                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = allProductSales.Count();
                ViewBag.PageSize = pageSize;
                var model = new SortViewModel<ProductSalesViewModel>
                {
                    Data = pagedData,
                    SortCriteria = new List<SortCriterion> { new SortCriterion { Field = sortField, Direction = sortOrder } }
                };

                return View("ViewForQuery1", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred in SalesByAllProducts: {ex.Message}");
                return View("Error", ex.Message);
            }
        }




        public async Task<List<Site.ViewModels.ProductSalesViewModel>> GetAllProductSales()
        {
            var db = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var pattern = "productTotalSales:*";
            var allProductSales = new List<Site.ViewModels.ProductSalesViewModel>();

            var keys = server.Keys(database: db.Database, pattern: pattern);
            foreach (var key in keys)
            {
                string value = await db.StringGetAsync(key);
                if (!string.IsNullOrEmpty(value))
                {
                    // Assuming the value is serialized JSON of ProductSalesViewModel
                    var productSales = JsonConvert.DeserializeObject<Site.ViewModels.ProductSalesViewModel>(value);
                    if (productSales != null)
                    {
                        // Extract product stock code from the key, assuming key format is "productTotalSales:{StockCode}"
                        productSales.ProductStockCode = key.ToString().Split(':')[1];
                        allProductSales.Add(productSales);
                    }
                }
            }

            return allProductSales;
        }



       [HttpGet]
public async Task<IActionResult> InvoiceSummary(int pageNumber = 1, int pageSize = 10, string sortField = "InvoiceNo", string sortOrder = "asc")
{
    try
    {
        _logger.LogInformation("Fetching Invoice Summary Data");

        // Use a cache key that is independent of sortField and sortOrder
        var cacheKey = "InvoiceSummary:Data";
        var db = _redis.GetDatabase();

        // Try to get cached data
        string cachedData = await db.StringGetAsync(cacheKey);
        List<InvoiceSummaryViewModel> invoiceSummaries;

        if (!string.IsNullOrEmpty(cachedData))
        {
            invoiceSummaries = JsonConvert.DeserializeObject<List<InvoiceSummaryViewModel>>(cachedData);
            _logger.LogInformation("Retrieved data from cache.");
        }
        else
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var pattern = "invoiceSummary:*";
            var keys = server.Keys(database: db.Database, pattern: pattern);
            invoiceSummaries = new List<InvoiceSummaryViewModel>();

            foreach (var key in keys)
            {
                var value = await db.StringGetAsync(key);
                if (!value.IsNullOrEmpty)
                {
                    var summary = JsonConvert.DeserializeObject<InvoiceSummaryViewModel>(value);
                    invoiceSummaries.Add(summary);
                }
            }
            
            // Cache the unsorted data
            await db.StringSetAsync(cacheKey, JsonConvert.SerializeObject(invoiceSummaries), TimeSpan.FromMinutes(1)); // Cache for a longer duration
            _logger.LogInformation("Cached the invoice summaries.");
        }

        // Apply dynamic sorting after data retrieval
        var sortedData = invoiceSummaries.AsQueryable().OrderByDynamicRedis(sortField, sortOrder);
        var pagedData = sortedData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var totalRecords = sortedData.Count();
        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        var model = new SortViewModel<InvoiceSummaryViewModel>
        {
            Data = pagedData,
            SortCriteria = new List<SortCriterion> { new SortCriterion { Field = sortField, Direction = sortOrder } }
        };

        return View("ViewForQuery0", model);
    }
    catch (Exception ex)
    {
        _logger.LogError($"An error occurred while fetching invoice summary data: {ex.Message}", ex);
        return View("Error", model: ex.Message);
    }
}


[HttpGet]
public async Task<IActionResult> CustomerLifetimeValue(int pageNumber = 1, int pageSize = 10, string sortField = "CustomerID", string sortOrder = "asc")
{
    
    _logger.LogInformation("Trying to get Customer");

    try
    {
        var cacheKey = $"CustomerLifetimeValue:Data"; // Consistent cache key for unsorted data
        var db = _redis.GetDatabase();
        
        // Try to get cached data
        string cachedData = await db.StringGetAsync(cacheKey);
        List<CustomerLifetimeValueViewModel> clvs;

        if (!string.IsNullOrEmpty(cachedData))
        {
            clvs = JsonConvert.DeserializeObject<List<CustomerLifetimeValueViewModel>>(cachedData);
            _logger.LogInformation("Retrieved data from cache.");
        }
        else
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var pattern = "customerLifetimeValue:*";
            var keys = server.Keys(database: db.Database, pattern: pattern);
            clvs = new List<CustomerLifetimeValueViewModel>();

            foreach (var key in keys)
            {
                var value = await db.StringGetAsync(key);
                if (value.HasValue)
                {
                    clvs.Add(new CustomerLifetimeValueViewModel
                    {
                        CustomerID = key.ToString().Split(':')[1],
                        LifetimeValue = double.Parse(value)
                    });
                }
            }

            // Cache the raw data
            await db.StringSetAsync(cacheKey, JsonConvert.SerializeObject(clvs), TimeSpan.FromMinutes(1));
            _logger.LogInformation("Cached the customer lifetime values.");
        }

        // Apply sorting and pagination directly in the controller
        var queryableData = clvs.AsQueryable();
        var sortedData = queryableData.OrderByDynamicRedis(sortField, sortOrder);
        var pagedData = sortedData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var totalRecords = queryableData.Count();
        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        
        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        var model = new SortViewModel<CustomerLifetimeValueViewModel>
        {
            Data = pagedData,
            SortCriteria = new List<SortCriterion> { new SortCriterion { Field = sortField, Direction = sortOrder } }
        };

        return View("ViewForQuery3", model);
    }
    catch (Exception ex)
    {
        _logger.LogError($"An error occurred in CustomerLifetimeValue: {ex.Message}");
        return View("Error", ex.Message);
    }
}


        [HttpGet]
public async Task<IActionResult> SalesTrends(int pageNumber = 1, int pageSize = 10, string sortField = "Year", string sortOrder = "asc")
{
    try
    {
        var cacheKey = $"SalesTrends:Data";
        var db = _redis.GetDatabase();
        
        // Try to get cached data
        string cachedData = await db.StringGetAsync(cacheKey);
        List<SalesTrendViewModel> trends;

        if (!string.IsNullOrEmpty(cachedData))
        {
            trends = JsonConvert.DeserializeObject<List<SalesTrendViewModel>>(cachedData);
            _logger.LogInformation("Retrieved data from cache.");
        }
        else
        {
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var pattern = "salesTrend:*";
            var keys = server.Keys(database: db.Database, pattern: pattern);
            trends = new List<SalesTrendViewModel>();

            foreach (var key in keys)
            {
                var value = await db.StringGetAsync(key);
                if (value.HasValue)
                {
                    var trend = JsonConvert.DeserializeObject<SalesTrendViewModel>(value);
                    trends.Add(trend);
                }
            }

            // Cache the data
            var serializedData = JsonConvert.SerializeObject(trends);
            await db.StringSetAsync(cacheKey, serializedData, TimeSpan.FromMinutes(1)); // Cache for 30 minutes
            _logger.LogInformation("Cached the sales trends.");
        }

        // Sort and paginate
        IQueryable<SalesTrendViewModel> queryableData = trends.AsQueryable();
        var sortedData = queryableData.OrderByDynamicRedis(sortField, sortOrder);
        var pagedData = sortedData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var totalRecords = queryableData.Count();
        
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        var model = new SortViewModel<SalesTrendViewModel>
        {
            Data = pagedData,
            SortCriteria = new List<SortCriterion> { new SortCriterion { Field = sortField, Direction = sortOrder } }
        };

        return View("ViewForQuery4", model);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error in SalesTrends: {ex.Message}");
        return View("Error", ex.Message);
    }
}








    }
    

    



}
