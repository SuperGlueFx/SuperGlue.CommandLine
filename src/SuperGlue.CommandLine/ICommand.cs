using System.Threading.Tasks;

namespace SuperGlue
{
    public interface ICommand
    {
        Task Execute();
    }
}