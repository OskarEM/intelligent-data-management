using System;
using System.IO;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;
using Site.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Collections.Generic;

namespace Site.Data
{
    public class MongoDbInitializer
    {
       public static void Initialize(IMongoClient client)
{
    if (client == null)
    {
        Console.WriteLine("MongoDB client is not initialized.");
        return;
    }

    var database = client.GetDatabase("MyAppDatabase");
    var salesCollection = database.GetCollection<MongoSale>("Sales");
    long documentCount = salesCollection.CountDocuments(FilterDefinition<MongoSale>.Empty);
    Console.WriteLine($"Documents in Sales Collection: {documentCount}");

    if (documentCount == 0)
    {
        Console.WriteLine("No documents found on first attempt, populating...");
        PopulateDataFromCsv(database, "Data/data.csv");
    }
    else
    {
        Console.WriteLine("Database already contains data. No need to repopulate.");
    }
    
    var customersCollection = database.GetCollection<MongoCustomer>("Customers");
    var productsCollection = database.GetCollection<MongoProduct>("Products");
    var countriesCollection = database.GetCollection<MongoCountry>("Countries");
    var datesCollection = database.GetCollection<MongoDate>("Dates");
    // Get the count for customers collection
    var customerCount = customersCollection.CountDocuments(_ => true);

// Get the count for products collection
    var productCount = productsCollection.CountDocuments(_ => true);

// Get the count for countries collection
    var countryCount = countriesCollection.CountDocuments(_ => true);

// Get the count for dates collection
    var dateCount = datesCollection.CountDocuments(_ => true);
    var salesCount = salesCollection.CountDocuments(_ => true);

    Console.WriteLine($"Customers count: {customerCount}");
    Console.WriteLine($"Products count: {productCount}");
    Console.WriteLine($"Countries count: {countryCount}");
    Console.WriteLine($"Dates count: {dateCount}");
    Console.WriteLine($"Sales count: {salesCount}");

}
        

        private static void PopulateDataFromCsv(IMongoDatabase database, string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"CSV file not found at path: {csvFilePath}");
                return;
            }

            EnsureCollectionsExist(database);

            var customersCollection = database.GetCollection<MongoCustomer>("Customers");
            var productsCollection = database.GetCollection<MongoProduct>("Products");
            var countriesCollection = database.GetCollection<MongoCountry>("Countries");
            var salesCollection = database.GetCollection<MongoSale>("Sales");
            var datesCollection = database.GetCollection<MongoDate>("Dates");

            var bulkCustomers = new List<WriteModel<MongoCustomer>>();
            var bulkProducts = new List<WriteModel<MongoProduct>>();
            var bulkCountries = new List<WriteModel<MongoCountry>>();
            var bulkDates = new List<WriteModel<MongoDate>>();
            var bulkSales = new List<WriteModel<MongoSale>>();

            using (var reader = new StreamReader(csvFilePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null }))
            {
                csv.Read();
                csv.ReadHeader();
                while (csv.Read())
                {
                   var customer = new MongoCustomer(csv.GetField<string>("CustomerID"));
					var product = new MongoProduct(csv.GetField<string>("StockCode"), csv.GetField<string>("Description"));
					var country = new MongoCountry(csv.GetField<string>("CountryID"), csv.GetField<string>("Country"));
					var date = new MongoDate(DateTime.Parse(csv.GetField<string>("InvoiceDate")));
					var sale = new MongoSale(
					    csv.GetField<string>("InvoiceNo"),
					    product.StockCode,
  						  csv.GetField<int>("Quantity"),
   						 csv.GetField<double>("UnitPrice"),
   						 customer.CustomerID,
  						  country.CountryName,
 						   date.InvoiceDate,
					    product.StockCode
					);


                    // Prepare bulk operations
                    bulkCustomers.Add(new InsertOneModel<MongoCustomer>(customer));
                    bulkProducts.Add(new InsertOneModel<MongoProduct>(product));
                    bulkCountries.Add(new InsertOneModel<MongoCountry>(country));
                    bulkDates.Add(new InsertOneModel<MongoDate>(date));
                    bulkSales.Add(new InsertOneModel<MongoSale>(sale));
                }
            }
// Execute bulk operations
            if (bulkCustomers.Any())
            {
                customersCollection.BulkWrite(bulkCustomers);
                Console.WriteLine("Customers bulk inserted. Sample Customer ID: {0}", ((InsertOneModel<MongoCustomer>)bulkCustomers.First()).Document.CustomerID);
            }

            if (bulkProducts.Any())
            {
                productsCollection.BulkWrite(bulkProducts);
                Console.WriteLine("Products bulk inserted. Sample Product Stock Code: {0}", ((InsertOneModel<MongoProduct>)bulkProducts.First()).Document.StockCode);
            }

            if (bulkCountries.Any())
            {
                countriesCollection.BulkWrite(bulkCountries);
                Console.WriteLine("Countries bulk inserted. Sample Country Name: {0}", ((InsertOneModel<MongoCountry>)bulkCountries.First()).Document.CountryName);
            }

            if (bulkDates.Any())
            {
                datesCollection.BulkWrite(bulkDates);
                Console.WriteLine("Dates bulk inserted. Sample Invoice Date: {0}", ((InsertOneModel<MongoDate>)bulkDates.First()).Document.InvoiceDate.ToString("yyyy-MM-dd"));
            }

            if (bulkSales.Any())
            {
                salesCollection.BulkWrite(bulkSales);
                Console.WriteLine("Sales bulk inserted. Sample Invoice No: {0}", ((InsertOneModel<MongoSale>)bulkSales.First()).Document.InvoiceNo);
            }

            CreateIndexes(database);
            Console.WriteLine("Data populated successfully with bulk operations.");


        }

        private static void EnsureCollectionsExist(IMongoDatabase database)
        {
            var collectionNames = database.ListCollectionNames().ToList();
            var requiredCollections = new List<string> { "Customers", "Products", "Countries", "Sales", "Dates" };

            foreach (var collection in requiredCollections)
            {
                if (!collectionNames.Contains(collection))
                {
                    database.CreateCollection(collection);
                    Console.WriteLine($"Created collection: {collection}");
                }
            }
        }
        
        private static void CreateIndexes(IMongoDatabase database)
        {
            // Indexes for the Dates collection
            var dateCollection = database.GetCollection<MongoDate>("Dates");
            var dateIndexModel = new CreateIndexModel<MongoDate>(Builders<MongoDate>.IndexKeys.Ascending(x => x.InvoiceDate));
            dateCollection.Indexes.CreateOne(dateIndexModel);
            Console.WriteLine("Index created on Dates collection for InvoiceDate.");

            // Indexes for the Sales collection
            var salesCollection = database.GetCollection<MongoSale>("Sales");
            var salesIndexModel = new CreateIndexModel<MongoSale>(Builders<MongoSale>.IndexKeys.Ascending(x => x.InvoiceNo));
            salesCollection.Indexes.CreateOne(salesIndexModel);
            Console.WriteLine("Index created on Sales collection for InvoiceNo.");

            // Indexes for the Customers collection
            var customersCollection = database.GetCollection<MongoCustomer>("Customers");
            var customersIndexModel = new CreateIndexModel<MongoCustomer>(Builders<MongoCustomer>.IndexKeys.Ascending(x => x.CustomerID));
            customersCollection.Indexes.CreateOne(customersIndexModel);
            Console.WriteLine("Index created on Customers collection for CustomerID.");

            // Indexes for the Products collection
            var productsCollection = database.GetCollection<MongoProduct>("Products");
            var productsIndexModel = new CreateIndexModel<MongoProduct>(Builders<MongoProduct>.IndexKeys.Ascending(x => x.StockCode));
            productsCollection.Indexes.CreateOne(productsIndexModel);
            Console.WriteLine("Index created on Products collection for StockCode.");

            // Indexes for the Countries collection
            var countriesCollection = database.GetCollection<MongoCountry>("Countries");
            var countriesIndexModel = new CreateIndexModel<MongoCountry>(Builders<MongoCountry>.IndexKeys.Ascending(x => x.CountryName));
            countriesCollection.Indexes.CreateOne(countriesIndexModel);
            Console.WriteLine("Index created on Countries collection for CountryID.");
        }


        
    }
}
