Dungeon Builder Research Export Bundle (MVP)

Folder structure
- Each branch folder contains:
  - research_nodes.json
  - tables.json
  - cost_profiles.json
  - migration_map.json
  - lint_rules.json

tables.json
- Monsterology: Families, Species, Evolutions, StatProfiles, AIProfiles, UpkeepProfiles, plus Effect profile blocks embedded in those sheets
- Other branches: <BranchName>Effects

Global file
- all_research_nodes.json combines every branch ResearchNodes row and adds a branch field.

Notes
- IDs are stable. Do not rename IDs post ship. If needed, add a mapping entry in migration_map.json.
- This bundle is data only. Your Unity loader should validate ids and prerequisites before use.
