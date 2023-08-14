namespace Common.Models
{
    /// <summary>
    /// Продукт в заказе
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// Id продукта
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// Название продукта
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Цена продукта
        /// </summary>
        public double Price { get; set; }
        /// <summary>
        /// Количество товара
        /// </summary>
        public int ProductCount { get; set; }
    }
}
