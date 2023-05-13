# CSFL Draft Creator

## Introduction
This application was written in support of the CSFL DDSPFB23 league.  The application is written to take a list of draftees, randomize attributes, and format the file so this can be imported into the game.

## Definitions
A few terms need to be defined to assist in later sections when discussing configurations.
- "Player" and "Draftee" - refers to the same object, which is the target to be imported into the game.
- "Ability" and "Attribute" - refers to the same objects, which defines a player's str, int, spe, etc.
- "Personality" - defines the players work ethic, competitiveness, etc.
- "Trait" and "Tag" - refers to the same object, which is the in game field called "Trait"
- "Tier" - is a configurable label which is assigned to players to determine how to randomize thier abilities
- "Style" - is a configurable label which is assigned to player to overlay another layer of randomizing player abilities, as well as personality.

## List values used by configs
- Attributes - "Str", "Agi", "Arm", "Spe", "Han", "Intel", "Acc", "PBl", "RBl", "Tck", "KDi", "KAc", "End"
- Personality - "Lea", "Wor", "Com", "TmPl", "Spor", "Soc", "Mny", "Sec", "Loy", "Win", "PT", "Home", "Mkt", "Mor"
- Position - "QB", "RB", "FB", "G", "T", "C", "TE", "WR", "CB", "LB", "DT", "DE", "FS", "SS", "K", "P"

## Input File Definition
Input file is a CSV format.  The following columns should be added:
- "FirstName" - Required 
- "LastName" - Required 
- "College" - If left blank a random college will be generated
- "Age" - If set to 0 a random age will be generated
- "Height" - If set to 0 a random Height will be generated based on the player's position
- "Weight" - If set to 0 a random Weight will be generated based on the player's position
- "Position" - player position *(see "List values used by configs" above)*
- "Tier" - Required.  This should match one of the tier names defined in the tier.csv config.
- "Trait" - Not required.  Should match that of the game trait defined later in this document.  Multiple Traits can be added by seperating with a pipe "|"
- "Style" - Not required.  If used must match a style name defined in the style.csv configuration file.  If this is left blank the style will randomly be selected based on weights set for styles matchin the player's position.

## Player Randomization Process
For each player
1. Information from the input file on the player is validated to be well formed.  
2. Traits are assigned to players based on input file, position, and configuration settings.
3. Styles are assigned to players based on input file, position, traits, and configuration settings.
4. Base personality is randomly selected for player
5. Secondary skills are randomly selected for player based on configuration settings and position.
6. Abilities are randomly selected based on position, tier, and style. (see next section for more information)
7. Work Ethic, Endurance, Competitiveness, and Primary skill randomized based on tier and configuration settings
8. Personality adjustment made based on style and traits

## Abilitiy Randomization
Player abilities are randomized based on configuration options.  
- Highest priority are "KeyAttributes" defined in the assigned style.
- Next priority are "PrimaryAttributes" defined in the PositionalAttributes config.
- Next priority are "SecondaryAttributes" defined in the PositionalAttributes config.
- Lowest priority are any attributes (excluding endurance) not listed in the priorities above.  

## Configurations
### appsettings.json 
This file is in json format and has basic settings for the applicaion.  The following are the fields.
- "UsePassedInPercentileChart": [true/false] - Allows for you to use an existing percentile chart instead of needing the app to use the game player export. This is helpful if one wanted to keep this chart static, or override a position and percentile to help inflate or deflate players being imported into the game.
- "AppLogLocation": [text] - defines what folder to output the log file.  Note the format should use two slashes as it is need to escape the character in json format.  Example: "c:\\temp\\csfldraft\\"
- "PercentileChartCSV_InputFile": [text] - defines what folder and the name of the file containing the percentile chart. Example: "c:\\temp\\csfldraft\\1978_Percentile_Chart.csv"
- "ActivePlayerExportCSV_InputFile": [text] - defines what folder and the name of the file containing the game's active player csv export. Example: "c:\\temp\\csfldraft\\1977_ActivePlayers.csv"
- "DraftClassCSV_InputFile": [text] - defines what folder and the name of the csv file containing the user created draftee list to be used by the app to generate a draft export file. Example: "c:\\temp\\csfldraft\\1978_Draft_Input.csv"
- "DraftUpdateJSON_OutputFile": [text] - defines what folder and the name of the json file to be created by the application for export into the game. Example: "c:\\temp\\csfldraft\\1978_Draft_Class.json". *(note: three files are actually created. json for export, csv for easy review,  csv audit file to see what was assigned to each player as for styles and attributes added by the app)*
- "PlayerSummaryHTML_OutputFile":  [text] - defines what folder and the name of the html file containing an html formatted percentile chart for each position.  Example: "c:\\temp\\csfldraft\\activeplayerpercentile.html" *(note: a second file formatted as csv is also created and can be used for passing in the PercentilChart)*
- "DraftUpdateCSV_InputFile": [text] - defines what folder and the name of a csv draft export file that one might want to covert into json for export into the game. Example: "c:\\temp\\csfldraft\\draftupdate.csv",
- "UpcomingDraftJSON_InputFile": [text] - defines what folder and the name of a json file exported from the game's export draft. Example: "c:\\temp\\csfldraft\\UpcomingDraft.json",
- "UpcomingDraftCSV_OutputFile": [text] - defines what folder and the name of the csv file created from converted game's export draft json file. Example: "c:\\temp\\csfldraft\\UpcomingDraft.csv",
- "PosTraitPercentage": [number 1 to 100] - If tier allows for adding traits, this defines the chance a position trait is added to the player.
- "PerTraitPercentage": [number 1 to 100] - If tier allows for adding traits, this defines the chance a personality trait is added to the player.
- "AddPersonalityTraitToPosTraitPercentage": [number 1 to 100] - If tier allows for adding traits, this defines the chance a personality trait is added to a player with an exisiting or added positional trait.
- "UseStyles": [true/false] - determines if styles are layered on top of positional settings.
- "MinEnhancePersonality": [number 1 to 100] - if configuration settings define a enhanced personality, this will set the minimum allowed for that personality setting.
- "MaxMufflePersonality":  [number 1 to 100] - if configuration settings define a muffle personality, this will set the max allowed for that personality setting.
- "MinEnhanceAttrPercentage": [number 1 to 100] - if configuration settings define a enhanced attribute, this will set the minimum percentile allowed for that attribute setting.
- "MaxMuffleAttrPercentage": [number 1 to 100] - if configuration settings define a muffle attribute, this will set the maximum percentile allowed for that attribute setting.
- "MaxAllowedForUnimportantSkills": [number 1 to 100] sets the maximum percentile for attributes not found in Key, Primary, or Seconday of that player.
- "MaxSecondarySkill": [number 1 to 100] sets the maximum value a secondary skill may be set.
- "SecondarySkillChance": [number 1 to 100] sets the percentage that a secondary skill will be addede to that player.

### Tiers.csv
This file defines the tiers that are used to set ranges used by positional attributes, and styles.
- TierName [text] - Name of the tier.  This is what would be entered in the input file.
- Order [number] - Used for sorting in app. Lower is higher
- KeyAttributeMin [number 1 to 100] defines the lowest percentile for attributes defined as Key in styles config.
- KeyAttributeMax [number 1 to 100] defines the highest percentile for attributes defined as Key in styles config.
- PrimaryAttributeMin [number 1 to 100] defines the lowest percentile for attributes defined as primary in positional config.
- PrimaryAttributeMax [number 1 to 100] defines the highest percentile for attributes defined as primary in positional config.
- SecondaryAttributeMin [number 1 to 100] defines the lowest percentile for attributes defined as secondary in positional config.
- SecondaryAttributeMax [number 1 to 100] defines the highest percentile for attributes defined as secondary in positional config.
- SkillMin [number 1 to 100] defines the lowest value for the player's primary skill.
- SkillMax [number 1 to 100] defines the highest value for the player's primary skill.
- WorkEthicMin [number 1 to 100] defines the lowest value for the player's work ethic.
- EnduranceMin [number 1 to 100] defines the lowest value for the player's endurance.
- CompetitivenessMin [number 1 to 100] defines the lowest value for the player's competiitiveness.
- AllowPositionalRandomTag [true or false] - determines if this tier is allowed to have Positional Traits randomly assigned
- AllowPersonalityRandomTag [true or false] - determines if this tier is allowed to have Personality Traits randomly assigned

### PositionalAttributes.csv
This file defines each position and the primary and key skills used by this position.  There should only be one row for each position
- PositionName [text] - Position this is representing.  *(see "List values used by configs" above)*
- PrimaryAttributes [List seperated by comma] - Sets the attributes that are important for that position.  Example: "Arm, Intel, Acc"
- SecondaryAttributes [List seperated by comma] - Sets the attributes that are helpful for that position. "Str, Agi, Spe"

### styles.csv
This file defines styles that can be assigned to players based on a position.
- StyleName [text] - Name of the style.  This is what would be entered in the input file or left blank to get a random style.
- ApplyToPosition [Text] - Single position this style can be applied.  *(see "List values used by configs" above)*
- RandomWeight [Number] - sets the weight to be used when randomly selecting a style for that position.  If one does not want to randomize styles then set this to 0.
- KeyAttributes [List seperated by comma] - Sets the attributes that are key for that positions style.  Example: "Arm, Acc"
- EnhancePersonalities [List seperated by comma] - Sets the personalities that should be enhanced for this style.
- MufflePersonalities [List seperated by comma] - Sets the personalities that should be muffled for this style.
- AllowedPositionalTraits [List seperated by comma] - Sets allowed traits that best match with the style of play. 

### traits.csv
This file defines traits and allows for adjusting players attributes/personalities to better match the trait.
- TraitName [text] -  Name of the trait.  This is what would be entered in the input file or left blank to get a random trait.
- RandomWeight [number] - Allows to configure adding more or less chance at randomizing the trait.  
- Type [position or personality] - Used to define how the app classifies the trait.
- AllowedPositions [List seperated by comma] - Define the positions allowed to get this trait.  Use "any" for traits that can be applied to any position.
- GameTraitName [text] - the actual name of the trait needed to be passed to the game.  This is case sensitive.
- EnhanceAttributes [List seperated by comma] - Sets the attributes that should be enhanced for this style.
- MuffleAttributes [List seperated by comma] - Sets the attributes that should be muffled for this style.
- EnhancePersonalities [List seperated by comma] - Sets the personalities that should be enhanced for this style.
- MufflePersonalities [List seperated by comma] - Sets the personalities that should be muffled for this style.

## Menu Options
1. Create draft file json from draft class csv - This is the process to convert the draft class to a file that can be imported into the game.  It also creates a csv file that can be used to manually review the created file.
2. Get League Player Summary - This creates an HTML of a summary for each postion and thier respective attributes by percentile
3. Update draft export from csv - If one wanted to make changes to the output csv, then use this to reformat this file into json so it can be imported into the game.
4. Converts an exported draft json draft file from the game into a csv file format


---
# LEGACY - VERSION 1.x
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
- PositionalTagPercentage - chance a draftee will have a tag positional tag added
- PersonalityTagPercetage - chance a draftee will have a tag personality tag added
- AddSecondTagPercentage - chance if a tag is added that another tag will be added
- TierDefinitions - Label of the Tier for each player.  
  - Id - Label used 
  - Order - used by the app to order the tiers from highest to lowest
  - KeyMax - Highest percentile the key attribute can go
  - KeyMin - Lowest percentile the key attribute can go
  - SecMax - Highest percentile the secondary attribute can go
  - SecMin - Lowest percentile the secondary attribute can go
  - Skill - Lowest score the position skill can go
  - WE - Lowest work ethic the position Work Ethic can go 
  - END - Lowest Endurance can be set
  - Allow Tag - Determines if this tier can get traits added (true | false)
- PosTraits - Array of game traits that will be randomily selected for each position if a player is tagged.
  - Position (e.g. QB, RB...) is referenced when PositionalTagPercentage is triggered
  - Personality is referenced when PersonalityTagPercetage is triggered
- x - used to display attributes spelling for reference in positional skills
- PostionalSkills - Array defining what attributes are key and secondary for the position.  If not listed they will be randomized based on the position percentile
  - Position - Defines the position
  - KeySkill - Defines when this attribute is randomized to use KeyMax and KeyMin 
  - SecondarySkill - Defines when this attribute is randomized to use SecMax and SecMin 

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




 





