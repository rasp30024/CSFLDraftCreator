# CSFLDraftCreator

## Introduction
This application was written in support of the CSFL DDSPFB23 league.  The application is written to take a list of draftees, randomize attributes, and format the file so this can be imported into the game.

## App Settings
The application settings is a file named "appsettings.json" configured in the application director.  

Please note that as this is JSON format slashes need to be escaped.  (example: c:\temp should be c:\\temp)

The following are the settings:
- AppLogLocation - Directory and filename of the application log.  
- ActivePlayerExportCSV_InputFile - Directory and filename of active player list exported from the game.  (csv)
- DraftClassCSV_InputFile - Directory and filename of the draft class to convert (csv)
- DraftUpdateJSON_OutputFile - Directory and filename of the converted file (json)
- PlayerSummaryHTML_OutputFile - Directory and filename of the positions percentile summary
- DraftUpdateCSV_InputFile - Directory and filename of the converted csv that was modified and used to be converted to json for import into the game (csv)
- TagPercentage - change a draftee will have a tag added
- TierDefinitions - Label of the Tier for each player.  
  - Id - Label used 
  - Order - used by the app to order the tiers from highest to lowest
  - Max - Highest percentile the attribute can go
  - Min - Lowest percentile the attribute can go
  - Skill - Lowest score the position skill can go
  - WE - Lowest work ethic the position Work Ethic can go 
  - Allow Tag - Determines if this tier can get traits added (true | false)
- PosTraits - Array of game traits that will be randomily selected for each position if a player is tagged.

## Draft Input File
The file used to import and convert is a csv and has the following headers:
- FirstName - Player first name
- LastName - Player last name
- College - college the player graduated 
- Age - age of player
- Height - player height (supports formats of inches or feet'inch ( i.e. 6'1)
- Weight - player weight
- Position - player position 
- Tier - The name of the tier to use for the player.  The tier names should match that from the TierDefinitions
- Trait - allows one to manually add a trait to the player instead of making it random.

## Active Player Input File
This file is generated inside the game by going to "Search" menu and the clicking on "Active Players" and then "Export"

## Menu Options
1. Create draft file json from draft class csv - This is the process to convert the draft class to a file that can be imported into the game.  It also creates a csv file that can be used to manually review the created file.
2. Get League Player Summary - This creates an HTML of a summary for each postion and thier respective attributes by percentile
3. Update draft export from csv - If one wanted to make changes to the output csv, then use this to reformat this file into json so it can be imported into the game.




 





