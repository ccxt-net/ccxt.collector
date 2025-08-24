# Releases

This folder stores release notes of CCXT.Collector by version. Each file corresponds to one version and follows the `MAJOR.MINOR.PATCH.md` naming convention.

## Index

- [2.1.7 - 2025-08-13](./2.1.7.md)
- [2.1.6 - 2025-08-12](./2.1.6.md)
- [2.1.5 - 2025-08-11](./2.1.5.md)
- [2.1.4](./2.1.4.md)
- [2.1.3](./2.1.3.md)
- [2.1.2](./2.1.2.md)
- [2.1.1](./2.1.1.md)
- [2.1.0](./2.1.0.md)
- [2.0.0](./2.0.0.md)

## Authoring guidelines

- Filename: `x.y.z.md`
- Header: `# x.y.z - YYYY-MM-DD`
- Sections (use only what you need):
  - Added
  - Changed
  - Fixed
  - Removed
  - Performance
  - Security
  - Migration
- If the date is not decided yet, start the draft with `# x.y.z (Unreleased)`.

## Tips

- Record changes concisely but with enough detail to reproduce.
- If relevant, append major issue or PR numbers at the end of the item in parentheses.
- If there is an impact on backward compatibility, explicitly mark it under “Migration” or “Breaking”.