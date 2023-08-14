namespace Common.Models
{
    /// <summary>
    /// Цена и название продукта
    /// </summary>
    public class ProductPrice
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
    }
}
