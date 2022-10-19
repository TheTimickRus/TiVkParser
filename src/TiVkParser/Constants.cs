// ReSharper disable MemberCanBePrivate.Global

using Spectre.Console;

namespace TiVkParser;

public static class Constants
{
    public static class Titles
    {
        /// <summary>
        /// *Версия программы* (v.1.0)
        /// </summary>
        public const string Version = "v.2.1";
        /// <summary>
        /// *Версия программы с датой* (v.1.0 (02.09.2022))
        /// </summary>
        public const string VersionWithDate = $"{Version} (19.10.2022)";
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

    public static class Colors
    {
        /// <summary>
        /// Основной цвет
        /// </summary>
        public static readonly Color MainColor = Color.Plum4;
        /// <summary>
        /// Второй цвет
        /// </summary>
        public static readonly Color SecondColor = Color.SlateBlue1;
        /// <summary>
        /// Цвет успеха
        /// </summary>
        public static readonly Color SuccessColor = Color.Lime;
        /// <summary>
        /// Цвет ошибки
        /// </summary>
        public static readonly Color ErrorColor = Color.Red; 
    }
}