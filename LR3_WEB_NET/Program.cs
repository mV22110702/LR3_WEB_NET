using System;

class Program
{
    static private int RandomizerCeil = 3;
    static private Random Randomizer;
    static private List<List<int>> Ints;
    static Barrier Barrier;

    static void PrintListOfLists<T>(List<List<T>> lists)
    {
        foreach (var intList in lists)
        {
            foreach (var num in intList)
            {
                Console.Write(num.ToString() + '\t');
            }
            Console.WriteLine();
        }
    }
    static void Main()
    {

        Randomizer = new Random();
        Ints = new(Program.Randomizer.Next(3, RandomizerCeil));
        Barrier = new Barrier(Ints.Capacity + 1);
        Task.WaitAll(Main2());
    }
    static void FillSingleList(object? threadData)
    {

        if (threadData is not int)
        {
            throw new ArgumentException("For filling a single list the list is required");
        }
        var index = (int)threadData;
        var listToFill = Ints[index];
        ConsoleWriteLine($"Inside a thread (pooled) for a list filler for list # {index}");
        for (int i = 0; i < listToFill.Capacity; i++)
        {
            ConsoleWriteLine($"Assigning value # {i} for list # {index}");
            listToFill.Add(Randomizer.Next(50));
        }
        Barrier.SignalAndWait();
    }
    static void CreateSingleList(object? threadData)
    {
        if (threadData is not int)
        {
            throw new ArgumentException("For creating a single list an index is required");
        }
        var index = (int)threadData;
        lock (Program.Ints)
        {
            Program.Ints.Add(new(Program.Randomizer.Next(1, RandomizerCeil)));
        }
        ConsoleWriteLine($"Created list for index # {index}");
        Barrier.SignalAndWait();
    }

    static void CreateLists()
    {
        Parallel.For(0, Program.Ints.Capacity, (index) =>
        {
            ConsoleWriteLine($"Creating a thread to create a list # {index}");
            new Thread(Program.CreateSingleList).Start(index);
        });

    }

    static Func<object?, double> CalculateListSum = (object? taskData) =>
    {
        if (taskData is not int)
        {
            throw new ArgumentException("For calculating average an index is required");
        }
        var indexOfListToParse = (int)taskData;
        var listToParse = Program.Ints[indexOfListToParse];
        var sum = (double)listToParse.Sum();
        return sum;
    };

    static Func<object?, int> GetListCount = (object? taskData) =>
    {

        if (taskData is not int)
        {
            throw new ArgumentException("For getting list count out of lists, an index is required");
        }
        var index = (int)taskData;
        return Program.Ints[index].Count;
    };

    static async Task<double> CalculateListsAverage()
    {
        Console.WriteLine("Before calculating average");
        List<Task<double>> calcAverageTasks = new List<Task<double>>();
        Parallel.For(0, Program.Ints.Count, (index) =>
        {
            Console.WriteLine($"Adding a task to a task list to calculate list # {index}");
            lock (calcAverageTasks)
            {
                calcAverageTasks.Add(Task<double>.Factory.StartNew(CalculateListSum, index));
            }
        });
        var sum = 0d;
        for (int i = 0; i < calcAverageTasks.Count; i++)
        {
            sum += await calcAverageTasks[i];
        }

        var countSum = 0;
        List<Task<int>> calcCountsTasks = new List<Task<int>>();
        Parallel.For(0, Program.Ints.Count, (index) =>
        {
            Console.WriteLine($"Adding a task to a task list to get list items count # {index}");
            lock (calcCountsTasks)
            {
                calcCountsTasks.Add(Task<int>.Factory.StartNew(GetListCount, index));
            }
        });
        for (int i = 0; i < calcCountsTasks.Count; i++)
        {
            countSum += await calcCountsTasks[i];
        }

        return (double)(sum) / countSum;
    }
    static void FillLists()
    {
        Parallel.For(0, Program.Ints.Capacity, (index) =>
        {
            new Thread(FillSingleList).Start(index);
        });
    }
    static async Task Main2()
    {
        ConsoleWriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name} -- before filling with ints");

        CreateLists();
        Barrier.SignalAndWait();
        Console.WriteLine("Lists allocated");

        FillLists();
        Barrier.SignalAndWait();
        PrintListOfLists(Ints);

        var average = await CalculateListsAverage();
        Console.WriteLine($"Average: {average}");
        Console.ReadKey();
    }
    static void ConsoleWriteLine(string str)
    {
        int threadId = Thread.CurrentThread.ManagedThreadId;
        Console.ForegroundColor = threadId == 1 ? ConsoleColor.White : ConsoleColor.Green;
        Console.WriteLine(
           $"{str}  Thread {threadId}");
    }
}