using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Universal Tree Navigator");
        var navigator = new TreeNavigator();
        var view = new TreeView();

        while (true)
        {
            view.Display(navigator.CurrentModel);
            Console.WriteLine("\nEnter command (cd <name>, cd .., exit):");
            var input = Console.ReadLine();

            if (input.ToLower() == "exit")
                break;

            if (input.StartsWith("cd ", StringComparison.OrdinalIgnoreCase))
            {
                var path = input.Substring(3);
                navigator.Navigate(path);
            }
        }
    }
}

public class TreeView
{
    public void Display(TreeModel model)
    {
        Console.WriteLine($"Current: {model.NodeName}");
        Console.WriteLine("Contents:");
        foreach (var item in model.Children)
        {
            Console.WriteLine($"  {item}");
        }
    }
}

public enum TreeNavResult
{
    Inside,
    RootParent,
    EndChild
}

public class TreeModel
{
    public string NodeName { get; init; }
    public string[] Children { get; init; }
    public TreeNavResult NavResult { get; init; }
    public string TreeType { get; init; }
}

public interface ITreeEngine
{
    TreeModel GetCurrentModel();
    TreeModel GoRelative(string path);
    string TreeType { get; }

    void SetRoot(string path);
}

public class TreeNavigator
{
    private Dictionary<string, ITreeEngine> treeEngines;
    private ITreeEngine currentEngine;

    public TreeNavigator()
    {
        treeEngines = new Dictionary<string, ITreeEngine>
        {
            { "Disk", new DiskTreeEngine() },
            { "FileSystem", new FileSystemTreeEngine() }
        };
        currentEngine = treeEngines["Disk"];
    }

    public TreeModel CurrentModel => currentEngine.GetCurrentModel();

    public void Navigate(string path)
    {
        var result = currentEngine.GoRelative(path);

        switch (result.NavResult)
        {
            case TreeNavResult.RootParent:
                var parentTreeType = GetParentTreeType(currentEngine.TreeType);
                if (parentTreeType != null)
                {
                    currentEngine = treeEngines[parentTreeType];
                }
                break;

            case TreeNavResult.EndChild:
                var childTreeType = GetChildTreeType(currentEngine.TreeType, path);
                if (childTreeType != null)
                {
                    currentEngine = treeEngines[childTreeType];
                    currentEngine.SetRoot(path+"\\");
                }
                break;

            case TreeNavResult.Inside:
                // Stay in the current tree
                break;
        }
    }

    private string GetParentTreeType(string currentTreeType)
    {
        return currentTreeType == "FileSystem" ? "Disk" : null;
    }

    private string GetChildTreeType(string currentTreeType, string path)
    {
        return currentTreeType == "Disk" ? "FileSystem" : null;
    }
}

public class DiskTreeEngine : ITreeEngine
{
    public string TreeType => "Disk";

    public TreeModel GetCurrentModel()
    {
        return new TreeModel
        {
            NodeName = "Disks",
            Children = DriveInfo.GetDrives().Select(d => d.Name.TrimEnd('\\')).ToArray(),
            NavResult = TreeNavResult.Inside,
            TreeType = TreeType
        };
    }

    public TreeModel GoRelative(string path)
    {
        if (path == "..")
        {
            return new TreeModel
            {
                NodeName = "Disks",
                Children = DriveInfo.GetDrives().Select(d => d.Name.TrimEnd('\\')).ToArray(),
                NavResult = TreeNavResult.RootParent,
                TreeType = TreeType
            };
        }
        else if (DriveInfo.GetDrives().Any(d => d.Name.TrimEnd('\\') == path))
        {
            return new TreeModel
            {
                NodeName = path,
                Children = new string[0],
                NavResult = TreeNavResult.EndChild,
                TreeType = TreeType
            };
        }
        else
        {
            return GetCurrentModel();
        }
    }

    public void SetRoot(string path)
    {
    }
}

public class FileSystemTreeEngine : ITreeEngine
{
    private string currentPath = "C:\\";

    public string TreeType => "FileSystem";

    public TreeModel GetCurrentModel()
    {
        return new TreeModel
        {
            NodeName = currentPath,
            Children = Directory.GetFileSystemEntries(currentPath).Select(Path.GetFileName).ToArray(),
            NavResult = TreeNavResult.Inside,
            TreeType = TreeType
        };
    }

    public TreeModel GoRelative(string path)
    {
        if (path == ".." && Path.GetPathRoot(currentPath) == currentPath)
        {
            return new TreeModel
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
            return new TreeModel
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
            return GetCurrentModel();
        }

        // If path doesn't exist, stay at the current location
        return GetCurrentModel();
    }

    public void SetRoot(string path)
    {
        currentPath = path;
    }
}