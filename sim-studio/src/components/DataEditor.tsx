import { useCallback, useEffect, useMemo, useState } from 'react';

type DataFile = {
  path: string;
  size: number;
  modifiedUtc: string;
  etag: string;
};

type FileResponse = {
  path: string;
  content: string;
  etag: string;
  modifiedUtc: string;
};

type FilesResponse = {
  root: string;
  files: DataFile[];
};

type Props = {
  apiBaseUrl: string;
};

function formatBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  const kb = bytes / 1024;
  if (kb < 1024) return `${kb.toFixed(1)} KB`;
  return `${(kb / 1024).toFixed(1)} MB`;
}

function recordLabel(record: unknown, index: number) {
  if (record && typeof record === 'object') {
    const candidate = record as Record<string, unknown>;
    const labelKey = ['id', 'name', 'key', 'role', 'type'].find(k => typeof candidate[k] === 'string' || typeof candidate[k] === 'number');
    if (labelKey) {
      return `${index} · ${candidate[labelKey]}`;
    }
  }
  return `${index}`;
}

export default function DataEditor({ apiBaseUrl }: Props) {
  const [files, setFiles] = useState<DataFile[]>([]);
  const [selectedPath, setSelectedPath] = useState<string | null>(null);
  const [documentValue, setDocumentValue] = useState<unknown>(null);
  const [rawDraft, setRawDraft] = useState('');
  const [etag, setEtag] = useState<string | null>(null);
  const [modifiedUtc, setModifiedUtc] = useState<string | null>(null);
  const [selectedIndex, setSelectedIndex] = useState<number | null>(null);
  const [recordDraft, setRecordDraft] = useState('');
  const [viewMode, setViewMode] = useState<'table' | 'raw'>('table');
  const [filterText, setFilterText] = useState('');
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [newFieldKey, setNewFieldKey] = useState('');
  const [newFieldValue, setNewFieldValue] = useState('');
  const [newFilePath, setNewFilePath] = useState('new-data.json');
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [activeArrayPath, setActiveArrayPath] = useState<string>('__root__');

  const isArray = useMemo(() => Array.isArray(documentValue), [documentValue]);

  const getPathValue = useCallback((root: unknown, path: (string | number)[]) => {
    let current: unknown = root;
    for (const segment of path) {
      if (current === null || current === undefined) return undefined;
      if (typeof segment === 'number' && Array.isArray(current)) {
        current = current[segment];
      } else if (typeof segment === 'string' && typeof current === 'object' && !Array.isArray(current)) {
        current = (current as Record<string, unknown>)[segment];
      } else {
        return undefined;
      }
    }
    return current;
  }, []);

  const setPathValue = useCallback((root: unknown, path: (string | number)[], value: unknown) => {
    if (path.length === 0) return value;
    const [head, ...rest] = path;
    if (typeof head === 'number') {
      const next = Array.isArray(root) ? [...root] : [];
      next[head] = setPathValue(next[head], rest, value);
      return next;
    }
    const base = root && typeof root === 'object' && !Array.isArray(root) ? { ...(root as Record<string, unknown>) } : {};
    base[head] = setPathValue(base[head], rest, value);
    return base;
  }, []);

  const arrayCandidates = useMemo(() => {
    const candidates: { label: string; path: (string | number)[] }[] = [];
    if (!documentValue) return candidates;
    if (Array.isArray(documentValue)) {
      candidates.push({ label: 'root', path: [] });
      if (documentValue.length === 1 && documentValue[0] && typeof documentValue[0] === 'object' && !Array.isArray(documentValue[0])) {
        Object.entries(documentValue[0] as Record<string, unknown>).forEach(([key, value]) => {
          if (Array.isArray(value)) {
            candidates.push({ label: key, path: [0, key] });
          }
        });
      }
      return candidates;
    }
    if (documentValue && typeof documentValue === 'object') {
      Object.entries(documentValue as Record<string, unknown>).forEach(([key, value]) => {
        if (Array.isArray(value)) {
          candidates.push({ label: key, path: [key] });
        }
      });
    }
    return candidates;
  }, [documentValue]);

  const activeArrayPathSegments = useMemo<(string | number)[]>(() => {
    if (activeArrayPath === '__root__') return [];
    return activeArrayPath.split('.').map(segment => {
      const numeric = Number(segment);
      return Number.isNaN(numeric) ? segment : numeric;
    });
  }, [activeArrayPath]);

  const activeArray = useMemo(() => {
    if (!documentValue) return null;
    const path = activeArrayPath === '__root__' ? [] : activeArrayPathSegments;
    const value = getPathValue(documentValue, path);
    return Array.isArray(value) ? value : null;
  }, [activeArrayPath, activeArrayPathSegments, documentValue, getPathValue]);

  const isObjectArray = useMemo(() => {
    if (!Array.isArray(activeArray)) return false;
    return activeArray.every(item => item && typeof item === 'object' && !Array.isArray(item));
  }, [activeArray]);

  const loadFiles = useCallback(async () => {
    setError(null);
    try {
      const res = await fetch(`${apiBaseUrl}/data/files`);
      if (!res.ok) throw new Error('Failed to load file list.');
      const data = (await res.json()) as FilesResponse;
      setFiles(data.files);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error.');
    }
  }, [apiBaseUrl]);

  const loadFile = useCallback(async (path: string) => {
    setError(null);
    setStatus(null);
    setIsLoading(true);
    try {
      const res = await fetch(`${apiBaseUrl}/data/file?path=${encodeURIComponent(path)}`);
      if (!res.ok) throw new Error('Failed to load file.');
      const data = (await res.json()) as FileResponse;
      const parsed = JSON.parse(data.content);
      setSelectedPath(path);
      setDocumentValue(parsed);
      setRawDraft(JSON.stringify(parsed, null, 2));
      setEtag(data.etag);
      setModifiedUtc(data.modifiedUtc);
      if (Array.isArray(parsed) && parsed.length > 0) {
        setSelectedIndex(0);
        setRecordDraft(JSON.stringify(parsed[0], null, 2));
      } else {
        setSelectedIndex(null);
        setRecordDraft('');
      }
      setViewMode(Array.isArray(parsed) ? 'table' : 'raw');
      setFilterText('');
      setSortKey(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error.');
    } finally {
      setIsLoading(false);
    }
  }, [apiBaseUrl]);

  const applyRawDraft = useCallback(() => {
    try {
      const parsed = JSON.parse(rawDraft);
      setDocumentValue(parsed);
      setStatus('Raw JSON applied.');
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid JSON.');
    }
  }, [rawDraft]);

  const applyRecordDraft = useCallback(() => {
    if (!activeArray || selectedIndex === null) return;
    try {
      const parsed = JSON.parse(recordDraft);
      const nextArray = [...activeArray];
      nextArray[selectedIndex] = parsed;
      setDocumentValue(prev => {
        const updated = setPathValue(prev, activeArrayPathSegments, nextArray);
        setRawDraft(JSON.stringify(updated, null, 2));
        return updated;
      });
      setStatus('Record updated.');
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Invalid record JSON.');
    }
  }, [activeArray, activeArrayPathSegments, recordDraft, selectedIndex, setPathValue]);

  const addRecord = useCallback(() => {
    if (!activeArray) return;
    const nextArray = [...activeArray, {}];
    const newIndex = nextArray.length - 1;
    setDocumentValue(prev => {
      const updated = setPathValue(prev, activeArrayPathSegments, nextArray);
      setRawDraft(JSON.stringify(updated, null, 2));
      return updated;
    });
    setSelectedIndex(newIndex);
    setRecordDraft(JSON.stringify(nextArray[newIndex], null, 2));
    setStatus('Record added.');
  }, [activeArray, activeArrayPathSegments, setPathValue]);

  const deleteRecord = useCallback(() => {
    if (!activeArray || selectedIndex === null) return;
    const nextArray = [...activeArray];
    nextArray.splice(selectedIndex, 1);
    const nextIndex = nextArray.length ? Math.min(selectedIndex, nextArray.length - 1) : null;
    setDocumentValue(prev => {
      const updated = setPathValue(prev, activeArrayPathSegments, nextArray);
      setRawDraft(JSON.stringify(updated, null, 2));
      return updated;
    });
    setSelectedIndex(nextIndex);
    setRecordDraft(nextIndex !== null ? JSON.stringify(nextArray[nextIndex], null, 2) : '');
    setStatus('Record removed.');
  }, [activeArray, activeArrayPathSegments, selectedIndex, setPathValue]);

  const saveFile = useCallback(async () => {
    if (!selectedPath) return;
    setError(null);
    setStatus(null);
    const content = JSON.stringify(documentValue, null, 2);
    try {
      const res = await fetch(`${apiBaseUrl}/data/file?path=${encodeURIComponent(selectedPath)}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content, etag })
      });
      if (res.status === 409) {
        const data = await res.json();
        setError('Conflict detected. Reload before saving.');
        setEtag(data.etag ?? null);
        return;
      }
      if (!res.ok) throw new Error('Failed to save file.');
      const data = await res.json();
      setEtag(data.etag ?? null);
      setStatus('Saved.');
      loadFiles();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error.');
    }
  }, [apiBaseUrl, documentValue, etag, loadFiles, selectedPath]);

  const createFile = useCallback(async () => {
    if (!newFilePath.trim()) return;
    setError(null);
    try {
      const res = await fetch(`${apiBaseUrl}/data/file?path=${encodeURIComponent(newFilePath)}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: '[]' })
      });
      if (!res.ok) throw new Error('Failed to create file.');
      await loadFiles();
      await loadFile(newFilePath);
      setStatus('File created.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error.');
    }
  }, [apiBaseUrl, loadFile, loadFiles, newFilePath]);

  const deleteFile = useCallback(async () => {
    if (!selectedPath) return;
    if (!window.confirm(`Delete ${selectedPath}?`)) return;
    setError(null);
    try {
      const res = await fetch(`${apiBaseUrl}/data/file?path=${encodeURIComponent(selectedPath)}`, {
        method: 'DELETE'
      });
      if (!res.ok) throw new Error('Failed to delete file.');
      setSelectedPath(null);
      setDocumentValue(null);
      setRawDraft('');
      setSelectedIndex(null);
      setRecordDraft('');
      await loadFiles();
      setStatus('File deleted.');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error.');
    }
  }, [apiBaseUrl, loadFiles, selectedPath]);

  useEffect(() => {
    loadFiles();
  }, [loadFiles]);

  useEffect(() => {
    if (!documentValue) return;
    if (arrayCandidates.length > 0) {
      let nextCandidate = arrayCandidates[0];
      if (Array.isArray(documentValue) && documentValue.length === 1) {
        const nested = arrayCandidates.find(candidate => candidate.label !== 'root');
        if (nested) {
          nextCandidate = nested;
        }
      }
      const nextPath = nextCandidate.path;
      const pathKey = nextPath.length === 0 ? '__root__' : nextPath.join('.');
      setActiveArrayPath(pathKey);
    } else {
      setActiveArrayPath('__root__');
    }
  }, [arrayCandidates, documentValue]);

  useEffect(() => {
    if (activeArray && activeArray.length > 0) {
      setSelectedIndex(0);
      setRecordDraft(JSON.stringify(activeArray[0], null, 2));
      setViewMode('table');
    } else {
      setSelectedIndex(null);
      setRecordDraft('');
      if (!isArray) {
        setViewMode('raw');
      }
    }
  }, [activeArray, isArray]);

  useEffect(() => {
    if (!activeArray || selectedIndex === null) return;
    if (selectedIndex >= activeArray.length) return;
    setRecordDraft(JSON.stringify(activeArray[selectedIndex], null, 2));
  }, [activeArray, selectedIndex]);

  const tableRows = useMemo(() => {
    if (!Array.isArray(activeArray)) return [];
    return activeArray.map((record, index) => ({ record, index }));
  }, [activeArray]);

  const filteredRows = useMemo(() => {
    if (!filterText.trim()) return tableRows;
    const needle = filterText.trim().toLowerCase();
    return tableRows.filter(row => {
      try {
        return JSON.stringify(row.record).toLowerCase().includes(needle);
      } catch {
        return false;
      }
    });
  }, [filterText, tableRows]);

  const columns = useMemo(() => {
    if (!Array.isArray(activeArray)) return [];
    if (!isObjectArray) return ['__value'];
    const keys = new Set<string>();
    for (let i = 0; i < Math.min(activeArray.length, 50); i += 1) {
      const item = activeArray[i];
      if (item && typeof item === 'object' && !Array.isArray(item)) {
        Object.keys(item as Record<string, unknown>).forEach(key => keys.add(key));
      }
    }
    return Array.from(keys);
  }, [activeArray, isObjectArray]);

  const sortedRows = useMemo(() => {
    if (!sortKey) return filteredRows;
    const next = [...filteredRows];
    const dir = sortDir === 'asc' ? 1 : -1;
    next.sort((a, b) => {
      const aVal = sortKey === '__value'
        ? a.record
        : (a.record as Record<string, unknown>)[sortKey];
      const bVal = sortKey === '__value'
        ? b.record
        : (b.record as Record<string, unknown>)[sortKey];
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return (aVal - bVal) * dir;
      }
      const aStr = aVal === undefined || aVal === null ? '' : String(aVal);
      const bStr = bVal === undefined || bVal === null ? '' : String(bVal);
      return aStr.localeCompare(bStr) * dir;
    });
    return next;
  }, [filteredRows, sortDir, sortKey]);

  const parsedRecordDraft = useMemo(() => {
    if (!recordDraft) return null;
    try {
      return JSON.parse(recordDraft) as Record<string, unknown>;
    } catch {
      return null;
    }
  }, [recordDraft]);

  const recordFields = useMemo(() => {
    if (!parsedRecordDraft || typeof parsedRecordDraft !== 'object' || Array.isArray(parsedRecordDraft)) {
      return [];
    }
    return Object.entries(parsedRecordDraft).sort(([a], [b]) => a.localeCompare(b));
  }, [parsedRecordDraft]);

  const formatCell = (value: unknown) => {
    if (value === null || value === undefined) return '';
    if (typeof value === 'string') return value.length > 80 ? `${value.slice(0, 77)}...` : value;
    if (typeof value === 'number' || typeof value === 'boolean') return String(value);
    try {
      const text = JSON.stringify(value);
      return text.length > 80 ? `${text.slice(0, 77)}...` : text;
    } catch {
      return String(value);
    }
  };

  const updateRecordField = (key: string, value: unknown) => {
    if (!parsedRecordDraft || typeof parsedRecordDraft !== 'object' || Array.isArray(parsedRecordDraft)) return;
    const next = { ...parsedRecordDraft, [key]: value };
    setRecordDraft(JSON.stringify(next, null, 2));
    setStatus(null);
  };

  const handleAddField = () => {
    if (!newFieldKey.trim()) return;
    let nextValue: unknown = newFieldValue;
    if (newFieldValue.trim()) {
      try {
        nextValue = JSON.parse(newFieldValue);
      } catch {
        nextValue = newFieldValue;
      }
    }
    updateRecordField(newFieldKey.trim(), nextValue);
    setNewFieldKey('');
    setNewFieldValue('');
  };

  return (
    <div className="data-editor">
      <aside className="panel data-editor-sidebar">
        <h2>Data Files</h2>
        <div className="data-editor-create">
          <input
            type="text"
            value={newFilePath}
            onChange={e => setNewFilePath(e.target.value)}
            placeholder="new-data.json"
          />
          <button className="btn-primary" onClick={createFile}>Create</button>
        </div>
        <div className="data-editor-file-list">
          {files.map(file => (
            <button
              key={file.path}
              className={`data-file ${file.path === selectedPath ? 'active' : ''}`}
              onClick={() => loadFile(file.path)}
            >
              <div className="data-file-name">{file.path}</div>
              <div className="data-file-meta">{formatBytes(file.size)}</div>
            </button>
          ))}
        </div>
      </aside>

      <section className="panel data-editor-main">
        <div className="data-editor-header">
          <div>
            <h2>Editor</h2>
            {selectedPath && (
              <div className="data-editor-subtitle">
                {selectedPath}
                {modifiedUtc && <span> · {new Date(modifiedUtc).toLocaleString()}</span>}
              </div>
            )}
          </div>
          <div className="data-editor-actions">
            <div className="data-editor-tabs">
              <button
                className={`tab-button ${viewMode === 'table' ? 'active' : ''}`}
                onClick={() => setViewMode('table')}
                disabled={!activeArray}
              >
                Table
              </button>
              <button
                className={`tab-button ${viewMode === 'raw' ? 'active' : ''}`}
                onClick={() => setViewMode('raw')}
              >
                Raw JSON
              </button>
            </div>
            <button className="btn-secondary" onClick={loadFiles}>Refresh</button>
            <button className="btn-secondary" onClick={deleteFile} disabled={!selectedPath}>Delete File</button>
            <button className="btn-primary" onClick={saveFile} disabled={!selectedPath}>Save</button>
          </div>
        </div>

        {error && <div className="data-editor-error">{error}</div>}
        {status && <div className="data-editor-status">{status}</div>}
        {isLoading && <div className="data-editor-status">Loading...</div>}

        {!selectedPath && (
          <div className="data-editor-empty">Select a file to edit.</div>
        )}

        {selectedPath && (
          <div className="data-editor-body">
            {viewMode === 'table' && activeArray && (
              <div className="data-editor-table">
                <div className="data-editor-toolbar">
                  <input
                    type="text"
                    placeholder="Search records..."
                    value={filterText}
                    onChange={e => setFilterText(e.target.value)}
                  />
                  {arrayCandidates.length > 1 && (
                    <select
                      className="data-editor-select"
                      value={activeArrayPath}
                      onChange={(e) => {
                        setActiveArrayPath(e.target.value);
                        setSelectedIndex(0);
                        setRecordDraft('');
                      }}
                    >
                      {arrayCandidates.map(candidate => {
                        const value = candidate.path.length === 0 ? '__root__' : candidate.path.join('.');
                        return (
                          <option key={value} value={value}>
                            {candidate.label}
                          </option>
                        );
                      })}
                    </select>
                  )}
                  <div className="data-editor-toolbar-meta">
                    {sortedRows.length} / {tableRows.length} records
                  </div>
                  <div className="data-editor-record-actions">
                    <button className="btn-secondary" onClick={addRecord}>Add</button>
                    <button className="btn-secondary" onClick={deleteRecord} disabled={selectedIndex === null}>Remove</button>
                  </div>
                </div>

                <div className="data-table-wrap">
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th>#</th>
                        {columns.map(col => (
                          <th
                            key={col}
                            onClick={() => {
                              if (sortKey === col) {
                                setSortDir(sortDir === 'asc' ? 'desc' : 'asc');
                              } else {
                                setSortKey(col);
                                setSortDir('asc');
                              }
                            }}
                            className={sortKey === col ? 'sorted' : ''}
                          >
                            {col}
                            {sortKey === col && <span className="sort-indicator">{sortDir === 'asc' ? '▲' : '▼'}</span>}
                          </th>
                        ))}
                      </tr>
                    </thead>
                    <tbody>
                      {sortedRows.map(({ record, index }) => (
                        <tr
                          key={index}
                          className={selectedIndex === index ? 'active' : ''}
                          onClick={() => setSelectedIndex(index)}
                        >
                          <td className="row-index">{index}</td>
                          {columns.map(col => (
                            <td key={col}>
                              {col === '__value'
                                ? formatCell(record)
                                : formatCell((record as Record<string, unknown>)[col])}
                            </td>
                          ))}
                        </tr>
                      ))}
                      {sortedRows.length === 0 && (
                        <tr>
                          <td colSpan={columns.length + 1} className="data-table-empty">
                            No matching records
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>

                <div className="data-editor-record-detail">
                  <div className="data-editor-record-header">
                    <h3>Record Detail</h3>
                    <button className="btn-primary" onClick={applyRecordDraft} disabled={selectedIndex === null}>
                      Apply Record
                    </button>
                  </div>
                  {selectedIndex === null && (
                    <div className="data-editor-empty">Select a record to edit.</div>
                  )}
                  {selectedIndex !== null && (
                    <div className="data-editor-record-fields">
                      {recordFields.length === 0 && (
                        <div className="data-editor-empty">Record is not an object. Use Raw JSON to edit.</div>
                      )}
                      {recordFields.map(([key, value]) => {
                        const valueType = typeof value;
                        if (valueType === 'string' || valueType === 'number' || valueType === 'boolean' || value === null) {
                          return (
                            <div key={key} className="data-field-row">
                              <div className="data-field-label">{key}</div>
                              <div className="data-field-input">
                                {valueType === 'boolean' ? (
                                  <input
                                    type="checkbox"
                                    checked={Boolean(value)}
                                    onChange={e => updateRecordField(key, e.target.checked)}
                                  />
                                ) : (
                                  <input
                                    type={valueType === 'number' ? 'number' : 'text'}
                                    value={value === null ? '' : String(value)}
                                    onChange={e => {
                                      if (valueType === 'number') {
                                        const nextVal = e.target.value === '' ? null : Number(e.target.value);
                                        if (e.target.value === '' || !Number.isNaN(nextVal)) {
                                          updateRecordField(key, nextVal);
                                        }
                                      } else {
                                        updateRecordField(key, e.target.value);
                                      }
                                    }}
                                  />
                                )}
                              </div>
                            </div>
                          );
                        }

                        return (
                          <div key={key} className="data-field-row">
                            <div className="data-field-label">{key}</div>
                            <div className="data-field-input">
                              <pre className="data-field-readonly">
                                {formatCell(value)}
                              </pre>
                              <span className="data-field-hint">Edit this field in Record JSON.</span>
                            </div>
                          </div>
                        );
                      })}
                      <div className="data-field-row data-field-add">
                        <div className="data-field-label">Add Field</div>
                        <div className="data-field-input data-field-add-inputs">
                          <input
                            type="text"
                            placeholder="field_name"
                            value={newFieldKey}
                            onChange={e => setNewFieldKey(e.target.value)}
                          />
                          <input
                            type="text"
                            placeholder='value (JSON or text)'
                            value={newFieldValue}
                            onChange={e => setNewFieldValue(e.target.value)}
                          />
                          <button className="btn-secondary" onClick={handleAddField} disabled={!newFieldKey.trim()}>
                            Add
                          </button>
                        </div>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}

            {viewMode === 'raw' && (
              <div className="data-editor-raw">
                <h3>Raw JSON</h3>
                <textarea
                  value={rawDraft}
                  onChange={e => setRawDraft(e.target.value)}
                  rows={16}
                />
                <button className="btn-secondary" onClick={applyRawDraft}>
                  Apply Raw JSON
                </button>
              </div>
            )}
          </div>
        )}
      </section>
    </div>
  );
}
