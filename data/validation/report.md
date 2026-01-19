# Schema Validation Report

Generated: 2026-01-19

## Validation Summary

### References Directory
| Schema File | Data File | Status | Issues |
|-------------|-----------|--------|--------|
| `unit-stats.schema.json` | `data/references/units.json` | ✅ VALID | 0 |
| `skill-reference.schema.json` | `data/references/skills.json` | ✅ VALID | 0 |
| `tower-reference.schema.json` | `data/references/towers.json` | ✅ VALID | 0 |
| `wave-definition.schema.json` | `data/references/waves.json` | ⚠️ N/A | File does not exist yet |

### Processed Directory
| Schema File | Data File | Status | Issues |
|-------------|-----------|--------|--------|
| `unit-stats.schema.json` | `data/processed/units.json` | ✅ VALID | 0 |
| `skill-reference.schema.json` | `data/processed/skills.json` | ✅ VALID | 0 |
| `tower-reference.schema.json` | `data/processed/towers.json` | ✅ VALID | 0 |

## Validation Details

### units.json
- **Total Units**: 13
- **Schema Version**: JSON Schema Draft-07
- **Result**: All units conform to schema definition

### skills.json
- **Total Skills**: 8
- **Schema Version**: JSON Schema Draft-07
- **Result**: All skills conform to schema definition
- **Skill Types Validated**: DeathSpawn, DeathDamage, Shield, ChargeAttack, SplashDamage

### towers.json
- **Total Towers**: 2
- **Schema Version**: JSON Schema Draft-07
- **Result**: All towers conform to schema definition

## Data Pipeline (M2.2)

The data transformation pipeline has been implemented with the following scripts:

| Script | Command | Description |
|--------|---------|-------------|
| `normalize.js` | `npm run data:normalize` | Transform references/ → processed/ |
| `diff.js` | `npm run data:diff` | Compare references vs processed |
| `build.js` | `npm run data:build` | Full pipeline (normalize + validate + diff) |

### Pipeline Workflow
```
references/*.json → normalize.js → processed/*.json → ajv validate → diff.js → Build Complete
```

### Data Diff Summary (latest)
- **units.json**: 5 entries with changes (field normalization)
- **skills.json**: No changes
- **towers.json**: No changes

## Notes

- All schemas have been updated to JSON Schema Draft-07 for compatibility with ajv-cli
- Wave definition schema is defined but no data file exists yet (planned for future)
- Validation performed using ajv-cli v5.0.0
- Build pipeline fails on schema validation errors (exit code 1)

## Next Steps

1. ✅ Integrate validation into CI/CD pipeline (completed in M2.1)
2. Implement M2.3: Runtime data loader
3. Create waves.json data file
