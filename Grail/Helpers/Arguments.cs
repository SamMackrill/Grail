using System;
using System.Text;

namespace Grail
{
    /// <summary>
    /// Arguments class nicked from http://jake.ginnivan.net/c-sharp-argument-parser/
    /// </summary>
    public static class Arguments
    {
        /// <summary>
        /// Splits the command line. When main(string[] args) is used escaped quotes (ie a path "c:\folder\")
        /// Will consume all the following command line arguments as the one argument. 
        /// This function ignores escaped quotes making handling paths much easier.
        /// </summary>
        /// <param name="commandLine">The command line.</param>
        /// <returns></returns>
        public static string[] SplitCommandLine(string commandLine)
        {
            var translatedArguments = new StringBuilder(commandLine);
            var escaped = false;
            for (var i = 0; i < translatedArguments.Length; i++)
            {
                switch (translatedArguments[i])
                {
                    case '"':
                        escaped = !escaped;
                        break;
                    case ' ' when !escaped:
                        translatedArguments[i] = '\n';
                        break;
                }
            }

            var toReturn = translatedArguments.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < toReturn.Length; i++)
            {
                toReturn[i] = RemoveMatchingQuotes(toReturn[i]);
            }
            return toReturn;
        }

        private static string RemoveMatchingQuotes(string stringToTrim)
        {
            var firstQuoteIndex = stringToTrim.IndexOf('"');
            var lastQuoteIndex = stringToTrim.LastIndexOf('"');
            while (firstQuoteIndex != lastQuoteIndex)
            {
                stringToTrim = stringToTrim.Remove(firstQuoteIndex, 1);
                stringToTrim = stringToTrim.Remove(lastQuoteIndex - 1, 1); //-1 because we've shifted the indicies left by one
                firstQuoteIndex = stringToTrim.IndexOf('"');
                lastQuoteIndex = stringToTrim.LastIndexOf('"');
            }

            return stringToTrim;
        }
    }
}
