/*
Copyright (C) 2024 Dea Brcka

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Drawing;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("css_addstartzone", "Pridá počiatočnú zónu do súboru mapdata.json")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddStartZoneCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (playerTimers[player!.Slot].IsAddingStartZone == true)
            {
                playerTimers[player.Slot].IsAddingStartZone = false;
                playerTimers[player.Slot].IsAddingEndZone = false;
                playerTimers[player.Slot].StartZoneC2 = $"{player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin.X} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Y} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Z}";
                player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Grey}Štartovacia zóna nastavená...");
            }
            else
            {
                playerTimers[player.Slot].StartZoneC1 = $"{player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin.X} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Y} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Z}";
                playerTimers[player.Slot].StartZoneC2 = "";
                playerTimers[player.Slot].IsAddingStartZone = true;
                playerTimers[player.Slot].IsAddingEndZone = false;
                player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Default} Teraz choďte do rohu opačnej zóny");
                player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Default} a napíš {primaryChatColor}!addstartzone {ChatColors.Default}znova");
            }
        }

        [ConsoleCommand("css_addendzone", "Pridá koncovú zónu do súboru mapdata.json")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddEndZoneCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (playerTimers[player!.Slot].IsAddingEndZone == true)
            {
                playerTimers[player.Slot].IsAddingStartZone = false;
                playerTimers[player.Slot].IsAddingEndZone = false;
                playerTimers[player.Slot].EndZoneC2 = $"{player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin.X} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Y} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Z}";
                player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Grey}Koncová zóna nastavená...");
            }
            else
            {
                playerTimers[player.Slot].EndZoneC1 = $"{player.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin.X} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Y} {player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin.Z}";
                playerTimers[player.Slot].EndZoneC2 = "";
                playerTimers[player.Slot].IsAddingStartZone = false;
                playerTimers[player.Slot].IsAddingEndZone = true;
                player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Default} Teraz choďte do rohu opačnej zóny");
                player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Default} a napíš {primaryChatColor}!addendzone {ChatColors.Default}znova");
            }
        }

        [ConsoleCommand("css_addrespawnpos", "Pridá pozíciu Respawn do súboru mapdata.json")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void AddRespawnPosCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            // Get the player's current position
            Vector currentPosition = player!.Pawn.Value!.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);

            // Convert position
            string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
            playerTimers[player.Slot].RespawnPos = positionString;
            player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Default} Respawn pridaný!");
        }

        [ConsoleCommand("css_savezones", "Saves defined zones")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SaveZonesCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAllowedPlayer(player)) return;

            if (playerTimers[player!.Slot].EndZoneC1 == null || playerTimers[player.Slot].EndZoneC2 == null || playerTimers[player.Slot].StartZoneC1 == null || playerTimers[player.Slot].StartZoneC2 == null || playerTimers[player.Slot].RespawnPos == null)
            {
                player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Red} Uistite sa, že ste vykonali všetky 3 kroky zónovania (!addstartzone, !addendzone, !addrespawnpos)");
                return;
            }

            MapInfo newMapInfo = new()
            {
                MapStartC1 = playerTimers[player.Slot].StartZoneC1,
                MapStartC2 = playerTimers[player.Slot].StartZoneC2,
                MapEndC1 = playerTimers[player.Slot].EndZoneC1,
                MapEndC2 = playerTimers[player.Slot].EndZoneC2,
                RespawnPos = playerTimers[player.Slot].RespawnPos
            };

            string mapdataFileName = $"SharpTimer/MapData/{currentMapName}.json"; // Use the map name in the filename
            string mapdataPath = Path.Join(gameDir + "/csgo/cfg", mapdataFileName);

            string updatedJson = JsonSerializer.Serialize(newMapInfo, jsonSerializerOptions);
            File.WriteAllText(mapdataPath, updatedJson);

            player.PrintToChat($" {ChatColors.LightPurple}[ZÓNOVÝ NÁSTROJ]{ChatColors.Default}Zóny boli úspešne uložené! {ChatColors.Grey}Opätovné načítavanie údajov...");
            Server.ExecuteCommand("mp_restartgame 1");
        }

        public void OnTickZoneTool(CCSPlayerController? player)
        {
            try
            {
                if (player == null || !player.IsValid || player.PlayerPawn == null)
                    return;

                if (playerTimers.TryGetValue(player.Slot, out var playerTimer))
                {
                    if (playerTimer.IsAddingStartZone)
                    {
                        Vector pawnPosition = player.Pawn?.Value!.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
                        DrawZoneToolWireframe(ParseVector(playerTimer.StartZoneC1!), pawnPosition, player.Slot);
                    }
                    else if (playerTimer.IsAddingEndZone)
                    {
                        Vector pawnPosition = player.Pawn?.Value!.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
                        DrawZoneToolWireframe(ParseVector(playerTimer.EndZoneC1!), pawnPosition, player.Slot);
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.Message != "Invalid game event")
                    SharpTimerError($"Error in OnTickZoneTool: {ex.Message}");
            }
        }

        public void DrawZoneToolWireframe(Vector corner1, Vector corner8, int playerSlot)
        {
            try
            {
                Vector corner2 = new(corner1.X, corner8.Y, corner1.Z);
                Vector corner3 = new(corner8.X, corner8.Y, corner1.Z);
                Vector corner4 = new(corner8.X, corner1.Y, corner1.Z);

                Vector corner5 = new(corner8.X, corner1.Y, corner8.Z);
                Vector corner6 = new(corner1.X, corner1.Y, corner8.Z);
                Vector corner7 = new(corner1.X, corner8.Y, corner8.Z);

                if (corner1 != null && corner2 != null && corner3 != null && corner4 != null &&
                corner5 != null && corner6 != null && corner7 != null && corner8 != null)
                {
                    // top square
                    DrawZoneToolWire(corner1, corner2, playerSlot, 1);
                    DrawZoneToolWire(corner2, corner3, playerSlot, 2);
                    DrawZoneToolWire(corner3, corner4, playerSlot, 3);
                    DrawZoneToolWire(corner4, corner1, playerSlot, 4);

                    // bottom square
                    DrawZoneToolWire(corner5, corner6, playerSlot, 5);
                    DrawZoneToolWire(corner6, corner7, playerSlot, 6);
                    DrawZoneToolWire(corner7, corner8, playerSlot, 7);
                    DrawZoneToolWire(corner8, corner5, playerSlot, 8);

                    // connect them both to build a cube, 
                    DrawZoneToolWire(corner1, corner6, playerSlot, 9);
                    DrawZoneToolWire(corner2, corner7, playerSlot, 10);
                    DrawZoneToolWire(corner3, corner8, playerSlot, 11);
                    DrawZoneToolWire(corner4, corner5, playerSlot, 12);
                }
                else
                {
                    SharpTimerDebug("One of the vectors is null in DrawZoneToolWireframe");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in DrawZoneToolWireframe: {ex.Message}");
            }
        }

        public void DrawZoneToolWire(Vector startPos, Vector endPos, int playerSlot, int wireIndex)
        {
            try
            {
                if (playerTimers.ContainsKey(playerSlot) && playerTimers[playerSlot] != null)
                {
                    if (!playerTimers[playerSlot].ZoneToolWire!.ContainsKey(wireIndex))
                    {
                        playerTimers[playerSlot].ZoneToolWire![wireIndex] = Utilities.CreateEntityByName<CBeam>("beam")!;
                    }
                    else
                    {
                        playerTimers[playerSlot].ZoneToolWire![wireIndex].Remove();
                        playerTimers[playerSlot].ZoneToolWire![wireIndex] = Utilities.CreateEntityByName<CBeam>("beam")!;
                    }

                    CBeam wire = playerTimers[playerSlot].ZoneToolWire![wireIndex];

                    if (wire != null)
                    {
                        wire.Render = Color.Green;
                        wire.Width = 1.5f;
                        wire.Teleport(startPos, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                        wire.EndPos.X = endPos.X;
                        wire.EndPos.Z = endPos.Z;
                        wire.EndPos.Y = endPos.Y;
                        wire.FadeMinDist = 9999;

                        wire.DispatchSpawn();
                    }
                    else
                    {
                        SharpTimerDebug($"Failed to create ZoneTool beam for wireIndex {wireIndex}...");
                    }
                }
                else
                {
                    SharpTimerDebug($"Player slot {playerSlot} not found in the dictionary.");
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in DrawZoneToolWire: {ex.Message}");
            }
        }
    }
}