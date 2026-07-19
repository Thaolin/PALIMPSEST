---
status: accepted
---

# Pin the World Grammar version per Chronicle

Each Chronicle saves the World Grammar version chosen at its creation. Changing
the default generator affects new Chronicles only; an existing Chronicle keeps
regenerating untouched territory with its pinned version until an explicit
migration exists. This prevents a software update from silently rewriting
unsaved places, at the cost of retaining old generators or deliberately
migrating their Chronicles. Saves created before versioning are explicitly
assigned legacy version 0, never the current default.
