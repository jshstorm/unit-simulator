#!/usr/bin/env node

/**
 * Data Build Pipeline Script
 *
 * Executes the complete data pipeline:
 * 1. Normalize (references/ -> processed/)
 * 2. Validate (schema validation)
 * 3. Diff (show changes)
 *
 * Usage: node scripts/build.js [--verbose] [--skip-normalize]
 */

const { execSync } = require('child_process');
const path = require('path');

const VERBOSE = process.argv.includes('--verbose') || process.argv.includes('-v');
const SKIP_NORMALIZE = process.argv.includes('--skip-normalize');

const ROOT_DIR = path.join(__dirname, '..');

/**
 * Execute a command and return result
 */
function runCommand(command, description, options = {}) {
  console.log(`\n${'='.repeat(60)}`);
  console.log(`Step: ${description}`);
  console.log(`${'='.repeat(60)}`);
  console.log(`> ${command}\n`);

  try {
    const output = execSync(command, {
      cwd: ROOT_DIR,
      encoding: 'utf8',
      stdio: options.silent ? 'pipe' : 'inherit'
    });
    return { success: true, output };
  } catch (error) {
    return {
      success: false,
      error: error.message,
      output: error.stdout,
      stderr: error.stderr
    };
  }
}

/**
 * Main build pipeline
 */
async function build() {
  console.log('Data Build Pipeline');
  console.log('===================');
  console.log(`Started at: ${new Date().toISOString()}`);
  console.log(`Working directory: ${ROOT_DIR}`);
  console.log(`Options: verbose=${VERBOSE}, skip-normalize=${SKIP_NORMALIZE}`);

  const results = {
    normalize: null,
    validate: null,
    diff: null
  };

  // Step 1: Normalize
  if (!SKIP_NORMALIZE) {
    results.normalize = runCommand(
      'node scripts/normalize.js',
      'Normalize data (references/ -> processed/)'
    );

    if (!results.normalize.success) {
      console.error('\n[ERROR] Normalization failed!');
      console.error('Build aborted.');
      process.exit(1);
    }
  } else {
    console.log('\n[SKIPPED] Normalization (--skip-normalize flag)');
  }

  // Step 2: Validate
  results.validate = runCommand(
    'npm run data:validate:processed',
    'Validate processed data against schemas'
  );

  if (!results.validate.success) {
    console.error('\n[ERROR] Schema validation failed!');
    console.error('Build aborted. Fix validation errors before proceeding.');
    process.exit(1);
  }

  // Step 3: Diff
  const diffCmd = VERBOSE ? 'node scripts/diff.js --verbose' : 'node scripts/diff.js';
  results.diff = runCommand(
    diffCmd,
    'Show data changes (references vs processed)'
  );

  // Summary
  console.log(`\n${'='.repeat(60)}`);
  console.log('Build Summary');
  console.log(`${'='.repeat(60)}`);

  const steps = [
    { name: 'Normalize', result: results.normalize, skipped: SKIP_NORMALIZE },
    { name: 'Validate', result: results.validate, skipped: false },
    { name: 'Diff', result: results.diff, skipped: false }
  ];

  for (const step of steps) {
    if (step.skipped) {
      console.log(`  ${step.name}: SKIPPED`);
    } else if (step.result?.success) {
      console.log(`  ${step.name}: OK`);
    } else {
      console.log(`  ${step.name}: FAILED`);
    }
  }

  console.log(`\nCompleted at: ${new Date().toISOString()}`);
  console.log('Build successful!');

  return results;
}

// Run if called directly
if (require.main === module) {
  build()
    .then(() => process.exit(0))
    .catch((error) => {
      console.error('Build failed:', error.message);
      process.exit(1);
    });
}

module.exports = { build };
