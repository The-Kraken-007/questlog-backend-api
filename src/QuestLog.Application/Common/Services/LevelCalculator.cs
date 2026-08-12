namespace QuestLog.Application.Common.Services;

/// <summary>
/// Pure static service that computes level, title, and XP progress from total XP.
/// Uses an S-curve formula: XP needed = floor(100 * (level ^ 1.5)).
/// Both backend and frontend must use identical logic — frontend displays server-computed values.
/// </summary>
public static class LevelCalculator
{
    /// <summary>Calculate the current level from total XP.</summary>
    public static int CalculateLevel(int totalXp)
    {
        int level = 1;
        int xpNeeded = 100;
        while (totalXp >= xpNeeded)
        {
            totalXp -= xpNeeded;
            level++;
            xpNeeded = (int)Math.Floor(100 * Math.Pow(level, 1.5));
        }
        return level;
    }

    /// <summary>Get the RPG-themed title for a given level.</summary>
    public static string GetTitle(int level) => level switch
    {
        <= 4 => "Novice",
        <= 9 => "Apprentice",
        <= 19 => "Adept",
        <= 29 => "Specialist",
        <= 39 => "Expert",
        <= 49 => "Master",
        <= 74 => "Grandmaster",
        <= 99 => "Champion",
        _ => "Mythic"
    };

    /// <summary>XP needed to advance FROM this level to the next.</summary>
    public static int GetXpForLevel(int level)
        => (int)Math.Floor(100 * Math.Pow(level, 1.5));

    /// <summary>Cumulative XP required to reach this level from level 1.</summary>
    public static int GetTotalXpForLevel(int level)
    {
        int total = 0;
        for (int i = 1; i < level; i++)
            total += GetXpForLevel(i);
        return total;
    }

    /// <summary>XP earned within the current level.</summary>
    public static int GetXpInCurrentLevel(int totalXp)
    {
        int level = CalculateLevel(totalXp);
        int xpAtLevelStart = GetTotalXpForLevel(level);
        return totalXp - xpAtLevelStart;
    }

    /// <summary>XP remaining to reach the next level.</summary>
    public static int GetXpToNextLevel(int totalXp)
    {
        int level = CalculateLevel(totalXp);
        return GetXpForLevel(level) - GetXpInCurrentLevel(totalXp);
    }
}
