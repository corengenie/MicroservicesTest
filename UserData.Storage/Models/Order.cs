using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UserData.Storage.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime Created { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public double TotalPrice { get; set; }
        [JsonIgnore]
        public User User { get; set; }
        [NotMapped]
        public int DistinctItemsCount => Items.Count;
    }
}
