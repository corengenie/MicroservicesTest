namespace Common.Models
{
    /// <summary>
    /// Результат поиска
    /// </summary>
    /// <typeparam name="T">Тип сущности для поиска</typeparam>
    public class SearchResult<T> where T : class
    {
        /// <summary>
        /// Массив найденых записей
        /// </summary>
        public IEnumerable<T> Objects { get; set; } = null!;
        /// <summary>
        /// Запрошенное количество записей
        /// </summary>
        public int RequestedObjectsCount { get; set; }
        /// <summary>
        /// Запрошенный начальный индекс
        /// </summary>
        public int RequestedStartIndex { get; set; }
        /// <summary>
        /// Общее количество записей
        /// </summary>
        public int Total { get; set; }
    }
}
