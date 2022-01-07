### Introduction
For administrators its a tedious job to paste grids in once CLANG demanded a new sacrifice to be given. 

But to do that you have to paste the grid from workshop, or maybe even from a previous server iteration which results in OwnerID Changes. Also the "Keep Original Authorship" option in Space master is broken. 

When you still want to enforce your own PCU and Block limits you need to make sure PCUs are properly set. However PCUs can only be transferred if you are the owner of them and you need to transfer all connected and subgrids. 

This is what this plugin does

### Commands

#### Space Master Commands

- !freebuild 
 - Increases all limits for the user to be able to paste limit exceeding grids. Run again to disable

##### Transfer Commands

- !transfer &lt;PlayerName&gt;
 - Transfers all PCU and block ownership of the grid you are looking at over to the player.
- !transfer &lt;PlayerName&gt; &lt;GridName&gt;
 - Transfers all PCU and block ownership of the named grid to the player.
- !transferpcu &lt;PlayerName&gt;
 - Transfers only PCU of the grid you are looking at over to the player.
- !transferpcu &lt;PlayerName&gt; &lt;GridName&gt;
 - Transfers only PCU of the named grid to the player.
- !transferowner &lt;PlayerName&gt;
 - Transfers only block ownership of the grid you are looking at over to the player.
- !transferowner &lt;PlayerName&gt; &lt;GridName&gt;
 - Transfers only block ownership of the named grid to the player.
- !transfernobody [GrodName]
 - Transfers all PCU and block ownership of the grid with given name, or you are looking at over to nobody.
- !transferpcunobody [GridName]
 - Transfers only PCU of the grid with given name, or you are looking at over to nobody.
- !transferownernobody [GridName]
 - Transfers only block ownership of the grid with given name, or you are looking at over to nobody.
- !forcetransfer &lt;PlayerName&gt;
 - Transfers all PCU and block ownership of the grid you are looking at over to the player ignoring ownership.
- !forcetransfer &lt;PlayerName&gt; &lt;GridName&gt;
 - Transfers all PCU and block ownership of the named grid to the player ignoring limits.
- !forcetransferpcu &lt;PlayerName&gt;
 - Transfers only PCU of the grid you are looking at over to the player ignoring limits.
- !forcetransferpcu &lt;PlayerName&gt; &lt;GridName&gt;
 - Transfers only PCU of the named grid to the player ignoring limits.

When pasting some grids it may be possible they get some deformations or damage. To get rid of them easily there is also

##### Repair / Recharge Commands

- !repair
 - Repairs all damaged and deformed blocks of the grid you are looking at.
- !repair &lt;GridName&gt;
 - Repairs all damaged and deformed blocks of the given grid.
- !recharge &lt;battery|jumpdrive|o2tank|h2tank|tank&gt; &lt;percentage&gt;
 - Recharges/Fills the given blocks on the looked at grid to the given percentage
- !recharge grid &lt;gridname&gt; &lt;battery|jumpdrive|o2tank|h2tank|tank&gt; &lt;percentage&gt;
 - Recharges/Fills the given blocks on the given grid to the given percentage

Unfinished blocks will not be fixed unless they are deformed or damaged. So blocks that are left on grid stage on purpose will not be changed.

##### Protection Commands

- !protect [-allowDamage] [-allowEdit]
-- The grid the user looks at from damage or changes. If one of the two parameters is passed you can only protect a grid from damage and allow edits for example instead of both.
- !protect &lt;gridname&gt; [-allowDamage] [-allowEdit]
-- Protects a given grid from damage or changes. The other paremters do the same as the looked at variant.
- !unprotect [gridname] 
-- Clears the protection on a given or looked at grid set by the !protect command.

##### Cleanup Commands

- !gridcleanup &lt;days&gt; [-keepfactions]
 - Similar to !identity purge in essentials it deletes grids of inactive players, assigns ownership of shared grids to the biggest active owner and kicks inactives from the faction.
 - Unlike Essentials however it does not delete the Identity. Because identity purge will leave a then dead ID in the PCU ownership. Causing various issues depending on your world settings.
 - Auto reassinging PCU to the next player is a bad idea because that one player will most likely go over limits then. And just deleting blocks will destroy grids. Which is why just leaving the identity in there is probably the best solution. 
 - if -keepfactions is set the players wont be removed from the faction
- !deleteblocks buildby &lt;playername&gt; &lt;gridname&gt;
 - Deletes all blocks on specified grid build by the given player. Use "allgrids" to delete the blocks on ALL grids.
- !deleteblocks ownedby &lt;playername&gt; &lt;gridname&gt;
 - Deletes all blocks on specified grid owned by the given player. Use "allgrids" to delete the blocks on ALL grids.

##### Special Commands

- !seedcleanup all
 - Originally this command does not belong here as it has nothing to do with PCU or Grids, however, after running "!voxels cleanup asteroids true" all asteroids will be removed including their vx2 file. An asteroid being removed like that the game will never regenerate on its own as it thinks its been deleted on purpose or fully mined. 
 - To prevent that from happening and properly restore an asteroid this code removes all saved voxel seeds from the save game so after a restart everything will start being regenerated.
 - Basically its the same !sandbox clean does. But without messing with factions, identities, etc. 
 - **DOES NOT WORK FOR SE World Gen Plugin**

##### Statistic Commands

Finally we have commands for world analysis

!listblocks &lt;all|limited&gt; [-pcu] [-player=&lt;playerName&gt;] [-faction=&lt;factionTag&gt;]

- !listblocks all
 - Lists all blocks the world has.
- !listblocks all -pcu
 - Lists all blocks and also outputs the total pcu.
- !listblocks limited
 - similar to "!listblocks all" but shows only blocks that have block limits.
- !listblocks limited -pcu
 - similar to "!listblocks all -pcu" but shows only blocks that have block limits.
- !findblock &lt;blockpairname&gt; [-player=&lt;PlayerName&gt;] [-faction=&lt;FactionTag&gt;] [-groupby=&lt;player|faction|grid&gt;] [-metric=&lt;author|owner&gt;] [-findby=&lt;blockpair|type|subtype&gt;]
 - Shows which Faction or Player owns a specific block.
- !listallblocks [-metric=&lt;author|owner&gt;]
 - Outputs which player has how many of which blocks in the world. This allows for easier overview so you dont need to run !listblocks or !findblock multiple times. You can also export the results to CSV for easier access.

You can filter the results by adding -grid=Gridname, -faction=XYZ or -player=Playername. If any of these filters contains spaces you need to set the whole expression in "". Example:
!listblocks all -pcu "-player=My Player".

You can export the data to CSV using the -export=&lt;filename&gt; parameter. This will put the data into your Instances folder in ExportedStatistics.

Also sorting is possible with -orderby=blocks|name|pcu

#### Moderator Commands

##### Check Commands

- !checkowner
 - Lists the owners and amount of blocks they own in descending order of the grid you are looking at.
- !checkowner &lt;Gridname&gt;
 - Lists the owners and amount of blocks they own in descending order of the grid with the given name.
- !checkauthor
 - Lists the authors and amount of PCU they own in descending order of the grid you are looking at.
- !checkauthor &lt;Gridname&gt;
 - Lists the authors and amount of PCU they own in descending order of the grid with the given name.

##### List Commands

- !listgridsowner &lt;PlayerName&gt;
 - Lists all grids the player has block ownership on
- !listgridsowner &lt;PlayerName&gt; -gps
 - Lists all grids the player has block ownership on and adds gps to executing player
- !listgridsowner &lt;PlayerName&gt; -position
 - Lists all grids the player has block ownership also shows GPS in the window can be combined with -gps
- !listgridsowner &lt;PlayerName&gt; -id
 - Lists all grids the player has block ownership also shows EntityId of the grid
- !listgridsauthor &lt;PlayerName&gt;
 - Lists all grids the player has PCU ownership on
- !listgridsauthor &lt;PlayerName&gt; -gps
 - Lists all grids the player has PCU ownership on and adds gps to executing player
- !listgridsauthor &lt;PlayerName&gt; -position
 - Lists all grids the player has PCU ownership also shows GPS in the window can be combined with -gps
- !listgridsauthor &lt;PlayerName&gt; -id
 - Lists all grids the player has PCU ownership also shows EntityId of the grid
- !listgrids [-player=&lt;PlayerName&gt;] [-faction=&lt;FactionTag&gt;] [-orderby=&lt;pcu|name|blocks|faction|owner&gt;] [-id]
 - Lists all Grids and allows for filtering by player or faction and custom sorting.
- !listnoauthor [-gps] [-position] [-id]
 - Lists all grids which have at least 1 block without authorship.
- !checkusage player [-npc] [-online] [-faction=&lt;Tag&gt;] [-orderby=&lt;pcu|block|name&gt;] [-minblocks=&lt;number&gt;] [-minpcu=&lt;number&gt;]
 - Outputs the PCU and Blocks of ALL players on the server. Ordered by Block-Count by default. With min blocks and pcu 1.
- !checkusage faction [-npc] [-orderby=&lt;pcu|block|name&gt;] [-minblocks=&lt;number&gt;] [-minpcu=&lt;number&gt;]
 - Outputs the PCU and Blocks of ALL factions on the server. Ordered by Block-Count by default. With min blocks and pcu 1.

### Things to be aware of

- Similar to the game itself when transferring ownership to an other player connectors will unlock. So you need to ensure beforehand that nothing can get damaged in the process.

- Also when the grid you are transferring has no PCU owner it wont immediately be updated the player will see it in its info tab and block limits, but when looking with the welder at the block grid you just transferred it may be that it is not shown at the moment. Sadly there is some game features missing to do it right away. Transferring from Player to Player works without issue

- You can also transfer to players that are currently online.

- The plugin will respect PCU and block limits as well as block-type limits. The plugin will tell you what will be exceeded for the player if you perform the transfer and will not allow it. 
 - With the exception of the force commands. These will ignore the any limits. 

### Executing via Console
An online character is needed in order to run the commands that require looking at a grid. 

All commands that want you to pass a grid name can be run by console also. 

### Github
[See Here](https://github.com/LordTylus/SE-Torch-Plugin-ALE-PcuTransferrer)