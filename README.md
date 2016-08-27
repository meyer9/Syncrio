# Syncrio
A Scenario syncing mod for Kerbal Space Program.

   Installing Syncrio:

Client side: Put the folder named "Syncrio" in your Kerbal Space Program(KSP) "GameData" folder.
The first time you start KSP with Syncrio installed a disclaimer will pop up at the loading screen.
This disclaimer gives you a chance of disabling Syncrio before it sends any data to a server you connect to.
If the disclaimer is declined then Syncrio will be disabled!

Server side: Put "SyncrioServer.exe" and the other files that came with Syncrio in a folder named "Syncrio Server".
Then run "SyncrioServer.exe" once and wait til it says "Ready!".
-Note: When you run "SyncrioServer.exe" for the first time it will create several files.
Then close the server by typing "/exit" in the server's console window.
If you want to change any settings go to the folder named "Config" then open the "Settings.txt" file and follow the instructions.
Now if you haven't already port forwarded the port "7776", you will need to now. Unless you've change the port in "Settings.txt".
If you have changed the port you will need to port forward that port for Syncrio to work.
At this point Syncrio server side is ready to use.
-Note: The console commands for Syncrio can be viewed by typing "/help" in the server's console window.

   DarkMultiPlayer(DMP) Co-op Mode:
DMP Co-op Mode is a setting in the server's setting file/in the client's option window.
If activated on the server then it must be activated on the client as well and vise versa.
If activated on both the server and the client then the functions that both DMP and Syncrio have will be disabled in Syncrio.
If activated then on the client side join a Syncrio server first then a DMP server.
When activated joining a Syncrio server will not start a level, instead it will wait til you join a DMP server.
Do not set the Syncrio port to the same port as DMP! If you do you Will get an error!