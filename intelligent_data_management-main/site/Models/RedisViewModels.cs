using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using StackExchange.Redis;

namespace Site.ViewModels
{
    public class TotalSalesByCountryViewModel
    {
        public string Country { get; set; }
        public double TotalSales { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static TotalSalesByCountryViewModel Deserialize(string serialized) => JsonConvert.DeserializeObject<TotalSalesByCountryViewModel>(serialized);
    }
    

    public class ProductSalesViewModel
    {
        public string ProductStockCode { get; set; }
       
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        
        public double TotalAmount { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static ProductSalesViewModel Deserialize(string serialized) => JsonConvert.DeserializeObject<ProductSalesViewModel>(serialized);
    }

    public class InvoiceSummaryViewModel
    {
        public string InvoiceNo { get; set; }
        public double TotalAmount { get; set; }
        public string CustomerName { get; set; }
        public string CountryName { get; set; }
        public DateTime InvoiceDate { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static InvoiceSummaryViewModel Deserialize(string serialized) => JsonConvert.DeserializeObject<InvoiceSummaryViewModel>(serialized);
    }
    
    public class CustomerLifetimeValueViewModel
    {
        public string CustomerID { get; set; }
        public double LifetimeValue { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static CustomerLifetimeValueViewModel Deserialize(string serialized) => JsonConvert.DeserializeObject<CustomerLifetimeValueViewModel>(serialized);
    }
    
    public class SalesTrendViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public double TotalSales { get; set; }

        public string Serialize() => JsonConvert.SerializeObject(this);
        public static SalesTrendViewModel Deserialize(string serialized) => JsonConvert.DeserializeObject<SalesTrendViewModel>(serialized);
    }
}
