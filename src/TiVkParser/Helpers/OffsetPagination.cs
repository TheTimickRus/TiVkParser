namespace TiVkParser.Helpers;

/// <summary>
/// Класс - Helper для работы с VK пагинацией
/// </summary>
public class OffsetPagination
{
    /// <summary>
    /// Общее количество элементов
    /// </summary>
    public long TotalCount { get; set; } = 1;
    /// <summary>
    /// Текущий Offset. Передавать в API запрос
    /// </summary>
    public long CurrentOffset { get; private set; }
    /// <summary>
    /// Признак окончания работы пагинации. Передавать в условие цикла
    /// </summary>
    public bool IsNotFinal => TotalCount > CurrentOffset;
    
    /// <summary>
    /// Величина смещения
    /// </summary>
    private readonly long _offsetLenght;
    
    /// <summary>
    /// Конструктор с параметрами #2
    /// </summary>
    /// <param name="totalCount">Всего объектов</param>
    /// <param name="offsetLenght">Величина смещения. По умолчанию - 100</param>
    public OffsetPagination(long totalCount, long offsetLenght = 100)
    {
        TotalCount = totalCount;
        _offsetLenght = offsetLenght;
    }
    
    /// <summary>
    /// Метод для увеличения Offset. Вызывать в теле цикла, после выполнения API запроса
    /// </summary>
    public void Increment()
    {
        CurrentOffset += _offsetLenght;
    }
}