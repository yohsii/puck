# puck
Puck - Content Management System

cms based on .net mvc5. uses sqlserver and lucene for storage.

**features**

- multi-site - multiple site roots mapped to different domains
- multilingual - associate languages with nodes so you can for example, have different site roots with different associated languages. this is recursive so you can associate different languages to nodes further down the hierarchy if necessary. each content node may also have translations, opening up the possibility for 1:1 and multi-site approaches to multilingual websites.
- strongly typed design - data querying is done in a strongly typed manner, making use of query expressions and a fluent api. templates are also strongly typed.
- not much to learn - models designed as regular poco decorated with attributes as you normally would with .net mvc
write models as poco classes or generate models in admin interface (using CSharpCodeProvider compiler)
- full text search - data storage is lucene based and you can set analyzers and field settings (analyze,store,ignore,keep casing) per property in your model
- spatial search
- image cropping using imageprocessor.web
- basic user permissions to grant/deny permissions to particular actions and limit access to content based on a start path
- hooks - you can transform data before it is indexed using attributes to modify how a field is indexed and how it is stored
- display modes - supports conditional template switching
- redirects - you can manage both 301/302 redirect mappings
- caching - customisable output caching. (per node-type or catch-all. also supports explicit exclusion for any particular node)
- streamlined pipeline - data retrieval is fast
- media - media is handled just like any other content, you can expose a HttpPostedFileBase property in any of your models and it will be properly bound. you can then use data transformer attributes to decide what should happen to that file before indexing takes place
- task api - supports one-off and recurring background custom tasks with editable parameters
