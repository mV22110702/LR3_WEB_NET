﻿using System;

class Program
{
    static private int RandomizerCeil = 4;
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
        var s = Program.Randomizer.Next(3, RandomizerCeil);
        Ints = new(3);
        Barrier = new Barrier(Ints.Capacity + 1);
        Main2();
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
            Program.Ints.Add(new(Program.Randomizer.Next(3, RandomizerCeil)));
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

    static Func<object?, double> CalculateListAverage = (object? taskData) =>
    {
        if (taskData is not int)
        {
            throw new ArgumentException("For calculating average an index is required");
        }
        var indexOfListToParse = (int)taskData;
        //Console.WriteLine($"Inside a task to calculate list # {indexOfListToParse}");
        var listToParse = Program.Ints[indexOfListToParse];
        var sum = (double)listToParse.Sum();
        return sum / listToParse.Count;
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
                calcAverageTasks.Add(Task<double>.Factory.StartNew(CalculateListAverage, index));
            }
        });
        var sum = 0d;
        for (int i = 0; i < calcAverageTasks.Count; i++)
        {
            await calcAverageTasks[i];
            sum += calcAverageTasks[i].Result;
        }
        return (double)(sum) / Program.Ints.Count;
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

        var average = CalculateListsAverage().Result;
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