using System.Collections.Generic;

namespace Site.Models
{
    public class SortCriterion
    {
        public string Field { get; set; }
        public string Direction { get; set; } = "asc";
    }

    public class SortViewModel<T>
    {
        public IEnumerable<T> Data { get; set; }
        public List<SortCriterion> SortCriteria { get; set; } = new List<SortCriterion>();
    }

}