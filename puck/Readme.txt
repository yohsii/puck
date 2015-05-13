####    ####
	puck
    cms.
####    ####
			####hello :]####

-example custom route (signup) has been added to /app_start/routeconfig.cs
	- this is just to show an example of overriding puck routing to have request go to your controller first
	- check out signup controller in /controllers folder 
	- there is a corrosponding signup page in the cms
	- the request will go to the signup controller before being given back to puck
-requests are captured in the puck route
	-this points to /controllers/homecontroller for processing
	-this controller inherits puck.core.controllers.basecontroller and calls puck into action	

-there are bits of content in the cms by default, feel free to delete all of this stuff
	- same goes for the templates in the /views folder

-there is an example data transformer in the /transformers folder.
	- you can put your own transformers wherever you like. feel free to delete the folder

-viewmodels are Types, classes that represent Entities in your website. eg Section, Homepage, Folder etc
	- models are what viewmodels are composed of.
	- viewmodels might be composed entirely of simple types like char, string, int etc
	- but if you are composing your viewmodels with complex custom types, like SectionOptions or w/e,
		- you can stick these custom models in the model folder.

-media folder only exists as an example of how to handle images or other data.
	- there is an image model in the /models folder
	- it has a transformer attribute to control what happens when that model is saved
	- this transformer creates the media folder but you can handle images however you want
	- you can make your own image model and your own transformers etc

-global asax has some puck stuff going on
	- bootstraps puck.core
	- there is an example of using display modes for mobile / alternative templates
		- it's standard mvc but thought i'd leave it there to remind people
	- there are examples of how to register / unregister events for the lucene indexer
	- puck.core.Helpers.ApiHelper has more events to register to database operations

-localdb is used by default
	-it's code first so you can point to another database and schema will be generated
	-if you intend to use same database with another EF context, it won't work atm
	-i will update EF to allow multi tenancy for EF
	-for now, perhaps use a partial class to add stuff to the puck ef content
		- puck.core.Entities.PuckContext is a partial class for this reason

-puck.core.constants has some things you might want to know about
	-generatorvalues class is where you can add different types to the dynamic class generator
		-this class generator is in the developer section of cms interface
		-it's used when you want to use the cms without coding your models or templates
		-not really how i'd recommend using the cms		
	-pucksearcher, puckindexer, puckrepo can all be found here to get/save stuff in lucene and database, respectively
	-modify these values from your own bootstrap class, called from global.asax, at the bottom of Application_Start

-paths for email templates (for notifications) are in app_data

	contact me on bitbucket #yohsii
	https://bitbucket.org/yohsii/puck/wiki/Home