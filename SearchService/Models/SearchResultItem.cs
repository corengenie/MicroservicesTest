namespace SearchService.API.Models
{
    /// <summary>
    /// Элемент поиска по запросу
    /// </summary>
    public class SearchResultItem
    {
        /// <summary>
        /// Id записи
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Название или имя
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Тип сущности
        /// </summary>
        public string Type { get; set; }
    }
}
