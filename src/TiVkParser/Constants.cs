using System.Diagnostics.CodeAnalysis;
using Spectre.Console;

namespace TiVkParser;

public static class Constants
{
    public static class Titles
    {
        /// <summary>
        /// *Версия программы* (1.0)
        /// </summary>
        public const string Version = "v.1.0";
        /// <summary>
        /// *Версия программы с датой* (1.0 (02.09.2022))
        /// </summary>
        public const string VersionWithDate = "v.1.0 (02.09.2022)";
        /// <summary>
        /// *Название программы* (*Версия* (*дата*)) by *Разработчик*
        /// </summary>
        public const string FullTitle = $"TiVkParser ({VersionWithDate}) by Timick";
        /// <summary>
        /// *Название программы* by *Разработчик*
        /// </summary>
        public const string ShortTitle = "TiVkParser by Timick";
        /// <summary>
        /// *Название программы*
        /// </summary>
        public const string VeryShortTitle = "TiVkParser";
        /// <summary>
        /// Имя лог-файла
        /// </summary>
        public const string LogFileName = $"{VeryShortTitle}.log";
    }

    [SuppressMessage("Usage", "CA2211:Поля, не являющиеся константами, не должны быть видимыми")]
    public static class Colors
    {
        /// <summary>
        /// Основной цвет
        /// </summary>
        public static Color MainColor = Color.SteelBlue;
        /// <summary>
        /// Цвет успеха
        /// </summary>
        public static Color SuccessColor = Color.SeaGreen1;
        /// <summary>
        /// Цвет ошибки
        /// </summary>
        public static Color ErrorColor = Color.Red; 
    }
}