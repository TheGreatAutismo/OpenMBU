//-----------------------------------------------------------------------------
// Torque Shader Engine 
// Copyright (C) GarageGames.com, Inc.
//-----------------------------------------------------------------------------

if ( isObject( moveMap ) )
   moveMap.delete();
new ActionMap(moveMap);

$screenShotMode = true;

function screenShotMode()
{
   $screenShotMode = !$screenShotMode;   

   RootShapeNameHud.setVisible($screenShotMode);
   PlayGui.setVisible($screenShotMode);
   PlayerListGui.setVisible($screenShotMode);
   
//   CenterMessageDlg.setVisible($screenShotMode);
//   HUD_ShowGem.setVisible($screenShotMode);
//   SUMO_NegSign.setVisible($screenShotMode);
//   GemsFoundHundred.setVisible($screenShotMode);
//   GemsFoundTen.setVisible($screenShotMode);
//   GemsFoundOne.setVisible($screenShotMode);
//   GemsSlash.setVisible($screenShotMode);
//   GemsTotalHundred.setVisible($screenShotMode);
//   GemsTotalTen.setVisible($screenShotMode);
//   GemsTotalOne.setVisible($screenShotMode);
//   Min_Ten.setVisible($screenShotMode);
//   Min_One.setVisible($screenShotMode);
//   MinSec_Colon.setVisible($screenShotMode);
//   Sec_Ten.setVisible($screenShotMode);
//   Sec_One.setVisible($screenShotMode);
//   MinSec_Point.setVisible($screenShotMode);
//   Sec_Tenth.setVisible($screenShotMode);
//   Sec_Hundredth.setVisible($screenShotMode);
//   PG_NegSign.setVisible($screenShotMode);
//   PG_BoostBar.setVisible($screenShotMode);
//   HelpTextForeground.setVisible($screenShotMode);
//   ChatTextForeground.setVisible($screenShotMode);
//   HUD_ShowPowerUp.setVisible($screenShotMode);
//
//   PlayerListGuiList.setVisible($screenShotMode);
//   RootShapeNameHud.setVisible($screenShotMode);
}

GlobalActionMap.bindCmd(keyboard, "alt p", "", "screenShotMode();");

function saveMegaMission()
{
   levelOneGroup.setHidden(true);
   MegaMissionGroup.setName("MissionGroup");
   MissionGroup.save("marble/data/missions/megaMission.mis");
   MissionGroup.setName("MegaMissionGroup");
   levelOneGroup.setHidden(false);
   error("Saved marble/data/missions/megaMission.mis");
}

GlobalActionMap.bindCmd(keyboard, "alt m", "", "saveMegaMission();");

function pauseToggle(%defaultItem)
{
   if( $GameEndNoAllowPause )
      return;
      
   if (ServerConnection.isMultiplayer)
      %defaultItem = 0;
         
   if( $gamePauseWanted )
   {
      Canvas.popDialog(GamePauseGui);
      
      // Make sure controler remove isn't up
      %doNotResume = false;
      
      for( %i = 0; %i < Canvas.getCount(); %i++ )
      {
         if( Canvas.getObject(%i) == ErrorUnplugGui.getId() ||
             Canvas.getObject(%i) == NetworkDisconnectGui.getId() )
         {
            %doNotResume = true;
         }
      }
      
      if( %doNotResume == false )
         resumeGame();
   }
   else
   {
      Canvas.pushDialog(GamePauseGui);
      PauseMenu.setSelectedIndex(%defaultItem);
      pauseGame();
   }
}

//------------------------------------------------------------------------------
// Non-remapable binds
//------------------------------------------------------------------------------

function escapeFromGame(%forcePreviewMode) // its ok for this to be empty, default is don't force preview mode
{  
   $Client::willfullDisconnect = true;
   
   %killMission = MissionLoadingGui.isAwake();
   // if we are hosting a multiplayer server, we just re-enter preview mode
   // without disconnecting
   if ($Server::Hosting)
   {
      // return to Lobby unless they want to back out to preview mode
      if (!%forcePreviewMode && $Server::UsingLobby && PlayGui.isAwake() && (!$Client::UseXBLiveMatchMaking || XBLiveIsSignedInGold()))
         enterLobbyMode();
      else
         enterPreviewMode();
   }
   // client's just disconnect
   else if ($Client::connectedMultiplayer) // this is also true for hosts so we check hosting first
      disconnect();
   // if play gui is awake, return to Level Preview.  Otherwise Quit
   else if (PlayGui.isAwake() || MissionLoadingGui.isAwake())
   {
      if ($EnableFMS)
      {
         // Delete everything
         if (isObject($ServerGroup))
            $ServerGroup.delete();
         $ServerGroup = new SimGroup(ServerGroup);
         
         if (isObject(MissionCleanup))
            MissionCleanup.delete();
            
         if (isObject(MissionGroup))
            MissionGroup.delete();
   
         // Flip back to SinglePlayerMode
         // The current mission should still be unhidden
         setSinglePlayerMode(true);
      }

      // need to end the current game
      commandToServer('SetWaitState');
      if (!isObject($disconnectGui))
         $disconnectGui = levelPreviewGui;
      RootGui.setContent($disconnectGui);
      
      if( isDemoLaunch() && XBLiveIsSignedIn() )
         UpsellGui.displayUpsell();
   }
   else
      quit();
      
   // this is needed for games that are aborted during mission load
   if (%killMission)
   {
      //should be safe to do this even if we aren't hosting
      endMission();
      destroyGame();
   }
}

//------------------------------------------------------------------------------
// Movement Keys
//------------------------------------------------------------------------------

$movementSpeed = 1; // m/s

function setSpeed(%speed)
{
   if(%speed)
      $movementSpeed = %speed;
}

function moveleft(%val)
{
   $mvLeftAction = %val;
}

function moveright(%val)
{
   $mvRightAction = %val;
}

function moveforward(%val)
{
   $mvForwardAction = %val;
}

function movebackward(%val)
{
   $mvBackwardAction = %val;
}

function moveup(%val)
{
   $mvUpAction = %val;
}

function movedown(%val)
{
   $mvDownAction = %val;
}

function turnLeft( %val )
{
   $mvYawRightSpeed = %val ? $Pref::Input::KeyboardTurnSpeed : 0;
}

function turnRight( %val )
{
   $mvYawLeftSpeed = %val ? $Pref::Input::KeyboardTurnSpeed : 0;
}

function panUp( %val )
{
   $mvPitchDownSpeed = %val ? $Pref::Input::KeyboardTurnSpeed : 0;
}

function panDown( %val )
{
   $mvPitchUpSpeed = %val ? $Pref::Input::KeyboardTurnSpeed : 0;
}

function getMouseAdjustAmount(%val)
{
   // based on a default camera fov of 90'
   return(%val * ($cameraFov / 90) * $pref::Input::MouseSensitivity);
}

function spewmovevars()
{
   echo("yaw:" SPC $mvYaw SPC "pitch:" SPC $mvPitch);
   $Debug::spewmovesched = schedule(500,0,spewmovevars);
}

function gamepadYaw(%val)
{
   if( $pref::invertXCamera )
      %val *= -1.0;
      
   // if we get a non zero val, the user must have moved the stick, so switch 
   // out of keyboard/mouse mode
   if (%val != 0.0)
      $mvDeviceIsKeyboardMouse = false;
      
   // stick events come in even when the user isn't using the gamepad, so make 
   // sure we don't slam the move if we don't think the user is using the gamepad
   if (!$mvDeviceIsKeyboardMouse)
   {
      %scale = (ServerConnection.gameState $= "wait") ? -0.1 : 3.14;
      $mvYawRightSpeed = -(%scale * %val);
   }
}

function gamepadPitch(%val)
{
   if( $pref::invertYCamera )
      %val *= -1.0;

   // if we get a non zero val, the user must have moved the stick, so switch 
   // out of keyboard/mouse mode
   if (%val != 0.0)
      $mvDeviceIsKeyboardMouse = false;

   // stick events come in even when the user isn't using the gamepad, so make 
   // sure we don't slam the move if we don't think the user is using the gamepad
   if (!$mvDeviceIsKeyboardMouse)
   {
      %scale = (ServerConnection.gameState $= "wait") ? -0.05 : 3.14;
      $mvPitchUpSpeed = %scale * %val;
   }
}

function mouseYaw(%val)
{
   if( $pref::invertXCamera )
      %val *= -1.0;
      
   $mvDeviceIsKeyboardMouse = true;

   $mvYaw += getMouseAdjustAmount(%val);
}

function mousePitch(%val)
{
   if( $pref::invertYCamera )
      %val *= -1.0;
      
   $mvDeviceIsKeyboardMouse = true;
      
   $mvPitch += getMouseAdjustAmount(%val);
}

function jumpOrStart(%val)
{
   //$mvTriggerCount2++;
   
   if (%val)
	   $mvTriggerCount2 = 1;
	else
	   $mvTriggerCount2 = 0;
}

function jumpOrPowerup( %val )
{
   // LTrigger
   if( %val > 0.0 )
      $mvTriggerCount2++;
   else
      $mvTriggerCount0++;
}

function moveXAxisL(%val)
{
   if (%val != 0.0)
      $mvDeviceIsKeyboardMouse = false;

   $mvXAxis_L = %val;
}

function moveYAxisL(%val)
{
   if (%val != 0.0)
      $mvDeviceIsKeyboardMouse = false;

   $mvYAxis_L = -%val;
}

function centercam(%val)
{
   $mvTriggerCount3++;
}

function cycleDebugPredTiles()
{
   $curTile++;
   
   if( $curTile > getNumPredTiles() )
      $curTile = 0;
      
   setDebugPredTile( $curTile );
}

//------------------------------------------------------------------------------
// Mouse Trigger
//------------------------------------------------------------------------------

function mouseFire(%val)
{
   $mvTriggerCount0++;
}

function altTrigger(%val)
{
   $mvTriggerCount1++;
}

//------------------------------------------------------------------------------
// Camera & View functions
//------------------------------------------------------------------------------

function toggleFreeLook( %val )
{
   if ( %val )
      $mvFreeLook = true;
   else
      $mvFreeLook = false;
}

function toggleFirstPerson(%val)
{
   if (%val)
   {
      $firstPerson = !$firstPerson;
   }
}

function toggleCamera(%val)
{
   if (%val)
      commandToServer('ToggleCamera');
}

//------------------------------------------------------------------------------
// Demo recording functions
//------------------------------------------------------------------------------

function startRecordingDemo( %val )
{
//    if ( %val )
//       beginDemoRecord();
   error( "** This function has temporarily been disabled! **" );
}

function stopRecordingDemo( %val )
{
//    if ( %val )
//       stopRecord();
   error( "** This function has temporarily been disabled! **" );
}

//------------------------------------------------------------------------------
// Helper Functions
//------------------------------------------------------------------------------

function dropCameraAtPlayer(%val)
{
   if (%val)
      commandToServer('dropCameraAtPlayer');
}

function dropPlayerAtCamera(%val)
{
   if (%val)
      commandToServer('DropPlayerAtCamera');
}

function bringUpOptions(%val)
{
   if(%val)
      Canvas.pushDialog(OptionsDlg);
}

//------------------------------------------------------------------------------
// Dubuging Functions
//------------------------------------------------------------------------------

$MFDebugRenderMode = 0;
function cycleDebugRenderMode(%val)
{
   if (!%val)
      return;

   if (getBuildString() $= "Debug")
   {
      if($MFDebugRenderMode == 0)
      {
         // Outline mode, including fonts so no stats
         $MFDebugRenderMode = 1;
         GLEnableOutline(true);
      }
      else if ($MFDebugRenderMode == 1)
      {
         // Interior debug mode
         $MFDebugRenderMode = 2;
         GLEnableOutline(false);
         setInteriorRenderMode(7);
         showInterior();
      }
      else if ($MFDebugRenderMode == 2)
      {
         // Back to normal
         $MFDebugRenderMode = 0;
         setInteriorRenderMode(0);
         GLEnableOutline(false);
         show();
      }
   }
   else
   {
      echo("Debug render modes only available when running a Debug build.");
   }
}

function pauseOrEscape()
{
	if (Canvas.getContent() == EditorGui.getId())
	{
		Editor.close("PlayGui");
		Canvas.setContent(RootGui);
		RootGui.setContent(PlayGui);
	} else if (PlayGui.isAwake())
	{
		if (!GamePauseGui.isAwake())
		   pauseToggle(0);
		// otherwise wait for them to make the selection...
	}
	else
	{
      //escapeFromGame(false,true);
      RootGui.contentGui.onB();
	}
}

function respawn(%val)
{
   if (%val)
      LocalClientConnection.respawnPlayer();
}

//------------------------------------------------------------------------------
// Bind input to commands
//------------------------------------------------------------------------------

// Global Action Map:

// keyboard
GlobalActionMap.bind(keyboard, "tilde", toggleConsole);
GlobalActionMap.bindCmd(keyboard, "alt enter", "", "toggleFullScreen();");
GlobalActionMap.bind(keyboard, "F9", cycleDebugRenderMode);
GlobalActionMap.bindCmd(keyboard, "escape", "", "pauseOrEscape();");
GlobalActionMap.bindCmd(keyboard, "ctrl 1", "", "setVideoMode(640,480,32,0);");
GlobalActionMap.bindCmd(keyboard, "ctrl 2", "", "setVideoMode(1280,720,32,0);");
GlobalActionMap.bindCmd(keyboard, "ctrl 3", "", "setVideoMode(1024,768,32,0);");
GlobalActionMap.bindCmd(keyboard, "ctrl 4", "", "setVideoMode(800,600,32,0);");
GlobalActionMap.bindCmd(keyboard, "alt t", "", "reloadMaterials();");

// MoveMap:
// keyboard
moveMap.bind(keyboard, "F8", dropCameraAtPlayer);
moveMap.bind(keyboard, "F7", dropPlayerAtCamera);
moveMap.bind( keyboard, a, moveleft );
moveMap.bind( keyboard, d, moveright );
moveMap.bind( keyboard, w, moveforward );
moveMap.bind( keyboard, s, movebackward );
moveMap.bind( keyboard, space, jumpOrStart );
moveMap.bind(keyboard, "alt c", toggleCamera);
//moveMap.bind( keyboard, F3, startRecordingDemo );
//moveMap.bind( keyboard, F4, stopRecordingDemo );
//moveMap.bindCmd( keyboard, o, "", "pauseToggle(0);" );
//moveMap.bindCmd( keyboard, p, "", "pauseToggle(1);" );

// mouse
moveMap.bind( mouse, button0, mouseFire );
moveMap.bind( mouse, button1, altTrigger );
moveMap.bind( mouse, xaxis, mouseYaw );
moveMap.bind( mouse, yaxis, mousePitch );

// gamepad
if (isPCBuild())
{
   // set up some defaults
   moveMap.bind( xinput, btn_x, altTrigger );
   moveMap.bind( xinput, btn_a, jumpOrStart );
   moveMap.bind( xinput, btn_b, mouseFire );
   moveMap.bind( xinput, triggerr, mouseFire );
   moveMap.bind( xinput, triggerl, jumpOrStart );
   moveMap.bind( xinput, zaxis, jumpOrPowerup );
   moveMap.bind( xinput, btn_r, altTrigger );
   moveMap.bind( xinput, btn_l, altTrigger );

   moveMap.bindCmd( xinput, btn_back, "pauseToggle(1);", "" );
   moveMap.bindCmd( xinput, btn_start, "pauseToggle(0);", "" );
   moveMap.bindCmd( xinput, btn_y, "togglePlayerListLength();", "" );

   moveMap.bind( xinput, thumblx, "DN", "-0.23 0.23", moveXAxisL );
   moveMap.bind( xinput, thumbly, "DN", "-0.23 0.23", moveYAxisL );
   moveMap.bind( xinput, thumbrx, "D", "-0.23 0.23", gamepadYaw );
   moveMap.bind( xinput, thumbry, "D", "-0.23 0.23", gamepadPitch );

   moveMap.bind(keyboard, m, spawnStupidMarble);
   moveMap.bind( xinput, btn_y, spawnStupidMarble );
   
   
   moveMap.bind(keyboard, r, respawn);

   if (strstr(getGamepadName(), "Logitech") != -1)
   {
      error("Binding for" SPC getGamepadName() SPC "controller");
      moveMap.bind( gamepad, zaxis, "D", "-0.23 0.23", gamepadYaw );
      moveMap.bind( gamepad, rzaxis,  "D", "-0.23 0.23", gamepadPitch );
   }
}
else // xbox
{
   // gamepad
   //GlobalActionMap.bindCmd( gamepad, lshoulder, "cycleDebugPredTiles();", "" );

   moveMap.bind( gamepad, btn_x, altTrigger );
   moveMap.bind( gamepad, btn_a, jumpOrStart );
   moveMap.bind( gamepad, btn_b, mouseFire );
   moveMap.bind( gamepad, rtrigger, mouseFire );
   moveMap.bind( gamepad, ltrigger, jumpOrStart );
   moveMap.bind( gamepad, rshoulder, altTrigger );
   moveMap.bind( gamepad, lshoulder, altTrigger );

   moveMap.bindCmd( gamepad, back, "pauseToggle(1);", "" );
   moveMap.bindCmd( gamepad, start, "pauseToggle(0);", "" );
   moveMap.bindCmd( gamepad, btn_y, "togglePlayerListLength();", "" );

   moveMap.bind( gamepad, xaxis, "DN", "-0.23 0.23", moveXAxisL );
   moveMap.bind( gamepad, yaxis, "DN", "-0.23 0.23", moveYAxisL );
   moveMap.bind( gamepad, rxaxis, "D", "-0.23 0.23", gamepadYaw );
   moveMap.bind( gamepad, ryaxis, "D", "-0.23 0.23", gamepadPitch );
   //moveMap.bind( gamepad, zaxis, "D", "-0.23 0.23", gamepadYaw );
   //moveMap.bind( gamepad, rzaxis,  "D", "-0.23 0.23", gamepadPitch );

   //moveMap.bind( gamepad, btn_y, spawnStupidMarble );
}

//-------------------------------------------------
// Script code for doing profiling while holding
// down ctrl-F3 (bind only in pc build).
//-------------------------------------------------

function doProfile(%val)
{
   if (%val || isRelease())
   {
      // key down -- start profile
      echo("Starting profile session...");
      profilerDump();
      profilerEnable(true);
   }
   else
   {
      // key up -- finish off profile
      echo("Ending profile session...");
      profilerDump();
      profilerEnable(false);
   }
}

if (isPCBuild())
{
   GlobalActionMap.bind(keyboard, "ctrl F3", doProfile);
}

