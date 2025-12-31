using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ReferenceModels.Infrastructure;

/// <summary>
/// 레퍼런스 테이블의 공통 인터페이스.
/// </summary>
public interface IReferenceTable
{
    /// <summary>테이블 이름 (파일명에서 유래)</summary>
    string TableName { get; }

    /// <summary>레코드 수</summary>
    int Count { get; }

    /// <summary>모든 ID 목록</summary>
    IEnumerable<string> Keys { get; }
}

/// <summary>
/// 제네릭 레퍼런스 테이블.
/// JSON 파일에서 로드된 읽기 전용 데이터를 저장합니다.
/// </summary>
/// <typeparam name="T">레퍼런스 데이터 타입</typeparam>
public class ReferenceTable<T> : IReferenceTable where T : class
{
    private readonly IReadOnlyDictionary<string, T> _data;

    /// <summary>테이블 이름</summary>
    public string TableName { get; }

    /// <summary>레코드 수</summary>
    public int Count => _data.Count;

    /// <summary>모든 ID 목록</summary>
    public IEnumerable<string> Keys => _data.Keys;

    /// <summary>
    /// 레퍼런스 테이블을 생성합니다.
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="data">ID → 레퍼런스 매핑 데이터</param>
    public ReferenceTable(string tableName, Dictionary<string, T> data)
    {
        TableName = tableName;
        _data = new ReadOnlyDictionary<string, T>(data);
    }

    /// <summary>
    /// ID로 레퍼런스를 조회합니다.
    /// </summary>
    /// <param name="id">레퍼런스 ID</param>
    /// <returns>레퍼런스 데이터, 없으면 null</returns>
    public T? Get(string id)
    {
        return _data.TryGetValue(id, out var value) ? value : null;
    }

    /// <summary>
    /// ID로 레퍼런스 조회를 시도합니다.
    /// </summary>
    public bool TryGet(string id, out T? value)
    {
        if (_data.TryGetValue(id, out var result))
        {
            value = result;
            return true;
        }
        value = null;
        return false;
    }

    /// <summary>
    /// 레퍼런스가 존재하는지 확인합니다.
    /// </summary>
    public bool Contains(string id)
    {
        return _data.ContainsKey(id);
    }

    /// <summary>
    /// 모든 레퍼런스를 반환합니다.
    /// </summary>
    public IEnumerable<T> GetAll()
    {
        return _data.Values;
    }

    /// <summary>
    /// 모든 ID-레퍼런스 쌍을 반환합니다.
    /// </summary>
    public IEnumerable<KeyValuePair<string, T>> GetAllWithIds()
    {
        return _data;
    }
}
