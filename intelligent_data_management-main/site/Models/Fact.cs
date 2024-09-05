using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Confluent.Kafka.Admin;

namespace Site.Models
{
    public class Sale
    {
        public Sale() { }

        public Sale(string invoiceNo, string stockCode, int quantity, double unitPrice, 
            string customerId, string countryId, string dateId, string description)
        {
            InvoiceNo = invoiceNo;
            StockCode = stockCode;
            Quantity = quantity;
            UnitPrice = unitPrice;
            CustomerID = customerId;
            CountryID = countryId;
            InvoiceDateID = dateId;
            Description = description;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SalesID { get; set; } 
        public string InvoiceNo { get; set; }
        public string StockCode { get; set; }
        public string Description { get; set; }

        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get { return Quantity * UnitPrice; } }
        public string InvoiceDateID { get; set; }
        public string CustomerID { get; set; }
        public string CountryID { get; set; }

        // Navigation properties
        public virtual Date Dates { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual Country Country { get; set; }
        public virtual Product Product { get; set; }
    }
	


    public class Date
    {
        public Date() { }

        public Date(string dateID , DateTime invoiceDate)
        {
                DateID = dateID;
                InvoiceDate = invoiceDate;
                Year = invoiceDate.Year;
                Month = invoiceDate.Month;
                Day = invoiceDate.Day;
        }

        
        [Key]
        public string DateID { get; set; } 
        public DateTime InvoiceDate { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }

    public class Product
    {
        public Product() { }

        public Product(string stockCode, string description,double unitPrice)
        {
            StockCode = stockCode;
            Description = description;
            UnitPrice = unitPrice;
        }

        [Key] // Define a primary key for the Product entity
        public string StockCode { get; set; }
        public string Description { get; set; }
        
        public double UnitPrice { get; set; }

    }

    public class Customer
    {
        public Customer() { }

        public Customer(string customerId)
        {
            CustomerID = customerId;
        }

        public string CustomerID { get; set; }
    }

    public class Country
    {
        public Country() { }

        public Country(string countryId, string countryName)
        {
            CountryID = countryId;
            CountryName = countryName;
        }

        public string CountryID { get; set; }
        public string CountryName { get; set; }
    }
}



public class SalesByProductViewModel
{
    public string StockCode { get; set; }
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double TotalPrice { get; set; }
    
}

public class TotalSalesByCountryViewModel
{
    public string Country { get; set; }
    public double TotalSales { get; set; }
}

public class CustomerLifetimeValueViewModel
{
    public string CustomerID { get; set; }
    public double LifetimeValue { get; set; }
}
public class InvoiceSummaryViewModel
{
    public string InvoiceNo { get; set; }
    public double TotalAmount { get; set; }
    public string CustomerName { get; set; }
    public string CountryName { get; set; }
    public DateTime InvoiceDate { get; set; }
    // ... add other necessary fields
}
public class SalesTrendViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public double TotalSales { get; set; }
}
