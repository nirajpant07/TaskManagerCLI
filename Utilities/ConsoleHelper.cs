using System;

namespace TaskManagerCLI.Utilities;

public class ConsoleHelper
{
    public void WriteHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        var border = new string('=', title.Length + 6);
        Console.WriteLine(border);
        Console.WriteLine($"   {title}   ");
        Console.WriteLine(border);
        Console.ResetColor();
    }

    public void WriteLine(string message = "")
    {
        Console.WriteLine(message);
    }

    public void Write(string message)
    {
        Console.Write(message);
    }

    public void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteHighlight(string message)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void WriteBanner(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.BackgroundColor = ConsoleColor.DarkBlue;
        Console.WriteLine($" {message} ");
        Console.ResetColor();
    }

    public void WriteSeparator(char character = '-', int length = 50)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string(character, length));
        Console.ResetColor();
    }

    public void WriteColoredLine(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public void ClearScreen()
    {
        Console.Clear();
    }

    public ConsoleKeyInfo ReadKey(bool intercept = true)
    {
        return Console.ReadKey(intercept);
    }

    public string? ReadLine()
    {
        return Console.ReadLine();
    }

    public void SetCursorPosition(int left, int top)
    {
        Console.SetCursorPosition(left, top);
    }

    public void ShowProgress(string message, int percentage)
    {
        Console.Write($"\r{message} ");

        var progressBar = "[";
        var filled = percentage / 2; // 50 chars max

        for (int i = 0; i < 50; i++)
        {
            progressBar += i < filled ? "█" : "░";
        }

        progressBar += $"] {percentage}%";
        Console.Write(progressBar);
    }

    public void WriteTable(string[] headers, string[][] rows)
    {
        if (headers.Length == 0 || rows.Length == 0) return;

        // Calculate column widths
        var columnWidths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            columnWidths[i] = headers[i].Length;
            foreach (var row in rows)
            {
                if (i < row.Length && row[i].Length > columnWidths[i])
                {
                    columnWidths[i] = row[i].Length;
                }
            }
        }

        // Write headers
        Console.ForegroundColor = ConsoleColor.White;
        for (int i = 0; i < headers.Length; i++)
        {
            Console.Write(headers[i].PadRight(columnWidths[i] + 2));
        }
        Console.WriteLine();
        Console.ResetColor();

        // Write separator
        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (int i = 0; i < headers.Length; i++)
        {
            Console.Write(new string('-', columnWidths[i] + 2));
        }
        Console.WriteLine();
        Console.ResetColor();

        // Write rows
        foreach (var row in rows)
        {
            for (int i = 0; i < headers.Length && i < row.Length; i++)
            {
                Console.Write(row[i].PadRight(columnWidths[i] + 2));
            }
            Console.WriteLine();
        }
    }
}