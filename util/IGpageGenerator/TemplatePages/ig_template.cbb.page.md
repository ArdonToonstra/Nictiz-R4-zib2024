---
topic: {topic}
{lang}
---

<div style="float:right;width:85px;padding:10px;margin:10">
<p>{EN_page_link}  {NL_page_link}  {FR_page_link}<p>
</div>

# {lm_name}

{draft_cbb_disclaimer}

@```
from StructureDefinition
where url = '{lm_url}'
select 
CBB: id,
{fql_cbb_desc} 
Version: version,
Status: status
```

<div>
  <div class="tab">
     <button class="tablinks active" onclick="openTab(event, 'Rendered view')">Rendered view</button>
     <button class="tablinks" onclick="openTab(event, 'Table view')">Table view</button>
     <button class="tablinks" onclick="openTab(event, 'Detailed descriptions')">Detailed Descriptions</button>
     <button class="tablinks" onclick="openTab(event, 'Example')">Example</button>
     <button class="tablinks" onclick="openTab(event, 'Zib diff')">Zib diff</button>
     {fhir_profile_link}
  </div>

  <div id="Rendered view" class="tabcontent" style="display:block">
    <br>
      {{{{tree:{lm_url} , snapshot}}}}
  </div>

  <div id="Table view" class="tabcontent">
    <br>
      {{{{table:{lm_url} }}}}
  </div>

  <div id="Detailed descriptions" class="tabcontent">
   <br>
      {{{{dict:{lm_url} }}}}
  </div>

  <div id="Example" class="tabcontent">
      {{{{render:logical models/LogicalModel-{lm_id}.example.md}}}}
  </div>

  <div id="Zib diff" class="tabcontent">
      {{{{render:logical models/LogicalModel-{lm_id}.doc.md}}}}
  </div>

</div>

<br/><br/> 

## Terminology Bindings

@```
from StructureDefinition
where url = '{lm_url}'
for differential.element
select
Path: path.substring((1 + path.indexOf('.'))),
join binding.where(valueSet.exists())
      {{ 
        Name: valueSet.substring((9 + valueSet.indexOf('ValueSet/'))),
        Strength: strength,
        URL: valueSet
        }}
```  