using System;
using System.Collections.Generic;

public static class KMP
{
    public static List<int> Build(string p)
    {
        int m = p.Length;
        var nxt = new List<int> { 0, 0 };

        for (int i = 1, j = 0; i < m; i++)
        {
            while (j > 0 && p[i] != p[j])
                j = nxt[j];
            if (p[i] == p[j])
                j++;
            nxt.Add(j);
        }
        return nxt;
    }

    public static List<int> Match(string s, string p)
    {
        var nxt = Build(p);
        var ans = new List<int>();
        var n = s.Length;
        var m = p.Length;

        for (int i = 0, j = 0; i < n; i++)
        {
            while (j > 0 && s[i] != p[j])
                j = nxt[j];
            if (s[i] == p[j])
                j++;
            if (j == m)
            {
                ans.Add(i - m + 1);
                j = nxt[j];
            }
        }

        return ans;
    }

    public static void KMPMain(string[] args)
    {
        var positions = Match("ABC ABCDAB ABCDABCDABDEABCDABD4RTAABCDABD", "ABCDABD");
        Console.WriteLine(string.Join(", ", positions)); // Output: 15
        Console.ReadLine();
    }
}