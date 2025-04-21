---
topic: Resource-Search-Parameters
---

### Search Parameters
<fql>
    from SearchParameter
    where base contains '%subject'
    select name, url, type, description, expression
</fql>
