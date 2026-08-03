using ICE.Sounds;
using ICE.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICE.Ui.MainUi.Settings.Settings_Table
{
    internal class StopWhen
    {
        public static void Draw()
        {
            ImGui.Checkbox("目前任務完成後停止", ref Mission_Settings.StopAfterCurrent);

            #region CosmoCredits

            bool stopCosmic = C.StopOnceHitCosmoCredits;
            if (ImGui.Checkbox("宇宙信用點達到指定數量時停止", ref stopCosmic))
            {
                C.StopOnceHitCosmoCredits = stopCosmic;
                C.Save();
            }

            ImGui.SameLine();
            int cosmicCap = C.CosmoCreditsCap;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("##CosmicStop", ref cosmicCap, 0, 30_000))
            {
                if (cosmicCap > 30000)
                    cosmicCap = 30000;
                else if (cosmicCap < 0)
                    cosmicCap = 0;

                C.CosmoCreditsCap = cosmicCap;
                C.SaveDebounced();
            }

            #endregion

            #region Planet Credits

            bool stopLunar = C.StopOnceHitLunarCredits;
            if (ImGui.Checkbox("星球信用點達到指定數量時停止", ref stopLunar))
            {
                C.StopOnceHitLunarCredits = stopLunar;
                C.Save();
            }

            ImGui.SameLine();

            int lunarCap = C.LunarCreditsCap;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("##LunarStop", ref lunarCap, 0, 10_000))
            {
                C.LunarCreditsCap = lunarCap;
                C.SaveDebounced();
            }

            #endregion

            #region Cosmic Score

            bool stopScore = C.StopOnceHitCosmicScore;
            if (ImGui.Checkbox("宇宙探索分數達到指定值時停止", ref stopScore))
            {
                C.StopOnceHitCosmicScore = stopScore;
                C.BuyItems = false;
                C.Save();
            }

            ImGui.SameLine();

            int scoreCap = C.CosmicScoreCap;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("###ScoreStop", ref scoreCap, 10_000, 500_000))
            {
                C.CosmicScoreCap = scoreCap >= 0 ? scoreCap : 0;
                C.Save();
            }

            #endregion

            #region Level

            bool stopWhenLevel = C.StopWhenLevel;
            if (ImGui.Checkbox("達到指定等級時停止", ref stopWhenLevel))
            {
                C.StopWhenLevel = stopWhenLevel;
                C.Save();
            }

            ImGui.SameLine();

            int targetLevel = C.TargetLevel;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("##Level", ref targetLevel, 10, 100))
            {
                C.TargetLevel = targetLevel;
                C.SaveDebounced();
            }

            #endregion

            #region Relic Completed

            bool relicStop = C.StopOnceRelicFinished;
            if (ImGui.Checkbox("宇宙工具完成時停止", ref relicStop))
            {
                C.StopOnceRelicFinished = relicStop;
                C.Save();
            }

            #endregion

            #region Standard Missions Golded

            bool stopStandardGold = C.StopOnceStandardMissionsGolded;
            if (ImGui.Checkbox("選定職業的所有普通任務獲得金評時停止", ref stopStandardGold))
            {
                C.StopOnceStandardMissionsGolded = stopStandardGold;
                C.Save();
            }

            if (stopStandardGold || C.CosmicAgenda.Any(x => x.Goal == AgendaGoal.StandardMissionsGolded))
            {
                ImGui.Indent();
                ImGui.TextDisabled("納入等級：");
                ImGui.SameLine();
                var aRank = C.StopStandardGoldARank;
                if (ImGui.Checkbox("A 級", ref aRank)) { C.StopStandardGoldARank = aRank; C.Save(); }
                ImGui.SameLine();
                var bRank = C.StopStandardGoldBRank;
                if (ImGui.Checkbox("B 級", ref bRank)) { C.StopStandardGoldBRank = bRank; C.Save(); }
                ImGui.SameLine();
                var cRank = C.StopStandardGoldCRank;
                if (ImGui.Checkbox("C 級", ref cRank)) { C.StopStandardGoldCRank = cRank; C.Save(); }
                ImGui.SameLine();
                var dRank = C.StopStandardGoldDRank;
                if (ImGui.Checkbox("D 級", ref dRank)) { C.StopStandardGoldDRank = dRank; C.Save(); }
                ImGui.Unindent();
            }

            #endregion

            #region Sound Alert

            bool playSoundAlert = C.PlaySoundAlert;
            if (ImGui.Checkbox("停止時播放提示音", ref playSoundAlert))
            {
                C.PlaySoundAlert = playSoundAlert;
                C.Save();
            }
            if (playSoundAlert)
            {
                var soundVolume = C.SoundVolume;
                ImGui.Text("提示音音量");
                ImGui.SetNextItemWidth(200);
                if (ImGui.SliderFloat("##Sound Volume", ref soundVolume, 0f, 1f, "%.2f"))
                {
                    C.SoundVolume = soundVolume;
                    C.SaveDebounced();
                }
                if (ImGui.Button("測試提示音"))
                {
                    _ = SoundPlayer.PlaySoundAsync();
                }
            }

            #endregion
        }

    }
}
