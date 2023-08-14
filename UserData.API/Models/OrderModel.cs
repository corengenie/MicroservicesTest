namespace UserData.API.Models
{
    public class OrderModel
    {
        public int UserId { get; set; }
        public List<OrderItemModel> Items { get; set; } = new List<OrderItemModel>();
    }
}
