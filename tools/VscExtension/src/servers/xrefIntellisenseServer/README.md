# QuickStart
* open File->Prefences->settings->User Settings, adding<br/> 
    "[markdown]": {<br/>
     "editor.quickSuggestions": {<br/>
       "other": true,<br/>
       "comments": false,<br/>
       "strings": false<br/>
     },<br/>
      "editor.quickSuggestionsDelay": 0<br/>
   }
* open a docfx project which must have a docfx.json, there exists an item 
xrefService which is a list containing urls for querying uids.

* when type a uid starts with '@' or '<xref:' then a sugessted completion list will be give which was returned from server side.

# Feature Details
* code completion
* highlight
* document link