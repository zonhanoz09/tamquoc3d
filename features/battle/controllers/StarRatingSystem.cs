using Godot;
using TamQuoc3D.Data.Resources;

namespace TamQuoc3D.Features.Battle.Controllers;

public struct BattleResult
{
    public bool Won;
    public int SurvivingHeroes;
    public int TurnCount;
    public int TotalDamage;
    public float TimeTaken;
}

/// <summary>
/// StarRatingSystem - He thong danh gia so sao (1 - 3 sao) khi hoan thanh ai chien dich PvE.
/// </summary>
public static class StarRatingSystem
{
    /// <summary>
    /// Tinh toan tong so sao dat duoc dua tren ket qua tran dau va dieu kien cua ai.
    /// </summary>
    public static int CalculateStars(BattleResult result, StageData stageData)
    {
        if (!result.Won) return 0;

        int stars = 1; // Sao 1: Chien thang

        // Sao 2: So tuong song sot dat tieu chuan
        if (stageData != null && result.SurvivingHeroes >= stageData.Star2Condition)
        {
            stars++;
        }

        // Sao 3: Hoan thanh trong so luot quy dinh
        if (stageData != null && result.TurnCount <= stageData.Star3Condition)
        {
            stars++;
        }

        return Mathf.Clamp(stars, 1, 3);
    }

    /// <summary>
    /// Kiem tra tung tieu chi sao rieng le: [0] = Win, [1] = Surviving, [2] = TurnCount
    /// </summary>
    public static bool[] EvaluateStarConditions(BattleResult result, StageData stageData)
    {
        if (stageData == null)
        {
            return [result.Won, result.Won, result.Won];
        }

        return [
            result.Won,
            result.Won && result.SurvivingHeroes >= stageData.Star2Condition,
            result.Won && result.TurnCount <= stageData.Star3Condition
        ];
    }
}
