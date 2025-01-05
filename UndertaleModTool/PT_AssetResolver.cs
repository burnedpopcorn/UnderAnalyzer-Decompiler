// VERY WIP

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UndertaleModLib;
using UndertaleModLib.Models;
using System.Windows;

namespace UndertaleModTool
{
    public class PT_AssetResolver
    {
        public static Dictionary<string, string[]> builtin_funcs; // keys are function names

        public static Dictionary<string, string> builtin_vars; // keys are variable names

        public static Dictionary<int, string> PTStates = new(); // only for internal shit

        public static Dictionary<string, int> JSON_PTStates = new(); // for json/the only thing that is sent to the thing

        // Properly initializes per-project/game
        public static void InitializeTypes(UndertaleData data)
        {
            if (data == null) 
            {
                // Failsafe just in case user is dumb
                Application.Current.MainWindow.ShowWarning("No data.win was loaded\nLoad a data.win first");
                return;
            }

            PT_AssetResolver.PTStates.Clear();

            // do this to eventually convert to json
            // so that it can be read by GameSpecificRegistry(Convertor) or something
            builtin_funcs = new Dictionary<string, string[]>
            { };

            builtin_vars = new Dictionary<string, string>
            { };

            // Pizza Tower Enums
            try
            {
                // Check Pizza Tower States in these Scripts
                FindStateNames(data.Code.ByName("gml_Object_obj_player_Step_0"),
                    new[] { "scr_player_", "state_player_", "scr_playerN_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_cheeseslime_Step_0"),
                    new[] { "scr_enemy_", "scr_pizzagoblin_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_pepperman_Step_0"),
                    new[] { "scr_boss_", "scr_pepperman_", "scr_enemy_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_vigilanteboss_Step_0"),
                    new[] { "scr_vigilante_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_noiseboss_Step_0"),
                    new[] { "scr_noise_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_fakepepboss_Step_0"),
                    new[] { "scr_fakepepboss_", "scr_boss_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_pizzafaceboss_Step_0"),
                    new[] { "scr_pizzaface_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_pizzafaceboss_p2_Step_0"),
                    new[] { "scr_pizzaface_p2_", "scr_pizzaface_" }
                );
                FindStateNames(
                    data.Code.ByName("gml_Object_obj_pizzafaceboss_p3_Step_0"),
                    new[] { "scr_pizzaface_p3_" }
                );

                // Variable Definitions
                builtin_vars.Add("state", "Enum.states");
                builtin_vars.Add("_state", "Enum.states");
                builtin_vars.Add("prevstate", "Enum.states");
                builtin_vars.Add("_prevstate", "Enum.states");
                builtin_vars.Add("substate", "Enum.states");
                builtin_vars.Add("arenastate", "Enum.states");
                builtin_vars.Add("player_state", "Enum.states");
                builtin_vars.Add("tauntstoredstate", "Enum.states");
                builtin_vars.Add("taunt_storedstate", "Enum.states");
                builtin_vars.Add("storedstate", "Enum.states");
                builtin_vars.Add("chosenstate", "Enum.states");
                builtin_vars.Add("superattackstate", "Enum.states");
                builtin_vars.Add("text_state", "Enum.states");
                builtin_vars.Add("ministate", "Enum.states");
                builtin_vars.Add("dropstate", "Enum.states");
                builtin_vars.Add("verticalstate", "Enum.states");
                builtin_vars.Add("walkstate", "Enum.states");
                builtin_vars.Add("hitstate", "Enum.states");
                builtin_vars.Add("toppin_state", "Enum.states");
                builtin_vars.Add("bossintrostate", "Enum.states");
                builtin_vars.Add("introstate", "Enum.states");
                builtin_vars.Add("fadeoutstate", "Enum.states");
                builtin_vars.Add("supergrabstate", "Enum.states");
                builtin_vars.Add("startstate", "Enum.states");
                builtin_vars.Add("atstate", "Enum.states");
                builtin_vars.Add("attack_pool", "Enum.states");

                // Function Arguments
                builtin_funcs["gml_Script_vigilante_cancel_attack"] = new[] { "Enum.states", null };
            }
            catch (Exception e) 
            {
                Application.Current.MainWindow.ShowWarning("Failed to read data\nFailed to Extract Pizza Tower Enums");
            }

                // Variable Definitions
                builtin_vars.Add("leveltorestart", "Asset.Room");
                builtin_vars.Add("targetRoom", "Asset.Room");
                builtin_vars.Add("targetRoom2", "Asset.Room");
                builtin_vars.Add("backtohubroom", "Asset.Room");
                builtin_vars.Add("roomtorestart", "Asset.Room");
                builtin_vars.Add("checkpointroom", "Asset.Room");
                builtin_vars.Add("lastroom", "Asset.Room");
                builtin_vars.Add("hub_array", "Asset.Room");
                builtin_vars.Add("level_array", "Asset.Room");
                builtin_vars.Add("_levelinfo", "Asset.Room");

                builtin_vars.Add("objectlist", "Asset.Object");
                builtin_vars.Add("object_arr", "Asset.Object");
                builtin_vars.Add("objdark_arr", "Asset.Object");
                builtin_vars.Add("content_arr", "Asset.Object");
                builtin_vars.Add("spawnpool", "Asset.Object");
                builtin_vars.Add("spawn_arr", "Asset.Object");
                builtin_vars.Add("dark_arr", "Asset.Object");
                builtin_vars.Add("flash_arr", "Asset.Object");
                builtin_vars.Add("collision_list", "Asset.Object");

                builtin_vars.Add("content", "Asset.Object");
                builtin_vars.Add("player", "Asset.Object");
                builtin_vars.Add("targetplayer", "Asset.Object");
                builtin_vars.Add("target", "Asset.Object");

                builtin_vars.Add("playerid", "Asset.Object");
                builtin_vars.Add("_playerid", "Asset.Object");
                builtin_vars.Add("player_id", "Asset.Object");
                builtin_vars.Add("platformid", "Asset.Object");
                builtin_vars.Add("objID", "Asset.Object");
                builtin_vars.Add("objectID", "Asset.Object");
                builtin_vars.Add("spawnenemyID", "Asset.Object");
                builtin_vars.Add("ID", "Asset.Object");

                builtin_vars.Add("baddiegrabbedID", "Asset.Object");
                builtin_vars.Add("pizzashieldID", "Asset.Object");
                builtin_vars.Add("angryeffectid", "Asset.Object");
                builtin_vars.Add("pizzashieldid", "Asset.Object");
                builtin_vars.Add("superchargedeffectid", "Asset.Object");

                builtin_vars.Add("baddieID", "Asset.Object");
                builtin_vars.Add("baddieid", "Asset.Object");
                builtin_vars.Add("brickid", "Asset.Object");
                builtin_vars.Add("attackerID", "Asset.Object");

                builtin_vars.Add("object", "Asset.Object");
                builtin_vars.Add("obj", "Asset.Object");
                builtin_vars.Add("_obj", "Asset.Object");
                builtin_vars.Add("closestObj", "Asset.Object");
                builtin_vars.Add("solidObj", "Asset.Object");
                builtin_vars.Add("bg_obj", "Asset.Object");

                builtin_vars.Add("_obj_player", "Asset.Object");
                builtin_vars.Add("obj_explosion", "Asset.Object");
                builtin_vars.Add("my_obj_index", "Asset.Object");
                builtin_vars.Add("inst", "Asset.Object");

                builtin_vars.Add("chargeeffectid", "Asset.Object");
                builtin_vars.Add("dashcloudid", "Asset.Object");
                builtin_vars.Add("crazyruneffectid", "Asset.Object");
                builtin_vars.Add("superslameffectid", "Asset.Object");
                builtin_vars.Add("speedlineseffectid", "Asset.Object");

                builtin_vars.Add("room_arr", "Asset.Room");
                builtin_vars.Add("rm", "Asset.Room");
                builtin_vars.Add("room_index", "Asset.Room");
                builtin_vars.Add("levels", "Asset.Room");

                builtin_vars.Add("bpal", "Asset.Sprite");
                builtin_vars.Add("vstitle", "Asset.Sprite");
                builtin_vars.Add("bg", "Asset.Sprite");
                builtin_vars.Add("bg2", "Asset.Sprite");
                builtin_vars.Add("bg3", "Asset.Sprite");
                builtin_vars.Add("playersprshadow", "Asset.Sprite");
                builtin_vars.Add("bosssprshadow", "Asset.Sprite");
                builtin_vars.Add("portrait1_idle", "Asset.Sprite");
                builtin_vars.Add("portrait1_hurt", "Asset.Sprite");
                builtin_vars.Add("portrait2_idle", "Asset.Sprite");
                builtin_vars.Add("portrait2_hurt", "Asset.Sprite");
                builtin_vars.Add("boss_palette", "Asset.Sprite");
                builtin_vars.Add("panicspr", "Asset.Sprite");
                builtin_vars.Add("bossarr", "Asset.Sprite");
                builtin_vars.Add("palettetexture", "Asset.Sprite");
                builtin_vars.Add("switchstart", "Asset.Sprite");
                builtin_vars.Add("switchend", "Asset.Sprite");
                builtin_vars.Add("_hurt", "Asset.Sprite");
                builtin_vars.Add("_dead", "Asset.Sprite");
                builtin_vars.Add("treasure_arr", "Asset.Sprite");

                builtin_vars.Add("storedspriteindex", "Asset.Sprite");
                builtin_vars.Add("icon", "Asset.Sprite");

                builtin_vars.Add("spridle", "Asset.Sprite");
                builtin_vars.Add("sprgot", "Asset.Sprite");

                builtin_vars.Add("color", "Constant.Color");
                builtin_vars.Add("textcolor", "Constant.Color");
                builtin_vars.Add("bc", "Constant.Color");
                builtin_vars.Add("tc", "Constant.Color");
                builtin_vars.Add("gameframe_blend", "Constant.Color");
                builtin_vars.Add("c1", "Constant.Color");
                builtin_vars.Add("c2", "Constant.Color");

                builtin_vars.Add("gameframe_caption_icon", "Asset.Sprite");

                // Function Arguments
                builtin_funcs["gml_Script_instance_create_unique"] =
                    new[] { null, null, "Asset.Object" };
                builtin_funcs["gml_Script_instance_nearest_random"] =
                    new[] { "Asset.Object", null };

                builtin_funcs["gml_Script_draw_enemy"] =
                    new[] { null, null, "Constant.Color" };

                builtin_funcs["gml_Script_create_afterimage"] =
                    new[] { null, null, "Asset.Sprite", null };
                builtin_funcs["gml_Script_create_mach2effect"] =
                    new[] { null, null, "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_create_heatattack_afterimage"] =
                    new[] { null, null, "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_create_firemouth_afterimage"] =
                    new[] { null, null, "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_create_blue_afterimage"] =
                    new[] { null, null, "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_create_red_afterimage"] =
                    new[] { null, null, "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_create_blur_afterimage"] =
                    new[] { null, null, "Asset.Sprite", null, null };

                builtin_funcs["gml_Script_pal_swap_init_system"] =
                    new[] { "Asset.Shader" };
                builtin_funcs["gml_Script_pal_swap_init_system_fix"] =
                    new[] { "Asset.Shader" };
                builtin_funcs["gml_Script_pal_swap_set"] =
                    new[] { "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_pattern_set"] =
                    new[] { null, "Asset.Sprite", null, null, null, null };

                builtin_funcs["gml_Script_scr_sound"] =
                    new[] { "Asset.Sound", "Asset.Sound", "Asset.Sound", "Asset.Sound" };
                builtin_funcs["gml_Script_scr_music"] =
                    new[] { "Asset.Sound", "Asset.Sound", "Asset.Sound", "Asset.Sound" };
                builtin_funcs["gml_Script_scr_soundeffect"] =
                    new[] { "Asset.Sound", "Asset.Sound", "Asset.Sound", "Asset.Sound" };

                builtin_funcs["gml_Script_declare_particle"] =
                    new[] { null, "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_create_debris"] =
                    new[] { null, null, "Asset.Sprite", null };
                builtin_funcs["gml_Script_create_collect"] =
                    new[] { null, null, "Asset.Sprite", null };

                builtin_funcs["gml_Script_tv_do_expression"] =
                    new[] { "Asset.Sprite" };

                builtin_funcs["gml_Script_scr_pauseicon_add"] =
                    new[] { "Asset.Sprite", null, null, null };

                builtin_funcs["gml_Script_scr_room_goto"] =
                    new[] { "Asset.Room" };

                builtin_funcs["gml_Script_add_music"] =
                    new[] { "Asset.Room", null, null, null, null };
                builtin_funcs["gml_Script_hub_state"] =
                    new[] { "Asset.Room", null, null };

                builtin_funcs["gml_Script_draw_background_tiled"] =
                    new[] { "Asset.Sprite", null, null, null };
                builtin_funcs["gml_Script_scr_draw_granny_texture"] =
                    new[] { null, null, null, null,
                    null, null, "Asset.Sprite", "Asset.Sprite", };

                builtin_funcs["gml_Script_object_get_depth"] =
                    new[] { "Asset.Object" };

                builtin_funcs["gml_Script_scr_bosscontroller_particle_anim"] =
                    new[] { "Asset.Sprite", null, null,
                    null, null, "Asset.Sprite", null };
                builtin_funcs["gml_Script_scr_bosscontroller_particle_hp"] =
                    new[] { "Asset.Sprite", null, null,
                    null, null, "Asset.Sprite", null, "Asset.Sprite" };
                builtin_funcs["gml_Script_scr_bosscontroller_draw_health"] =
                    new[] { "Asset.Sprite",
                    null, null, null, null, null,
                    null, null, null, null, null,
                    "Asset.Sprite", null, "Asset.Sprite" };
                builtin_funcs["gml_Script_scr_boss_genericintro"] =
                    new[] { "Asset.Sprite" };
                builtin_funcs["gml_Script_boss_update_pizzaheadKO"] =
                    new[] { "Asset.Sprite", "Asset.Sprite" };
                builtin_funcs["gml_Script_scr_pizzaface_p3_do_player_attack"] =
                    new[] { "Asset.Object" };

                builtin_funcs["gml_Script_scr_boss_do_hurt_phase2"] =
                    new[] { "Asset.Object" };

                builtin_funcs["gml_Script_check_slope"] = new[] { "Asset.Object" };
                builtin_funcs["gml_Script_check_slope_player"] = new[] { "Asset.Object" };
                builtin_funcs["gml_Script_try_solid"] =
                    new[] { null, null, "Asset.Object", null };

                builtin_funcs["gml_Script_add_rank_achievements"] =
                    new[] { null, null, "Asset.Sprite", null,
                    null };
                builtin_funcs["gml_Script_add_boss_achievements"] =
                    new[] { null, "Asset.Room", "Asset.Sprite", null };

                builtin_funcs["gml_Script_achievement_unlock"] =
                    new[] { null, null, "Asset.Sprite", null };
                builtin_funcs["gml_Script_palette_unlock"] =
                    new[] { null, null, null, "Asset.Sprite" };

                builtin_funcs["gml_Script_scr_monsterinvestigate"] =
                    new[] { null, "Asset.Sprite", "Asset.Sprite" };

                builtin_funcs["gml_Script_timedgate_add_objects"] =
                    new[] { "Asset.Object", null };

                builtin_funcs["gml_Script_randomize_animations"] =
                    new[] { "Asset.Sprite", null };

                builtin_funcs["gml_Script_tdp_draw_text_color"] =
                    new[] {
                        null, null, null,
                        "Constant.Color", "Constant.Color", "Constant.Color", "Constant.Color",
                        null
                    };
                builtin_funcs["gml_Script_scr_draw_text_arr"] =
                    new[] {
                        null, null, null,
                        "Constant.Color", null
                    };
                builtin_funcs["gml_Script_lang_draw_sprite"] =
                    new[] { "Asset.Sprite", null, null, null };

            // Time for the goofy ahh JSON shit
            var enums = new Dictionary<string, object>
            {
                // what the fuck is this dense shite
                { 
                    "Enum.states", 
                    new 
                    { 
                        Name = "states",
                        Values = JSON_PTStates 
                    } 
                }
            };
            // Main JSON thingy
            var PTJSON = new
            {
                // Enum Only Branch
                Types = new 
                {
                    // goofy thing from above
                    Enums = enums,
                    // Shit just for the Template
                    Constants = new { },
                    General = new { }
                },
                // Other Shit Branch
                GlobalNames = new 
                {
                    // crackful
                    Variables = builtin_vars,
                    FunctionArguments = builtin_funcs,
                    // Shit just for the Template
                    FunctionReturn = new { }
                },
                // Shit just for the Template
                CodeEntryNames = new { }
            };

            // Convert the parent object to a JSON string
            string jsonString = JsonSerializer.Serialize(PTJSON, new JsonSerializerOptions { WriteIndented = true });
            try
            {
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + data.GeneralInfo.Name + ".json", jsonString);
            }
            catch (Exception e) 
            {
                Application.Current.MainWindow.ShowWarning("Failed to read data\nFailed to Read data.win's Internal Game Name");
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "GameDefinitions.json", jsonString);
            }
        }

        // Detects PT state names
        public static void FindStateNames(UndertaleCode code, string[] statePrefix)
        {
            if (code != null)
            {
                for (var i = 0; i < code.Instructions.Count; i++)
                {
                    UndertaleInstruction instr = code.Instructions[i];
                    if (
                        UndertaleInstruction.GetInstructionType(instr.Kind)
                        != UndertaleInstruction.InstructionType.PushInstruction
                        || !((instr.Value is int) || (instr.Value is short) || (instr.Value is long))
                    ) continue;

                    int stateID = Convert.ToInt32(instr.Value);
                    if (PTStates.ContainsKey(stateID)) continue;

                    UndertaleInstruction next = code.Instructions[i + 1];
                    if (next == null) continue;
                    if (
                        UndertaleInstruction.GetInstructionType(next.Kind)
                        != UndertaleInstruction.InstructionType.ComparisonInstruction
                        || next.ComparisonKind != UndertaleInstruction.ComparisonType.EQ
                    ) continue;
                    UndertaleInstruction next2 = code.Instructions[i + 2];
                    if (next2 == null) continue;
                    if (
                        next2.Kind != UndertaleInstruction.Opcode.Bt
                    ) continue;

                    UndertaleInstruction newInstr =
                        code.GetInstructionFromAddress(next2.Address + (uint)next2.JumpOffset);

                    if (newInstr == null) continue;

                    for (
                        var j = code.Instructions.IndexOf(newInstr);
                        j < code.Instructions.Count &&
                        UndertaleInstruction.GetInstructionType(code.Instructions[j].Kind) !=
                            UndertaleInstruction.InstructionType.GotoInstruction;
                        j++
                    )
                    {
                        UndertaleInstruction thisInstr = code.Instructions[j];
                        if (UndertaleInstruction.GetInstructionType(thisInstr.Kind)
                            != UndertaleInstruction.InstructionType.CallInstruction) continue;
                        string funcName = thisInstr?.Function?.Target?.Name?.Content;
                        if (funcName is null)
                            continue;
                        string stateName = null;
                        foreach (string prefix in statePrefix)
                        {
                            if (funcName.StartsWith(prefix))
                            {
                                stateName = funcName[prefix.Length..];
                                break;
                            }
                            else if (funcName.StartsWith("gml_Script_" + prefix))
                            {
                                stateName = funcName[("gml_Script_" + prefix).Length..];
                                break;
                            }
                        }
                        if (stateName == null) continue;
                        // Hooray! We got the state!
                        if (PTStates.ContainsKey(stateID)) break;

                        string actualStateName = stateName;
                        int dedupe = 1;
                        while (PTStates.ContainsValue(actualStateName))
                        {
                            dedupe++;
                            actualStateName = stateName + dedupe.ToString();
                        }

                        PTStates.TryAdd(stateID, actualStateName);
                        // idk man, for Json
                        JSON_PTStates.TryAdd(actualStateName, stateID);
                        break;
                    }
                }
            }
        }
    }
}