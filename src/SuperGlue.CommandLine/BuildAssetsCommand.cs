using System.IO;
using System.Threading.Tasks;
using SuperGlue.Web.Assets;

namespace SuperGlue
{
    public class BuildAssetsCommand : ICommand
    {
        public string AppPath { get; set; }
        public string Destination { get; set; }

        public Task Execute()
        {
            //HACK:Hard coded path to assets
            var settings = new AssetSettings()
                .SetSetupEnabled(true)
                .UseDestination(Destination)
                .AddSource(Path.Combine(AppPath, "assets"), 1);

            return Assets.CollectAllAssets(settings);
        }
    }
}