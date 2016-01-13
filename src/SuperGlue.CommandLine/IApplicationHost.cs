using System.Threading.Tasks;

namespace SuperGlue
{
    public interface IApplicationHost
    {
        Task Prepare(string applicationPath);
        Task TearDown(string applicationPath);
    }
}