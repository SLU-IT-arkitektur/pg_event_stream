public static class ArgsParser
{
    public static (bool, string, int) TryGetUnitAndNumber(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("no arguments provided, expecting -u <days|hours|minutes> -n <number as int>");
            return (false, "", 0);
        }

        if (!args.Contains("-u"))
        {
            Console.WriteLine("no unit provided, expecting -u <days|hours|minutes>");
            return (false, "", 0);
        }
        
        if (!args.Contains("-n"))
        {
            Console.WriteLine("no number provided, expecting -n <number as int>");
            return (false, "", 0);
        }
        
        var argsList = args.ToList();
        var unit = argsList[argsList.IndexOf("-u") + 1].Trim();
        var number = argsList[argsList.IndexOf("-n") + 1].Trim();

        if (!int.TryParse(number, out int n))
        {
            Console.WriteLine("number is not an integer");
            return (false, "", 0);
        }
        
        if (unit.ToLower() != "days" && unit.ToLower() != "hours" && unit.ToLower() != "minutes")
        {
            Console.WriteLine("unit is not days, hours or minutes");
            return (false, "", 0);
        }
        
        return (true, unit, n);
    }
}