using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JaJo.Projects.Templating;

namespace SuperGlue
{
    public class AddCommand : ICommand
    {
        public string Name { get; set; }
        public string Solution { get; set; }
        public string Template { get; set; }
        public List<string> TemplatePaths { get; set; }
        public string Location { get; set; }
        public string ProjectGuid { get; set; }
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

            var projectDirectory = TemplatePaths
                .Select(x => Path.Combine(x, $"projects\\{Template}"))
                .FirstOrDefault(Directory.Exists);

            if (string.IsNullOrEmpty(projectDirectory))
            {
                projectDirectory = TemplatePaths
                    .Select(x => Path.Combine(x, "projects\\base"))
                    .FirstOrDefault(Directory.Exists);
            }

            var substitutions = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"PROJECT_NAME", Name},
                {"SOLUTION", Solution},
                {"PROJECT_GUID", ProjectGuid}
            });

            if (!string.IsNullOrEmpty(projectDirectory))
            {
                await
                    engine.RunTemplate(
                        new ProjectTemplateType(Name, Solution, Location, Path.Combine(Location, $"src\\{Name}"),
                            ProjectGuid, substitutions), projectDirectory).ConfigureAwait(false);

                await new AlterCommand
                {
                    Name = Name,
                    Solution = Solution,
                    Location = Location,
                    TemplatePaths = TemplatePaths,
                    Template = Template,
                    LogTo = LogTo
                }.Execute().ConfigureAwait(false);
            }
        }
    }
}