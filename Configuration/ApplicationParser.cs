using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;

namespace QuantConnect.Configuration
{
    /// <summary>
    /// Command Line application parser
    /// </summary>
    public static class ApplicationParser
    {
        /// <summary>
        /// This function will parse args based on options and will show application name, version, help
        /// </summary>
        /// <param name="applicationName">The application name to be shown</param>
        /// <param name="applicationDescription">The application description to be shown</param>
        /// <param name="applicationHelpText">The application help text</param>
        /// <param name="args">The command line arguments</param>
        /// <param name="options">The applications command line available options</param>
        /// <param name="noArgsShowHelp">To show help when no command line arguments were provided</param>
        /// <returns>The user provided options. Key is option name</returns>
        public static Dictionary<string, object> Parse(string applicationName, string applicationDescription, string applicationHelpText,
                                                       string[] args, List<CommandLineOption> options, bool noArgsShowHelp = false)
        {
            var application = new CommandLineApplication
            {
                Name = applicationName,
                Description = applicationDescription,
                ExtendedHelpText = applicationHelpText
            };

            application.HelpOption("-?|-h|--help");

            // This is a helper/shortcut method to display version info - it is creating a regular Option, with some defaults.
            // The default help text is "Show version Information"
            application.VersionOption("-v|-V|--version",
                () =>
                    $"Version {Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");

            var optionsObject = new Dictionary<string, object>();

            var listOfOptions = new List<CommandOption>();

            foreach (var option in options)
            {
                listOfOptions.Add(application.Option($"--{option.Name}", option.Description, option.Type));
            }

            application.OnExecute(() =>
            {
                foreach (var commandOption in listOfOptions.Where(option => option.HasValue()))
                {
                    var optionKey = commandOption.Template.Replace("--", "");
                    var matchingOption = options.Find(o => o.Name == optionKey);
                    switch (matchingOption.Type)
                    {
                        // Booleans
                        case CommandOptionType.NoValue:
                            optionsObject[optionKey] = true;
                            break;

                        // Strings and numbers
                        case CommandOptionType.SingleValue:
                            optionsObject[optionKey] = commandOption.Value();
                            break;

                        // Parsing nested objects
                        case CommandOptionType.MultipleValue:
                            var keyValuePairs = commandOption.Value().Split(',');
                            var subDictionary = new Dictionary<string, string>();
                            foreach (var keyValuePair in keyValuePairs)
                            {
                                var subKeys = keyValuePair.Split(':');
                                subDictionary[subKeys[0]] = subKeys.Length > 1 ? subKeys[1] : "";
                            }

                            optionsObject[optionKey] = subDictionary;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                return 0;
            });

            application.Execute(args);
            if (noArgsShowHelp && args.Length == 0)
            {
                application.ShowHelp();
            }
            return optionsObject;
        }

        /// <summary>
        /// Prints a message advising the user to use the --help parameter for more information
        /// </summary>
        public static void PrintMessageAndExit(int exitCode = 0, string message = "")
        {
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine("\n" + message);
            }
            Console.WriteLine("\nUse the '--help' parameter for more information");
            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Gets the parameter object from the given parameter (if it exists)
        /// </summary>
        public static string GetParameterOrExit(IReadOnlyDictionary<string, object> optionsObject, string parameter)
        {
            if (!optionsObject.ContainsKey(parameter))
            {
                PrintMessageAndExit(1, "ERROR: REQUIRED parameter --" + parameter + "= is missing");
            }
            return optionsObject[parameter].ToString();
        }

        /// <summary>
        /// Gets the parameter object from the given parameter. If it does not exists, it returns a default parameter object
        /// </summary>
        public static string GetParameterOrDefault(IReadOnlyDictionary<string, object> optionsObject, string parameter, string defaultValue)
        {
            object value;
            if (!optionsObject.TryGetValue(parameter, out value))
            {
                Console.WriteLine($"'{parameter}' was not specified. Using default value: '{defaultValue}'");
                return defaultValue;
            }

            return value.ToString();
        }
    }
}
