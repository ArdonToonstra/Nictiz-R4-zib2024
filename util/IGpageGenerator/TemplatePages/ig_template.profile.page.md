---
topic: {id}
---

# {{{{page-title}}}}

@```
from StructureDefinition
where url = '{url}'
select 
Profile: '<a href="#'+ id.lower() + '">' + id + '</a>',
Description: description,
Version: version,
Status: status,
URL: url
order by Profile
```

Canonical: `{url}` 

<div>
  <div class="tab">
     <button class="tablinks active" onclick="openTab(event, 'Hybrid view')">Hybrid view</button>
     <button class="tablinks" onclick="openTab(event, 'Diff view')">Diff view</button>
     <button class="tablinks" onclick="openTab(event, 'Snapshot view')">Snapshot view</button>
     <button class="tablinks" onclick="openTab(event, 'Examples')">Examples</button>
     <button class="tablinks refs" onclick="location.href='https://simplifier.net/guide/hdbe-r4-cbb/Home/FHIR/HdBe-{cbb_profile_id}.page.md?version=current'" type="button">CBB profile</button>
  </div>

  <div id="Snapshot view" class="tabcontent">
    <br>
      {{{{tree:{url}, snapshot}}}}
  </div>

  <div id="Hybrid view" class="tabcontent" style="display:block">
    <br>
      {{{{tree:{url}, hybrid}}}}
  </div>

  <div id="Diff view" class="tabcontent">
    <br>
      {{{{tree:{url}, diff}}}}
  </div>
  
  <div id="Examples" class="tabcontent">
  Examples are provided in the future.
  </div>

</div>

## Terminology <a name="Terminology"></a>

This overview only provides terminology that is composed for this DCD and ValueSets that deviate from the terminology used in the CBB.

### ValueSets

@```
from StructureDefinition
where url.startsWith('{url}')

for differential.element
select
Path: path,
join binding.where(valueSet.exists())
{{
	Name: valueSet.substring((9 + valueSet.indexOf('ValueSet/'))),
	Strength: strength,
	URL: valueSet
	}}
```