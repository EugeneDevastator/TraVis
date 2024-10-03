using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Universal Tree Navigator");
        MainLoop();
    }

    static async void MainLoop()
    {
        var navigator = new TreeNavigator();
        var view = new TreeView();

        while (true)
        {
            view.Display(await navigator.GetCurrentModel());
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
    Task<TreeModel> GetCurrentModel();
    Task<TreeModel> GoRelative(string path);
    string TreeType { get; }

    Task SetRoot(string path);
}

public class TreeNavigator
{
    private Dictionary<string, ITreeEngine> treeEngines;
    private ITreeEngine currentEngine;

    public TreeNavigator()
    {
        var diskTree = new DiskTreeEngine();
        var rootTree = new ConcatTrees(diskTree, new WindowsTreeEngine());
        treeEngines = new Dictionary<string, ITreeEngine>
        {
            { "root", rootTree },
            { "FileSystem", new FileSystemTreeEngine() },
            { "Disk", diskTree },
         //   { "FileSystem", new FileSystemTreeEngine() }
          //  { "Windows", new WindowsTreeEngine() }
        };
        currentEngine = treeEngines["root"];
    }

    public async Task<TreeModel> GetCurrentModel() => await currentEngine.GetCurrentModel();

    public async Task Navigate(string path)
    {
        var result = await currentEngine.GoRelative(path);

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
        return currentTreeType == "FileSystem" ? "Disk" : "root";
    }

    private string GetChildTreeType(string currentTreeType, string path)
    {
        return currentTreeType == "Disk" ? "FileSystem" : null;
    }
}

public class FileSystemTreeEngine : ITreeEngine
{
    private string currentPath = "C:\\";

    public string TreeType => "FileSystem";

    public async Task<TreeModel> GetCurrentModel()
    {
        return new TreeModel
        {
            NodeName = currentPath,
            Children = Directory.GetFileSystemEntries(currentPath).Select(Path.GetFileName).ToArray(),
            NavResult = TreeNavResult.Inside,
            TreeType = TreeType
        };
    }

    public async Task<TreeModel> GoRelative(string path)
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
            return await GetCurrentModel();
        }

        // If path doesn't exist, stay at the current location
        return await GetCurrentModel();
    }

    public async Task SetRoot(string path)
    {
        currentPath = path;
    }
}


public class WindowsTreeEngine : ITreeEngine
{
    public string TreeType => "Windows";

    public async Task<TreeModel> GetCurrentModel()
    {
        var processes = Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
            .Select(p => p.MainWindowTitle)
            .ToArray();

        return new TreeModel
        {
            NodeName = "Open Windows",
            Children = processes,
            NavResult = TreeNavResult.Inside,
            TreeType = TreeType
        };
    }

    public async Task<TreeModel> GoRelative(string path)
    {
        if (path == "..")
        {
            return new TreeModel
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

public class ConcatTrees : ITreeEngine
{
    private List<ITreeEngine> trees;
    private int currentTreeIndex;

    public string TreeType => "ConcatTrees";

    public ConcatTrees(params ITreeEngine[] treesToConcat)
    {
        trees = new List<ITreeEngine>(treesToConcat);
        currentTreeIndex = -1;
    }

    public async Task<TreeModel> GetCurrentModel()
    {
        if (currentTreeIndex == -1)
        {
            // We're at the root level, show all tree roots
            var rootChildren = new List<string>();
            for (int i = 0; i < trees.Count; i++)
            {
                var treeModel = await trees[i].GetCurrentModel();
                rootChildren.Add($"{i}:{treeModel.NodeName}");
            }

            return new TreeModel
            {
                NodeName = "ConcatTrees Root",
                Children = rootChildren.ToArray(),
                NavResult = TreeNavResult.Inside,
                TreeType = TreeType
            };
        }
        else
        {
            // We're inside a specific tree
            return await trees[currentTreeIndex].GetCurrentModel();
        }
    }

    public async Task<TreeModel> GoRelative(string path)
    {
        if (currentTreeIndex == -1)
        {
            // We're at the root level
            if (path == "..")
            {
                return new TreeModel
                {
                    NodeName = "ConcatTrees Root",
                    Children = new string[0],
                    NavResult = TreeNavResult.RootParent,
                    TreeType = TreeType
                };
            }
            else if (int.TryParse(path.Split(':')[0], out int index) && index >= 0 && index < trees.Count)
            {
                currentTreeIndex = index;
                return new TreeModel
                {
                    NodeName = (await trees[currentTreeIndex].GetCurrentModel()).TreeType,
                    Children = new string[0],
                    NavResult = TreeNavResult.EndChild,
                    TreeType = TreeType
                };
                
            }
        }
        else
        {
            // We're inside a specific tree
            var result = await trees[currentTreeIndex].GoRelative(path);
            if (result.NavResult == TreeNavResult.RootParent)
            {
                currentTreeIndex = -1;
                return await GetCurrentModel();
            }
            return result;
        }

        // If the path is invalid, stay at the current location
        return await GetCurrentModel();
    }

    public async Task SetRoot(string path)
    {
        // Not applicable for ConcatTrees
    }
}
