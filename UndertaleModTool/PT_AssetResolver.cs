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

            PT_AssetResolver.PTStates.Clear();

            // do this to eventually convert to json
            // so that it can be read by GameSpecificRegistry(Convertor) or something
            builtin_funcs = new Dictionary<string, string[]>
            { };

            builtin_vars = new Dictionary<string, string>
            { };

            functionArguments = new Dictionary<string, object>
            { };

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
                    // New ones
                    builtin_vars.Add("attack_pool", "Array<Enum.states>");
                    builtin_vars.Add("transformation", "Enum.states");

                    // Function Arguments
                    builtin_funcs["gml_Script_vigilante_cancel_attack"] = new[] { "Enum.states", null };
                    builtin_funcs["gml_Script_scr_bombshoot"] = new[] { "Enum.states" };
                }
            }
            catch (Exception e)
            {
                Application.Current.MainWindow.ShowWarning("Failed to read data\nFailed to Extract Pizza Tower Enums");
            }

            // Variable Definitions

            // Rooms
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
            builtin_vars.Add("rm", "Asset.Room");
            builtin_vars.Add("room_index", "Asset.Room");

            // Objects
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

            // Sprites
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
            builtin_vars.Add("storedspriteindex", "Asset.Sprite");
            builtin_vars.Add("icon", "Asset.Sprite");
            builtin_vars.Add("spridle", "Asset.Sprite");
            builtin_vars.Add("sprgot", "Asset.Sprite");

            // Colors
            builtin_vars.Add("color", "Constant.Color");
            builtin_vars.Add("textcolor", "Constant.Color");
            builtin_vars.Add("bc", "Constant.Color");
            builtin_vars.Add("tc", "Constant.Color");
            builtin_vars.Add("gameframe_blend", "Constant.Color");
            builtin_vars.Add("c1", "Constant.Color");
            builtin_vars.Add("c2", "Constant.Color");

            builtin_vars.Add("gameframe_caption_icon", "Asset.Sprite");

            // Add all from this repo: https://github.com/avievie/PizzaTowerGameSpecificData
            // thanks so much @avievie
            builtin_vars.Add("landspr", "Asset.Sprite");
            builtin_vars.Add("idlespr", "Asset.Sprite");
            builtin_vars.Add("fallspr", "Asset.Sprite");
            builtin_vars.Add("stunfallspr", "Asset.Sprite");
            builtin_vars.Add("walkspr", "Asset.Sprite");
            builtin_vars.Add("turnspr", "Asset.Sprite");
            builtin_vars.Add("recoveryspr", "Asset.Sprite");
            builtin_vars.Add("grabbedspr", "Asset.Sprite");
            builtin_vars.Add("scaredspr", "Asset.Sprite");
            builtin_vars.Add("ragespr", "Asset.Sprite");
            builtin_vars.Add("spr_dead", "Asset.Sprite");
            builtin_vars.Add("spr_palette", "Asset.Sprite");
            builtin_vars.Add("tube_spr", "Asset.Sprite");
            builtin_vars.Add("spr_intro", "Asset.Sprite");
            builtin_vars.Add("spr_introidle", "Asset.Sprite");
            builtin_vars.Add("ts", "Asset.Tileset");
            builtin_vars.Add("t", "Asset.Sprite");
            builtin_vars.Add("spr_attack", "Asset.Sprite");
            builtin_vars.Add("spr_hidden", "Asset.Sprite");
            builtin_vars.Add("spr_idle", "Asset.Sprite");
            builtin_vars.Add("stunspr", "Asset.Sprite");
            builtin_vars.Add("bgsprite", "Asset.Sprite");
            builtin_vars.Add("ratpowerup", "Asset.Object");
            builtin_vars.Add("boss_hpsprite", "Asset.Sprite");
            builtin_vars.Add("pl", "Asset.Object");
            builtin_vars.Add("spr", "Asset.Sprite");
            builtin_vars.Add("expressionsprite", "Asset.Sprite");
            builtin_vars.Add("_spr", "Asset.Sprite");
            builtin_vars.Add("attackdash", "Asset.Sprite");
            builtin_vars.Add("airattackdash", "Asset.Sprite");
            builtin_vars.Add("airattackdashstart", "Asset.Sprite");
            builtin_vars.Add("tauntstoredsprite", "Asset.Sprite");
            builtin_vars.Add("movespr", "Asset.Sprite");
            builtin_vars.Add("spr_joystick", "Asset.Sprite");
            builtin_vars.Add("_select", "Constant.GamepadButton");
            builtin_vars.Add("_back", "Constant.GamepadButton");
            builtin_vars.Add("tvsprite", "Asset.Sprite");
            builtin_vars.Add("sprite", "Asset.Sprite");
            builtin_vars.Add("divisionjustforplayersprites", "Asset.Sprite");
            builtin_vars.Add("spr_move", "Asset.Sprite");
            builtin_vars.Add("spr_crawl", "Asset.Sprite");
            builtin_vars.Add("spr_hurt", "Asset.Sprite");
            builtin_vars.Add("spr_jump", "Asset.Sprite");
            builtin_vars.Add("spr_jump2", "Asset.Sprite");
            builtin_vars.Add("spr_fall", "Asset.Sprite");
            builtin_vars.Add("spr_fall2", "Asset.Sprite");
            builtin_vars.Add("spr_crouch", "Asset.Sprite");
            builtin_vars.Add("spr_crouchjump", "Asset.Sprite");
            builtin_vars.Add("spr_crouchfall", "Asset.Sprite");
            builtin_vars.Add("spr_couchstart", "Asset.Sprite");
            builtin_vars.Add("spr_bump", "Asset.Sprite");
            builtin_vars.Add("spr_land", "Asset.Sprite");
            builtin_vars.Add("spr_land2", "Asset.Sprite");
            builtin_vars.Add("spr_lookdoor", "Asset.Sprite");
            builtin_vars.Add("spr_walkfront", "Asset.Sprite");
            builtin_vars.Add("spr_victory", "Asset.Sprite");
            builtin_vars.Add("spr_Ladder", "Asset.Sprite");
            builtin_vars.Add("spr_laddermove", "Asset.Sprite");
            builtin_vars.Add("spr_ladderdown", "Asset.Sprite");
            builtin_vars.Add("spr_keyget", "Asset.Sprite");
            builtin_vars.Add("spr_crouchslip", "Asset.Sprite");
            builtin_vars.Add("spr_pistolshot", "Asset.Sprite");
            builtin_vars.Add("spr_pistolwalk", "Asset.Sprite");
            builtin_vars.Add("spr_longjump", "Asset.Sprite");
            builtin_vars.Add("spr_longjumpend", "Asset.Sprite");
            builtin_vars.Add("spr_breakdance", "Asset.Sprite");
            builtin_vars.Add("spr_machslideboostfall", "Asset.Sprite");
            builtin_vars.Add("spr_mach3boostfall", "Asset.Sprite");
            builtin_vars.Add("spr_mrpinch", "Asset.Sprite");
            builtin_vars.Add("spr_rampjump", "Asset.Sprite");
            builtin_vars.Add("spr_mach1", "Asset.Sprite");
            builtin_vars.Add("spr_mach", "Asset.Sprite");
            builtin_vars.Add("spr_secondjump1", "Asset.Sprite");
            builtin_vars.Add("spr_secondjump2", "Asset.Sprite");
            builtin_vars.Add("spr_machslidestart", "Asset.Sprite");
            builtin_vars.Add("spr_machslide", "Asset.Sprite");
            builtin_vars.Add("spr_machslideend", "Asset.Sprite");
            builtin_vars.Add("spr_machslideboost", "Asset.Sprite");
            builtin_vars.Add("spr_catched", "Asset.Sprite");
            builtin_vars.Add("spr_punch", "Asset.Sprite");
            builtin_vars.Add("spr_backkick", "Asset.Sprite");
            builtin_vars.Add("spr_shoulder", "Asset.Sprite");
            builtin_vars.Add("spr_uppunch", "Asset.Sprite");
            builtin_vars.Add("spr_stomp", "Asset.Sprite");
            builtin_vars.Add("spr_stompprep", "Asset.Sprite");
            builtin_vars.Add("spr_crouchslide", "Asset.Sprite");
            builtin_vars.Add("spr_climbwall", "Asset.Sprite");
            builtin_vars.Add("spr_grab", "Asset.Sprite");
            builtin_vars.Add("spr_mach2jump", "Asset.Sprite");
            builtin_vars.Add("spr_Timesup", "Asset.Sprite");
            builtin_vars.Add("spr_deathstart", "Asset.Sprite");
            builtin_vars.Add("spr_deathend", "Asset.Sprite");
            builtin_vars.Add("spr_machpunch1", "Asset.Sprite");
            builtin_vars.Add("spr_machpunch2", "Asset.Sprite");
            builtin_vars.Add("spr_hurtjump", "Asset.Sprite");
            builtin_vars.Add("spr_entergate", "Asset.Sprite");
            builtin_vars.Add("spr_gottreasure", "Asset.Sprite");
            builtin_vars.Add("spr_bossintro", "Asset.Sprite");
            builtin_vars.Add("spr_hurtidle", "Asset.Sprite");
            builtin_vars.Add("spr_hurtwalk", "Asset.Sprite");
            builtin_vars.Add("spr_suplexmash1", "Asset.Sprite");
            builtin_vars.Add("spr_suplexmash2", "Asset.Sprite");
            builtin_vars.Add("spr_suplexmash3", "Asset.Sprite");
            builtin_vars.Add("spr_suplexmash4", "Asset.Sprite");
            builtin_vars.Add("spr_tackle", "Asset.Sprite");
            builtin_vars.Add("spr_airdash1", "Asset.Sprite");
            builtin_vars.Add("spr_airdash2", "Asset.Sprite");
            builtin_vars.Add("spr_idle1", "Asset.Sprite");
            builtin_vars.Add("spr_idle2", "Asset.Sprite");
            builtin_vars.Add("spr_idle3", "Asset.Sprite");
            builtin_vars.Add("spr_idle4", "Asset.Sprite");
            builtin_vars.Add("spr_idle5", "Asset.Sprite");
            builtin_vars.Add("spr_idle6", "Asset.Sprite");
            builtin_vars.Add("spr_wallsplat", "Asset.Sprite");
            builtin_vars.Add("spr_piledriver", "Asset.Sprite");
            builtin_vars.Add("spr_piledriverland", "Asset.Sprite");
            builtin_vars.Add("spr_charge", "Asset.Sprite");
            builtin_vars.Add("spr_mach3jump", "Asset.Sprite");
            builtin_vars.Add("spr_mach4", "Asset.Sprite");
            builtin_vars.Add("spr_machclimbwall", "Asset.Sprite");
            builtin_vars.Add("spr_dive", "Asset.Sprite");
            builtin_vars.Add("spr_machroll", "Asset.Sprite");
            builtin_vars.Add("spr_hitwall", "Asset.Sprite");
            builtin_vars.Add("spr_superjumpland", "Asset.Sprite");
            builtin_vars.Add("spr_walljumpstart", "Asset.Sprite");
            builtin_vars.Add("spr_superjumpprep", "Asset.Sprite");
            builtin_vars.Add("spr_superjump", "Asset.Sprite");
            builtin_vars.Add("spr_superjumppreplight", "Asset.Sprite");
            builtin_vars.Add("spr_superjumpright", "Asset.Sprite");
            builtin_vars.Add("spr_superjumpleft", "Asset.Sprite");
            builtin_vars.Add("spr_machfreefall", "Asset.Sprite");
            builtin_vars.Add("spr_mach3hit", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepwalk", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepjump", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepfall", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepidle", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepjumpstart", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepthunder", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepland", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepdownslope", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepstart", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepcharge", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepdoublejump", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepfly", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepdowntrust", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepupslope", "Asset.Sprite");
            builtin_vars.Add("spr_knightpepbump", "Asset.Sprite");
            builtin_vars.Add("spr_bodyslamfall", "Asset.Sprite");
            builtin_vars.Add("spr_bodyslamstart", "Asset.Sprite");
            builtin_vars.Add("spr_bodyslamland", "Asset.Sprite");
            builtin_vars.Add("spr_crazyrun", "Asset.Sprite");
            builtin_vars.Add("spr_bombpeprun", "Asset.Sprite");
            builtin_vars.Add("spr_bombpepintro", "Asset.Sprite");
            builtin_vars.Add("spr_bombpeprunabouttoexplode", "Asset.Sprite");
            builtin_vars.Add("spr_bombpepend", "Asset.Sprite");
            builtin_vars.Add("spr_jetpackstart2", "Asset.Sprite");
            builtin_vars.Add("spr_fireass", "Asset.Sprite");
            builtin_vars.Add("spr_fireassground", "Asset.Sprite");
            builtin_vars.Add("spr_fireassend", "Asset.Sprite");
            builtin_vars.Add("spr_tumblestart", "Asset.Sprite");
            builtin_vars.Add("spr_tumbleend", "Asset.Sprite");
            builtin_vars.Add("spr_tumble", "Asset.Sprite");
            builtin_vars.Add("spr_stunned", "Asset.Sprite");
            builtin_vars.Add("spr_clown", "Asset.Sprite");
            builtin_vars.Add("spr_clownbump", "Asset.Sprite");
            builtin_vars.Add("spr_clowncrouch", "Asset.Sprite");
            builtin_vars.Add("spr_clownfall", "Asset.Sprite");
            builtin_vars.Add("spr_clownjump", "Asset.Sprite");
            builtin_vars.Add("spr_clownwallclimb", "Asset.Sprite");
            builtin_vars.Add("spr_downpizzabox", "Asset.Sprite");
            builtin_vars.Add("spr_uppizzabox", "Asset.Sprite");
            builtin_vars.Add("spr_slipnslide", "Asset.Sprite");
            builtin_vars.Add("spr_mach3boost", "Asset.Sprite");
            builtin_vars.Add("spr_facehurtup", "Asset.Sprite");
            builtin_vars.Add("spr_facehurt", "Asset.Sprite");
            builtin_vars.Add("spr_walljumpend", "Asset.Sprite");
            builtin_vars.Add("spr_suplexdash", "Asset.Sprite");
            builtin_vars.Add("spr_suplexdashjumpstart", "Asset.Sprite");
            builtin_vars.Add("spr_suplexdashjump", "Asset.Sprite");
            builtin_vars.Add("spr_shotgunsuplexdash", "Asset.Sprite");
            builtin_vars.Add("spr_rollgetup", "Asset.Sprite");
            builtin_vars.Add("spr_swingding", "Asset.Sprite");
            builtin_vars.Add("spr_swingdingend", "Asset.Sprite");
            builtin_vars.Add("spr_haulingjump", "Asset.Sprite");
            builtin_vars.Add("spr_haulingidle", "Asset.Sprite");
            builtin_vars.Add("spr_haulingwalk", "Asset.Sprite");
            builtin_vars.Add("spr_haulingstart", "Asset.Sprite");
            builtin_vars.Add("spr_haulingfall", "Asset.Sprite");
            builtin_vars.Add("spr_haulingland", "Asset.Sprite");
            builtin_vars.Add("spr_uppercutfinishingblow", "Asset.Sprite");
            builtin_vars.Add("spr_finishingblow1", "Asset.Sprite");
            builtin_vars.Add("spr_finishingblow2", "Asset.Sprite");
            builtin_vars.Add("spr_finishingblow3", "Asset.Sprite");
            builtin_vars.Add("spr_finishingblow4", "Asset.Sprite");
            builtin_vars.Add("spr_finishingblow5", "Asset.Sprite");
            builtin_vars.Add("spr_winding", "Asset.Sprite");
            builtin_vars.Add("spr_3hpwalk", "Asset.Sprite");
            builtin_vars.Add("spr_3hpidle", "Asset.Sprite");
            builtin_vars.Add("spr_panic", "Asset.Sprite");
            builtin_vars.Add("spr_facestomp", "Asset.Sprite");
            builtin_vars.Add("spr_freefall", "Asset.Sprite");
            builtin_vars.Add("spr_shotgunsuplex", "Asset.Sprite");
            builtin_vars.Add("spr_pushback1", "Asset.Sprite");
            builtin_vars.Add("spr_pushback2", "Asset.Sprite");
            builtin_vars.Add("spr_throw", "Asset.Sprite");
            builtin_vars.Add("spr_run", "Asset.Sprite");
            builtin_vars.Add("spr_shotgunidle", "Asset.Sprite");
            builtin_vars.Add("spr_sworddash", "Asset.Sprite");

            // New shit by CST
            // bro is literally insane
            builtin_vars.Add("_face", "Constant.GamepadButton");
            builtin_vars.Add("objectlist", "Array<Asset.Object>");
            builtin_vars.Add("object_arr", "Array<Asset.Object>");
            builtin_vars.Add("objdark_arr", "Array<Asset.Object>");
            builtin_vars.Add("content_arr", "Array<Asset.Object>");
            builtin_vars.Add("spawnpool", "Array<Asset.Object>");
            builtin_vars.Add("spawn_arr", "Array<Asset.Object>");
            builtin_vars.Add("dark_arr", "Array<Asset.Object>");
            builtin_vars.Add("flash_arr", "Array<Asset.Object>");
            builtin_vars.Add("collision_list", "Array<Asset.Object>");

            builtin_vars.Add("levels", "Array<Asset.Room>");
            builtin_vars.Add("room_arr", "Array<Asset.Room>");

            builtin_vars.Add("treasure_arr", "Array<Asset.Sprite>");

            // Extra Shit i found
            builtin_vars.Add("particlespr", "Asset.Sprite");
            // Global Vars that sometimes are used in older builds
            builtin_vars.Add("heatmeterspr", "Asset.Sprite");
            builtin_vars.Add("heatmetersprfill", "Asset.Sprite");
            builtin_vars.Add("heatmetersprpalette", "Asset.Sprite");
            builtin_vars.Add("pizzascorespr", "Asset.Sprite");
            builtin_vars.Add("rankshudspr", "Asset.Sprite");
            builtin_vars.Add("rankshudsprfill", "Asset.Sprite");
            builtin_vars.Add("tv_combobubble", "Asset.Sprite");
            builtin_vars.Add("tv_combobubblefill", "Asset.Sprite");
            builtin_vars.Add("tv_escapeG", "Asset.Sprite");
            builtin_vars.Add("tv_happyG", "Asset.Sprite");
            builtin_vars.Add("tv_hurtG", "Asset.Sprite");
            builtin_vars.Add("tv_idleG", "Asset.Sprite");
            builtin_vars.Add("tv_barrel", "Asset.Sprite");
            builtin_vars.Add("tv_bombpep", "Asset.Sprite");
            builtin_vars.Add("tv_boxxedpep", "Asset.Sprite");
            builtin_vars.Add("tv_cheeseball", "Asset.Sprite");
            builtin_vars.Add("tv_cheesepep", "Asset.Sprite");
            builtin_vars.Add("tv_clown", "Asset.Sprite");
            builtin_vars.Add("tv_exprcollect", "Asset.Sprite");
            builtin_vars.Add("tv_exprcombo", "Asset.Sprite");
            builtin_vars.Add("tv_exprheat", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt", "Asset.Sprite");
            builtin_vars.Add("tv_exprmach3", "Asset.Sprite");
            builtin_vars.Add("tv_exprmach4", "Asset.Sprite");
            builtin_vars.Add("tv_exprpanic", "Asset.Sprite");
            builtin_vars.Add("tv_fireass", "Asset.Sprite");
            builtin_vars.Add("tv_firemouth", "Asset.Sprite");
            builtin_vars.Add("tv_ghost", "Asset.Sprite");
            builtin_vars.Add("tv_golf", "Asset.Sprite");
            builtin_vars.Add("tv_idle", "Asset.Sprite");
            builtin_vars.Add("tv_idlesecret", "Asset.Sprite");
            builtin_vars.Add("tv_idleanim1", "Asset.Sprite");
            builtin_vars.Add("tv_idleanim2", "Asset.Sprite");
            builtin_vars.Add("tv_knight", "Asset.Sprite");
            builtin_vars.Add("tv_mort", "Asset.Sprite");
            builtin_vars.Add("tv_rocket", "Asset.Sprite");
            builtin_vars.Add("tv_scaredjump", "Asset.Sprite");
            builtin_vars.Add("tv_shotgun", "Asset.Sprite");
            builtin_vars.Add("tv_squished", "Asset.Sprite");
            builtin_vars.Add("tv_tumble", "Asset.Sprite");
            builtin_vars.Add("tv_weenie", "Asset.Sprite");
            builtin_vars.Add("tv_off", "Asset.Sprite");
            builtin_vars.Add("tv_open", "Asset.Sprite");
            builtin_vars.Add("tv_whitenoise", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt1", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt2", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt3", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt4", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt5", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt6", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt7", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt8", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt9", "Asset.Sprite");
            builtin_vars.Add("tv_exprhurt10", "Asset.Sprite");

            builtin_funcs["gml_Script_randomize_animations"] = new[] { "Array<Asset.Sprite>" };

            // Function Arguments that potentially have many arguments
            functionArguments.Add("gml_Script_scr_boss_genericintro", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, null, "Asset.Sprite" },
                    new List<object> { null, null, "Asset.Sprite", null }
                })
            );
            functionArguments.Add("gml_Script_create_debris", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sprite" },
                    new List<object> { "Asset.Sprite", null }
                })
            );
            functionArguments.Add("gml_Script_tdp_action", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, "Constant.GamepadButton" },
                    new List<object> { null, "Constant.GamepadButton", null }
                })
            );
            functionArguments.Add("gml_Script_tdp_input_action", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, "Constant.GamepadButton" },
                    new List<object> { null, "Constant.GamepadButton", null }
                })
            );
            functionArguments.Add("gml_Script_palette_unlock", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { null, null, null, "Asset.Sprite" },
                    new List<object> { null, null, null, "Asset.Sprite", null },
                    new List<object> { null, null, null, "Asset.Sprite", null, null }
                })
            );

            functionArguments.Add("gml_Script_scr_sound", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sound" },
                    new List<object> { "Asset.Sound", null },
                    new List<object> { "Asset.Sound", null, null }
                })
            );
            functionArguments.Add("gml_Script_scr_soundeffect", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sound" },
                    new List<object> { "Asset.Sound", null },
                    new List<object> { "Asset.Sound", null, null }
                })
            );
            functionArguments.Add("gml_Script_scr_music", new MacroEntry(
            "Union",
            new List<List<object>>
                {
                    new List<object> { "Asset.Sound" },
                    new List<object> { "Asset.Sound", null },
                    new List<object> { "Asset.Sound", null, null }
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
                enums.Add(
                   "Enum.states",
                    new
                    {
                        Name = "states",
                        Values = JSON_PTStates
                    }
                );
            }

            // Call for other Enums
            // was exported to void function so that decompiler script can use it
            FindOtherEnums(data);

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
                    General = new Dictionary<string, object>
                    {
                        { "Array<Asset.Room>", new { MacroType = "ArrayInit", Macro = "Asset.Room" } },
                        { "Array<Asset.Object>", new { MacroType = "ArrayInit", Macro = "Asset.Object" } },
                        { "Array<Enum.states>", new { MacroType = "ArrayInit", Macro = "Enum.states" } },
                        { "Array<Enum.TVPromptTypes>", new { MacroType = "ArrayInit", Macro = "Enum.TVPromptTypes" } }
                    }
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

        public static void FindOtherEnums(UndertaleData data) 
        {
            // Particle Enums
            if (data.Code.ByName("gml_Object_obj_particlesystem_Create_0") != null)
            {
                particle_enums = new Dictionary<string, int>
                {
                    { "enum_start", 0 }, //just because
                    { "cloudeffect", 1 },
                    { "crazyrunothereffect", 2 },
                    { "highjumpcloud1", 3 },
                    { "highjumpcloud2", 4 },
                    { "jumpdust", 5 },
                    { "balloonpop", 6 },
                    { "shotgunimpact", 7 },
                    { "impact", 8 },
                    { "genericpoofeffect", 9 },
                    { "keyparticles", 10 },
                    { "teleporteffect", 11 },
                    { "landcloud", 12 },
                    { "ratmountballooncloud", 13 },
                    { "groundpoundeffect", 14 },
                    { "noisegrounddash", 15 },
                    { "bubblepop", 16 },
                    { "enum_length", 17 }, // just because
                };
                builtin_funcs["gml_Script_declare_particle"] = new[] { "Enum.particle", "Asset.Sprite", null, null };

                functionArguments.Add("gml_Script_create_particle", new MacroEntry(
                "Union",
                new List<List<object>>
                    {
                        new List<object> { null, null, "Enum.particle" },
                        new List<object> { null, null, "Enum.particle", null }
                    })
                );
                enums.Add(
                   "Enum.particle",
                    new
                    {
                        Name = "particle",
                        Values = particle_enums
                    }
                );
            }

            // Notification Enums
            if (data.Code.ByName("gml_Script_notification_push") != null) //Searching for this should work
            {
                notification_enums = new Dictionary<string, int>
                // maybe just public dictionary if i can get the decomp script to work with this
                {
                    { "bodyslam_start", 0 },
                    { "bodyslam_end", 1 },
                    { "generic_killed", 2 },
                    { "room_enemiesdead", 3 },
                    { "enemy_parried", 4 },
                    { "level_finished", 5 },
                    { "mortcube_destroyed", 6 },
                    { "hurt", 7 },
                    { "fell_into_pit", 8 },
                    { "beer_knocked", 9 },
                    { "touched_timedgate", 10 },
                    { "flush_done", 11 },
                    { "baddie_killed_projectile", 12 },
                    { "treasureguy_uncovered", 13 },
                    { "special_destroyable_destroyed", 14 },
                    { "custom_destructibles_destroyed", 15 },
                    { "pizzaball_shot", 16 },
                    { "pizzaball_kill", 17 },
                    { "pizzaball_goal", 18 },
                    { "brickball_start", 19 },
                    { "john_destroyed", 20 },
                    { "brickball_kill", 21 },
                    { "pigcitizen_taunt", 22 },
                    { "pizzaboy_killed", 23 },
                    { "touched_mrpinch", 24 },
                    { "priest_touched", 25 },
                    { "secret_entered", 26 },
                    { "secret_exited", 27 },
                    { "iceblock_bird_freed", 28 },
                    { "monster_killed", 29 },
                    { "monster_activated", 30 },
                    { "jumpscared", 31 },
                    { "knightpep_bumped", 32 },
                    { "cheeseblock_destroyed", 33 },
                    { "rat_destroyed_with_baddie", 34 },
                    { "rattumble_destroyed", 35 },
                    { "rat_destroyed", 36 },
                    { "touched_lava", 37 },
                    { "touched_cow", 38 },
                    { "touched_cow_once", 39 },
                    { "touched_gravesurf_once", 40 },
                    { "touched_ghostfollow", 41 },
                    { "ghost_end", 42 },
                    { "superjump_end", 43 },
                    { "shotgun_shot", 44 },
                    { "shotgun_shot_end", 45 },
                    { "destroyable_destroyed", 46 },
                    { "bazooka_explosion", 47 },
                    { "wartimer_finished", 48 },
                    { "totem_reactivated", 49 },
                    { "boss_defeated", 50 },
                    { "combo_end", 51 },
                    { "achievement_unlocked", 52 },
                    { "crouched_in_poo", 53 },
                    { "game_beaten", 54 },
                    { "taunted", 55 },
                    { "john_resurrected", 56 },
                    { "knight_obtained", 57 },
                    { "mooney_unlocked", 58 },
                    { "unknown59", 59 },
                    { "pumpkin_gotten", 60 },
                    { "pumpkindoor_entered", 61 },
                    { "trickytreat_failed", 62 },
                    { "trickytreat_door_entered", 63 },
                    { "tornadoattack_end", 64 },
                    { "gate_taunted", 65 },
                    { "noisebomb_wasted", 66 },
                    { "got_endingrank", 67 },
                    { "breakdance_start", 68 },
                    { "touched_banana", 69 },
                    { "level_finished_pizzaface", 70 },
                    { "player_antigrav", 71 },
                    { "ptg_seen", 72 },
                    { "touched_granny", 73 },
                };
                builtin_funcs["gml_Script_notification_push"] = new[] { "Enum.Notification", null };

                enums.Add(
                   "Enum.Notification",
                    new
                    {
                        Name = "notifications",
                        Values = notification_enums
                    }
                );
            }

            // HOLIDAY
            if (data.Code.ByName("gml_Script_is_holiday") != null)
            {
                holiday_enums = new Dictionary<string, int>
                {
                    { "normal", 0 },
                    { "halloween", 1 }
                };
                builtin_funcs["gml_Script_is_holiday"] = new[] { "Enum.Holiday" };
                enums.Add(
                   "Enum.Holiday",
                    new
                    {
                        Name = "holidays",
                        Values = holiday_enums
                    }
                );
            }

            // TV Prompt Enums
            if (data.Code.ByName("gml_Script_tv_push_prompt") != null)
            {
                tv_enums = new Dictionary<string, int>
                {
                    { "start", 0 },
                    { "highprio", 1 },
                    { "normal", 2 }
                };

                builtin_funcs["gml_Script_tv_push_prompt"] = new[] { null, "Enum.TVPromptTypes", null, null };
                enums.Add(
                   "Enum.TVPromptTypes",
                    new
                    {
                        Name = "tv_prompttypes",
                        Values = tv_enums
                    }
                );
            }

            // Text Enums
            if (data.Code.ByName("gml_Script_scr_draw_text_arr") != null)
            {
                text_enums = new Dictionary<string, int>
                {
                    { "none", 0 },
                    { "shake", 1 },
                    { "wave", 2 }
                };

                builtin_funcs["gml_Script_scr_draw_text_arr"] = new[] { null, null, null, null, "Enum.TextEffect" };
                enums.Add(
                   "Enum.TextEffect",
                    new
                    {
                        Name = "text_effects",
                        Values = text_enums
                    }
                );
            }

            // Menu ID Enums
            if (data.Code.ByName("gml_Script_menu_goto") != null)
            {
                menuID_enums = new Dictionary<string, int>
                {
                    { "categories", 0 },
                    { "audio", 1 },
                    { "video", 2 },
                    { "window", 3 },
                    { "resolution", 4 },
                    { "unknown5", 5 },
                    { "game", 6 },
                    { "controls", 7 },
                    { "controller", 8 },
                    { "keyboard", 9 },
                    { "deadzones", 10 },
                    { "last", 11 }
                };

                builtin_funcs["gml_Script_menu_goto"] = new[] { "Enum.MenuIDs" };
                enums.Add(
                   "Enum.MenuIDs",
                    new
                    {
                        Name = "menuids",
                        Values = menuID_enums
                    }
                );
            }

            // Editor Enums (Unused, but might as well)
            if (data.Code.ByName("gml_Script_editor_set_state") != null)
            {
                editor_enums = new Dictionary<string, int>
                {
                    { "init", 0 },
                    { "instance_edit", 1 },
                    { "unknown2", 2 },
                    { "resize_room", 3 },
                    { "save_level", 4 },
                    { "load_level", 5 }
                };

                builtin_funcs["gml_Script_editor_set_state"] = new[] { "Enum.EditorState" };
                enums.Add(
                   "Enum.EditorState",
                    new
                    {
                        Name = "editorstates",
                        Values = editor_enums
                    }
                );
            }
            // AfterImage Enums
            if (data.Code.ByName("gml_Script_particle_set_scale") != null)
            {
                afterimg_enums = new Dictionary<string, int>
                {
                    { "normal", 0 },
                    { "mach3effect", 1 },
                    { "heatattack", 2 },
                    { "firemouth", 3 },
                    { "blue", 4 },
                    { "blur", 5 },
                    { "red", 6 },
                    { "red_alt", 7 },
                    { "noise", 8 },
                    { "last", 9 }
                };
                builtin_vars.Add("identifier", "Enum.AfterimageType");
                builtin_funcs["gml_Script_particle_set_scale"] = new[] { "Enum.AfterimageType", null, null };
                enums.Add(
                   "Enum.AfterimageType",
                    new
                    {
                        Name = "afterimagetype",
                        Values = afterimg_enums
                    }
                );
            }

            // TDP Input Enums
            if (data.Code.ByName("gml_Script_tdp_get_icon") != null)
            {
                tdp_input_enums = new Dictionary<string, int>
                {
                    { "keyboard", 0 },
                    { "gamepad_button", 1 },
                    { "gamepad_axis", 2 }
                };

                // WHAT THE FUCK IS THIS
                functionArguments.Add("gml_Script_anon_tdp_input_key_gml_GlobalScript_tdp_input_classes_316_tdp_input_key_gml_GlobalScript_tdp_input_classes", new MacroEntry(
                "Union",
                new List<List<object>>
                    {
                    new List<object> { "Enum.TDPInputActionType", null },
                    new List<object> { "Enum.TDPInputActionType", null, null }
                    })
                );

                enums.Add(
                   "Enum.TDPInputActionType",
                    new
                    {
                        Name = "tdp_input_actiontypes",
                        Values = tdp_input_enums
                    }
                );
            }

            // Menu Anchor Enums
            if (data.Code.ByName("gml_Script_create_menu_fixed") != null)
            {
                menuanchors_enums = new Dictionary<string, int>
                {
                    { "center", 0 },
                    { "left", 1 }
                };

                functionArguments.Add("gml_Script_create_menu_fixed", new MacroEntry(
                "Union",
                new List<List<object>>
                    {
                        new List<object> { "Enum.MenuIDs", "Enum.MenuAnchor", null, null },
                        new List<object> { "Enum.MenuIDs", "Enum.MenuAnchor", null, null, null }
                    })
                );

                enums.Add(
                   "Enum.MenuAnchor",
                    new
                    {
                        Name = "menuanchors",
                        Values = menuanchors_enums
                    }
                );
            }

            // add new ones here
        }

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