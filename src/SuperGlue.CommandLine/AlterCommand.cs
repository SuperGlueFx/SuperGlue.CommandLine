using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JaJo.Projects.Templating;

namespace SuperGlue
{
    public class AlterCommand : ICommand
    {
        public string Name { get; set; }
        public string Solution { get; set; }
        public string Template { get; set; }
        public List<string> TemplatePaths { get; set; }
        public string Location { get; set; }
        public string LogTo { get; set; }

        public async Task Execute()
        {
            var loggers = new List<ILog>
            {
                new ConsoleLog()
            };

            if (!string.IsNullOrEmpty(LogTo))
                loggers.Add(new FileLog(LogTo));

            var engine = TemplatingEngine.Init(loggers.ToArray());

            var alterationDirectories = TemplatePaths
                .Select(x => Path.Combine(x, "alterations\\base"))
                .Where(Directory.Exists)
                .ToList();

            alterationDirectories.AddRange(TemplatePaths
                .Select(x => Path.Combine(x, $"alterations\\{Template}"))
                .Where(Directory.Exists));

            var substitutions = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"PROJECT_NAME", Name},
                {"SOLUTION", Solution}
            });

            foreach (var alterationDirectory in alterationDirectories)
                await
                    engine.RunTemplate(
                        new AlterationTemplateType(Name, Location, Path.Combine(Location, $"src\\{Name}"), substitutions),
                        alterationDirectory).ConfigureAwait(false);
        }
    }
}