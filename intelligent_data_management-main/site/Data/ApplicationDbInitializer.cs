﻿using System;
using System.IO;
using System.Linq;
using Site.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Site.Data
{
    public class ApplicationDbInitializer
    {
        public static async Task Initialize(ApplicationDbContext db, UserManager<IdentityUser> um, RoleManager<IdentityRole> rm)
        {

            db.Database.EnsureCreated();
// Assuming 'db' is your ApplicationDbContext instance
            var csvFilePath = "Data/data.csv";
            await PopulateDataFromCsv(db, csvFilePath);

            // Check if any of the required tables exist in the database
            if (!db.Customers.Any() || !db.Products.Any() || !db.Countries.Any() || !db.Dates.Any())
            {
                // If any of the required tables are empty, populate them from CSV file
            }

            
            
            // Check if the 'Admin' role exists, create if not
            var adminRoleExists = await rm.RoleExistsAsync("Admin");
            if (!adminRoleExists)
            {
                var adminRole = new IdentityRole("Admin");
                var roleResult = await rm.CreateAsync(adminRole);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Failed to create 'Admin' role.");
                }
            }

// Check if the admin user exists
            var admin = await um.FindByEmailAsync("admin@uia.no");
            if (admin == null)
            {
                // Create the admin user if they don't exist
                admin = new IdentityUser { UserName = "admin@uia.no", Email = "admin@uia.no" };
                var adminResult = await um.CreateAsync(admin, "Password1.");
                if (!adminResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create admin user: {string.Join(", ", adminResult.Errors.Select(e => e.Description))}");
                }
            }

// Ensure the admin user is in the 'Admin' role
            var isInAdminRole = await um.IsInRoleAsync(admin, "Admin");
            if (!isInAdminRole)
            {
                var addToRoleResult = await um.AddToRoleAsync(admin, "Admin");
                if (!addToRoleResult.Succeeded)
                {
                    throw new InvalidOperationException("Failed to assign 'Admin' role.");
                }
            }

// Repeat the process for the standard user
            var user = await um.FindByEmailAsync("user@uia.no");
            if (user == null)
            {
                user = new IdentityUser { UserName = "user@uia.no", Email = "user@uia.no" };
                var userResult = await um.CreateAsync(user, "Password1.");
                if (!userResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create standard user: {string.Join(", ", userResult.Errors.Select(e => e.Description))}");
                }
            }

// Save any changes to the DbContext
            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error saving changes to the database.", ex);
            }

        }
        
private static async Task PopulateDataFromCsv(ApplicationDbContext db, string csvFilePath)
{
    // Load existing data
    var existingCustomers = await db.Customers.ToDictionaryAsync(c => c.CustomerID);
    var existingProducts = await db.Products.ToDictionaryAsync(p => p.StockCode);
    var existingCountries = await db.Countries.ToDictionaryAsync(c => c.CountryName);
    var existingDates = await db.Dates.ToDictionaryAsync(d => d.InvoiceDate);

    // Lists to hold new entities
    List<Customer> newCustomers = new List<Customer>();
    List<Product> newProducts = new List<Product>();
    List<Country> newCountries = new List<Country>();
    List<Date> newDates = new List<Date>();
    List<Sale> newSales = new List<Sale>();
    

    using (var reader = new StreamReader(csvFilePath))
    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null }))
    {
        csv.Read();
        csv.ReadHeader();
        while (csv.Read())
        {
            var invoiceNo = csv.GetField<string>("InvoiceNo");
            var stockCode = csv.GetField<string>("StockCode");
            var description = csv.GetField<string>("Description");
            var quantity = csv.GetField<int>("Quantity");
            var unitPrice = csv.GetField<double>("UnitPrice");
            var invoiceDateStr = csv.GetField<string>("InvoiceDate");
            var customerId = csv.GetField<string>("CustomerID");
            var countryName = csv.GetField<string>("Country");

            if (DateTime.TryParse(invoiceDateStr, out var invoiceDateValue))
            {
                invoiceDateValue = DateTime.SpecifyKind(invoiceDateValue, DateTimeKind.Utc);

                Country country = existingCountries.ContainsKey(countryName) ? existingCountries[countryName] : newCountries.FirstOrDefault(c => c.CountryName == countryName);
                if (country == null)
                {
                    country = new Country { CountryID = Guid.NewGuid().ToString(), CountryName = countryName };
                    existingCountries[countryName] = country;
                    newCountries.Add(country);
                }

                Customer customer = existingCustomers.ContainsKey(customerId) ? existingCustomers[customerId] : newCustomers.FirstOrDefault(c => c.CustomerID == customerId);
                if (customer == null)
                {
                    customer = new Customer { CustomerID = customerId };
                    existingCustomers[customerId] = customer;
                    newCustomers.Add(customer);
                }

                Product product = existingProducts.ContainsKey(stockCode) ? existingProducts[stockCode] : newProducts.FirstOrDefault(p => p.StockCode == stockCode);
                if (product == null)
                {
                    product = new Product { StockCode = stockCode, Description = description };
                    existingProducts[stockCode] = product;
                    newProducts.Add(product);
                }

                Date date = existingDates.ContainsKey(invoiceDateValue) ? existingDates[invoiceDateValue] : newDates.FirstOrDefault(d => d.InvoiceDate == invoiceDateValue);
                if (date == null)
                {
                    date = new Date { DateID = Guid.NewGuid().ToString(), InvoiceDate = invoiceDateValue };
                    existingDates[invoiceDateValue] = date;
                    newDates.Add(date);
                }

                var sale = new Sale
                {
                    InvoiceNo = invoiceNo,
                    StockCode = stockCode,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    CustomerID = customer.CustomerID,
                    CountryID = country.CountryID,
                    InvoiceDateID = date.DateID,
                    Description = description,
                    Customer = customer,  // Assign the Customer navigation property
                    Country = country,    // Assign the Country navigation property
                    Product = product,    // Assign the Product navigation property
                    Dates = date    // Assign the Date navigation property
                };
                newSales.Add(sale);
            }
        }
    }

    // Save new records to the database
    using (var transaction = db.Database.BeginTransaction())
    {
        
       

        
        try
        {
            db.Countries.AddRange(newCountries);
            db.Customers.AddRange(newCustomers);
            db.Products.AddRange(newProducts);
            db.Dates.AddRange(newDates);
            db.Sales.AddRange(newSales);

            await db.SaveChangesAsync();
            transaction.Commit();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Console.WriteLine("Error occurred: " + ex.Message);
            throw;
        }
    }
}

        
        
        
        private static void PopulateDataFromCsv2(ApplicationDbContext db, string csvFilePath)
{
    var customerIds = new HashSet<string>();
    var productStockCodes = new HashSet<string>();
    
    var countryNames = new HashSet<string>();
    var dates = new Dictionary<string,DateTime>();
    var sales = new List<Sale>();
    string format = "MM/dd/yyyy H:mm"; // Date format
    CultureInfo provider = CultureInfo.InvariantCulture;

    using (var reader = new StreamReader(csvFilePath))
    using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
    {
        HeaderValidated = null, MissingFieldFound = null
    }))
    {
        csv.Read();
        csv.ReadHeader(); 
        while (csv.Read())
        {
            try
            {
                var invoiceNo = csv.GetField<string>("InvoiceNo");
                var stockCode = csv.GetField<string>("StockCode");
                var description = csv.GetField<string>("Description");
                var quantity = csv.GetField<int>("Quantity");
                var unitPrice = csv.GetField<double>("UnitPrice");
                var invoiceDateStr = csv.GetField<string>("InvoiceDate");
                var customerId = csv.GetField<string>("CustomerID");
                var countryName = csv.GetField<string>("Country");
                
               var CountryID = Guid.NewGuid().ToString();
               var invoiceDateId = Guid.NewGuid().ToString();
               var saleID = Guid.NewGuid().ToString();


               if (DateTime.TryParseExact(csv.GetField<string>("InvoiceDate"), format, provider, DateTimeStyles.None, out var invoiceDateValue))
               {
                   // Convert to UTC as you have, ensuring the Kind is explicitly set to DateTimeKind.Utc
                   invoiceDateValue = DateTime.SpecifyKind(invoiceDateValue, DateTimeKind.Utc);
               }


                dates.Add(invoiceDateId,invoiceDateValue);
                customerIds.Add(customerId);
                productStockCodes.Add(stockCode);
                countryNames.Add(countryName);

                // Constructing Sale object
                var sale = new Sale
                {
                    InvoiceNo = invoiceNo,
                    StockCode = stockCode,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    CustomerID = customerId,
                    CountryID = CountryID, 
                    InvoiceDateID = invoiceDateId,
                    Description = description
                    
                };
                sales.Add(sale);
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                Console.WriteLine(ex.Message); // Modify with your logging approach
            }
        }
    }
        
    // Bulk add dates, customers, products, and countries
    AddDateRecords(db, dates);
    AddCustomerRecords(db, customerIds);
    AddProductRecords(db, productStockCodes);
    AddCountryRecords(db, countryNames);
    
    
    db.SaveChanges();

    
    
    db.Sales.AddRange(sales);
    db.SaveChanges();
}

        private static void AddDateRecords(ApplicationDbContext db, Dictionary<string, DateTime> dates)
        {
            // Fetch all existing dates to avoid adding duplicates.
            var existingDateIds = new HashSet<string>(db.Dates.Select(d => d.DateID));

            var newDates = new List<Date>();
            foreach (var entry in dates)
            {
                // Ensure the DateTime is in UTC
                DateTime utcDate = DateTime.SpecifyKind(entry.Value, DateTimeKind.Utc);

                // Only add new dates whose IDs do not exist already
                if (!existingDateIds.Contains(entry.Key))
                {
                    newDates.Add(new Date { DateID = entry.Key, InvoiceDate = utcDate });
                    existingDateIds.Add(entry.Key); // Add the new ID to the set to prevent duplicates within the loop
                }
            }

            if (newDates.Any())
            {
                db.Dates.AddRange(newDates);
            }
        }





private static void AddCustomerRecords(ApplicationDbContext db, HashSet<string> customerIds)
{
    var existingCustomerIds = db.Customers.Select(c => c.CustomerID).ToHashSet();
    var newCustomers = customerIds.Except(existingCustomerIds).Select(id => new Customer(id));
    db.Customers.AddRange(newCustomers);
}

private static void AddProductRecords(ApplicationDbContext db, HashSet<string> productStockCodes )
{
    var existingStockCodes = db.Products.Select(p => p.StockCode).ToHashSet();
    var newProducts = productStockCodes.Except(existingStockCodes).Select(code => new Product(code, "w", 22)); // Modify as needed
    db.Products.AddRange(newProducts);
}

private static void AddCountryRecords(ApplicationDbContext db, HashSet<string> countryNames)
{
    var existingCountryNames = db.Countries.Select(c => c.CountryName).ToHashSet();
    var newCountries = countryNames.Except(existingCountryNames).Select(name => new Country(Guid.NewGuid().ToString(), name)); // Modify as needed
    db.Countries.AddRange(newCountries);
}

    }
}