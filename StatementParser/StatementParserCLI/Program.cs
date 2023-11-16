using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Commander.NET;
using Commander.NET.Exceptions;
using ConsoleTables;
using StatementParser;
using StatementParser.Models;

namespace StatementParserCLI;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var parser = new CommanderParser<Options>();

        try
        {
            var options = parser.Parse(args);
            await RunAsync(options);
        }
        catch (ParameterMissingException)
        {
            Console.WriteLine(parser.Usage());
        }
    }

    private static IList<string> ResolveFilePaths(string[] paths)
    {
        var output = new List<string>();
        foreach (var path in paths)
        {
            var sanitizedPath = path.TrimEnd('/', '\\');

            if (Directory.Exists(sanitizedPath))
            {
                var directoryFiles = Directory.GetFiles(sanitizedPath, "*.*", SearchOption.AllDirectories);
                output.AddRange(directoryFiles);
            }
            else if (File.Exists(sanitizedPath))
            {
                output.Add(sanitizedPath);
            }
        }

        return output;
    }

    private static async Task RunAsync(Options option)
    {
        var parser = new TransactionParser();
        var filePaths = ResolveFilePaths(option.StatementFilePaths);

        if (filePaths.Count == 0)
        {
            Console.WriteLine("No valid path to scan found. Check that file or directory exist.");
            return;
        }

        var tasks = filePaths
            .Select(file => parser.ParseAsync(file))
            .ToList();

        await Task.WhenAll(tasks);

        List<Transaction> transactions = tasks.SelectMany(t => t.Result).ToList();

        decimal sellingPrice = 256.38M;
        decimal currentPrice = 376M;
        int left = 13; // How many stocks left after selling

        DateTime soldAtDate = new DateTime(2023, 3, 6);
        DateTime eligibleDate = new DateTime(2020, 3, 6);

        var eligibleTransactions = transactions
            .Where(t => t is DepositTransaction or ESPPTransaction)
            .Where(t => t.Date >= eligibleDate && t.Date < soldAtDate)
            .ToList();

        decimal counter = 0;
        var leftoverTransactions = transactions
            .Where(t => t is DepositTransaction or ESPPTransaction)
            .Where(t => t.Date >= eligibleDate && t.Date < soldAtDate)
            .OrderByDescending(t => t.Date)
            .TakeWhile(t =>
            {
                counter += t.Amount;
                return counter < left;
            })
            .ToList();

        var taxableTransactions = eligibleTransactions
            .Except(leftoverTransactions)
            .ToList();

        var table = new ConsoleTable("Date", "Amount", "Price", "Gain");

        decimal gain = taxableTransactions
            .Sum(t =>
            {
                (decimal amount, decimal price) = t switch
                {
                    DepositTransaction d => (d.Amount, d.Price),
                    ESPPTransaction e => (e.Amount, e.MarketPrice),
                    _ => throw new ArgumentOutOfRangeException(nameof(t))
                };

                var gain = amount * (sellingPrice - price);
                table.AddRow(t.Date, amount, price, gain);
                return gain;
            });

        decimal loss = -gain;

        Console.WriteLine($"Gain: {gain} USD");
        Console.WriteLine($"Loss: {loss} USD");

        table.Write(Format.Minimal);

        // New stocks we can sell
        var newStocks = transactions
            .Where(t => t is DepositTransaction or ESPPTransaction)
            .Where(t => t.Date > soldAtDate)
            .Concat(leftoverTransactions)
            .OrderBy(t => t.Date)
            .ToList();

        Console.WriteLine();
        Console.WriteLine();
        table = new ConsoleTable("New stocks", "Amount", "Date");

        foreach (var newStock in newStocks)
        {
            table.AddRow(newStock.Name, newStock.Amount, newStock.Date);
        }

        table.Write(Format.Minimal);

        var newGain = 0M;
        var stocksToSell = newStocks
            .TakeWhile(t =>
            {
                newGain = t switch
                {
                    DepositTransaction d => d.Amount * (currentPrice - d.Price),
                    ESPPTransaction e => e.Amount * (currentPrice - e.MarketPrice),
                    _ => throw new ArgumentOutOfRangeException(nameof(t))
                };

                return newGain < loss;
            })
            .ToList();

        Console.WriteLine($"Can sell: {stocksToSell.Sum(t => t.Amount)} stocks");
        Console.WriteLine($"Sell gain: {newGain}");
    }
}
