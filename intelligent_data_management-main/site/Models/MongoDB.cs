using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Site.Models
{
    public class MongoSale
    {
        [BsonElement("SalesID")]
		
        public string SalesID { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("invoiceNo")]
        public string InvoiceNo { get; set; }
        [BsonElement("stockCode")]
        public string StockCode { get; set; }
        [BsonElement("quantity")]
        public int Quantity { get; set; }
        [BsonElement("unitPrice")]
        public double UnitPrice { get; set; }
       
        [BsonIgnore]
        public double TotalPrice => Quantity * UnitPrice;

        [BsonElement("invoiceDate")]
        public DateTime InvoiceDate { get; set; }
        [BsonElement("customerId")]
        public string CustomerID { get; set; }
        [BsonElement("countryName")]
        public string CountryName { get; set; }
        [BsonElement("productStockCode")]
        public string ProductStockCode { get; set; }

        public MongoSale(string invoiceNo, string stockCode, int quantity, double unitPrice, 
                         string customerId, string countryname, DateTime invoiceDate, string productStockCode)
        {
            InvoiceNo = invoiceNo;
            StockCode = stockCode;
            Quantity = quantity;
            UnitPrice = unitPrice;
            CustomerID = customerId;
            CountryName = countryname;
            InvoiceDate = invoiceDate;
            ProductStockCode = productStockCode;
        }
    }

    public class MongoDate
    {
        [BsonElement("DateID")]
        public string DateID { get; set; }
        
        [BsonElement("invoiceDate")]
        public DateTime InvoiceDate { get; set; }

        // These properties might be useful for queries on specific dates.
        [BsonElement("year")]
        public int Year { get; set; }
        [BsonElement("month")]
        public int Month { get; set; }
        [BsonElement("day")]
        public int Day { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public MongoDate(DateTime invoiceDate)
        {
            InvoiceDate = invoiceDate;
            Year = invoiceDate.Year;
            Month = invoiceDate.Month;
            Day = invoiceDate.Day;
        }
    }

    public class MongoProduct
    {
        [BsonElement("StockCode")]
        public string StockCode { get; set; }
        
        [BsonElement("UnitPrice")]

        public double UnitPrice { get; set; }
        [BsonElement("description")]
        public string Description { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public MongoProduct(string stockCode, string description)
        {
            StockCode = stockCode;
            Description = description;
        }
    }

    public class MongoCustomer
    {
        [BsonElement("CustomerID")]

        public string CustomerID { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
       
        public MongoCustomer(string customerId)
        {
            CustomerID = customerId;
            // Initialize other properties as needed
        }
    }

    public class MongoCountry
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        [BsonElement("CountryID")]
        public string CountryID { get; set; }

        [BsonElement("countryName")]
        public string CountryName { get; set; }

        public MongoCountry(string countryId, string countryName)
        {
            CountryID = countryId;
            CountryName = countryName;
        }
    }
    
    
    
}
