public class FileSystemTreeEngine : ITreeEngine
{
    private string currentPath = "C:\\";

    public string TreeType => "FileSystem";

    public async Task<TreeNavNormal> GetCurrentModel()
    {
        return new TreeNavNormal
        {
            NodeName = currentPath,
            Children = Directory.GetFileSystemEntries(currentPath).Select(Path.GetFileName).ToArray(),
            NavResult = TreeNavResult.Inside,
            TreeType = TreeType
        };
    }

    public async Task<TreeNavNormal> GoRelative(string path)
    {
        if (path == ".." && Path.GetPathRoot(currentPath) == currentPath)
        {
            return new TreeNavNormal
            {
                NodeName = currentPath,
                Children = Directory.GetFileSystemEntries(currentPath).Select(Path.GetFileName).ToArray(),
                NavResult = TreeNavResult.RootParent,
                TreeType = TreeType
            };
        }

        string newPath = Path.GetFullPath(Path.Combine(currentPath, path));

        if (File.Exists(newPath))
        {
            return new TreeNavNormal
            {
                NodeName = newPath,
                Children = new string[0],
                NavResult = TreeNavResult.EndChild,
                TreeType = TreeType
            };
        }

        if (Directory.Exists(newPath))
        {
            currentPath = newPath;
            return new TreeNavNormal
            {
                NodeName = currentPath,
                Children = Directory.GetFileSystemEntries(currentPath).Select(Path.GetFileName).ToArray(),
                NavResult = TreeNavResult.Inside,
                TreeType = TreeType
            };
        }

        // If path doesn't exist, stay at the current location
        return await GetCurrentModel();
    }

    public async Task SetRoot(string path)
    {
        currentPath = path;
    }
}
