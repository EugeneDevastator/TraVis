using System;
using System.Collections.Generic;
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
    public void Display(TreeNavNormal model)
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

public interface ITreeNavResult { };

public struct TreeNavOutside : ITreeNavResult
{
    public bool IsRoot => String.IsNullOrEmpty(ForwardArgument);
    public string ForwardArgument { get; init; }

    public string RequestedTreeEngine { get; init; }
}

public struct TreeNavNormal : ITreeNavResult
{
    public string NodeName { get; init; }
    public string[] Children { get; init; }
    public TreeNavResult NavResult { get; init; }
    public string TreeType { get; init; }
}

public interface ITreeEngine
{

    Task<TreeNavNormal> GoRelative(string path = "");
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
        var windows = new WindowsTreeEngine();
        var rootTree = new ConcatTrees(diskTree, windows);
        treeEngines = new Dictionary<string, ITreeEngine>
        {
            { "root", rootTree },
            { "FileSystem", new FileSystemTreeEngine() },
            { "Disk", diskTree },
            { "Windows", windows },
         //   { "FileSystem", new FileSystemTreeEngine() }
          //  { "Windows", new WindowsTreeEngine() }
        };
        currentEngine = treeEngines["root"];
    }

    public async Task<TreeNavNormal> GetCurrentModel() => await currentEngine.GoRelative();

    public async Task Navigate(string path)
    {
        var result = await currentEngine.GoRelative(path);

        switch (result.NavResult)
        {
            case TreeNavResult.RootParent:
                var parentTreeType = GetParentTreeType(currentEngine.TreeType);
                if (parentTreeType != null)
                {
                    var returnpath = result.NodeName;
                    currentEngine = treeEngines[parentTreeType];
                    currentEngine.SetRoot(path + "\\");
                }
                break;

            case TreeNavResult.EndChild:
                // internal resolve
                var childTreeType = GetChildTreeType(currentEngine.TreeType, path);
                if (childTreeType != null)
                {
                    currentEngine = treeEngines[childTreeType];
                    currentEngine.SetRoot(path + "\\");
                }
                // on demand resolve
                else if (treeEngines.TryGetValue(result.TreeType, out var t))
                {
                    currentEngine = t;
                    currentEngine.SetRoot(result.NodeName);
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

    private async Task<TreeNavNormal> GetCurrentModel()
    {
        if (currentTreeIndex == -1)
        {
            // We're at the root level, show all tree roots
            var rootChildren = new List<string>();
            for (int i = 0; i < trees.Count; i++)
            {
                rootChildren.Add($"{i}:{trees[i].TreeType}");
            }

            return new TreeNavNormal
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
            return new TreeNavNormal
            {
                NodeName = "ConcatTrees Root",
                NavResult = TreeNavResult.EndChild,
                TreeType = trees[currentTreeIndex].TreeType
            };
        }
    }

    public async Task<TreeNavNormal> GoRelative(string path)
    {
        if (currentTreeIndex == -1)
        {
            // We're at the root level
            if (path == "..")
            {
                return new TreeNavNormal
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
                return await GetCurrentModel();
            }
        }
        else
        {
            // We're inside a specific tree
            //var result = await trees[currentTreeIndex].GoRelative(path);
            //if (result.NavResult == TreeNavResult.RootParent)
            //{
            //    currentTreeIndex = -1;
            //    return await GetCurrentModel();
            //}
            return await GetCurrentModel();
        }

        // If the path is invalid, stay at the current location
        return await GetCurrentModel();
    }

    public async Task SetRoot(string path)
    {
        currentTreeIndex = -1;
        // Not applicable for ConcatTrees
    }
}
