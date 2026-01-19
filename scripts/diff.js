#!/usr/bin/env node

/**
 * Data Diff Script
 *
 * Compares references/ and processed/ directories to show changes.
 *
 * Usage: node scripts/diff.js [--verbose]
 */

const fs = require('fs');
const path = require('path');

const DATA_DIR = path.join(__dirname, '..', 'data');
const REFERENCES_DIR = path.join(DATA_DIR, 'references');
const PROCESSED_DIR = path.join(DATA_DIR, 'processed');

const VERBOSE = process.argv.includes('--verbose') || process.argv.includes('-v');

/**
 * Deep comparison of two objects
 */
function deepEqual(obj1, obj2) {
  if (obj1 === obj2) return true;
  if (typeof obj1 !== typeof obj2) return false;
  if (typeof obj1 !== 'object' || obj1 === null || obj2 === null) return false;

  const keys1 = Object.keys(obj1);
  const keys2 = Object.keys(obj2);

  if (keys1.length !== keys2.length) return false;

  for (const key of keys1) {
    if (!keys2.includes(key)) return false;
    if (!deepEqual(obj1[key], obj2[key])) return false;
  }

  return true;
}

/**
 * Get differences between two objects
 */
function getDiff(refObj, procObj, path = '') {
  const diffs = [];

  const allKeys = new Set([...Object.keys(refObj || {}), ...Object.keys(procObj || {})]);

  for (const key of allKeys) {
    const fullPath = path ? `${path}.${key}` : key;
    const refVal = refObj?.[key];
    const procVal = procObj?.[key];

    if (refVal === undefined && procVal !== undefined) {
      diffs.push({ type: 'added', path: fullPath, newValue: procVal });
    } else if (refVal !== undefined && procVal === undefined) {
      diffs.push({ type: 'removed', path: fullPath, oldValue: refVal });
    } else if (typeof refVal === 'object' && refVal !== null && typeof procVal === 'object' && procVal !== null) {
      if (Array.isArray(refVal) && Array.isArray(procVal)) {
        if (!deepEqual(refVal, procVal)) {
          diffs.push({ type: 'changed', path: fullPath, oldValue: refVal, newValue: procVal });
        }
      } else if (!Array.isArray(refVal) && !Array.isArray(procVal)) {
        diffs.push(...getDiff(refVal, procVal, fullPath));
      } else {
        diffs.push({ type: 'changed', path: fullPath, oldValue: refVal, newValue: procVal });
      }
    } else if (refVal !== procVal) {
      diffs.push({ type: 'changed', path: fullPath, oldValue: refVal, newValue: procVal });
    }
  }

  return diffs;
}

/**
 * Format a value for display
 */
function formatValue(val) {
  if (typeof val === 'object') {
    return JSON.stringify(val);
  }
  return String(val);
}

/**
 * Compare two data files
 */
function compareFiles(filename) {
  const refPath = path.join(REFERENCES_DIR, filename);
  const procPath = path.join(PROCESSED_DIR, filename);

  const result = {
    filename,
    exists: { references: false, processed: false },
    entries: { references: 0, processed: 0, added: 0, removed: 0, modified: 0 },
    changes: []
  };

  if (fs.existsSync(refPath)) {
    result.exists.references = true;
  }
  if (fs.existsSync(procPath)) {
    result.exists.processed = true;
  }

  if (!result.exists.references && !result.exists.processed) {
    return result;
  }

  const refData = result.exists.references
    ? JSON.parse(fs.readFileSync(refPath, 'utf8'))
    : {};
  const procData = result.exists.processed
    ? JSON.parse(fs.readFileSync(procPath, 'utf8'))
    : {};

  result.entries.references = Object.keys(refData).length;
  result.entries.processed = Object.keys(procData).length;

  const allIds = new Set([...Object.keys(refData), ...Object.keys(procData)]);

  for (const id of allIds) {
    const refEntry = refData[id];
    const procEntry = procData[id];

    if (!refEntry && procEntry) {
      result.entries.added++;
      result.changes.push({
        id,
        type: 'added',
        details: VERBOSE ? procEntry : null
      });
    } else if (refEntry && !procEntry) {
      result.entries.removed++;
      result.changes.push({
        id,
        type: 'removed',
        details: VERBOSE ? refEntry : null
      });
    } else {
      const diffs = getDiff(refEntry, procEntry, id);
      if (diffs.length > 0) {
        result.entries.modified++;
        result.changes.push({
          id,
          type: 'modified',
          diffs
        });
      }
    }
  }

  return result;
}

/**
 * Print diff results
 */
function printResults(results) {
  console.log('Data Diff Report');
  console.log('================\n');

  let totalChanges = 0;

  for (const result of results) {
    const hasChanges = result.changes.length > 0;
    const statusIcon = hasChanges ? '!' : '=';

    console.log(`[${statusIcon}] ${result.filename}`);
    console.log(`    References: ${result.entries.references} entries`);
    console.log(`    Processed:  ${result.entries.processed} entries`);

    if (hasChanges) {
      console.log(`    Changes:`);
      console.log(`      + Added:    ${result.entries.added}`);
      console.log(`      - Removed:  ${result.entries.removed}`);
      console.log(`      ~ Modified: ${result.entries.modified}`);

      if (VERBOSE) {
        console.log(`\n    Details:`);
        for (const change of result.changes) {
          if (change.type === 'added') {
            console.log(`      + ${change.id} (new entry)`);
          } else if (change.type === 'removed') {
            console.log(`      - ${change.id} (removed)`);
          } else if (change.type === 'modified') {
            console.log(`      ~ ${change.id}:`);
            for (const diff of change.diffs) {
              const diffPath = diff.path.replace(`${change.id}.`, '');
              if (diff.type === 'added') {
                console.log(`          + ${diffPath}: ${formatValue(diff.newValue)}`);
              } else if (diff.type === 'removed') {
                console.log(`          - ${diffPath}: ${formatValue(diff.oldValue)}`);
              } else {
                console.log(`          ~ ${diffPath}: ${formatValue(diff.oldValue)} -> ${formatValue(diff.newValue)}`);
              }
            }
          }
        }
      }
    } else {
      console.log(`    No changes`);
    }

    totalChanges += result.changes.length;
    console.log('');
  }

  console.log('Summary');
  console.log('-------');
  console.log(`Total files compared: ${results.length}`);
  console.log(`Total entries with changes: ${totalChanges}`);

  return totalChanges;
}

/**
 * Main diff function
 */
function diff() {
  const filesToCompare = ['units.json', 'skills.json', 'towers.json'];
  const results = [];

  for (const filename of filesToCompare) {
    results.push(compareFiles(filename));
  }

  const totalChanges = printResults(results);

  return { results, totalChanges };
}

// Run if called directly
if (require.main === module) {
  try {
    const { totalChanges } = diff();
    // Exit with 0 even if there are changes (diff is informational)
    process.exit(0);
  } catch (error) {
    console.error('Diff failed:', error.message);
    process.exit(1);
  }
}

module.exports = { diff, compareFiles };
