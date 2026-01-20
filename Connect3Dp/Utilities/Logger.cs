using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Connect3Dp.Utilities
{
    public class Logger
    {
        //internal static Logger Default { get; } = new Logger("Connect3Dp");

        private static readonly Dictionary<string, Level> OverriddenCategoryLevels = [];

        private static readonly Dictionary<string, Logger> CategoriesToLogger = [];

        public string CategoryName { get; }
        public Level VisibleLevels { get; }

        private readonly object _LOCK = new();

        public Logger(string categoryName, Level visibleLevels = Level.Trace)
        {
            this.CategoryName = categoryName;
            this.VisibleLevels = visibleLevels;
        }

        public void Log(Level level, string message, ConsoleColor foregroundColor = ConsoleColor.White)
        {
            Level visibleLevels = this.VisibleLevels;
            if (OverriddenCategoryLevels.TryGetValue(this.CategoryName, out var overiddenLevel))
            {
                visibleLevels = overiddenLevel;
            }
            if (level > visibleLevels) return;

            lock (_LOCK)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{DateTime.Now.ToString("h:mm tt")}] ");

                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write($"{CategoryName} ");

                Console.ForegroundColor = level switch
                {
                    Level.Error => ConsoleColor.Red,
                    Level.Warning => ConsoleColor.Yellow,
                    Level.Trace => ConsoleColor.DarkCyan,
                    _ => ConsoleColor.White
                };
                Console.Write($"[{Enum.GetName(level)!.ToUpper()}] ");

                Console.ForegroundColor = foregroundColor;

                Console.WriteLine(message);

                Console.ResetColor();
            }
        }

        public void Trace(string message)
        {
            Log(Level.Trace, message);
        }

        public void TraceSuccess(string message)
        {
            Log(Level.Trace, message, ConsoleColor.Green);
        }

        public void TraceFunctionEnter([CallerMemberName] string callerName = "")
        {
            Log(Level.Trace, $"Entered {callerName}()");
        }

        public void TraceFunctionExit([CallerMemberName] string callerName = "")
        {
            Log(Level.Trace, $"Exited {callerName}()");
        }

        public void Info(string message)
        {
            Log(Level.Info, message);
        }

        public void Warning(string message)
        {
            Log(Level.Warning, message);
        }

        public void Error(string message)
        {
            Log(Level.Error, message);
        }

        public void Info(string message, ConsoleColor foregroundColor)
        {
            Console.ForegroundColor = foregroundColor;
            Info(message);
        }

        public static Logger Category(string categoryName)
        {
            if (!CategoriesToLogger.TryGetValue(categoryName, out Logger? value))
            {
                value = new Logger(categoryName);
                CategoriesToLogger[categoryName] = value;
            }
            return value;
        }

        public static void OverrideCategoryLevel(string category, Level desiredLevel)
        {
            OverriddenCategoryLevels.Add(category, desiredLevel);
        }

        public enum Level
        {
            Error,
            Warning,
            Info,
            Trace
        }
    }
}
