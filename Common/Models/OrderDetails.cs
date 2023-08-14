namespace Common.Models
{
    /// <summary>
    /// Детали заказа
    /// </summary>
    public class OrderDetails
    {
        /// <summary>
        /// Id заказа
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Дата создания заказа
        /// </summary>
        public DateTime Created { get; set; }
        /// <summary>
        /// Продукты в заказе
        /// </summary>
        public IEnumerable<OrderItem> Items { get; set; } = new List<OrderItem>();
        /// <summary>
        /// Общая стоимость
        /// </summary>
        public double TotalPrice { get; set; }
        /// <summary>
        /// Кол-во позиций в заказе
        /// </summary>
        public int DistinctItemsCount => Items.Count();
    }
}