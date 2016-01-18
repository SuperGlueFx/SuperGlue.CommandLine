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
        private static readonly IDictionary<string, Func<FluentCommandLineParser, string[], ICommand>> CommandBuilders = new Dictionary<string, Func<FluentCommandLineParser, string[], ICommand>>
        {
            {"buildassets", BuildBuildAssetsCommand},
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
                Console.WriteLine("{0} isn't a valid command. Available commands: {1}.", commandName, string.Join(", ", CommandBuilders.Select(x => x.Key)));
                return;
            }

            var commandArgs = args.Skip(1).ToArray();

            var command = CommandBuilders[commandName](parser, commandArgs);

            command.Execute().Wait();
        }

        private static BuildAssetsCommand BuildBuildAssetsCommand(FluentCommandLineParser parser, string[] args)
        {
            var command = new BuildAssetsCommand();

            parser
                .Setup<string>('p', "path")
                .Callback(x => command.AppPath = Path.IsPathRooted(x) ? x : Path.Combine(Environment.CurrentDirectory, x))
                .SetDefault(Environment.CurrentDirectory);

            parser
                .Setup<string>('d', "destination")
                .Callback(x => command.Destination = Path.IsPathRooted(x) ? x : Path.Combine(Environment.CurrentDirectory, x))
                .SetDefault("/_assets");

            parser.Parse(args);

            return command;
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
                .Setup<string>('p', "templatepath")
                .Callback(x => command.TemplatePaths.Add(x));

            parser
                .Setup<string>('g', "guid")
                .Callback(x => command.ProjectGuid = x)
                .SetDefault(Guid.NewGuid().ToString());

            parser.Parse(args);

            command.TemplatePaths.Add(Path.Combine(command.Location, "Templates"));
            command.TemplatePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Templates"));

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
                .Setup<string>('p', "templatepath")
                .Callback(x => command.TemplatePaths.Add(x));

            parser
                .Setup<string>('g', "guid")
                .Callback(x => command.ProjectGuid = x)
                .SetDefault(Guid.NewGuid().ToString());

            parser.Parse(args);

            command.TemplatePaths.Add(Path.Combine(command.Location, "Templates"));
            command.TemplatePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Templates"));

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
                .Setup<string>('p', "templatepath")
                .Callback(x => command.TemplatePaths.Add(x));

            parser.Parse(args);

            command.TemplatePaths.Add(Path.Combine(command.Location, "Templates"));
            command.TemplatePaths.Add(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Templates"));

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
                .Setup<string>('n', "nodes")
                .Callback(x => command.NodeTypes = x.Split(',').Where(y => !string.IsNullOrWhiteSpace(y)).ToList())
                .SetDefault("");

            parser.Parse(args);

            return command;
        }
    }
}
