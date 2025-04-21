---
topic: Resource-Constraints
---

### Constraints
<fql>
    from StructureDefinition
    where url = %canonical
    select differential.element {
    path,
    join constraint {
        key,
        severity,
        human,
        expression
        }
    }
</fql>