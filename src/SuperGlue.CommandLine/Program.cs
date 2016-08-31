using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Fclp;

namespace SuperGlue
{
    public class Program
    {
        private static readonly IDictionary<string, Func<FluentCommandLineParser, string[], ICommand>> CommandBuilders = new Dictionary
            <string, Func<FluentCommandLineParser, string[], ICommand>>
        {
            {"run", BuildRunCommand},
            {"new", BuildNewCommand},
            {"add", BuildAddCommand},
            {"alter", BuildAlterCommand}
        };

        public static void Main(string[] args)
        {
            var parser = new FluentCommandLineParser();

            var commandName = args.FirstOrDefault() ?? "";

            if (!CommandBuilders.ContainsKey(commandName))
            {
                Console.WriteLine(
                    $"{commandName} isn't a valid command. Available commands: {string.Join(", ", CommandBuilders.Select(x => x.Key))}.");
                return;
            }

            var commandArgs = args.Skip(1).ToArray();

            var command = CommandBuilders[commandName](parser, commandArgs);

            command.Execute().Wait();
        }

        private static NewCommand BuildNewCommand(FluentCommandLineParser parser, string[] args)
        {
            var command = new NewCommand();

            parser
                .Setup<string>('n', "name")
                .Callback(x => command.Name = x)
                .Required();

            parser
                .Setup<string>('t', "template")
                .Callback(x => command.Template = x)
                .Required();

            parser
                .Setup<string>('l', "location")
                .Callback(x => command.Location = x)
                .SetDefault(Environment.CurrentDirectory);

            parser
                .Setup<string>('p', "templatepaths")
                .Callback(x => command.TemplatePaths.AddRange((x ?? "").Split(';').Where(y => !string.IsNullOrEmpty(y))));

            parser
                .Setup<string>('g', "guid")
                .Callback(x => command.ProjectGuid = x)
                .SetDefault(Guid.NewGuid().ToString());

            parser
                .Setup<string>('o', "output")
                .Callback(x => command.LogTo = x);

            parser.Parse(args);

            command.TemplatePaths.Add(Path.Combine(command.Location, "Templates"));
            command.TemplatePaths.Add(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Templates"));

            return command;
        }

        private static AddCommand BuildAddCommand(FluentCommandLineParser parser, string[] args)
        {
            var command = new AddCommand();

            parser
                .Setup<string>('n', "name")
                .Callback(x => command.Name = x)
                .Required();

            parser
                .Setup<string>('s', "solution")
                .Callback(x => command.Solution = x);

            parser
                .Setup<string>('t', "template")
                .Callback(x => command.Template = x)
                .Required();

            parser
                .Setup<string>('l', "location")
                .Callback(x => command.Location = x)
                .SetDefault(Environment.CurrentDirectory);

            parser
                .Setup<string>('p', "templatepaths")
                .Callback(x => command.TemplatePaths.AddRange((x ?? "").Split(';').Where(y => !string.IsNullOrEmpty(y))));

            parser
                .Setup<string>('g', "guid")
                .Callback(x => command.ProjectGuid = x)
                .SetDefault(Guid.NewGuid().ToString());

            parser
                .Setup<string>('o', "output")
                .Callback(x => command.LogTo = x);

            parser.Parse(args);

            command.TemplatePaths.Add(Path.Combine(command.Location, "Templates"));
            command.TemplatePaths.Add(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Templates"));

            return command;
        }

        private static AlterCommand BuildAlterCommand(FluentCommandLineParser parser, string[] args)
        {
            var command = new AlterCommand();

            parser
                .Setup<string>('n', "name")
                .Callback(x => command.Name = x)
                .Required();

            parser
                .Setup<string>('s', "solution")
                .Callback(x => command.Solution = x);

            parser
                .Setup<string>('t', "template")
                .Callback(x => command.Template = x)
                .Required();

            parser
                .Setup<string>('l', "location")
                .Callback(x => command.Location = x)
                .SetDefault(Environment.CurrentDirectory);

            parser
                .Setup<string>('p', "templatepaths")
                .Callback(x => command.TemplatePaths.AddRange((x ?? "").Split(';').Where(y => !string.IsNullOrEmpty(y))));

            parser
                .Setup<string>('o', "output")
                .Callback(x => command.LogTo = x);

            parser.Parse(args);

            command.TemplatePaths.Add(Path.Combine(command.Location, "Templates"));
            command.TemplatePaths.Add(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Templates"));

            return command;
        }

        private static RunCommand BuildRunCommand(FluentCommandLineParser parser, string[] args)
        {
            var command = new RunCommand();

            parser
                .Setup<string>('a', "application")
                .Callback(x => command.Application = x)
                .SetDefault(Environment.CurrentDirectory);

            parser
                .Setup<string>('c', "config")
                .Callback(x => command.ConfigFile = x);

            parser
                .Setup<string>('e', "environment")
                .Callback(x => command.Environment = x)
                .SetDefault("local");

            parser
                .Setup<string>('h', "hosts")
                .Callback(x => command.Hosts = x.Split(',').Where(y => !string.IsNullOrWhiteSpace(y)).ToList());

            parser
                .Setup<string>('i', "ignore")
                .Callback(x => command.IgnoredPaths = x.Split(',').Where(y => !string.IsNullOrWhiteSpace(y)).ToList());

            parser.Parse(args);

            return command;
        }
    }
}