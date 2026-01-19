#!/usr/bin/env node

/**
 * Data Normalization Script
 *
 * Transforms data from references/ to processed/ directory,
 * ensuring consistency with JSON schemas.
 *
 * Usage: node scripts/normalize.js
 */

const fs = require('fs');
const path = require('path');

const DATA_DIR = path.join(__dirname, '..', 'data');
const REFERENCES_DIR = path.join(DATA_DIR, 'references');
const PROCESSED_DIR = path.join(DATA_DIR, 'processed');
const SCHEMAS_DIR = path.join(DATA_DIR, 'schemas');

/**
 * Unit normalization - extracts only schema-defined fields
 */
function normalizeUnit(id, unit) {
  return {
    displayName: unit.displayName || id,
    maxHP: unit.maxHP,
    damage: unit.damage,
    moveSpeed: unit.moveSpeed,
    turnSpeed: unit.turnSpeed ?? 0.1,
    attackRange: unit.attackRange,
    radius: unit.radius,
    role: normalizeRole(unit.role),
    layer: unit.layer || 'Ground',
    canTarget: unit.canTarget || 'Ground',
    skills: unit.skills || []
  };
}

/**
 * Normalize role to schema-allowed values
 */
function normalizeRole(role) {
  const allowedRoles = ['Melee', 'Ranged', 'Tank', 'Support', 'MiniTank', 'GlassCannon', 'Swarm'];
  if (allowedRoles.includes(role)) {
    return role;
  }
  // Map non-standard roles to standard ones
  const roleMapping = {
    'DPS': 'Melee',
    'Healer': 'Support',
    'Bruiser': 'MiniTank'
  };
  return roleMapping[role] || role;
}

/**
 * Skill normalization - preserves all fields
 */
function normalizeSkill(id, skill) {
  const normalized = {
    type: skill.type
  };

  // Add type-specific fields
  switch (skill.type) {
    case 'DeathSpawn':
      normalized.spawnUnitId = skill.spawnUnitId;
      normalized.spawnCount = skill.spawnCount;
      if (skill.spawnRadius !== undefined) normalized.spawnRadius = skill.spawnRadius;
      if (skill.spawnUnitHP !== undefined) normalized.spawnUnitHP = skill.spawnUnitHP;
      break;
    case 'DeathDamage':
      normalized.damage = skill.damage;
      normalized.radius = skill.radius;
      if (skill.knockbackDistance !== undefined) normalized.knockbackDistance = skill.knockbackDistance;
      break;
    case 'Shield':
      normalized.maxShieldHP = skill.maxShieldHP;
      if (skill.blocksStun !== undefined) normalized.blocksStun = skill.blocksStun;
      if (skill.blocksKnockback !== undefined) normalized.blocksKnockback = skill.blocksKnockback;
      break;
    case 'ChargeAttack':
      normalized.triggerDistance = skill.triggerDistance;
      normalized.requiredChargeDistance = skill.requiredChargeDistance;
      if (skill.damageMultiplier !== undefined) normalized.damageMultiplier = skill.damageMultiplier;
      if (skill.speedMultiplier !== undefined) normalized.speedMultiplier = skill.speedMultiplier;
      break;
    case 'SplashDamage':
      normalized.radius = skill.radius;
      if (skill.damageFalloff !== undefined) normalized.damageFalloff = skill.damageFalloff;
      break;
    default:
      // Copy all fields for unknown types
      Object.assign(normalized, skill);
  }

  return normalized;
}

/**
 * Tower normalization
 */
function normalizeTower(id, tower) {
  return {
    displayName: tower.displayName || id,
    type: tower.type || 'Princess',
    maxHP: tower.maxHP,
    damage: tower.damage,
    attackSpeed: tower.attackSpeed,
    attackRadius: tower.attackRadius || tower.attackRange || 100,
    radius: tower.radius || 30,
    canTarget: tower.canTarget || 'Ground'
  };
}

/**
 * Process a single data file
 */
function processFile(filename, normalizer) {
  const inputPath = path.join(REFERENCES_DIR, filename);
  const outputPath = path.join(PROCESSED_DIR, filename);

  if (!fs.existsSync(inputPath)) {
    console.log(`  Skipping ${filename} (not found in references)`);
    return { skipped: true };
  }

  const inputData = JSON.parse(fs.readFileSync(inputPath, 'utf8'));
  const outputData = {};

  let processedCount = 0;
  for (const [id, item] of Object.entries(inputData)) {
    outputData[id] = normalizer(id, item);
    processedCount++;
  }

  // Ensure processed directory exists
  if (!fs.existsSync(PROCESSED_DIR)) {
    fs.mkdirSync(PROCESSED_DIR, { recursive: true });
  }

  fs.writeFileSync(outputPath, JSON.stringify(outputData, null, 2) + '\n');

  return {
    processed: processedCount,
    inputPath,
    outputPath
  };
}

/**
 * Main normalization function
 */
function normalize() {
  console.log('Data Normalization Pipeline');
  console.log('===========================\n');
  console.log(`Source: ${REFERENCES_DIR}`);
  console.log(`Target: ${PROCESSED_DIR}\n`);

  const results = [];

  // Process units
  console.log('Processing units.json...');
  const unitsResult = processFile('units.json', normalizeUnit);
  if (!unitsResult.skipped) {
    console.log(`  Normalized ${unitsResult.processed} units`);
    results.push({ file: 'units.json', ...unitsResult });
  }

  // Process skills
  console.log('Processing skills.json...');
  const skillsResult = processFile('skills.json', normalizeSkill);
  if (!skillsResult.skipped) {
    console.log(`  Normalized ${skillsResult.processed} skills`);
    results.push({ file: 'skills.json', ...skillsResult });
  }

  // Process towers
  console.log('Processing towers.json...');
  const towersResult = processFile('towers.json', normalizeTower);
  if (!towersResult.skipped) {
    console.log(`  Normalized ${towersResult.processed} towers`);
    results.push({ file: 'towers.json', ...towersResult });
  }

  console.log('\nNormalization complete!');
  console.log(`Processed ${results.length} files`);

  return results;
}

// Run if called directly
if (require.main === module) {
  try {
    normalize();
    process.exit(0);
  } catch (error) {
    console.error('Normalization failed:', error.message);
    process.exit(1);
  }
}

module.exports = { normalize, normalizeUnit, normalizeSkill, normalizeTower };
