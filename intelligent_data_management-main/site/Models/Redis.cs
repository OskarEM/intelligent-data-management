using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using StackExchange.Redis;

namespace Site.Models
{
    public class RedisSale
    {
        public string SalesID { get; set; }
        public string InvoiceNo { get; set; }
        public string StockCode { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public DateTime InvoiceDate { get; set; } // Changed from string to DateTime
        public string CustomerID { get; set; }
        public string CountryID { get; set; }
        public string ProductStockCode { get; set; }
        public string InvoiceDateID { get; set; } // Unique identifier for InvoiceDate
        

        public RedisSale(string salesID,string invoiceNo, string stockCode, string description, int quantity, double unitPrice, 
                        string customerId, string countryId, string invoiceDateId, string productStockCode)
        {
            SalesID = salesID;
            InvoiceNo = invoiceNo;
            StockCode = stockCode;
            Description = description;
            Quantity = quantity;
            UnitPrice = unitPrice;
            CustomerID = customerId;
            CountryID = countryId;
            InvoiceDateID = invoiceDateId;
            ProductStockCode = productStockCode;
        }

        public string Serialize()
        {
            var settings = new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ssZ"
            };
            return JsonConvert.SerializeObject(this, Formatting.Indented, settings);
        }

        public static RedisSale Deserialize(string serialized)
        {
            return JsonConvert.DeserializeObject<RedisSale>(serialized);
        }
    }


    public class RedisDate
    {
        public string DateID { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }

        public RedisDate(DateTime invoiceDate, string dateID)
        {
            DateID = dateID;
            InvoiceDate = invoiceDate;
            Year = invoiceDate.Year;
            Month = invoiceDate.Month;
            Day = invoiceDate.Day;
        }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static RedisDate Deserialize(string serialized) => JsonConvert.DeserializeObject<RedisDate>(serialized);
    }
    public class RedisProduct
    {
        public string StockCode { get; set; }
        public string Description { get; set; }
        public double UnitPrice { get; set; }


        public RedisProduct(string stockCode, string description, double unitPrice)
        {
            StockCode = stockCode;
            Description = description;
            UnitPrice = unitPrice;
        }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static RedisProduct Deserialize(string serialized) => JsonConvert.DeserializeObject<RedisProduct>(serialized);
    }
    public class RedisCustomer
    {
        public string CustomerID { get; set; }

        public RedisCustomer(string customerId)
        {
            CustomerID = customerId;
        }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static RedisCustomer Deserialize(string serialized) => JsonConvert.DeserializeObject<RedisCustomer>(serialized);
    }
    public class RedisCountry
    {
        public string CountryID { get; set; }
        public string CountryName { get; set; }

        public RedisCountry(string countryName)
        {
            CountryID = Guid.NewGuid().ToString();
            CountryName = countryName;
        }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static RedisCountry Deserialize(string serialized) => JsonConvert.DeserializeObject<RedisCountry>(serialized);
    }

    
}
