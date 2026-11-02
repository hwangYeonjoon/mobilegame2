using System;
using ScrapGuard.Tests;
using ScrapGuard.Core;

class Program
{
    static int Main(string[] args)
    {
        if (Array.IndexOf(args, "--simulate") >= 0) return Simulate();
        int failures = 0;
        foreach (var name in CoreSpecification.Names)
        {
            try { CoreSpecification.Run(name); Console.WriteLine("PASS " + name); }
            catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + " " + (e.InnerException ?? e).Message); }
        }
        Console.WriteLine((CoreSpecification.Names.Length - failures) + "/" + CoreSpecification.Names.Length + " passed");
        return failures == 0 ? 0 : 1;
    }
    static int Simulate()
    {
        for (int seed = 0; seed < 5; seed++)
        {
            var c = new Combat(GameState.New(), 0, "sim", seed);
            for (int i = 0; i < 10000 && !c.Ended; i++)
            {
                if (c.Choosing)
                {
                    int choice = c.Choices[0]; int[] priority = { 1, 2, 0, 3, 4, 5, 6, 7 };
                    foreach (int skill in priority) if (Array.IndexOf(c.Choices, skill) >= 0) { choice = skill; break; }
                    c.Choose(choice);
                }
                V2 away = new V2();
                foreach (var e in c.Enemies) { V2 delta = c.Position - e.position; float d = delta.Length; if (d < 100) away += delta.Normal * ((100 - d) / 90); }
                V2 target = new V2(270 + (float)Math.Cos(c.Elapsed * .4f) * 160, 490 + (float)Math.Sin(c.Elapsed * .4f) * 210);
                c.Tick(.05f, ((target - c.Position).Normal + away).Normal);
            }
            Console.WriteLine("Seed=" + seed + " won=" + c.Won + " time=" + c.Elapsed.ToString("F1") + " kills=" + c.Kills + " level=" + c.Level + " hp=" + c.Hp.ToString("F0") + " skills=" + string.Join(",",c.Skills));
        }
        return 0;
    }
}
