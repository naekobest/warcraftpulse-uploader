# CRITICAL

**`.claude/` darf WEDER committed noch in `.gitignore` aufgenommen werden.**
- Nicht committen: enthält lokale Gesprächsverläufe und Memory-Dateien
- Nicht in `.gitignore`: damit andere Entwickler ihre eigene `.claude/`-Konfiguration lokal verwenden können, ohne dass git sie ignoriert
- Stattdessen: in der globalen `~/.gitignore_global` eintragen (einmalig pro Rechner)
