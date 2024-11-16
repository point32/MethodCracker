using Microsoft.Build.Framework;

namespace MethodCracker.Tasks;

class EditAssemblyTask : ITask
{
    public IBuildEngine BuildEngine { get; set; } = null!;
    public ITaskHost HostObject { get; set; }

    public bool Execute()
    {
        return true;
    }
}
