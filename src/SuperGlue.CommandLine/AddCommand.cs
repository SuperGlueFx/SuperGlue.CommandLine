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
        public ICollection<string> TemplatePaths { get; set; }
        public string Location { get; set; }
        public string ProjectGuid { get; set; }

        public async Task Execute()
        {
            var engine = TemplatingEngine.Init();

            var projectDirectory = TemplatePaths
                .Select(x => Path.Combine(x, $"projects\\{Template}"))
                .FirstOrDefault(Directory.Exists);

            var substitutions = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"PROJECT_NAME", Name},
                {"SOLUTION", Solution},
                {"PROJECT_GUID", ProjectGuid}
            });

            if (!string.IsNullOrEmpty(projectDirectory))
            {
                await engine.RunTemplate(new ProjectTemplateType(Name, Solution, Location, Path.Combine(Location, $"src\\{Name}"), ProjectGuid, substitutions), projectDirectory);

                await new AlterCommand
                {
                    Name = Name,
                    Solution = Solution,
                    Location = Location,
                    TemplatePaths = TemplatePaths,
                    Template = Template
                }.Execute();
            }
        }
    }

    public class AlterCommand : ICommand
    {
        public string Name { get; set; }
        public string Solution { get; set; }
        public string Template { get; set; }
        public ICollection<string> TemplatePaths { get; set; }
        public string Location { get; set; }

        public async Task Execute()
        {
            var engine = TemplatingEngine.Init();

            var alterationDirectories = TemplatePaths
                    .Select(x => Path.Combine(x, $"alterations\\{Template}"))
                    .Where(Directory.Exists)
                    .ToList();

            var substitutions = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
            {
                {"PROJECT_NAME", Name},
                {"SOLUTION", Solution}
            });

            foreach (var alterationDirectory in alterationDirectories)
                await engine.RunTemplate(new AlterationTemplateType(Name, Location, Path.Combine($"src\\{Name}"), substitutions), alterationDirectory);
        }
    }
}