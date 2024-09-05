using System;
 using System.Collections.Generic;
 using System.Globalization;
 using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Site.Data;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Site.Models;

namespace Site.Controllers
{



    public static class IQueryableExtensions
    {
        public static IQueryable<T> OrderByDynamic<T>(this IQueryable<T> query, string sortField, string sortOrder)        {
            if (string.IsNullOrWhiteSpace(sortField))
                return query; // Default sort if no sort field is provided or it's whitespace

            var param = Expression.Parameter(typeof(T), "x");
            Expression property;
            try
            {
                property = Expression.Property(param, sortField);
            }
            catch (ArgumentException)
            {
                // Log the error and use a default sort field if sortField is invalid
                return query.OrderBy(x => EF.Property<object>(x, "DefaultSortField"));
            }

            var lambda = Expression.Lambda(property, param);
            string methodName = sortOrder == "asc" ? "OrderBy" : "OrderByDescending";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] {typeof(T), property.Type},
                query.Expression,
                Expression.Quote(lambda));

            return query.Provider.CreateQuery<T>(resultExpression);
        }

    }

    public class PostgresController : Controller
    {
        private readonly ILogger<PostgresController> _logger;
        private readonly ApplicationDbContext _dbContext;
        private UserManager<IdentityUser> _um;
        private RoleManager<IdentityRole> _rm;
        private readonly DataSyncService _dataSyncService;

        public PostgresController(ILogger<PostgresController> logger, ApplicationDbContext dbContext,UserManager<IdentityUser> um, RoleManager<IdentityRole> rm, DataSyncService dataSyncService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _rm = rm;
            _um = um;
            _dataSyncService = dataSyncService;
        }

        public IActionResult QueryOptions()
        {
            return View("QueryOptions");
        }

        
        
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AddSaleData()
        {
            return View(new Sale()); // Ensure you are passing a new Sale if the form expects a model
        }
        
        [HttpPost]
        [Authorize(Roles = "Admin")] // Ensure this is under authorization

        public async Task<IActionResult> AddSaleData(Sale model)
        {
            if (!ModelState.IsValid)
            {
                return View("AddSaleData", model); // Pass the model back to the view to display validation messages
            }

                model.CountryID = Guid.NewGuid().ToString();
                var invoiceDateId = Guid.NewGuid().ToString();
            
                
        try
        {
            // Check and add Customer if necessary
            var customer = await _dbContext.Customers.FindAsync(model.CustomerID);
            if (customer == null)
            {
                customer = new Customer { CustomerID = model.CustomerID };
                _dbContext.Customers.Add(customer);
                await _dbContext.SaveChangesAsync();
            }

            // Check and add Country if necessary
            var country = await _dbContext.Countries.FindAsync(model.CountryID);
            if (country == null)
            {
                country = new Country { CountryID = model.CountryID, CountryName = "Demo" };
                _dbContext.Countries.Add(country);
                await _dbContext.SaveChangesAsync();
            }

            // Check and add Product if necessary
            var product = await _dbContext.Products.FindAsync(model.StockCode);
            if (product == null)
            {
                product = new Product { StockCode = model.StockCode, Description = model.Description };
                _dbContext.Products.Add(product);
                await _dbContext.SaveChangesAsync();
            }

            // Check and add InvoiceDate if necessary
            var invoiceDate = await _dbContext.Dates.FindAsync(invoiceDateId);
            if (invoiceDate == null)
            {
                invoiceDate = new Date { DateID = invoiceDateId, InvoiceDate = DateTime.UtcNow }; // Adjust the Date property as necessary
                _dbContext.Dates.Add(invoiceDate);
                await _dbContext.SaveChangesAsync();
            }

            // Now create the sale
            var sale = new Sale
            {
                InvoiceNo = model.InvoiceNo,
                StockCode = model.StockCode,
                Description = model.Description,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
                CustomerID = model.CustomerID,
                CountryID = model.CountryID,
                InvoiceDateID = invoiceDateId,
                Product = product,
                Country = country,
                Dates = invoiceDate,
                Customer = customer
            };

            // Save the sale
            _dbContext.Sales.Add(sale);
            await _dbContext.SaveChangesAsync();
            TempData["Message"] = "Sale data added successfully!";
            return await TriggerSync(sale);

        }
        catch (Exception ex)
        {
            
            TempData["Error"] = "Error adding sale data: " + ex.Message;
            return View("AddSaleData", model);
        }

          
        }

            
          [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TriggerSync(Sale model)
        {
            await _dataSyncService.SyncToMongoDB(model);
            await _dataSyncService.SyncToRedis(model);

            return RedirectToAction("QueryOptions");
        }
        
        
        
        
        [HttpGet]
        public async Task<IActionResult> TotalSalesByCountry(string sortField = "Country", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                // Fetch the data first then perform grouping in-memory (client-side)
                var salesData = await _dbContext.Sales.Include(s => s.Country).ToListAsync();

                var groupedData = salesData.GroupBy(f => f.Country.CountryName)
                    .Select(group => new TotalSalesByCountryViewModel
                    {
                        Country = group.Key,
                        TotalSales = group.Sum(f => f.TotalPrice)
                    }).AsQueryable();

                // Apply dynamic sorting and pagination in-memory
                var sortedData = groupedData.OrderByDynamic(sortField, sortOrder);
                var pagedData = sortedData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

                int totalRecords = groupedData.Count();

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                ViewBag.PageSize = pageSize;

                var viewModel = new SortViewModel<TotalSalesByCountryViewModel>
                {
                    Data = pagedData,
                    SortCriteria = new List<SortCriterion> { new SortCriterion { Field = sortField, Direction = sortOrder } }
                };

                return View("ViewForQuery2", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred in TotalSalesByCountry: {Error}", ex);
                return View("Error");
            }
        }




        
        [HttpGet]
        public async Task<IActionResult> SalesByProduct(string sortField = "StockCode", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
        {

            _logger.LogInformation("Fetching all sales data for verification.");
            try
            {
                // Remove specific product filter and aggregate data for all products
                var query = _dbContext.Sales
                    .GroupBy(s => s.StockCode)
                    .Select(g => new SalesByProductViewModel
                    {
                        StockCode = g.Key,
                        Quantity = g.Sum(x => x.Quantity),  // Sum all quantities for each product
                        UnitPrice = g.First().UnitPrice,  // Use the unit price from the first entry assuming it's consistent
                        TotalPrice = g.Sum(x => x.UnitPrice * x.Quantity)  // Sum all total prices for each product
                    });

                // Apply dynamic sorting based on the sortField and sortOrder
                query = query.OrderByDynamic(sortField, sortOrder);

                // Count the total records to handle pagination
                var totalRecords = await query.CountAsync();

                // Fetch the page of data
                var pagedData =  query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                ViewBag.PageSize = pageSize;

                var viewModel = new SortViewModel<SalesByProductViewModel>
                {
                    Data = pagedData,
                    SortCriteria = new List<SortCriterion>
                    {
                        new SortCriterion { Field = sortField, Direction = sortOrder }
                    }
                };

                return View("ViewForQuery1", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred in SalesByProduct: {Error}", ex);
                return View("Error");
            }
        }


       [HttpGet]
public async Task<IActionResult> InvoiceSummary(string sortField = "InvoiceNo", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
{
    try
    {
        var baseQuery = _dbContext.Sales
            .Include(s => s.Customer)    // Include customer data
            .Include(s => s.Country)     // Include country data
            .Include(s => s.Dates) // Include invoice date data
            .AsSplitQuery()
            // Use AsSplitQuery to optimize query performance
            .Select(s => new InvoiceSummaryViewModel {
                InvoiceNo = s.InvoiceNo,
                TotalAmount = s.UnitPrice * s.Quantity, // Assuming TotalPrice is calculated as needed
                CustomerName = s.Customer != null ? s.Customer.CustomerID : "Unknown", // Safe navigation
                CountryName = s.Country != null ? s.Country.CountryName : "Unknown",    // Safe navigation
                InvoiceDate = s.Dates != null ? s.Dates.InvoiceDate : DateTime.MinValue // Handling potential null InvoiceDate
            }).AsQueryable();
        
        
        var groupedQuery = baseQuery
            .GroupBy(i => i.InvoiceNo)
            .Select(g => new InvoiceSummaryViewModel {
                InvoiceNo = g.Key,
                TotalAmount = g.Sum(i => i.TotalAmount),
                CustomerName = g.FirstOrDefault().CustomerName,
                CountryName = g.FirstOrDefault().CountryName,
                InvoiceDate = g.FirstOrDefault().InvoiceDate
            });
        
        // Log a sample from the grouped query for debugging.
        var sampleFromGrouped = await baseQuery.FirstOrDefaultAsync();
      

        // Dynamically apply sorting based on the provided field and direction
        var sortedQuery = groupedQuery.OrderByDynamic(sortField, sortOrder); // Ensure DynamicSort method exists to handle sorting
        var pagedData = await sortedQuery.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        var totalRecords = await baseQuery.CountAsync();

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        ViewBag.PageSize = pageSize;

        var viewModel = new SortViewModel<InvoiceSummaryViewModel> {
            Data = pagedData,
            SortCriteria = new List<SortCriterion> {
                new SortCriterion { Field = sortField, Direction = sortOrder }
            }
        };

        return View("ViewForQuery0", viewModel); // Adjust view name as necessary
    }
    catch (Exception ex)
    {
        _logger.LogError($"An error occurred in InvoiceSummary: {ex.Message}", ex);
        return View("Error");
    }
}






        
         
        [HttpGet]
        public async Task<IActionResult> CustomerLifetimeValue(string sortField = "CustomerID", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                // Fetch initial data
                var data = await _dbContext.Sales
                    .Select(x => new { x.CustomerID, x.TotalPrice })
                    .ToListAsync();

                // Perform grouping and aggregation in-memory
                var groupedData = data
                    .GroupBy(f => f.CustomerID)
                    .Select(group => new CustomerLifetimeValueViewModel
                    {
                        CustomerID = group.Key,
                        LifetimeValue = group.Sum(g => g.TotalPrice)
                    })
                    .AsQueryable();

                // Apply dynamic sorting
                var sortedData = groupedData.OrderByDynamic(sortField, sortOrder);

                // Apply pagination
                var pagedData = sortedData
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var totalRecords = sortedData.Count();

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                ViewBag.PageSize = pageSize;

                var viewModel = new SortViewModel<CustomerLifetimeValueViewModel>
                {
                    Data = pagedData,
                    SortCriteria = new List<SortCriterion>
                    {
                        new SortCriterion { Field = sortField, Direction = sortOrder }
                    }
                };

                return View("ViewForQuery3", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred in CustomerLifetimeValue: {Error}", ex);
                return View("Error");
            }
        }



        [HttpGet]
        public async Task<IActionResult> SalesTrends3(string sortField = "Year", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Starting SalesTrends retrieval.");
            
                // Define the query with projection, grouping, and aggregation performed on the database
                var query = _dbContext.Sales
                    .GroupBy(s => new {
                        Year = s.Dates.Year,
                        Month = s.Dates.Month
                    })
                    .Select(group => new SalesTrendViewModel {
                        Year = group.Key.Year,
                        Month = group.Key.Month,
                        TotalSales = group.Sum(g => g.UnitPrice * g.Quantity)
                    });

                    
               
                query = query.OrderByDynamic(sortField,sortOrder );
               

                // Efficient pagination and execution
                var totalRecords = await query.CountAsync();
                var pagedData = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

                _logger.LogInformation($"Data retrieved and sorted by {sortField} in {sortOrder} order. Page {pageNumber} of {Math.Ceiling((double)totalRecords / pageSize)}.");

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                ViewBag.PageSize = pageSize;

                var viewModel = new SortViewModel<SalesTrendViewModel> {
                    Data = pagedData,
                    SortCriteria = new List<SortCriterion> {
                        new SortCriterion { Field = sortField, Direction = sortOrder }
                    }
                };

                _logger.LogInformation("SalesTrends data ready for view rendering.");

                return View("ViewForQuery4", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred in SalesTrends: {ex.Message}", ex);
                return View("Error");
            }
        }




        [HttpGet]
        public async Task<IActionResult> SalesTrends(int pageNumber = 1, int pageSize = 10, string sortField = "Year", string sortOrder = "asc")
        {
            try
            {
                _logger.LogInformation("Fetching Sales Trends Data");
    
                // Fetch and group data
                var groupedData = _dbContext.Sales
                    .GroupBy(s => new 
                    { 
                        Year = s.Dates.InvoiceDate.Year,  // Assuming InvoiceDate is a DateTime type
                        Month = s.Dates.InvoiceDate.Month 
                    })
                    .Select(g => new 
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalSales = g.Sum(s => s.UnitPrice * s.Quantity)
                    });


                var totalRecords = await groupedData.CountAsync();
                // Apply pagination and project to ViewModel
                groupedData = groupedData.OrderByDynamic(sortField,sortOrder );

                
                var pagedData = groupedData
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(result => new SalesTrendViewModel 
                    {
                        Year = result.Year,
                        Month = result.Month,
                        TotalSales = result.TotalSales
                    }).ToList();

                // Prepare the view model
                var viewModel = new SortViewModel<SalesTrendViewModel>
                {
                    Data = pagedData,
                    SortCriteria = new List<SortCriterion>
                    {
                        new SortCriterion { Field = sortField, Direction = sortOrder }
                    }
                };

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                ViewBag.PageSize = pageSize;
                return View("ViewForQuery4", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in SalesTrends: {Error}", ex);
                return View("Error", model: ex.Message);
            }
        }




}





        
        
 }
