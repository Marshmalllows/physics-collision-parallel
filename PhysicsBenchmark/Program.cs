using PhysicsBenchmark;

while (true)
{
    Console.WriteLine("Physics Benchmark");
    Console.WriteLine("1. Generate scenarios");
    Console.WriteLine("2. Run verification tests");
    Console.WriteLine("3. Benchmark: all series");
    Console.WriteLine("4. Benchmark: body count scaling");
    Console.WriteLine("5. Benchmark: thread count impact");
    Console.WriteLine("6. Benchmark: strategy comparison");
    Console.WriteLine("7. Run everything");
    Console.WriteLine("0. Exit");
    Console.Write("> ");

    var choice = Console.ReadLine()?.Trim();
    Console.WriteLine();

    switch (choice)
    {
        case "1":
            ScenarioGenerator.GenerateAll();
            break;
        case "2":
            VerificationRunner.Run();
            break;
        case "3":
            BenchmarkRunner.RunAll();
            break;
        case "4":
            BenchmarkRunner.RunSeries1();
            break;
        case "5":
            BenchmarkRunner.RunSeries2();
            break;
        case "6":
            BenchmarkRunner.RunSeries3();
            break;
        case "7":
            ScenarioGenerator.GenerateAll();
            VerificationRunner.Run();
            BenchmarkRunner.RunAll();
            break;
        case "0":
            return;
        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
}
