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

using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace SharpTimer
{
    public partial class SharpTimer
    {
        public void OnTimerStart(CCSPlayerController? player, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;

            if (bonusX != 0)
            {
                if (useTriggers) SharpTimerDebug($"Starting Bonus Timer for {player!.PlayerName}");

                // Remove checkpoints for the current player
                playerCheckpoints.Remove(player!.Slot);

                playerTimers[player.Slot].IsTimerRunning = false;
                playerTimers[player.Slot].TimerTicks = 0;

                playerTimers[player.Slot].IsBonusTimerRunning = true;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerTimers[player.Slot].BonusStage = bonusX;
            }
            else
            {
                if (useTriggers) SharpTimerDebug($"Starting Timer for {player!.PlayerName}");

                // Remove checkpoints for the current player
                playerCheckpoints.Remove(player!.Slot);

                playerTimers[player.Slot].IsTimerRunning = true;
                playerTimers[player.Slot].TimerTicks = 0;

                playerTimers[player.Slot].IsBonusTimerRunning = false;
                playerTimers[player.Slot].BonusTimerTicks = 0;
                playerTimers[player.Slot].BonusStage = bonusX;
            }

            playerTimers[player.Slot].IsRecordingReplay = true;

        }

        public void OnTimerStop(CCSPlayerController? player)
        {

            var playerName = player!.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();
            var playerTimer = playerTimers[playerSlot];
            var currentTicks = playerTimer.TimerTicks;

            if (!IsAllowedPlayer(player) || playerTimer.IsTimerRunning == false) return;

            if (useStageTriggers == true && useCheckpointTriggers == true)
            {
                if (playerTimer.CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == true)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Chyba pri ukladaní času: Aktuálna fáza hráča sa nezhoduje s konečnou ({stageTriggerCount})");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }

                if (playerTimer.CurrentMapCheckpoint != cpTriggerCount)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Chyba pri ukladaní času: Aktuálny kontrolný bod hráča sa nezhoduje s konečným bodom ({cpTriggerCount})");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == true && useCheckpointTriggers == false)
            {
                if (playerTimer.CurrentMapStage != stageTriggerCount && currentMapOverrideStageRequirement == true)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Chyba pri ukladaní času: Aktuálna fáza hráča sa nezhoduje s konečnou ({stageTriggerCount})");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useStageTriggers == false && useCheckpointTriggers == true)
            {
                if (playerTimer.CurrentMapCheckpoint != cpTriggerCount)
                {
                    player.PrintToChat(msgPrefix + $"{ChatColors.LightRed} Chyba pri ukladaní času: Aktuálny kontrolný bod hráča sa nezhoduje s konečným bodom ({cpTriggerCount})");
                    playerTimer.IsTimerRunning = false;
                    playerTimer.IsRecordingReplay = false;
                    return;
                }
            }

            if (useTriggers) SharpTimerDebug($"Stopping Timer for {playerName}");

            if (!ignoreJSON) SavePlayerTime(player, currentTicks);
            if (useMySQL == true) _ = Task.Run(async () => await SavePlayerTimeToDatabase(player, currentTicks, steamID, playerName, playerSlot));
            //if (enableReplays == true) _ = Task.Run(async () => await DumpReplayToJson(player!, steamID, playerSlot));
            playerTimer.IsTimerRunning = false;
            playerTimer.IsRecordingReplay = false;

            if (useMySQL == false) _ = Task.Run(async () => await RankCommandHandler(player, steamID, playerSlot, playerName, true));
        }

        public void OnBonusTimerStop(CCSPlayerController? player, int bonusX)
        {
            if (!IsAllowedPlayer(player) || playerTimers[player!.Slot].IsBonusTimerRunning == false) return;

            var playerName = player.PlayerName;
            var playerSlot = player.Slot;
            var steamID = player.SteamID.ToString();

            if (useTriggers) SharpTimerDebug($"Stopping Bonus Timer for {playerName}");

            int currentTicks = playerTimers[player.Slot].BonusTimerTicks;

            if (!ignoreJSON) SavePlayerTime(player, currentTicks, bonusX);
            if (useMySQL == true) _ = Task.Run(async () => await SavePlayerTimeToDatabase(player, currentTicks, steamID, playerName, playerSlot, bonusX));
            //if (enableReplays == true) _ = Task.Run(async () => await DumpReplayToJson(player!, steamID, playerSlot, bonusX));
            playerTimers[player.Slot].IsBonusTimerRunning = false;
            playerTimers[player.Slot].IsRecordingReplay = false;
        }

        public void SavePlayerTime(CCSPlayerController? player, int timerTicks, int bonusX = 0)
        {
            if (!IsAllowedPlayer(player)) return;
            var playerName = player!.PlayerName;
            var playerSlot = player!.Slot;
            var steamId = player.SteamID.ToString();
            if ((bonusX == 0 && playerTimers[playerSlot].IsTimerRunning == false) || (bonusX != 0 && playerTimers[playerSlot].IsBonusTimerRunning == false)) return;

            SharpTimerDebug($"Saving player {(bonusX != 0 ? $"bonus {bonusX} time" : "time")} of {timerTicks} ticks for {playerName} to json");
            string mapRecordsPath = Path.Combine(playerRecordsPath!, bonusX == 0 ? $"{currentMapName}.json" : $"{currentMapName}_bonus{bonusX}.json");

            Task.Run(async () =>
            {
                try
                {
                    using (JsonDocument? jsonDocument = await LoadJson(mapRecordsPath)!)
                    {
                        Dictionary<string, PlayerRecord> records;

                        if (jsonDocument != null)
                        {
                            string json = jsonDocument.RootElement.GetRawText();
                            records = JsonSerializer.Deserialize<Dictionary<string, PlayerRecord>>(json) ?? [];
                        }
                        else
                        {
                            records = [];
                        }

                        if (!records.ContainsKey(steamId) || records[steamId].TimerTicks > timerTicks)
                        {
                            if (!useMySQL) await PrintMapTimeToChat(player, steamId, playerName, records.GetValueOrDefault(steamId)?.TimerTicks ?? 0, timerTicks, bonusX);

                            records[steamId] = new PlayerRecord
                            {
                                PlayerName = playerName,
                                TimerTicks = timerTicks
                            };

                            string updatedJson = JsonSerializer.Serialize(records, jsonSerializerOptions);
                            File.WriteAllText(mapRecordsPath, updatedJson);

                            if ((stageTriggerCount != 0 || cpTriggerCount != 0) && bonusX == 0 && useMySQL == false) _ = Task.Run(async () => await DumpPlayerStageTimesToJson(player, steamId, playerSlot));
                            if (enableReplays == true && useMySQL == false) _ = Task.Run(async () => await DumpReplayToJson(player!, steamId, playerSlot, bonusX));
                        }
                        else
                        {
                            if (!useMySQL) await PrintMapTimeToChat(player, steamId, playerName, records[steamId].TimerTicks, timerTicks, bonusX);
                        }
                    }

                }
                catch (Exception ex)
                {
                    SharpTimerError($"Error in SavePlayerTime: {ex.Message}");
                }
            });
        }

        private async Task HandlePlayerStageTimes(CCSPlayerController player, nint triggerHandle, int playerSlot, string playerSteamID, string playerName)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                {
                    return;
                }

                SharpTimerDebug($"Player {playerName} has a stage trigger with handle {triggerHandle}");

                if (stageTriggers.TryGetValue(triggerHandle, out int stageTrigger))
                {
                    var playerTimerTicks = playerTimers[playerSlot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (playerTimers[playerSlot].CurrentMapStage == stageTrigger || playerTimers[playerSlot] == null) return;
                    if (useMySQL == true)
                    {
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
                    }
                    else
                    {
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID();
                    }

                    var (previousStageTime, previousStageSpeed) = await GetStageTime(playerSteamID, stageTrigger);
                    var (srStageTime, srStageSpeed) = await GetStageTime(srSteamID, stageTrigger);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player)) return;
                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {

                            if (playerTimer.CurrentMapStage == stageTrigger || playerTimer == null) return;

                            string currentStageSpeed = Math.Round(use2DSpeed ? Math.Sqrt(player.PlayerPawn.Value!.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y)
                                                                                : Math.Sqrt(player.PlayerPawn.Value!.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y + player.PlayerPawn.Value.AbsVelocity.Z * player.PlayerPawn.Value.AbsVelocity.Z))
                                                                                .ToString("0000");

                            if (previousStageTime != 0)
                            {
                                player.PrintToChat(msgPrefix + $" Vstup na scénu: {stageTrigger}");
                                player.PrintToChat(msgPrefix + $" Čas: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                               $" [{FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                               $" {(previousStageTime != srStageTime ? $"[SR {FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentStageSpeed, out float speed) && speed >= 100) //workaround for staged maps with not telehops
                                player.PrintToChat(msgPrefix + $" Rýchlosť: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                               $" [{FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                               $" {(previousStageSpeed != srStageSpeed ? $"[SR {FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null && playerTimer.IsTimerRunning == true && IsAllowedPlayer(player))
                            {
                                try
                                {
                                    playerTimer.StageTimes[stageTrigger] = playerTimerTicks;
                                    playerTimer.StageVelos[stageTrigger] = $"{currentStageSpeed}";
                                    SharpTimerDebug($"Player {playerName} Entering stage {stageTrigger} Time {playerTimer.StageTimes[stageTrigger]}");
                                }
                                catch (Exception ex)
                                {
                                    SharpTimerError($"Error updating StageTimes dictionary: {ex.Message}");
                                    SharpTimerDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                }
                            }

                            playerTimer.CurrentMapStage = stageTrigger;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in HandlePlayerStageTimes: {ex.Message}");
            }
        }

        private async Task HandlePlayerCheckpointTimes(CCSPlayerController player, nint triggerHandle, int playerSlot, string playerSteamID, string playerName)
        {
            try
            {
                if (!IsAllowedPlayer(player))
                {
                    return;
                }

                if (cpTriggers.TryGetValue(triggerHandle, out int cpTrigger))
                {
                    if (useStageTriggers == true) //use stagetime instead
                    {
                        playerTimers[playerSlot].CurrentMapCheckpoint = cpTrigger;
                        return;
                    }

                    SharpTimerDebug($"Player {playerName} has a checkpoint trigger with handle {triggerHandle}");

                    var playerTimerTicks = playerTimers[playerSlot].TimerTicks; // store so its in sync with player

                    var (srSteamID, srPlayerName, srTime) = ("null", "null", "null");
                    if (playerTimers[playerSlot].CurrentMapCheckpoint == cpTrigger || playerTimers[playerSlot] == null) return;
                    if (useMySQL == true)
                    {
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamIDFromDatabase();
                    }
                    else
                    {
                        (srSteamID, srPlayerName, srTime) = await GetMapRecordSteamID();
                    }

                    var (previousStageTime, previousStageSpeed) = await GetStageTime(playerSteamID, cpTrigger);
                    var (srStageTime, srStageSpeed) = await GetStageTime(srSteamID, cpTrigger);

                    Server.NextFrame(() =>
                    {
                        if (!IsAllowedPlayer(player)) return;
                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {

                            if (playerTimer.CurrentMapCheckpoint == cpTrigger || playerTimer == null) return;

                            string currentStageSpeed = Math.Round(use2DSpeed ? Math.Sqrt(player.PlayerPawn.Value!.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y)
                                                                                : Math.Sqrt(player.PlayerPawn.Value!.AbsVelocity.X * player.PlayerPawn.Value.AbsVelocity.X + player.PlayerPawn.Value.AbsVelocity.Y * player.PlayerPawn.Value.AbsVelocity.Y + player.PlayerPawn.Value.AbsVelocity.Z * player.PlayerPawn.Value.AbsVelocity.Z))
                                                                                .ToString("0000");

                            if (previousStageTime != 0)
                            {
                                player.PrintToChat(msgPrefix + $" Kontrolný bod: {cpTrigger}");
                                player.PrintToChat(msgPrefix + $" Čas: {ChatColors.White}[{primaryChatColor}{FormatTime(playerTimerTicks)}{ChatColors.White}] " +
                                                               $" [{FormatTimeDifference(playerTimerTicks, previousStageTime)}{ChatColors.White}]" +
                                                               $" {(previousStageTime != srStageTime ? $"[SR {FormatTimeDifference(playerTimerTicks, srStageTime)}{ChatColors.White}]" : "")}");

                                if (float.TryParse(currentStageSpeed, out float speed) && speed >= 100) //workaround for staged maps with not telehops
                                    player.PrintToChat(msgPrefix + $" Rýchlosť: {ChatColors.White}[{primaryChatColor}{currentStageSpeed}u/s{ChatColors.White}]" +
                                                                   $" [{FormatSpeedDifferenceFromString(currentStageSpeed, previousStageSpeed)}u/s{ChatColors.White}]" +
                                                                   $" {(previousStageSpeed != srStageSpeed ? $"[SR {FormatSpeedDifferenceFromString(currentStageSpeed, srStageSpeed)}u/s{ChatColors.White}]" : "")}");
                            }

                            if (playerTimer.StageVelos != null && playerTimer.StageTimes != null && playerTimer.IsTimerRunning == true && IsAllowedPlayer(player))
                            {
                                if (!playerTimer.StageTimes.ContainsKey(cpTrigger))
                                {
                                    SharpTimerDebug($"Player {playerName} cleared StageTimes before (cpTrigger)");
                                    playerTimer.StageTimes.Add(cpTrigger, playerTimerTicks);
                                    playerTimer.StageVelos.Add(cpTrigger, $"{currentStageSpeed}");
                                }
                                else
                                {
                                    try
                                    {
                                        playerTimer.StageTimes[cpTrigger] = playerTimerTicks;
                                        playerTimer.StageVelos[cpTrigger] = $"{currentStageSpeed}";
                                        SharpTimerDebug($"Player {playerName} Entering checkpoint {cpTrigger} Time {playerTimer.StageTimes[cpTrigger]}");
                                    }
                                    catch (Exception ex)
                                    {
                                        SharpTimerError($"Error updating StageTimes dictionary: {ex.Message}");
                                        SharpTimerDebug($"Player {playerName} dictionary keys: {string.Join(", ", playerTimer.StageTimes.Keys)}");
                                    }
                                }
                            }
                            playerTimer.CurrentMapCheckpoint = cpTrigger;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in HandlePlayerCheckpointTimes: {ex.Message}");
            }
        }

        public async Task DumpPlayerStageTimesToJson(CCSPlayerController? player, string playerId, int playerSlot)
        {
            if (!IsAllowedPlayer(player)) return;

            string fileName = $"{currentMapName!.ToLower()}_stage_times.json";
            string playerStageRecordsPath = Path.Join(gameDir, "csgo", "cfg", "SharpTimer", "PlayerStageData", fileName);

            try
            {
                using (JsonDocument? jsonDocument = await LoadJson(playerStageRecordsPath)!)
                {
                    if (jsonDocument != null)
                    {
                        string jsonContent = jsonDocument.RootElement.GetRawText();

                        Dictionary<string, PlayerStageData> playerData;
                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            playerData = JsonSerializer.Deserialize<Dictionary<string, PlayerStageData>>(jsonContent)!;
                        }
                        else
                        {
                            playerData = [];
                        }

                        if (!playerData!.ContainsKey(playerId))
                        {
                            playerData[playerId] = new PlayerStageData();
                        }

                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {
                            playerData[playerId].StageTimes = playerTimer.StageTimes;
                            playerData[playerId].StageVelos = playerTimer.StageVelos;
                        }
                        else
                        {
                            SharpTimerError($"Error in DumpPlayerStageTimesToJson: playerTimers does not have the requested playerSlot");
                        }

                        string updatedJson = JsonSerializer.Serialize(playerData, jsonSerializerOptions);
                        await File.WriteAllTextAsync(playerStageRecordsPath, updatedJson);
                    }
                    else
                    {
                        Dictionary<string, PlayerStageData> playerData = [];

                        if (playerTimers.TryGetValue(playerSlot, out PlayerTimerInfo? playerTimer))
                        {
                            playerData[playerId] = new PlayerStageData
                            {
                                StageTimes = playerTimers[playerSlot].StageTimes,
                                StageVelos = playerTimers[playerSlot].StageVelos
                            };
                        }
                        else
                        {
                            SharpTimerError($"Error in DumpPlayerStageTimesToJson: playerTimers does not have the requested playerSlot");
                        }

                        string updatedJson = JsonSerializer.Serialize(playerData, jsonSerializerOptions);
                        await File.WriteAllTextAsync(playerStageRecordsPath, updatedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                SharpTimerError($"Error in DumpPlayerStageTimesToJson: {ex.Message}");
            }
        }
    }
}