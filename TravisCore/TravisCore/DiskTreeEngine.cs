public class DiskTreeEngine : ITreeEngine
{
    public string TreeType => "Disk";

    public async Task<TreeNavNormal> GoRelative(string path)
    {
        if (path == "..")
        {
            return new TreeNavNormal
            {
                NodeName = "Disks",
                Children = DriveInfo.GetDrives().Select(d => d.Name.TrimEnd('\\')).ToArray(),
                NavResult = TreeNavResult.RootParent,
                TreeType = TreeType
            };
        }
        else if (DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\') == path))
        {
            return new TreeNavNormal
            {
                NodeName = path,
                Children = new string[0],
                NavResult = TreeNavResult.EndChild,
                TreeType = TreeType
            };
        }
        else
        {
            return new TreeNavNormal
            {
                NodeName = "Disks",
                Children = DriveInfo.GetDrives().Select(d => d.Name.TrimEnd('\\')).ToArray(),
                NavResult = TreeNavResult.Inside,
                TreeType = TreeType
            };
        }
    }

    public async Task SetRoot(string path)
    {
    }
}
