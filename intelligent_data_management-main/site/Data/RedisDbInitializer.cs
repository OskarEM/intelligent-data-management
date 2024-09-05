using System;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Generic;
using Site.Models; // Assuming RedisModel classes are defined here
using System.Threading.Tasks;
using System.Linq;
using Site.ViewModels;



namespace Site.Data
{
    public class RedisDbInitializer
    {
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly string _csvFilePath;

        public RedisDbInitializer(IConnectionMultiplexer redisConnection, string csvFilePath)
        {
            _redisConnection = redisConnection;
            _csvFilePath = csvFilePath;
        }

        public async Task InitializeAsync()
        {
            var db = _redisConnection.GetDatabase();
            var hasData = await db.StringGetAsync("data:initialized");

            if (!hasData.HasValue)
            {
                await PopulateDataFromCsv(_csvFilePath);

                // Mark as initialized
                await db.StringSetAsync("data:initialized", "true");
            }
        }
        public class ProductAggregation
        {
            public int Quantity { get; set; }
            public double TotalAmount { get; set; }
            public double UnitPrice { get; set; }
        }


        private async Task PopulateDataFromCsv(string csvFilePath)
        {
            var customers = new HashSet<RedisCustomer>();
            var products = new HashSet<RedisProduct>();
            var countries = new HashSet<RedisCountry>();
            var sales = new List<RedisSale>();
            var dates = new HashSet<RedisDate>();
            var totalSalesByCountry = new Dictionary<string, double>();
            //var totalSalesByProduct = new Dictionary<string, decimal>();
            var totalSalesByProduct = new Dictionary<string, ProductAggregation>();

            var invoiceSummaries = new Dictionary<string, InvoiceSummaryViewModel>();
            var customerLifetimeValues = new Dictionary<string, double>();
            var salesTrends = new Dictionary<string, double>();
            var countryIdMap = new Dictionary<string, string>();
            var monthlySalesTrends = new Dictionary<(int Year, int Month), double>();




            try
            {
                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null, MissingFieldFound = null
                }))
                {
                    var records = csv.GetRecords<dynamic>();
                    foreach (var record in records)
                    {
                         // Generate a unique InvoiceDateID
                        var invoiceDateId = Guid.NewGuid().ToString();
                        var saleid = Guid.NewGuid().ToString();

                        var invoiceDate = DateTime.Parse(record.InvoiceDate);
                        var date = new RedisDate(invoiceDate, invoiceDateId);
                        dates.Add(date);

                       

                        var customer = new RedisCustomer(record.CustomerID);
                        customers.Add(customer);

                        var product = new RedisProduct(record.StockCode, record.Description, double.Parse(record.UnitPrice));
                        products.Add(product);

                         if (!countryIdMap.ContainsKey(record.Country))
                        {
                            var newCountry = new RedisCountry(record.Country);
                            countries.Add(newCountry);
                            countryIdMap[record.Country] = newCountry.CountryID;
                        }
                        var countryId = countryIdMap[record.Country];

                        var sale = new RedisSale(saleid,
                        record.InvoiceNo,
                        record.StockCode,
                        record.Description, 
                        int.Parse(record.Quantity),
                        double.Parse(record.UnitPrice),
                        record.CustomerID,
                        countryId,
                        invoiceDateId, 
                        record.StockCode
                        );

                        sales.Add(sale);

                        // total sale per country
                        var quantity = int.Parse(record.Quantity);
                        var unitPrice = double.Parse(record.UnitPrice);
                        
                        var saleAmount = quantity * unitPrice;

                        // Use the country as the key
                        var countryKey = record.Country;

                        // Check if the key already exists in the dictionary
                        if (totalSalesByCountry.ContainsKey(countryKey))
                        {
                            // If so, add the current saleAmount to the existing total
                            totalSalesByCountry[countryKey] += saleAmount;
                        }
                        else
                        {
                            // Otherwise, add a new entry to the dictionary
                            totalSalesByCountry.Add(countryKey, saleAmount);
                        }

                        

                         // Calculate total sales amount for the product
                        var productKey = record.StockCode;
                       
                        

                        if (totalSalesByProduct.ContainsKey(productKey))
                        {
                            // Update existing record
                            totalSalesByProduct[productKey].Quantity += quantity;
                            totalSalesByProduct[productKey].TotalAmount += saleAmount;
                            totalSalesByProduct[productKey].UnitPrice = unitPrice; // Assuming you want the last unit price
                        }
                        else
                        {
                            // Add new record
                            totalSalesByProduct.Add(productKey, new ProductAggregation
                            {
                                Quantity = quantity,
                                TotalAmount = saleAmount,
                                UnitPrice = unitPrice
                            });
                        }

                       
                        
                        Dictionary<string, string> countryNames = new Dictionary<string, string>();

                        foreach (var country in countries)
                        {
                            if (!countryNames.ContainsKey(country.CountryID))
                            {
                                countryNames.Add(country.CountryID, country.CountryName);
                            }
                        }
                        
                        


                        // Aggregate for InvoiceSummary
                        if (!invoiceSummaries.ContainsKey(sale.InvoiceNo))
                        {
                            // Retrieve country name using country ID
                            string countryName = countryNames.TryGetValue(sale.CountryID, out var name) ? name : "Unknown";

                    

                            var invoiceSummary = new InvoiceSummaryViewModel
                            {
                                InvoiceNo = sale.InvoiceNo,
                                TotalAmount = sale.Quantity * sale.UnitPrice, // Calculate total amount
                                CustomerName = sale.CustomerID, 
                                CountryName = countryName,   // Now using the country name
                                InvoiceDate = DateTime.Parse(record.InvoiceDate)  // Directly assigning DateTime
                            };

                            // Initially setting the total amount for the invoice
                            invoiceSummaries[sale.InvoiceNo] = invoiceSummary;
                        }
                        else
                        {
                            // If the invoice already exists, update the total amount
                            invoiceSummaries[sale.InvoiceNo].TotalAmount += sale.Quantity * sale.UnitPrice;
                        }



                        invoiceSummaries[sale.InvoiceNo].TotalAmount += sale.Quantity * sale.UnitPrice;

                        //customerlifetimevalue
                        if (customerLifetimeValues.ContainsKey(sale.CustomerID))
                        {
                            customerLifetimeValues[sale.CustomerID] += sale.Quantity * sale.UnitPrice;
                        }
                        else
                        {
                            customerLifetimeValues[sale.CustomerID] = sale.Quantity * sale.UnitPrice;
                        }

                        //sales trends                      
                       
                        string invoiceDateStr = record.InvoiceDate; // Cast to string if necessary
                        string quantityStr = record.Quantity.ToString(); // Ensuring it's a string
                        string unitPriceStr = record.UnitPrice.ToString(); // Ensuring it's a string

                        DateTime invoiceDate_trends;
                        int quantity_trends;
                        double unitPrice_trends;

                        if (DateTime.TryParse(invoiceDateStr, out invoiceDate_trends) &&
                            int.TryParse(quantityStr, out quantity_trends) &&
                            double.TryParse(unitPriceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out unitPrice_trends))
                        {
                            // Perform your calculations with the parsed data
                            var saleAmount_trends = quantity_trends * unitPrice_trends;
                            // Now, we are sure that invoiceDate2, quantity2, and unitPrice2 are properly typed
                            var yearMonthKey = (Year: invoiceDate_trends.Year, Month: invoiceDate_trends.Month);

                            // Update your monthly sales trends dictionary
                            if (monthlySalesTrends.ContainsKey(yearMonthKey))
                            {
                                monthlySalesTrends[yearMonthKey] += saleAmount_trends;
                            }
                            else
                            {
                                monthlySalesTrends.Add(yearMonthKey, saleAmount_trends);
                            }
                        }

                                }
                            }

                var db = _redisConnection.GetDatabase();
                var hasData = await db.StringGetAsync("data:initialized");

                if (hasData.HasValue)
                {
                    return; // Data already initialized, no further action required
                }

                // Start a batch operation
                var batch = db.CreateBatch();
                var tasks = new List<Task>();

                // Adding sales
                foreach (var sale in sales)
                {
                    var key = $"sale:{sale.SalesID}";
                    var value = JsonConvert.SerializeObject(sale);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Adding customers
                foreach (var customer in customers)
                {
                    var key = $"customer:{customer.CustomerID}";
                    var value = JsonConvert.SerializeObject(customer);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Adding products
                foreach (var product in products)
                {
                    var key = $"product:{product.StockCode}";
                    var value = JsonConvert.SerializeObject(product);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Adding countries
                foreach (var country in countries)
                {
                    var key = $"country:{country.CountryID}";
                    var value = JsonConvert.SerializeObject(country);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Adding dates
                foreach (var date in dates)
                {
                    var key = $"date:{date.DateID}";
                    var value = JsonConvert.SerializeObject(date);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Adding total sales by country
                foreach (var kvp in totalSalesByCountry)
                {
                    var key = $"totalSalesByCountry:{kvp.Key}";
                    tasks.Add(batch.StringSetAsync(key, kvp.Value.ToString(CultureInfo.InvariantCulture)));
                }

                // Adding total sales per product
                foreach (var kvp in totalSalesByProduct)
                {
                    var productSalesData = new ProductSalesViewModel
                    {
                        ProductStockCode = kvp.Key,
                        Quantity = kvp.Value.Quantity,
                        UnitPrice = kvp.Value.UnitPrice,
                        TotalAmount = kvp.Value.TotalAmount
                    };
                    var key = $"productTotalSales:{kvp.Key}";
                    var value = JsonConvert.SerializeObject(productSalesData);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Adding aggregated invoice summaries
                foreach (var summary in invoiceSummaries.Values)
                {
                    var key = $"invoiceSummary:{summary.InvoiceNo}";
                    var value = JsonConvert.SerializeObject(summary);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Adding customer lifetime values
                foreach (var kvp in customerLifetimeValues)
                {
                    var key = $"customerLifetimeValue:{kvp.Key}";
                    tasks.Add(batch.StringSetAsync(key, kvp.Value.ToString(CultureInfo.InvariantCulture)));
                }

                // Adding sales trends
                foreach (var kvp in monthlySalesTrends)
                {
                    var salesTrendValue = new SalesTrendViewModel
                    {
                        Year = kvp.Key.Year,
                        Month = kvp.Key.Month,
                        TotalSales = kvp.Value
                    };
                    var key = $"salesTrend:{kvp.Key.Year}-{kvp.Key.Month}";
                    var value = JsonConvert.SerializeObject(salesTrendValue);
                    tasks.Add(batch.StringSetAsync(key, value));
                }

                // Execute the batch
                batch.Execute();
                await Task.WhenAll(tasks);

                // Mark data as initialized
                await db.StringSetAsync("data:initialized", "true");
            

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred during initialization: {ex.Message}");
            }
        }
    }
}