---
topic: FQL-get-examples
---
<fql>
	from
		Resource
	where 
		meta.profile.empty().not()
	select
		Id: id,
		Profile: meta.profile
</fql>