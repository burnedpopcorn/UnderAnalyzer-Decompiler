// Make JSON File for Pizza Tower data.win
// This is basically completely from UTMTCE, but is used to make JSON files for UA

using System;
using System.Collections.Generic;
using System.IO;
// Json stuff
using System.Text.Json;
// for the data.win, duh
using UndertaleModLib;
using UndertaleModLib.Models;
// for ShowWarning
using System.Windows;
using System.Linq;

namespace UndertaleModTool
{
    public class PT_AssetResolver
    {
        public static Dictionary<string, string[]> builtin_funcs; // keys are function names

        public static Dictionary<string, string> builtin_vars; // keys are variable names

        public static Dictionary<int, string> PTStates = new(); // only for internal shit

        public static Dictionary<string, int> JSON_PTStates = new(); // for json/the only thing that is sent to the thing

        public static Dictionary<string, object> enums = new(); // Main enums

        public static Dictionary<string, object> generalarrays = new(); // General Arrays

        // All other enums
        public static Dictionary<string, int> particle_enums = new();
        public static Dictionary<string, int> notification_enums = new();
        public static Dictionary<string, int> holiday_enums = new();
        public static Dictionary<string, int> tv_enums = new();
        public static Dictionary<string, int> text_enums = new();
        public static Dictionary<string, int> menuID_enums = new();
        public static Dictionary<string, int> editor_enums = new();
        public static Dictionary<string, int> afterimg_enums = new();
        public static Dictionary<string, int> tdp_input_enums = new();
        public static Dictionary<string, int> menuanchors_enums = new();

        public class MacroEntry // for new shit
        {
            public string MacroType { get; set; }
            public List<List<object>> Macros { get; set; }

            public MacroEntry(string macroType, List<List<object>> macros)
            {
                MacroType = macroType;
                Macros = macros;
            }
        }
        // also new stuffs that will merge with builtin_funcs
        public static Dictionary<string, object> functionArguments = new();

        // Make the JSON Files
        public static void InitializeTypes(UndertaleData data)
        {
            if (data == null)
            {
                // Failsafe just in case user is dumb
                Application.Current.MainWindow.ShowWarning("No data.win was loaded\nLoad a data.win first");
                return;
            }

            // Clear all if already filled
            PT_AssetResolver.PTStates.Clear();
            builtin_funcs = new Dictionary<string, string[]> { };
            builtin_vars = new Dictionary<string, string> { };
            functionArguments = new Dictionary<string, object> { };
            generalarrays = new Dictionary<string, object> { };

            // Pizza Tower Enums
            // how these work is that:
            //                                      v--- Code Entry to search               v--- find state name from, ex: (scr_player_normal(); --> normal (it then adds states.))
            // FindStateNames(data.Code.ByName("gml_Object_obj_player_Step_0"), new[] { "scr_player_", "state_player_", "scr_playerN_" });
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

                // ONLY Add if the FindStateNames Function actually found states
                if (JSON_PTStates.Count > 0)
                {
                    // Variables that should == Enum
                    builtin_vars.TryAdd("state", "Enum.states");
                    builtin_vars.TryAdd("_state", "Enum.states");
                    builtin_vars.TryAdd("prevstate", "Enum.states");
                    builtin_vars.TryAdd("_prevstate", "Enum.states");
                    builtin_vars.TryAdd("substate", "Enum.states");
                    builtin_vars.TryAdd("arenastate", "Enum.states");
                    builtin_vars.TryAdd("player_state", "Enum.states");
                    builtin_vars.TryAdd("tauntstoredstate", "Enum.states");
                    builtin_vars.TryAdd("taunt_storedstate", "Enum.states");
                    builtin_vars.TryAdd("storedstate", "Enum.states");
                    builtin_vars.TryAdd("chosenstate", "Enum.states");
                    builtin_vars.TryAdd("superattackstate", "Enum.states");
                    builtin_vars.TryAdd("text_state", "Enum.states");
                    builtin_vars.TryAdd("ministate", "Enum.states");
                    builtin_vars.TryAdd("dropstate", "Enum.states");
                    builtin_vars.TryAdd("verticalstate", "Enum.states");
                    builtin_vars.TryAdd("walkstate", "Enum.states");
                    builtin_vars.TryAdd("hitstate", "Enum.states");
                    builtin_vars.TryAdd("toppin_state", "Enum.states");
                    builtin_vars.TryAdd("bossintrostate", "Enum.states");
                    builtin_vars.TryAdd("introstate", "Enum.states");
                    builtin_vars.TryAdd("fadeoutstate", "Enum.states");
                    builtin_vars.TryAdd("supergrabstate", "Enum.states");
                    builtin_vars.TryAdd("startstate", "Enum.states");
                    builtin_vars.TryAdd("atstate", "Enum.states");
                    // New ones
                    builtin_vars.TryAdd("attack_pool", "Array<Enum.states>");
                    builtin_vars.TryAdd("transformation", "Enum.states");

                    // Function Arguments
                    builtin_funcs["gml_Script_vigilante_cancel_attack"] = new[] { "Enum.states", null };
                    builtin_funcs["gml_Script_scr_bombshoot"] = new[] { "Enum.states" };

                    // Apply States to Arrays as well
                    generalarrays.TryAdd("Array<Enum.states>", new { MacroType = "ArrayInit", Macro = "Enum.states" });
                }
            }
            catch (Exception e)
            {
                Application.Current.MainWindow.ShowWarning("Failed to read data\nFailed to Extract Pizza Tower Enums");
            }

            // Variable Definitions

            // Rooms
            builtin_vars.TryAdd("leveltorestart", "Asset.Room");
            builtin_vars.TryAdd("targetRoom", "Asset.Room");
            builtin_vars.TryAdd("targetRoom2", "Asset.Room");
            builtin_vars.TryAdd("backtohubroom", "Asset.Room");
            builtin_vars.TryAdd("roomtorestart", "Asset.Room");
            builtin_vars.TryAdd("checkpointroom", "Asset.Room");
            builtin_vars.TryAdd("lastroom", "Asset.Room");
            builtin_vars.TryAdd("hub_array", "Asset.Room");
            builtin_vars.TryAdd("level_array", "Asset.Room");
            builtin_vars.TryAdd("_levelinfo", "Asset.Room");
            builtin_vars.TryAdd("rm", "Asset.Room");
            builtin_vars.TryAdd("room_index", "Asset.Room");

            // Objects
            builtin_vars.TryAdd("content", "Asset.Object");
            builtin_vars.TryAdd("player", "Asset.Object");
            builtin_vars.TryAdd("targetplayer", "Asset.Object");
            builtin_vars.TryAdd("target", "Asset.Object");
            builtin_vars.TryAdd("playerid", "Asset.Object");
            builtin_vars.TryAdd("_playerid", "Asset.Object");
            builtin_vars.TryAdd("player_id", "Asset.Object");
            builtin_vars.TryAdd("platformid", "Asset.Object");
            builtin_vars.TryAdd("objID", "Asset.Object");
            builtin_vars.TryAdd("objectID", "Asset.Object");
            builtin_vars.TryAdd("spawnenemyID", "Asset.Object");
            builtin_vars.TryAdd("ID", "Asset.Object");
            builtin_vars.TryAdd("baddiegrabbedID", "Asset.Object");
            builtin_vars.TryAdd("pizzashieldID", "Asset.Object");
            builtin_vars.TryAdd("angryeffectid", "Asset.Object");
            builtin_vars.TryAdd("pizzashieldid", "Asset.Object");
            builtin_vars.TryAdd("superchargedeffectid", "Asset.Object");
            builtin_vars.TryAdd("baddieID", "Asset.Object");
            builtin_vars.TryAdd("baddieid", "Asset.Object");
            builtin_vars.TryAdd("brickid", "Asset.Object");
            builtin_vars.TryAdd("attackerID", "Asset.Object");
            builtin_vars.TryAdd("object", "Asset.Object");
            builtin_vars.TryAdd("obj", "Asset.Object");
            builtin_vars.TryAdd("_obj", "Asset.Object");
            builtin_vars.TryAdd("closestObj", "Asset.Object");
            builtin_vars.TryAdd("solidObj", "Asset.Object");
            builtin_vars.TryAdd("bg_obj", "Asset.Object");
            builtin_vars.TryAdd("_obj_player", "Asset.Object");
            builtin_vars.TryAdd("obj_explosion", "Asset.Object");
            builtin_vars.TryAdd("my_obj_index", "Asset.Object");
            builtin_vars.TryAdd("inst", "Asset.Object");
            builtin_vars.TryAdd("chargeeffectid", "Asset.Object");
            builtin_vars.TryAdd("dashcloudid", "Asset.Object");
            builtin_vars.TryAdd("crazyruneffectid", "Asset.Object");
            builtin_vars.TryAdd("superslameffectid", "Asset.Object");
            builtin_vars.TryAdd("speedlineseffectid", "Asset.Object");

            // Sprites
            builtin_vars.TryAdd("bpal", "Asset.Sprite");
            builtin_vars.TryAdd("vstitle", "Asset.Sprite");
            builtin_vars.TryAdd("bg", "Asset.Sprite");
            builtin_vars.TryAdd("bg2", "Asset.Sprite");
            builtin_vars.TryAdd("bg3", "Asset.Sprite");
            builtin_vars.TryAdd("playersprshadow", "Asset.Sprite");
            builtin_vars.TryAdd("bosssprshadow", "Asset.Sprite");
            builtin_vars.TryAdd("portrait1_idle", "Asset.Sprite");
            builtin_vars.TryAdd("portrait1_hurt", "Asset.Sprite");
            builtin_vars.TryAdd("portrait2_idle", "Asset.Sprite");
            builtin_vars.TryAdd("portrait2_hurt", "Asset.Sprite");
            builtin_vars.TryAdd("boss_palette", "Asset.Sprite");
            builtin_vars.TryAdd("panicspr", "Asset.Sprite");
            builtin_vars.TryAdd("bossarr", "Asset.Sprite");
            builtin_vars.TryAdd("palettetexture", "Asset.Sprite");
            builtin_vars.TryAdd("switchstart", "Asset.Sprite");
            builtin_vars.TryAdd("switchend", "Asset.Sprite");
            builtin_vars.TryAdd("_hurt", "Asset.Sprite");
            builtin_vars.TryAdd("_dead", "Asset.Sprite");
            builtin_vars.TryAdd("storedspriteindex", "Asset.Sprite");
            builtin_vars.TryAdd("icon", "Asset.Sprite");
            builtin_vars.TryAdd("spridle", "Asset.Sprite");
            builtin_vars.TryAdd("sprgot", "Asset.Sprite");

            // Colors
            builtin_vars.TryAdd("color", "Constant.Color");
            builtin_vars.TryAdd("textcolor", "Constant.Color");
            builtin_vars.TryAdd("bc", "Constant.Color");
            builtin_vars.TryAdd("tc", "Constant.Color");
            builtin_vars.TryAdd("gameframe_blend", "Constant.Color");
            builtin_vars.TryAdd("c1", "Constant.Color");
            builtin_vars.TryAdd("c2", "Constant.Color");

            builtin_vars.TryAdd("gameframe_caption_icon", "Asset.Sprite");

            // Add all from this repo: https://github.com/avievie/PizzaTowerGameSpecificData
            // thanks so much @avievie
            builtin_vars.TryAdd("landspr", "Asset.Sprite");
            builtin_vars.TryAdd("idlespr", "Asset.Sprite");
            builtin_vars.TryAdd("fallspr", "Asset.Sprite");
            builtin_vars.TryAdd("stunfallspr", "Asset.Sprite");
            builtin_vars.TryAdd("walkspr", "Asset.Sprite");
            builtin_vars.TryAdd("turnspr", "Asset.Sprite");
            builtin_vars.TryAdd("recoveryspr", "Asset.Sprite");
            builtin_vars.TryAdd("grabbedspr", "Asset.Sprite");
            builtin_vars.TryAdd("scaredspr", "Asset.Sprite");
            builtin_vars.TryAdd("ragespr", "Asset.Sprite");
            builtin_vars.TryAdd("spr_dead", "Asset.Sprite");
            builtin_vars.TryAdd("spr_palette", "Asset.Sprite");
            builtin_vars.TryAdd("tube_spr", "Asset.Sprite");
            builtin_vars.TryAdd("spr_intro", "Asset.Sprite");
            builtin_vars.TryAdd("spr_introidle", "Asset.Sprite");
            builtin_vars.TryAdd("ts", "Asset.Tileset");
            builtin_vars.TryAdd("t", "Asset.Sprite");
            builtin_vars.TryAdd("spr_attack", "Asset.Sprite");
            builtin_vars.TryAdd("spr_hidden", "Asset.Sprite");
            builtin_vars.TryAdd("spr_idle", "Asset.Sprite");
            builtin_vars.TryAdd("stunspr", "Asset.Sprite");
            builtin_vars.TryAdd("bgsprite", "Asset.Sprite");
            builtin_vars.TryAdd("ratpowerup", "Asset.Object");
            builtin_vars.TryAdd("boss_hpsprite", "Asset.Sprite");
            builtin_vars.TryAdd("pl", "Asset.Object");
            builtin_vars.TryAdd("spr", "Asset.Sprite");
            builtin_vars.TryAdd("expressionsprite", "Asset.Sprite");
            builtin_vars.TryAdd("_spr", "Asset.Sprite");
            builtin_vars.TryAdd("attackdash", "Asset.Sprite");
            builtin_vars.TryAdd("airattackdash", "Asset.Sprite");
            builtin_vars.TryAdd("airattackdashstart", "Asset.Sprite");
            builtin_vars.TryAdd("tauntstoredsprite", "Asset.Sprite");
            builtin_vars.TryAdd("movespr", "Asset.Sprite");
            builtin_vars.TryAdd("spr_joystick", "Asset.Sprite");
            builtin_vars.TryAdd("_select", "Constant.GamepadButton");
            builtin_vars.TryAdd("_back", "Constant.GamepadButton");
            builtin_vars.TryAdd("tvsprite", "Asset.Sprite");
            builtin_vars.TryAdd("sprite", "Asset.Sprite");
            builtin_vars.TryAdd("divisionjustforplayersprites", "Asset.Sprite");
            builtin_vars.TryAdd("spr_move", "Asset.Sprite");
            builtin_vars.TryAdd("spr_crawl", "Asset.Sprite");
            builtin_vars.TryAdd("spr_hurt", "Asset.Sprite");
            builtin_vars.TryAdd("spr_jump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_jump2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_fall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_fall2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_crouch", "Asset.Sprite");
            builtin_vars.TryAdd("spr_crouchjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_crouchfall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_couchstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_land", "Asset.Sprite");
            builtin_vars.TryAdd("spr_land2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_lookdoor", "Asset.Sprite");
            builtin_vars.TryAdd("spr_walkfront", "Asset.Sprite");
            builtin_vars.TryAdd("spr_victory", "Asset.Sprite");
            builtin_vars.TryAdd("spr_Ladder", "Asset.Sprite");
            builtin_vars.TryAdd("spr_laddermove", "Asset.Sprite");
            builtin_vars.TryAdd("spr_ladderdown", "Asset.Sprite");
            builtin_vars.TryAdd("spr_keyget", "Asset.Sprite");
            builtin_vars.TryAdd("spr_crouchslip", "Asset.Sprite");
            builtin_vars.TryAdd("spr_pistolshot", "Asset.Sprite");
            builtin_vars.TryAdd("spr_pistolwalk", "Asset.Sprite");
            builtin_vars.TryAdd("spr_longjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_longjumpend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_breakdance", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machslideboostfall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach3boostfall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mrpinch", "Asset.Sprite");
            builtin_vars.TryAdd("spr_rampjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach", "Asset.Sprite");
            builtin_vars.TryAdd("spr_secondjump1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_secondjump2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machslidestart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machslide", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machslideend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machslideboost", "Asset.Sprite");
            builtin_vars.TryAdd("spr_catched", "Asset.Sprite");
            builtin_vars.TryAdd("spr_punch", "Asset.Sprite");
            builtin_vars.TryAdd("spr_backkick", "Asset.Sprite");
            builtin_vars.TryAdd("spr_shoulder", "Asset.Sprite");
            builtin_vars.TryAdd("spr_uppunch", "Asset.Sprite");
            builtin_vars.TryAdd("spr_stomp", "Asset.Sprite");
            builtin_vars.TryAdd("spr_stompprep", "Asset.Sprite");
            builtin_vars.TryAdd("spr_crouchslide", "Asset.Sprite");
            builtin_vars.TryAdd("spr_climbwall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_grab", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach2jump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_Timesup", "Asset.Sprite");
            builtin_vars.TryAdd("spr_deathstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_deathend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machpunch1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machpunch2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_hurtjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_entergate", "Asset.Sprite");
            builtin_vars.TryAdd("spr_gottreasure", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bossintro", "Asset.Sprite");
            builtin_vars.TryAdd("spr_hurtidle", "Asset.Sprite");
            builtin_vars.TryAdd("spr_hurtwalk", "Asset.Sprite");
            builtin_vars.TryAdd("spr_suplexmash1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_suplexmash2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_suplexmash3", "Asset.Sprite");
            builtin_vars.TryAdd("spr_suplexmash4", "Asset.Sprite");
            builtin_vars.TryAdd("spr_tackle", "Asset.Sprite");
            builtin_vars.TryAdd("spr_airdash1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_airdash2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_idle1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_idle2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_idle3", "Asset.Sprite");
            builtin_vars.TryAdd("spr_idle4", "Asset.Sprite");
            builtin_vars.TryAdd("spr_idle5", "Asset.Sprite");
            builtin_vars.TryAdd("spr_idle6", "Asset.Sprite");
            builtin_vars.TryAdd("spr_wallsplat", "Asset.Sprite");
            builtin_vars.TryAdd("spr_piledriver", "Asset.Sprite");
            builtin_vars.TryAdd("spr_piledriverland", "Asset.Sprite");
            builtin_vars.TryAdd("spr_charge", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach3jump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach4", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machclimbwall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_dive", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machroll", "Asset.Sprite");
            builtin_vars.TryAdd("spr_hitwall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_superjumpland", "Asset.Sprite");
            builtin_vars.TryAdd("spr_walljumpstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_superjumpprep", "Asset.Sprite");
            builtin_vars.TryAdd("spr_superjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_superjumppreplight", "Asset.Sprite");
            builtin_vars.TryAdd("spr_superjumpright", "Asset.Sprite");
            builtin_vars.TryAdd("spr_superjumpleft", "Asset.Sprite");
            builtin_vars.TryAdd("spr_machfreefall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach3hit", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepwalk", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepfall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepidle", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepjumpstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepthunder", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepland", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepdownslope", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepcharge", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepdoublejump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepfly", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepdowntrust", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepupslope", "Asset.Sprite");
            builtin_vars.TryAdd("spr_knightpepbump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bodyslamfall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bodyslamstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bodyslamland", "Asset.Sprite");
            builtin_vars.TryAdd("spr_crazyrun", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bombpeprun", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bombpepintro", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bombpeprunabouttoexplode", "Asset.Sprite");
            builtin_vars.TryAdd("spr_bombpepend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_jetpackstart2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_fireass", "Asset.Sprite");
            builtin_vars.TryAdd("spr_fireassground", "Asset.Sprite");
            builtin_vars.TryAdd("spr_fireassend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_tumblestart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_tumbleend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_tumble", "Asset.Sprite");
            builtin_vars.TryAdd("spr_stunned", "Asset.Sprite");
            builtin_vars.TryAdd("spr_clown", "Asset.Sprite");
            builtin_vars.TryAdd("spr_clownbump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_clowncrouch", "Asset.Sprite");
            builtin_vars.TryAdd("spr_clownfall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_clownjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_clownwallclimb", "Asset.Sprite");
            builtin_vars.TryAdd("spr_downpizzabox", "Asset.Sprite");
            builtin_vars.TryAdd("spr_uppizzabox", "Asset.Sprite");
            builtin_vars.TryAdd("spr_slipnslide", "Asset.Sprite");
            builtin_vars.TryAdd("spr_mach3boost", "Asset.Sprite");
            builtin_vars.TryAdd("spr_facehurtup", "Asset.Sprite");
            builtin_vars.TryAdd("spr_facehurt", "Asset.Sprite");
            builtin_vars.TryAdd("spr_walljumpend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_suplexdash", "Asset.Sprite");
            builtin_vars.TryAdd("spr_suplexdashjumpstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_suplexdashjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_shotgunsuplexdash", "Asset.Sprite");
            builtin_vars.TryAdd("spr_rollgetup", "Asset.Sprite");
            builtin_vars.TryAdd("spr_swingding", "Asset.Sprite");
            builtin_vars.TryAdd("spr_swingdingend", "Asset.Sprite");
            builtin_vars.TryAdd("spr_haulingjump", "Asset.Sprite");
            builtin_vars.TryAdd("spr_haulingidle", "Asset.Sprite");
            builtin_vars.TryAdd("spr_haulingwalk", "Asset.Sprite");
            builtin_vars.TryAdd("spr_haulingstart", "Asset.Sprite");
            builtin_vars.TryAdd("spr_haulingfall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_haulingland", "Asset.Sprite");
            builtin_vars.TryAdd("spr_uppercutfinishingblow", "Asset.Sprite");
            builtin_vars.TryAdd("spr_finishingblow1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_finishingblow2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_finishingblow3", "Asset.Sprite");
            builtin_vars.TryAdd("spr_finishingblow4", "Asset.Sprite");
            builtin_vars.TryAdd("spr_finishingblow5", "Asset.Sprite");
            builtin_vars.TryAdd("spr_winding", "Asset.Sprite");
            builtin_vars.TryAdd("spr_3hpwalk", "Asset.Sprite");
            builtin_vars.TryAdd("spr_3hpidle", "Asset.Sprite");
            builtin_vars.TryAdd("spr_panic", "Asset.Sprite");
            builtin_vars.TryAdd("spr_facestomp", "Asset.Sprite");
            builtin_vars.TryAdd("spr_freefall", "Asset.Sprite");
            builtin_vars.TryAdd("spr_shotgunsuplex", "Asset.Sprite");
            builtin_vars.TryAdd("spr_pushback1", "Asset.Sprite");
            builtin_vars.TryAdd("spr_pushback2", "Asset.Sprite");
            builtin_vars.TryAdd("spr_throw", "Asset.Sprite");
            builtin_vars.TryAdd("spr_run", "Asset.Sprite");
            builtin_vars.TryAdd("spr_shotgunidle", "Asset.Sprite");
            builtin_vars.TryAdd("spr_sworddash", "Asset.Sprite");

            // New shit by CST
            // bro is literally insane
            builtin_vars.TryAdd("_face", "Constant.GamepadButton");
            builtin_vars.TryAdd("objectlist", "Array<Asset.Object>");
            builtin_vars.TryAdd("object_arr", "Array<Asset.Object>");
            builtin_vars.TryAdd("objdark_arr", "Array<Asset.Object>");
            builtin_vars.TryAdd("content_arr", "Array<Asset.Object>");
            builtin_vars.TryAdd("spawnpool", "Array<Asset.Object>");
            builtin_vars.TryAdd("spawn_arr", "Array<Asset.Object>");
            builtin_vars.TryAdd("dark_arr", "Array<Asset.Object>");
            builtin_vars.TryAdd("flash_arr", "Array<Asset.Object>");
            builtin_vars.TryAdd("collision_list", "Array<Asset.Object>");

            builtin_vars.TryAdd("levels", "Array<Asset.Room>");
            builtin_vars.TryAdd("room_arr", "Array<Asset.Room>");

            builtin_vars.TryAdd("treasure_arr", "Array<Asset.Sprite>");

            // Extra Shit i found
            builtin_vars.TryAdd("particlespr", "Asset.Sprite");
            // Global Vars that sometimes are used in older builds
            builtin_vars.TryAdd("heatmeterspr", "Asset.Sprite");
            builtin_vars.TryAdd("heatmetersprfill", "Asset.Sprite");
            builtin_vars.TryAdd("heatmetersprpalette", "Asset.Sprite");
            builtin_vars.TryAdd("pizzascorespr", "Asset.Sprite");
            builtin_vars.TryAdd("rankshudspr", "Asset.Sprite");
            builtin_vars.TryAdd("rankshudsprfill", "Asset.Sprite");
            builtin_vars.TryAdd("tv_combobubble", "Asset.Sprite");
            builtin_vars.TryAdd("tv_combobubblefill", "Asset.Sprite");
            builtin_vars.TryAdd("tv_escapeG", "Asset.Sprite");
            builtin_vars.TryAdd("tv_happyG", "Asset.Sprite");
            builtin_vars.TryAdd("tv_hurtG", "Asset.Sprite");
            builtin_vars.TryAdd("tv_idleG", "Asset.Sprite");
            builtin_vars.TryAdd("tv_barrel", "Asset.Sprite");
            builtin_vars.TryAdd("tv_bombpep", "Asset.Sprite");
            builtin_vars.TryAdd("tv_boxxedpep", "Asset.Sprite");
            builtin_vars.TryAdd("tv_cheeseball", "Asset.Sprite");
            builtin_vars.TryAdd("tv_cheesepep", "Asset.Sprite");
            builtin_vars.TryAdd("tv_clown", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprcollect", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprcombo", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprheat", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprmach3", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprmach4", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprpanic", "Asset.Sprite");
            builtin_vars.TryAdd("tv_fireass", "Asset.Sprite");
            builtin_vars.TryAdd("tv_firemouth", "Asset.Sprite");
            builtin_vars.TryAdd("tv_ghost", "Asset.Sprite");
            builtin_vars.TryAdd("tv_golf", "Asset.Sprite");
            builtin_vars.TryAdd("tv_idle", "Asset.Sprite");
            builtin_vars.TryAdd("tv_idlesecret", "Asset.Sprite");
            builtin_vars.TryAdd("tv_idleanim1", "Asset.Sprite");
            builtin_vars.TryAdd("tv_idleanim2", "Asset.Sprite");
            builtin_vars.TryAdd("tv_knight", "Asset.Sprite");
            builtin_vars.TryAdd("tv_mort", "Asset.Sprite");
            builtin_vars.TryAdd("tv_rocket", "Asset.Sprite");
            builtin_vars.TryAdd("tv_scaredjump", "Asset.Sprite");
            builtin_vars.TryAdd("tv_shotgun", "Asset.Sprite");
            builtin_vars.TryAdd("tv_squished", "Asset.Sprite");
            builtin_vars.TryAdd("tv_tumble", "Asset.Sprite");
            builtin_vars.TryAdd("tv_weenie", "Asset.Sprite");
            builtin_vars.TryAdd("tv_off", "Asset.Sprite");
            builtin_vars.TryAdd("tv_open", "Asset.Sprite");
            builtin_vars.TryAdd("tv_whitenoise", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt1", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt2", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt3", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt4", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt5", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt6", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt7", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt8", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt9", "Asset.Sprite");
            builtin_vars.TryAdd("tv_exprhurt10", "Asset.Sprite");

            builtin_funcs["gml_Script_randomize_animations"] = new[] { "Array<Asset.Sprite>" };

            // Function Arguments that potentially have many arguments
            functionArguments.TryAdd("gml_Script_scr_boss_genericintro", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, null, "Asset.Sprite" },
                    new List<object> { null, null, "Asset.Sprite", null }
                })
            );
            functionArguments.TryAdd("gml_Script_create_debris", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sprite" },
                    new List<object> { "Asset.Sprite", null }
                })
            );
            functionArguments.TryAdd("gml_Script_tdp_action", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, "Constant.GamepadButton" },
                    new List<object> { null, "Constant.GamepadButton", null }
                })
            );
            functionArguments.TryAdd("gml_Script_tdp_input_action", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, "Constant.GamepadButton" },
                    new List<object> { null, "Constant.GamepadButton", null }
                })
            );
            functionArguments.TryAdd("gml_Script_palette_unlock", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, null, null, "Asset.Sprite" },
                    new List<object> { null, null, null, "Asset.Sprite", null },
                    new List<object> { null, null, null, "Asset.Sprite", null, null }
                })
            );

            functionArguments.TryAdd("gml_Script_scr_sound", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sound" },
                    new List<object> { "Asset.Sound", null },
                    new List<object> { "Asset.Sound", null, null }
                })
            );
            functionArguments.TryAdd("gml_Script_scr_soundeffect", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sound" },
                    new List<object> { "Asset.Sound", null },
                    new List<object> { "Asset.Sound", null, null }
                })
            );
            functionArguments.TryAdd("gml_Script_scr_music", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sound" },
                    new List<object> { "Asset.Sound", null },
                    new List<object> { "Asset.Sound", null, null }
                })
            );
            functionArguments.TryAdd("gml_Script_create_debris", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, null, "Asset.Sprite" },
                    new List<object> { null, null, "Asset.Sprite", null }
                })
            );

            // Function Arguments (From UTMTCE)
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

            builtin_funcs["gml_Script_declare_particle"] =
                new[] { null, "Asset.Sprite", null, null };
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

            // add only if PT States were found
            if (JSON_PTStates.Count > 0)
            { 
                enums.TryAdd(
                   "Enum.states",
                    new
                    {
                        Name = "states",
                        Values = JSON_PTStates
                    }
                );
            }

            // Calls this function to search for other enums
            // Also used in the GMS2 Decomp Script
            FindOtherEnums(data);
            // Because this codebase hates me
            AddOtherEnums(data);

            // Init these Arrays
            generalarrays.TryAdd("Array<Asset.Room>", new { MacroType = "ArrayInit", Macro = "Asset.Room" });
            generalarrays.TryAdd("Array<Asset.Object>", new { MacroType = "ArrayInit", Macro = "Asset.Object" });
            // Wasn't in original, but some still reference it, so yeah
            generalarrays.TryAdd("Array<Asset.Sprite>", new { MacroType = "ArrayInit", Macro = "Asset.Sprite" });

            // Merge functionArguments and builtin_funcs
            // Directly include the arrays from builtin_funcs as-is
            var mergedFunctionArguments = functionArguments.Concat(builtin_funcs.ToDictionary(
                pair => pair.Key,
                pair => (object)pair.Value)) // Cast string[] to object so it can merge with the MacroEntry objects
                .ToDictionary(pair => pair.Key, pair => pair.Value);

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
                    // thank you CST!!!
                    General = generalarrays
                },
                // Other Shit Branch
                GlobalNames = new
                {
                    // crackful
                    Variables = builtin_vars,
                    FunctionArguments = mergedFunctionArguments,
                    // Shit just for the Template
                    FunctionReturn = new { }
                },
                // Shit just for the Template
                CodeEntryNames = new { }
            };

            // Convert the parent object to a JSON string
            string jsonString = JsonSerializer.Serialize(PTJSON, new JsonSerializerOptions { WriteIndented = true });
            // goddamn it
            jsonString = jsonString.Replace("\\u003C", "<").Replace("\\u003E", ">");

            // Write main JSON File
            // this is so fucking dumb IT IS A STRING!!!!
            //string dataname = data.GeneralInfo.Name + "";
            string datanameclean = (data.GeneralInfo.Name + "").Replace("\"", "");
            File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Underanalyzer/" + datanameclean + ".json", jsonString);

            // Loader JSON
            string dispnameclean = (data.GeneralInfo.DisplayName + "").Replace("\"", "");
            var loader = new
            {
                LoadOrder = 1,
                Conditions = new[]
                {
                    new
                    {
                        ConditionKind = "DisplayName.Regex",
                        Value = $"(?i)^{dispnameclean}"
                    }
                },
                UnderanalyzerFilename = datanameclean + ".json"
            };
            // Write Loader JSON
            string loaderString = JsonSerializer.Serialize(loader, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Program.GetExecutableDirectory() + "/GameSpecificData/Definitions/" + datanameclean + "_loader.json", loaderString);
            // Notify User that it is done
            Application.Current.MainWindow.ShowMessage("Pizza Tower JSON File made\n\nTo apply the generated JSON File to the Decompiler, please restart the program");
        }
        #region Other Enum Shit
        public static void FindOtherEnums(UndertaleData data) 
        {
            // Particle Enums
            if (data.Code.ByName("gml_Object_obj_particlesystem_Create_0") != null)
            {
                particle_enums.TryAdd("enum_start", 0); // just because
                particle_enums.TryAdd("cloudeffect", 1);
                particle_enums.TryAdd("crazyrunothereffect", 2);
                particle_enums.TryAdd("highjumpcloud1", 3);
                particle_enums.TryAdd("highjumpcloud2", 4);
                particle_enums.TryAdd("jumpdust", 5);
                particle_enums.TryAdd("balloonpop", 6);
                particle_enums.TryAdd("shotgunimpact", 7);
                particle_enums.TryAdd("impact", 8);
                particle_enums.TryAdd("genericpoofeffect", 9);
                particle_enums.TryAdd("keyparticles", 10);
                particle_enums.TryAdd("teleporteffect", 11);
                particle_enums.TryAdd("landcloud", 12);
                particle_enums.TryAdd("ratmountballooncloud", 13);
                particle_enums.TryAdd("groundpoundeffect", 14);
                particle_enums.TryAdd("noisegrounddash", 15);
                particle_enums.TryAdd("bubblepop", 16);
                particle_enums.TryAdd("enum_length", 17); // just because
            }

            // Notification Enums
            if (data.Code.ByName("gml_Script_notification_push") != null) //Searching for this should work
            {
                notification_enums.TryAdd("bodyslam_start", 0);
                notification_enums.TryAdd("bodyslam_end", 1);
                notification_enums.TryAdd("generic_killed", 2);
                notification_enums.TryAdd("room_enemiesdead", 3);
                notification_enums.TryAdd("enemy_parried", 4);
                notification_enums.TryAdd("level_finished", 5);
                notification_enums.TryAdd("mortcube_destroyed", 6);
                notification_enums.TryAdd("hurt", 7);
                notification_enums.TryAdd("fell_into_pit", 8);
                notification_enums.TryAdd("beer_knocked", 9);
                notification_enums.TryAdd("touched_timedgate", 10);
                notification_enums.TryAdd("flush_done", 11);
                notification_enums.TryAdd("baddie_killed_projectile", 12);
                notification_enums.TryAdd("treasureguy_uncovered", 13);
                notification_enums.TryAdd("special_destroyable_destroyed", 14);
                notification_enums.TryAdd("custom_destructibles_destroyed", 15);
                notification_enums.TryAdd("pizzaball_shot", 16);
                notification_enums.TryAdd("pizzaball_kill", 17);
                notification_enums.TryAdd("pizzaball_goal", 18);
                notification_enums.TryAdd("brickball_start", 19);
                notification_enums.TryAdd("john_destroyed", 20);
                notification_enums.TryAdd("brickball_kill", 21);
                notification_enums.TryAdd("pigcitizen_taunt", 22);
                notification_enums.TryAdd("pizzaboy_killed", 23);
                notification_enums.TryAdd("touched_mrpinch", 24);
                notification_enums.TryAdd("priest_touched", 25);
                notification_enums.TryAdd("secret_entered", 26);
                notification_enums.TryAdd("secret_exited", 27);
                notification_enums.TryAdd("iceblock_bird_freed", 28);
                notification_enums.TryAdd("monster_killed", 29);
                notification_enums.TryAdd("monster_activated", 30);
                notification_enums.TryAdd("jumpscared", 31);
                notification_enums.TryAdd("knightpep_bumped", 32);
                notification_enums.TryAdd("cheeseblock_destroyed", 33);
                notification_enums.TryAdd("rat_destroyed_with_baddie", 34);
                notification_enums.TryAdd("rattumble_destroyed", 35);
                notification_enums.TryAdd("rat_destroyed", 36);
                notification_enums.TryAdd("touched_lava", 37);
                notification_enums.TryAdd("touched_cow", 38);
                notification_enums.TryAdd("touched_cow_once", 39);
                notification_enums.TryAdd("touched_gravesurf_once", 40);
                notification_enums.TryAdd("touched_ghostfollow", 41);
                notification_enums.TryAdd("ghost_end", 42);
                notification_enums.TryAdd("superjump_end", 43);
                notification_enums.TryAdd("shotgun_shot", 44);
                notification_enums.TryAdd("shotgun_shot_end", 45);
                notification_enums.TryAdd("destroyable_destroyed", 46);
                notification_enums.TryAdd("bazooka_explosion", 47);
                notification_enums.TryAdd("wartimer_finished", 48);
                notification_enums.TryAdd("totem_reactivated", 49);
                notification_enums.TryAdd("boss_defeated", 50);
                notification_enums.TryAdd("combo_end", 51);
                notification_enums.TryAdd("achievement_unlocked", 52);
                notification_enums.TryAdd("crouched_in_poo", 53);
                notification_enums.TryAdd("game_beaten", 54);
                notification_enums.TryAdd("taunted", 55);
                notification_enums.TryAdd("john_resurrected", 56);
                notification_enums.TryAdd("knight_obtained", 57);
                notification_enums.TryAdd("mooney_unlocked", 58);
                notification_enums.TryAdd("unknown59", 59);
                notification_enums.TryAdd("pumpkin_gotten", 60);
                notification_enums.TryAdd("pumpkindoor_entered", 61);
                notification_enums.TryAdd("trickytreat_failed", 62);
                notification_enums.TryAdd("trickytreat_door_entered", 63);
                notification_enums.TryAdd("tornadoattack_end", 64);
                notification_enums.TryAdd("gate_taunted", 65);
                notification_enums.TryAdd("noisebomb_wasted", 66);
                notification_enums.TryAdd("got_endingrank", 67);
                notification_enums.TryAdd("breakdance_start", 68);
                notification_enums.TryAdd("touched_banana", 69);
                notification_enums.TryAdd("level_finished_pizzaface", 70);
                notification_enums.TryAdd("player_antigrav", 71);
                notification_enums.TryAdd("ptg_seen", 72);
                notification_enums.TryAdd("touched_granny", 73);
            }

            // HOLIDAY
            if (data.Code.ByName("gml_Script_is_holiday") != null)
            {
                holiday_enums.TryAdd("normal", 0);
                holiday_enums.TryAdd("halloween", 1);
            }

            // TV Prompt Enums
            if (data.Code.ByName("gml_Script_tv_push_prompt") != null)
            {
                tv_enums.TryAdd("start", 0);
                tv_enums.TryAdd("highprio", 1);
                tv_enums.TryAdd("normal", 2);
            }

            // Text Enums
            if (data.Code.ByName("gml_Script_scr_draw_text_arr") != null)
            {
                text_enums.TryAdd("none", 0);
                text_enums.TryAdd("shake", 1);
                text_enums.TryAdd("wave", 2);
            }

            // Menu ID Enums
            if (data.Code.ByName("gml_Script_menu_goto") != null)
            {
                menuID_enums.TryAdd("categories", 0);
                menuID_enums.TryAdd("audio", 1);
                menuID_enums.TryAdd("video", 2);
                menuID_enums.TryAdd("window", 3);
                menuID_enums.TryAdd("resolution", 4);
                menuID_enums.TryAdd("unknown5", 5);
                menuID_enums.TryAdd("game", 6);
                menuID_enums.TryAdd("controls", 7);
                menuID_enums.TryAdd("controller", 8);
                menuID_enums.TryAdd("keyboard", 9);
                menuID_enums.TryAdd("deadzones", 10);
                menuID_enums.TryAdd("last", 11);
            }

            // Editor Enums (Unused, but might as well)
            if (data.Code.ByName("gml_Script_editor_set_state") != null)
            {
                editor_enums.TryAdd("init", 0);
                editor_enums.TryAdd("instance_edit", 1);
                editor_enums.TryAdd("unknown2", 2);
                editor_enums.TryAdd("resize_room", 3);
                editor_enums.TryAdd("save_level", 4);
                editor_enums.TryAdd("load_level", 5);
            }
            // AfterImage Enums
            if (data.Code.ByName("gml_Script_particle_set_scale") != null)
            {
                afterimg_enums.TryAdd("normal", 0);
                afterimg_enums.TryAdd("mach3effect", 1);
                afterimg_enums.TryAdd("heatattack", 2);
                afterimg_enums.TryAdd("firemouth", 3);
                afterimg_enums.TryAdd("blue", 4);
                afterimg_enums.TryAdd("blur", 5);
                afterimg_enums.TryAdd("red", 6);
                afterimg_enums.TryAdd("red_alt", 7);
                afterimg_enums.TryAdd("noise", 8);
                afterimg_enums.TryAdd("last", 9);
            }

            // TDP Input Enums
            if (data.Code.ByName("gml_Script_tdp_get_icon") != null)
            {
                tdp_input_enums.TryAdd("keyboard", 0);
                tdp_input_enums.TryAdd("gamepad_button", 1);
                tdp_input_enums.TryAdd("gamepad_axis", 2);
            }

            // Menu Anchor Enums
            if (data.Code.ByName("gml_Script_create_menu_fixed") != null)
            {
                menuanchors_enums.TryAdd("center", 0);
                menuanchors_enums.TryAdd("left", 1);
            }

            // add new ones here
        }

        public static void AddOtherEnums(UndertaleData data) 
        {
            if (data.Code.ByName("gml_Object_obj_particlesystem_Create_0") != null) 
            {
                builtin_funcs["gml_Script_declare_particle"] = new[] { "Enum.particle", "Asset.Sprite", null, null };
                builtin_funcs["gml_Script_particle_set_scale"] = new[] { "Enum.particle", null, null };

                functionArguments.TryAdd("gml_Script_create_particle", new MacroEntry(
                "Union",
                new List<List<object>>
                    {
                        new List<object> { null, null, "Enum.particle" },
                        new List<object> { null, null, "Enum.particle", null }
                    })
                );
                enums.TryAdd(
                   "Enum.particle",
                    new
                    {
                        Name = "particle",
                        Values = particle_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_notification_push") != null) 
            {
                builtin_funcs["gml_Script_notification_push"] = new[] { "Enum.Notification", null };

                enums.TryAdd(
                   "Enum.Notification",
                    new
                    {
                        Name = "notifications",
                        Values = notification_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_is_holiday") != null) 
            {
                builtin_funcs["gml_Script_is_holiday"] = new[] { "Enum.Holiday" };
                enums.TryAdd(
                   "Enum.Holiday",
                    new
                    {
                        Name = "holidays",
                        Values = holiday_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_tv_push_prompt") != null) 
            {
                builtin_funcs["gml_Script_tv_push_prompt"] = new[] { null, "Enum.TVPromptTypes", null, null };
                generalarrays.TryAdd("Array<Enum.TVPromptTypes>", new { MacroType = "ArrayInit", Macro = "Enum.TVPromptTypes" });
                enums.TryAdd(
                   "Enum.TVPromptTypes",
                    new
                    {
                        Name = "tv_prompttypes",
                        Values = tv_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_scr_draw_text_arr") != null) 
            {
                builtin_funcs["gml_Script_scr_draw_text_arr"] = new[] { null, null, null, null, "Enum.TextEffect" };
                enums.TryAdd(
                   "Enum.TextEffect",
                    new
                    {
                        Name = "text_effects",
                        Values = text_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_menu_goto") != null) 
            {
                builtin_funcs["gml_Script_menu_goto"] = new[] { "Enum.MenuIDs" };
                enums.TryAdd(
                   "Enum.MenuIDs",
                    new
                    {
                        Name = "menuids",
                        Values = menuID_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_editor_set_state") != null) 
            {
                builtin_funcs["gml_Script_editor_set_state"] = new[] { "Enum.EditorState" };
                enums.TryAdd(
                   "Enum.EditorState",
                    new
                    {
                        Name = "editorstates",
                        Values = editor_enums
                    }
                );
            }
            // AfterImages
            if (data.Code.ByName("gml_Script_particle_set_scale") != null) 
            {
                builtin_vars.TryAdd("identifier", "Enum.AfterimageType");
                enums.TryAdd(
                   "Enum.AfterimageType",
                    new
                    {
                        Name = "afterimagetype",
                        Values = afterimg_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_tdp_get_icon") != null) 
            {
                // WHAT THE FUCK IS THIS
                functionArguments.TryAdd("gml_Script_anon_tdp_input_key_gml_GlobalScript_tdp_input_classes_316_tdp_input_key_gml_GlobalScript_tdp_input_classes", new MacroEntry(
                "Union",
                new List<List<object>>
                    {
                    new List<object> { "Enum.TDPInputActionType", null },
                    new List<object> { "Enum.TDPInputActionType", null, null }
                    })
                );

                enums.TryAdd(
                   "Enum.TDPInputActionType",
                    new
                    {
                        Name = "tdp_input_actiontypes",
                        Values = tdp_input_enums
                    }
                );
            }
            if (data.Code.ByName("gml_Script_create_menu_fixed") != null) 
            {
                functionArguments.TryAdd("gml_Script_create_menu_fixed", new MacroEntry(
                    "Union",
                    new List<List<object>>
                        {
                        new List<object> { "Enum.MenuIDs", "Enum.MenuAnchor", null, null },
                        new List<object> { "Enum.MenuIDs", "Enum.MenuAnchor", null, null, null }
                        })
                    );

                enums.TryAdd(
                   "Enum.MenuAnchor",
                    new
                    {
                        Name = "menuanchors",
                        Values = menuanchors_enums
                    }
                );
            }
        }
        #endregion

        // Detects PT state names (thank you so much utmtce)
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
                    ) continue;

                    int stateID = instr.ValueInt;//Convert.ToInt32(instr.Value);
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
                    //if (
                    //    next2.Kind != UndertaleInstruction.Opcode.Bt
                    //) continue;
                    UndertaleInstruction newInstr =
                        code.GetInstructionFromAddress(next2.Address + (uint)next2.JumpOffset);

                    //if (newInstr == null) continue;

                    // for loop is broken
                    // probably because of IndexOf()
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
                        string funcName = thisInstr?.ValueFunction?.Name?.Content;
                        Application.Current.MainWindow.ShowMessage("Breakpoint WHO KNOWS: " + funcName);// TESTING
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
            /*
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
            */
        }
    }
}