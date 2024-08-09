//-----------------------------------------------------------------------------
// Torque Game Engine
// 
// Copyright (c) 2001 GarageGames.Com
// Portions Copyright (c) 2001 by Sierra Online, Inc.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Misc. server commands avialable to clients
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------

function serverCmdToggleCamera(%client)
{
   %control = %client.getControlObject();
   if (%control == %client.player)
   {
      %control = %client.camera;
      %control.mode = toggleCameraFly;
   }
   else
   {
      %control = %client.player;
      %control.mode = observerFly;
   }
   %client.setControlObject(%control);
}

function serverCmdDropPlayerAtCamera(%client)
{
   if ($Server::TestCheats)
   {
      %client.player.setTransform(%client.camera.getTransform());
      %client.player.setVelocity("0 0 0");
      %client.setControlObject(%client.player);
   }
}

function serverCmdDropCameraAtPlayer(%client)
{
   if ($Server::TestCheats)
   {
      %client.camera.setTransform(%client.player.getEyeTransform());
      %client.camera.setVelocity("0 0 0");
      %client.setControlObject(%client.camera);
   }
}

function serverCmdSetMarble( %client, %marbleIdx )
{
   %client.marbleIndex = %marbleIdx;
}


//-----------------------------------------------------------------------------

function serverCmdSuicide(%client)
{
   %client.player.kill("Suicide");
}   

function serverCmdPlayCel(%client,%anim)
{
   if (isObject(%client.player))
      %client.player.playCelAnimation(%anim);
}

function serverCmdPlayDeath(%client)
{
   if (isObject(%client.player))
      %client.player.playDeathAnimation();
}

function serverCmdSelectObject(%client, %mouseVec, %cameraPoint)
{
   //Determine how far should the picking ray extend into the world?
   %selectRange = 200;
   // scale mouseVec to the range the player is able to select with mouse
   %mouseScaled = VectorScale(%mouseVec, %selectRange);
   // cameraPoint = the world position of the camera
   // rangeEnd = camera point + length of selectable range
   %rangeEnd = VectorAdd(%cameraPoint, %mouseScaled);

   // Search for anything that is selectable � below are some examples
   %searchMasks = $TypeMasks::PlayerObjectType | $TypeMasks::CorpseObjectType |
      				$TypeMasks::ItemObjectType | $TypeMasks::TriggerObjectType;

   // Search for objects within the range that fit the masks above
   // If we are in first person mode, we make sure player is not selectable by setting fourth parameter (exempt
   // from collisions) when calling ContainerRayCast
   %player = %client.player;
   if ($firstPerson)
   {
	  %scanTarg = ContainerRayCast (%cameraPoint, %rangeEnd, %searchMasks, %player);
   }
   else //3rd person - player is selectable in this case
   {
	  %scanTarg = ContainerRayCast (%cameraPoint, %rangeEnd, %searchMasks);
   }

   // a target in range was found so select it
   if (%scanTarg)
   {
      %targetObject = firstWord(%scanTarg);

      %client.setSelectedObj(%targetObject);
   }
}

function serverCmdStartTalking(%client)
{
   
}

function serverCmdStopTalking(%client)
{
   
}

//----------------------------------------------------------------------------------
// Spectator Mode - Work in Progress
// Worked on by Connie and Yoshicraft224
//----------------------------------------------------------------------------------
// TODO list:
// Figure out how to make the timer not stop when you spectate (only visual bug)
// Figure out how to make the player not go OOB when they switch to spectator mode
// General code optimizations where possible & testing for bugs
//
// Separate the Player Orbiting Toggle function from the Player Orbit Cycling function
// (a.k.a. one button to toggle between free fly and orbiting someone while spectating,
// and another button for cycling between players while in orbit mode)
// Current function for both is currently: serverCmdPrepareSpecPlayer
//----------------------------------------------------------------------------------
// Code related to Spectator Mode is also present in:
// default.bind.cs
// game.cs (client/scripts)
// lobbyGui.gui
// lobbyPopupDlg.gui
// GamePauseGui.gui
// playGui.cs
//----------------------------------------------------------------------------------
// $Client::isspectating is the value stored locally, while %client.isspectating is the 
// value stored by the Server. Use %client.isspectating for server commands/server sided
// stuff, and $Client::isspectating for local commands.
//----------------------------------------------------------------------------------
// Keybinds:
// alt+c - toggle spectator mode on and off
// c - toggle between free fly and player orbiting modes - toggle between players in orbiting (to be separated)
//----------------------------------------------------------------------------------

// Set the value of isspectating locally - when in need to do stuff locally only
function clientCmdSpectateStatusLocally(%state)
{
   $Client::isspectating = %state;
}

// This handles the way the HUD is modified when you spectate or when you stop spectating
// There's probably a better way of handling this, but this gets the joj done ~Connie
function PlayGui::SpectatorHudTog(%this, %state)
{
   HUD_ShowPowerUp.setVisible(!%state);
   BoostBar.setVisible(!%state);   
}

// Called when the Game State goes to "Start". Will check for the client's "isspectating" value, and then act accordingly.
function StartStateSetSpectate()
{
   if ($Client::isspectating == true)
   {
      commandtoServer('ToggleSpecMode');
   }
}

// Used by the Popup when you ready up for setting the isspectating value.
function serverCmdSetSpectateStatus(%client, %state)
{
   %client.isspectating = %state;
   commandtoClient(%client, 'SpectateStatusLocally', %client.isspectating);
}

//Spectating Logic is here. ~Connie
function serverCmdToggleSpecMode(%client)
{
   %control = %client.getControlObject();

   if (!isObject(%client.speccamera))
   {
      CreateSpecCam(%client);
   }

   if (%control == %client.player)
   {
      %control = %client.speccamera;

      //Delete the Player Marble if they start spectating
      if (isObject(%client.player)) 
      {
         %client.player.delete();
         %client.player = "";

         %client.isspectating = true;
      }
   }
   else
   {
      %control = %client.player;

      //Respawn the Player Marble if the player decides to stop spectating
      %client.spawnPlayer();

      %client.isspectating = false;
   }

   commandtoClient(%client, 'SpectateStatusLocally', %client.isspectating);
   %client.setControlObject(%control);
}

function serverCmdPrepareSpecPlayer(%client)
{
   if (%client.playerspectating $= "" || %client.orderofplayerspectating $= "")
   {
      serverCmdSpecPlayer(%client);
   }
   else
   {
      serverCmdStopSpecPlayer(%client);
   }
}

//The function where someone can orbit around a player and spectate them that way.
function serverCmdSpecPlayer(%client)
{
   //Don't run this at all if you're not spectating
   if (%client.isspectating)
   {
      //Set the value.
      %client.orderofplayerspectating += 1;

      //Don't get a value greater than the number of players in the server
      if (%client.orderofplayerspectating >= ClientGroup.getCount())
      {
         %client.orderofplayerspectating = 0;
      }

      //Start from our value, check every player
      for (%i = %client.orderofplayerspectating; %i < ClientGroup.getCount(); %i++)
      {
         //If it doesn't exist, don't consider it.
         if (!isObject(ClientGroup.getObject(%i)))
         {
            continue;
         }

         if (ClientGroup.getObject(%i).isspectating)
         {
            continue; //Skip if spectating.
         }
         else
         {
            %client.orderofplayerspectating = %i;
            %client.playerspectating = ClientGroup.getObject(%i);
            break; //We found one, break the sequence!
         }

         //If we got to the last player in the hierarchy and he's spectating, do it all from 0
         if (%i == ClientGroup.getCount() && ClientGroup.getObject(%i).isspectating)
         {
            for (%j = 0; %j < ClientGroup.getCount(); %j++)
            {
               //If it doesn't exist, don't consider it.
               if (!isObject(ClientGroup.getObject(%j)))
               {
                  continue;
               }

               if (ClientGroup.getObject(%j).isspectating)
               {
                  continue; //Skip if spectating.. again.
               }
               else
               {
                  if (isObject(ClientGroup.getObject(%j))) 
                  {
                     %client.orderofplayerspectating = %j;
                     %client.playerspectating = ClientGroup.getObject(%j);
                     break; //Finally a player to spectate, break the sequence!
                  }
               }
            }
         }

         if (%client.playerspectating.isspectating || !isObject(%client.playerspectating))
         {
            //If we got here, there truly is no player to spectate.
            %client.orderofplayerspectating = "";
            %client.playerspectating = "";
            %client.speccamera.setFlyMode();
         }
      }

      //Pased all the checks? Good, spectate that mf'er
      if (%client.orderofplayerspectating !$= "" && %client.playerspectating !$= "") 
      {
         %client.speccamera.setOrbitMode(%client.playerspectating.player, %client.playerspectating.player.getTransform(), 0.5, 4.5, 4.5);
      }
   }
}

//The function to get out of orbiting a player when spectating.
function serverCmdStopSpecPlayer(%client)
{
   if (%client.isspectating)
   {
      //Reset values so when we start spectating other players in orbit mode again, we can rerun everything.
      if (%client.playerspectating !$= "" || %client.orderofplayerspectating !$= "")
      {
         %client.playerspectating = "";
         %client.orderofplayerspectating = "";
      }

      //Set 'em free.
      %client.speccamera.setFlyMode();
   }
}

//This will be the camera where we get to spectate someone
function CreateSpecCam(%client)
{
   %client.speccamera = new Camera() {
      dataBlock = Observer;
   };

   MissionCleanup.add(%client.newcamera);
}