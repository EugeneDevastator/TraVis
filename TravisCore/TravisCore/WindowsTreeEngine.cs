using System.Diagnostics;

public class WindowsTreeEngine : ITreeEngine
{
    public string TreeType => "Windows";

    private async Task<TreeNavNormal> GetCurrentModel()
    {
        var processes = Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
            .Select(p => p.MainWindowTitle)
            .ToArray();

        return new TreeNavNormal
        {
            NodeName = "Open Windows",
            Children = processes,
            NavResult = TreeNavResult.Inside,
            TreeType = TreeType
        };
    }

    public async Task<TreeNavNormal> GoRelative(string path)
    {
        if (path == "..")
        {
            return new TreeNavNormal
            {
                NodeName = "Open Windows",
                Children = new string[0],
                NavResult = TreeNavResult.RootParent,
                TreeType = TreeType
            };
        }
        else
        {
            return await GetCurrentModel();
        }
    }

    public async Task SetRoot(string path)
    {
        // Not applicable for Windows tree
    }
}
