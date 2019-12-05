using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Grail.Properties;

using JetBrains.Annotations;


namespace Grail
{
    [DebuggerDisplay("{Regex.Path,nq}, {Hits,nq}")]
    public class Expression
    {
        private const RegexOptions Options = RegexOptions.Singleline | RegexOptions.IgnoreCase;

        private const string PathExpression = @"[\w\\""-_\.~]*";

        private const string PathExpressionWithSpace = @"[\w\\-_\.~ ]*";

        public Expression([RegexPattern] string pattern)
        {
            pattern = pattern.Replace("{PathExpression}", PathExpression);

            pattern = pattern.Replace("{PathExpressionWithSpace}", PathExpressionWithSpace);

            Regex = new Regex(pattern, Options);
        }

        public Regex Regex { get; }

        public int Hits { get; private set; }

        public string Value { get; private set; }

        public bool IsMatch(string input)
        {
            var match = Regex.Match(input);

            var result = match.Success;

            if (result)
            {
                Hits++;

                Value = match.Groups["Value"].Value;
            }

            return result;
        }

        public string Replace(string input, string newValue)
        {
            return Regex.Replace(input, newValue);
        }

        public static IEnumerable<Expression> LoadPatterns(string filePath)
        {
            //Directory.GetCurrentDirectory()
            //var folder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            var folder = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(folder, filePath.StripLeadingDirectorySeparator());

            if (!File.Exists(path))
            {
                Console.WriteLine($"Cannot load patterns from {path}\n", ConsoleColor.Red);
                return new List<Expression>();
            }

            //Write($"Using {filePath.Replace(".txt", "")} patterns from {path}\n", ConsoleColor.Green);

            return File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#") && !l.StartsWith("//"))
                .Select(l => new Expression(l)).ToList();
        }


    }
}