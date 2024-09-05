using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Site.Models;
using DateTime = System.DateTime;

namespace Site.Controllers
{
    public class MongoDbController : Controller
    {
        private readonly ILogger<MongoDbController> _logger;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<MongoSale> _salesCollection;
       

        public MongoDbController(ILogger<MongoDbController> logger, IMongoClient client)
        {
            _logger = logger;
            _database = client.GetDatabase("MyAppDatabase");
            _logger.LogInformation("MongoDB Controller instantiated and database connection established.");
            _salesCollection = _database.GetCollection<MongoSale>("Sales");
           
        }
        public IActionResult QueryOptions()
        {
            return View("QueryOptions");
        }
     
[HttpGet]
public async Task<IActionResult> TotalSalesByCountry(string sortField = "CountryName", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
{
    try
    {
        IMongoCollection<MongoSale> _salesCollection;
        _salesCollection = _database.GetCollection<MongoSale>("Sales");        
        _logger.LogInformation("Fetching Total Sales by Country");

        // Building the sorting definition using the sort field and order
        var sortDefinition = sortOrder == "asc" 
            ? Builders<TotalSalesByCountryViewModel>.Sort.Ascending(sortField) 
            : Builders<TotalSalesByCountryViewModel>.Sort.Descending(sortField);

        // Performing the aggregation
        var aggregation = _salesCollection.Aggregate()
            .Group(sale => sale.CountryName, g => new 
            {
                CountryName = g.Key,
                TotalSales = g.Sum(x => x.Quantity * x.UnitPrice) // Calculating total sales
            })
            .Project(sale => new TotalSalesByCountryViewModel
            {
                Country = sale.CountryName,
                TotalSales = sale.TotalSales
            })
            .Sort(sortDefinition)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize);

        var results = await aggregation.ToListAsync();

        // Count total documents to handle pagination
        var totalRecords = await _salesCollection.CountDocumentsAsync(new BsonDocument());
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        _logger.LogInformation($"Processed {results.Count} records for total sales by country.");

        // Creating a view model to pass to the view
        var viewModel = new SortViewModel<TotalSalesByCountryViewModel>
        {
            Data = results,
            SortCriteria = new List<SortCriterion>
            {
                new SortCriterion { Field = sortField, Direction = sortOrder }
            }
        };

        return View("ViewForQuery2", viewModel);
    }
    catch (MongoCommandException ex)
    {
        _logger.LogError($"MongoDB command error: {ex.Message}");
        return View("Error", ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError($"An error occurred in TotalSalesByCountry: {ex.Message}");
        return View("Error", ex.ToString());
    }
}





        [HttpGet]
public async Task<IActionResult> SalesByProduct(string sortField = "StockCode", string sortOrder = "asc", int pageNumber = 1, int pageSize = 10)
{
    try
    {
        _logger.LogInformation("Fetching Sales by Products");
        IMongoCollection<MongoSale> _salesCollection;
        _salesCollection = _database.GetCollection<MongoSale>("Sales");       
        // Building the sorting definition using the sort field and order
        var sortDefinition = sortOrder == "asc" 
            ? Builders<SalesByProductViewModel>.Sort.Ascending(sortField) 
            : Builders<SalesByProductViewModel>.Sort.Descending(sortField);

      
        // Performing the aggregation
        var aggregation = _salesCollection.Aggregate()
            .Group(sale => sale.StockCode, g => new 
            {
                StockCode = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.First().UnitPrice,
                TotalPrice = g.Sum(x => x.Quantity * x.UnitPrice) // Calculating total sales
            })
            .Project(sale => new SalesByProductViewModel
            {
                // Ensure projection before sorting if needed
                StockCode = sale.StockCode,
                Quantity = sale.Quantity,
                UnitPrice = sale.UnitPrice,
                TotalPrice = sale.TotalPrice
            })
            .Sort(sortDefinition)
            .Skip((pageNumber - 1) * pageSize)
            .Limit(pageSize);
     
        

        var results = await aggregation.ToListAsync();

            
        // Count total documents to handle pagination
        var totalRecords = await _salesCollection.CountDocumentsAsync(new BsonDocument());
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

        ViewBag.CurrentPage = pageNumber;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;

        _logger.LogInformation($"Processed {results.Count} records for product sales.");

        // Creating a view model to pass to the view
        var viewModel = new SortViewModel<SalesByProductViewModel>
        {
            Data = results,
            SortCriteria = new List<SortCriterion>
            {
                new SortCriterion { Field = sortField, Direction = sortOrder }
            }
        };

        return View("ViewForQuery1", viewModel);
    }
    catch (MongoCommandException ex)
    {
        _logger.LogError($"MongoDB command error: {ex.Message}");
        return View("Error", ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError($"An error occurred in SalesByProduct: {ex.Message}");
        return View("Error", ex.ToString());
    }
}




        [HttpGet]
        public async Task<IActionResult> InvoiceSummary(int pageNumber = 1, int pageSize = 10,string sortField = "invoiceNo", string sortOrder = "asc")
        {
            try
            {
                IMongoCollection<MongoSale> _salesCollection;
                _salesCollection = _database.GetCollection<MongoSale>("Sales");       
                _logger.LogInformation("Fetching InvoiceSummary"); 
                var sortDefinition = sortOrder == "asc" ?
                    Builders<InvoiceSummaryViewModel>.Sort.Ascending(sortField) :
                    Builders<InvoiceSummaryViewModel>.Sort.Descending(sortField);
                var aggregation = _salesCollection.Aggregate()
                    .Group(
                        key => key.InvoiceNo, // Grouping key
                        g => new
                        {
                            InvoiceNo = g.Key,
                            TotalSales = g.Sum(x => x.Quantity * x.UnitPrice),
                            InvoiceDate = g.First().InvoiceDate,
                            CustomerID = g.First().CustomerID, // Assuming you store an identifier here
                            CountryName = g.First().CountryName
                        }
                    )
                    .Project(x => new InvoiceSummaryViewModel
                    {
                        InvoiceNo = x.InvoiceNo,
                        TotalAmount = x.TotalSales,
                        InvoiceDate = x.InvoiceDate,
                        CustomerName =
                            x.CustomerID, // This should be adjusted if CustomerName is different from CustomerID
                        CountryName = x.CountryName
                    })
                    .Sort(sortDefinition)
                    .Skip((pageNumber - 1) * pageSize)
                    .Limit(pageSize);

                var results = await aggregation.ToListAsync();
                // Calculate total number of records for pagination
                var totalRecords = await _salesCollection.CountDocumentsAsync(new BsonDocument());
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                _logger.LogInformation($"Processed {results.Count} records for invoice summaries.");
                
                
                var viewModel2 = new SortViewModel<InvoiceSummaryViewModel>
                {
                    Data = results,
                    SortCriteria = new List<SortCriterion>
                    {
                        new SortCriterion { Field = sortField, Direction = sortOrder }
                    }
                };
                return View("ViewForQuery0", viewModel2);
            }
            catch (MongoCommandException ex)
            {
                _logger.LogError($"MongoDB command error: {ex.Message}");
                return View("Error", ex.Message );
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in InvoiceSummary: {ex.Message}");
                return View("Error",  ex.Message );
            }
            
        }
        

        [HttpGet]
        public async Task<IActionResult> CustomerLifetimeValue(int pageNumber = 1, int pageSize = 10,string sortField = "CustomerID", string sortOrder = "asc")
        {
            try
            {
                IMongoCollection<MongoSale> _salesCollection;
                _salesCollection = _database.GetCollection<MongoSale>("Sales");       
                _logger.LogInformation("Querying Customer Lifetime Value");
                var sortDefinition = sortOrder == "asc" ?
                    Builders<CustomerLifetimeValueViewModel>.Sort.Ascending(sortField) :
                    Builders<CustomerLifetimeValueViewModel>.Sort.Descending(sortField);
                // Define the aggregation pipeline using fluent API
                var aggregation = _salesCollection.Aggregate()
                    .Match(Builders<MongoSale>.Filter.And(
                        Builders<MongoSale>.Filter.Type(s => s.Quantity, "int"),
                        Builders<MongoSale>.Filter.Type(s => s.UnitPrice, "double") // Ensure this matches the type stored in MongoDB
                    ))
                    .Group(
                        // Key selector using the customer ID
                        sale => sale.CustomerID,
                        // Aggregator to sum the product of Quantity and UnitPrice for each customer
                        g => new 
                        {
                            CustomerID = g.Key,
                            LifetimeValue = g.Sum(s => s.Quantity * s.UnitPrice)
                        }
                    )
                    .Project(result => new CustomerLifetimeValueViewModel
                    {
                        CustomerID = result.CustomerID,
                        LifetimeValue = result.LifetimeValue // Convert to decimal if necessary
                    })
                    .Sort(sortDefinition)
                    .Skip((pageNumber - 1) * pageSize)
                    .Limit(pageSize);

                var results = await aggregation.ToListAsync();
                // Log a few results for review
                // Calculate total number of records for pagination
                var totalRecords = await _salesCollection.CountDocumentsAsync(new BsonDocument());
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                
                _logger.LogInformation($"Retrieved {results.Count} customer lifetime values.");
                
                var viewModel2 = new SortViewModel<CustomerLifetimeValueViewModel>
                {
                    Data = results,
                    SortCriteria = new List<SortCriterion>
                    {
                        new SortCriterion { Field = sortField, Direction = sortOrder }
                    }
                };
                
                return View("ViewForQuery3", viewModel2);
            }
            catch (MongoCommandException ex)
            {
                _logger.LogError($"MongoDB command error: {ex.Message}");
                return View("Error", ex.Message );
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred in CustomerLifetimeValue: {ex.Message}", ex);
                return View("Error", ex.Message);
            }
        }




        [HttpGet]
        public async Task<IActionResult> SalesTrends(int pageNumber = 1, int pageSize = 10,string sortField = "Year", string sortOrder = "asc")
        {
            try {
                
                IMongoCollection<MongoSale> _salesCollection;
                _salesCollection = _database.GetCollection<MongoSale>("Sales");       

                _logger.LogInformation("Fetching Sales Trends Data");
                var sortDefinition = sortOrder == "asc" ?
                    Builders<SalesTrendViewModel>.Sort.Ascending(sortField) :
                    Builders<SalesTrendViewModel>.Sort.Descending(sortField);
                // Define the aggregation pipeline using the fluent API
                var aggregation = _salesCollection.Aggregate()
                    .Group(
                        // Key selector
                        sale => new { 
                            Year = sale.InvoiceDate.Year, 
                            Month = sale.InvoiceDate.Month 
                        },
                        // Result selector
                        g => new {
                            g.Key.Year,
                            g.Key.Month,
                            TotalSales = g.Sum(s => s.Quantity * s.UnitPrice)
                        }
                    )
                    .Project(result => new SalesTrendViewModel {
                        Year = result.Year,
                        Month = result.Month,
                        TotalSales = result.TotalSales
                    })
                    .Sort(sortDefinition)
                    .Skip((pageNumber - 1) * pageSize)
                    .Limit(pageSize);

                var salesTrendsData = await aggregation.ToListAsync();
                
                

                // Calculate total number of records for pagination
                var totalRecords = await _salesCollection.CountDocumentsAsync(new BsonDocument());
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                ViewBag.CurrentPage = pageNumber;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                
                var viewModel2 = new SortViewModel<SalesTrendViewModel>
                {
                    Data = salesTrendsData,
                    SortCriteria = new List<SortCriterion>
                    {
                        new SortCriterion { Field = sortField, Direction = sortOrder }
                    }
                };

                return View("ViewForQuery4", viewModel2);
            }
            catch (MongoCommandException ex)
            {
                _logger.LogError($"MongoDB command error: {ex.Message}");
                return View("Error", ex.Message );
            }
            catch (Exception ex)
            {
                _logger.LogError("Error in SalesTrends: {Error}", ex);
                return View("Error", model: ex.Message);
            }
        }



    }
}
