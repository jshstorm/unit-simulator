using System;
using System.Collections.Generic;
using System.IO;
using ReferenceModels.Models;

namespace ReferenceModels.Infrastructure;

/// <summary>
/// 모든 레퍼런스 테이블을 관리하는 매니저.
/// 지정된 디렉토리의 JSON 파일들을 로드하여 읽기 전용 데이터로 제공합니다.
/// </summary>
public class ReferenceManager
{
    private readonly Dictionary<string, IReferenceTable> _tables = new();
    private readonly Dictionary<string, Func<string, string, IReferenceTable>> _handlers = new();

    /// <summary>
    /// 로드된 테이블 이름 목록
    /// </summary>
    public IEnumerable<string> LoadedTables => _tables.Keys;

    /// <summary>
    /// Units 테이블 편의 접근자
    /// </summary>
    public ReferenceTable<UnitReference>? Units => GetTable<UnitReference>("units");

    /// <summary>
    /// Skills 테이블 편의 접근자
    /// </summary>
    public ReferenceTable<SkillReference>? Skills => GetTable<SkillReference>("skills");

    /// <summary>
    /// Buildings 테이블 편의 접근자
    /// </summary>
    public ReferenceTable<BuildingReference>? Buildings => GetTable<BuildingReference>("buildings");

    /// <summary>
    /// Spells 테이블 편의 접근자
    /// </summary>
    public ReferenceTable<SpellReference>? Spells => GetTable<SpellReference>("spells");

    /// <summary>
    /// Towers 테이블 편의 접근자
    /// </summary>
    public ReferenceTable<TowerReference>? Towers => GetTable<TowerReference>("towers");

    /// <summary>
    /// 테이블 핸들러를 등록합니다.
    /// </summary>
    /// <typeparam name="T">레퍼런스 데이터 타입</typeparam>
    /// <param name="tableName">테이블 이름 (파일명, 확장자 제외)</param>
    /// <param name="handler">JSON 내용을 파싱하여 ReferenceTable을 반환하는 핸들러</param>
    public void RegisterHandler<T>(string tableName, Func<string, string, ReferenceTable<T>> handler) where T : class
    {
        _handlers[tableName.ToLowerInvariant()] = (name, json) => handler(name, json);
    }

    /// <summary>
    /// 지정된 디렉토리의 모든 JSON 파일을 로드합니다.
    /// </summary>
    /// <param name="directoryPath">레퍼런스 디렉토리 경로</param>
    /// <param name="logger">로그 출력 액션 (선택)</param>
    public void LoadAll(string directoryPath, Action<string>? logger = null)
    {
        logger ??= Console.WriteLine;

        if (!Directory.Exists(directoryPath))
        {
            logger($"[ReferenceManager] Directory not found: {directoryPath}");
            return;
        }

        var jsonFiles = Directory.GetFiles(directoryPath, "*.json");
        logger($"[ReferenceManager] Found {jsonFiles.Length} JSON file(s) in {directoryPath}");

        foreach (var filePath in jsonFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

            if (!_handlers.TryGetValue(fileName, out var handler))
            {
                logger($"[Warning] No handler registered for '{fileName}', skipping");
                continue;
            }

            try
            {
                var jsonContent = File.ReadAllText(filePath);
                var table = handler(fileName, jsonContent);
                _tables[fileName] = table;
                logger($"[ReferenceManager] Loaded '{fileName}' with {table.Count} record(s)");
            }
            catch (Exception ex)
            {
                logger($"[Error] Failed to load '{fileName}': {ex.Message}");
            }
        }

        logger($"[ReferenceManager] Total {_tables.Count} table(s) loaded");
    }

    /// <summary>
    /// 테이블을 직접 등록합니다 (테스트용).
    /// </summary>
    public void RegisterTable<T>(ReferenceTable<T> table) where T : class
    {
        _tables[table.TableName.ToLowerInvariant()] = table;
    }

    /// <summary>
    /// 특정 타입의 테이블을 조회합니다.
    /// </summary>
    /// <typeparam name="T">레퍼런스 데이터 타입</typeparam>
    /// <param name="tableName">테이블 이름</param>
    /// <returns>테이블, 없으면 null</returns>
    public ReferenceTable<T>? GetTable<T>(string tableName) where T : class
    {
        if (_tables.TryGetValue(tableName.ToLowerInvariant(), out var table))
        {
            return table as ReferenceTable<T>;
        }
        return null;
    }

    /// <summary>
    /// 테이블이 로드되어 있는지 확인합니다.
    /// </summary>
    public bool HasTable(string tableName)
    {
        return _tables.ContainsKey(tableName.ToLowerInvariant());
    }

    /// <summary>
    /// 기본 핸들러가 등록된 ReferenceManager를 생성합니다.
    /// </summary>
    public static ReferenceManager CreateWithDefaultHandlers()
    {
        var manager = new ReferenceManager();
        manager.RegisterHandler<UnitReference>("units", ReferenceHandlers.ParseUnits);
        manager.RegisterHandler<SkillReference>("skills", ReferenceHandlers.ParseSkills);
        manager.RegisterHandler<BuildingReference>("buildings", ReferenceHandlers.ParseBuildings);
        manager.RegisterHandler<SpellReference>("spells", ReferenceHandlers.ParseSpells);
        manager.RegisterHandler<TowerReference>("towers", ReferenceHandlers.ParseTowers);
        return manager;
    }

    /// <summary>
    /// 기본 핸들러와 데이터가 로드된 ReferenceManager를 생성합니다.
    /// </summary>
    /// <param name="directoryPath">레퍼런스 디렉토리 경로</param>
    /// <param name="logger">로그 출력 액션 (선택)</param>
    public static ReferenceManager CreateAndLoad(string directoryPath, Action<string>? logger = null)
    {
        var manager = CreateWithDefaultHandlers();
        manager.LoadAll(directoryPath, logger);
        return manager;
    }
}
