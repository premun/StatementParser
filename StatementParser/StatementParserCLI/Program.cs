using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Commander.NET;
using Commander.NET.Exceptions;
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

        decimal soldAt = 256.38M;
        int left = 13;
        decimal counter = 0;

        DateTime soldAtDate = new DateTime(2023, 3, 6);
        DateTime eligibleDate = new DateTime(2020, 3, 6);

        var eligibleTransactions = transactions
            .Where(t => t is DepositTransaction or ESPPTransaction)
            .Where(t => t.Date >= eligibleDate && t.Date < soldAtDate)
            .ToList();

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

        decimal gain = taxableTransactions
            .Select(t =>
            {
                (decimal amount, decimal price) = t switch
                {
                    DepositTransaction d => (d.Amount, d.Price),
                    ESPPTransaction e => (e.Amount, e.MarketPrice),
                    _ => throw new Exception("Unknown transaction type")
                };

                var gain = amount * (soldAt - price);
                Console.WriteLine($"{t.Date} - {amount} - {price} - {gain}");
                return gain;
            })
            .Sum();

        var newStocks = transactions
            .Where(t => t is DepositTransaction or ESPPTransaction)
            .Where(t => t.Date > soldAtDate)
            .ToList();


        Console.WriteLine($"Gain: {gain} USD");

        //var printer = new Output();
        //if (option.ShouldPrintAsJson)
        //{
        //	printer.PrintAsJson(transactions);
        //}
        //else if (option.ExcelSheetPath != null)
        //{
        //	printer.SaveAsExcelSheet(option.ExcelSheetPath, transactions);
        //}
        //else
        //{
        //	printer.PrintAsPlainText(transactions);
        //}
    }

    private static async Task<IList<Transaction>> ParseFile(TransactionParser parser, string file)
    {
        return await parser.ParseAsync(file);
    }
}
