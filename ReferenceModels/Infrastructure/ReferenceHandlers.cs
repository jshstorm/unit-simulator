using System.Collections.Generic;
using System.Text.Json;
using ReferenceModels.Models;

namespace ReferenceModels.Infrastructure;

/// <summary>
/// JSON 파일을 ReferenceTable로 파싱하는 핸들러 모음.
/// </summary>
public static class ReferenceHandlers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// units.json을 파싱합니다.
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="jsonContent">JSON 내용</param>
    /// <returns>UnitReference 테이블</returns>
    public static ReferenceTable<UnitReference> ParseUnits(string tableName, string jsonContent)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, UnitReference>>(jsonContent, JsonOptions)
            ?? new Dictionary<string, UnitReference>();

        return new ReferenceTable<UnitReference>(tableName, data);
    }

    /// <summary>
    /// skills.json을 파싱합니다.
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="jsonContent">JSON 내용</param>
    /// <returns>SkillReference 테이블</returns>
    public static ReferenceTable<SkillReference> ParseSkills(string tableName, string jsonContent)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, SkillReference>>(jsonContent, JsonOptions)
            ?? new Dictionary<string, SkillReference>();

        return new ReferenceTable<SkillReference>(tableName, data);
    }

    /// <summary>
    /// buildings.json을 파싱합니다.
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="jsonContent">JSON 내용</param>
    /// <returns>BuildingReference 테이블</returns>
    public static ReferenceTable<BuildingReference> ParseBuildings(string tableName, string jsonContent)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, BuildingReference>>(jsonContent, JsonOptions)
            ?? new Dictionary<string, BuildingReference>();

        return new ReferenceTable<BuildingReference>(tableName, data);
    }

    /// <summary>
    /// spells.json을 파싱합니다.
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="jsonContent">JSON 내용</param>
    /// <returns>SpellReference 테이블</returns>
    public static ReferenceTable<SpellReference> ParseSpells(string tableName, string jsonContent)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, SpellReference>>(jsonContent, JsonOptions)
            ?? new Dictionary<string, SpellReference>();

        return new ReferenceTable<SpellReference>(tableName, data);
    }

    /// <summary>
    /// towers.json을 파싱합니다.
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="jsonContent">JSON 내용</param>
    /// <returns>TowerReference 테이블</returns>
    public static ReferenceTable<TowerReference> ParseTowers(string tableName, string jsonContent)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, TowerReference>>(jsonContent, JsonOptions)
            ?? new Dictionary<string, TowerReference>();

        return new ReferenceTable<TowerReference>(tableName, data);
    }
}
