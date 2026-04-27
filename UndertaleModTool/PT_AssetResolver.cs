using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public class PT_AssetResolver
    {
        // get data.win from MainWindow
        private static UndertaleData data = ((MainWindow)Application.Current.MainWindow).Data;

        // main stuff for JSON
        private static Dictionary<string, object> FuncEntries = [];
        private static Dictionary<string, string> VarEntries = [];
        private static Dictionary<string, object> EnumEntries = [];
        private static Dictionary<string, object> ArrayEntries = [];

        #region Helper Functions
        private static void AddVariable(string VarName, string AssetType) 
        {
            // if variable is missing in data.win, don't add
            if (data.Variables.FirstOrDefault(v => v.ToString() == VarName) != null)
                VarEntries.TryAdd(VarName, AssetType); 
        }

        private static void AddEnum(string EnumName, Dictionary<string, int> EnumSet)
            => EnumEntries.TryAdd($"Enum.{EnumName}", new { Name = EnumName, Values = EnumSet });

        private static void AddArray(string AssetType) 
            => ArrayEntries.TryAdd($"Array<{AssetType}>", new { MacroType = "ArrayInit", Macro = AssetType });

        private static void AddFunction(string FuncName, List<string> AssetTypes, string OptionalArg = null, int OptionalAmount = -1)
        {
            // if function is missing in data.win, don't add
            if (data.Functions.FirstOrDefault(f => f.ToString() == FuncName) == null)
                return;

            // Setup JSON element
            dynamic JSON;

            // if Complex Function with optional arguments
            if (OptionalAmount != -1)
            {
                JSON = new
                {
                    MacroType = "Union",
                    Macros = new List<List<string>>()
                };

                // Add however many optional arguments need to be added
                // +1 so that OptionalAmount can equal exactly to how many need to be added
                for (var i = 0; i < OptionalAmount + 1; i++)
                {
                    // Add Array (create a new list for this, so that it doesn't overwrite during json serialization)
                    JSON.Macros.Add(new List<string>(AssetTypes));
                    // Add Optional Arg to List for next cycle (if it makes it)
                    AssetTypes.Add(OptionalArg);
                }
            }
            else // else if simple function, just add simple arg array without the Macro shit
                JSON = AssetTypes;

            FuncEntries.TryAdd(FuncName, JSON);
        }
        #endregion

        // Make the JSON File
        public static void InitializeTypes()
        {
            #region Variables

            // there's probably duplicates, but idc
            #region Rooms
            AddVariable("leveltorestart", "Asset.Room");
            AddVariable("targetRoom", "Asset.Room");
            AddVariable("targetRoom2", "Asset.Room");
            AddVariable("backtohubroom", "Asset.Room");
            AddVariable("roomtorestart", "Asset.Room");
            AddVariable("checkpointroom", "Asset.Room");
            AddVariable("lastroom", "Asset.Room");
            AddVariable("hub_array", "Asset.Room");
            AddVariable("level_array", "Asset.Room");
            AddVariable("_levelinfo", "Asset.Room");
            AddVariable("rm", "Asset.Room");
            AddVariable("room_index", "Asset.Room");
            #endregion
            #region Objects
            AddVariable("content", "Asset.Object");
            AddVariable("player", "Asset.Object");
            AddVariable("targetplayer", "Asset.Object");
            AddVariable("target", "Asset.Object");
            AddVariable("playerid", "Asset.Object");
            AddVariable("_playerid", "Asset.Object");
            AddVariable("player_id", "Asset.Object");
            AddVariable("platformid", "Asset.Object");
            AddVariable("objID", "Asset.Object");
            AddVariable("objectID", "Asset.Object");
            AddVariable("spawnenemyID", "Asset.Object");
            AddVariable("ID", "Asset.Object");
            AddVariable("baddiegrabbedID", "Asset.Object");
            AddVariable("pizzashieldID", "Asset.Object");
            AddVariable("angryeffectid", "Asset.Object");
            AddVariable("pizzashieldid", "Asset.Object");
            AddVariable("superchargedeffectid", "Asset.Object");
            AddVariable("baddieID", "Asset.Object");
            AddVariable("baddieid", "Asset.Object");
            AddVariable("brickid", "Asset.Object");
            AddVariable("attackerID", "Asset.Object");
            AddVariable("object", "Asset.Object");
            AddVariable("obj", "Asset.Object");
            AddVariable("_obj", "Asset.Object");
            AddVariable("closestObj", "Asset.Object");
            AddVariable("solidObj", "Asset.Object");
            AddVariable("bg_obj", "Asset.Object");
            AddVariable("_obj_player", "Asset.Object");
            AddVariable("obj_explosion", "Asset.Object");
            AddVariable("my_obj_index", "Asset.Object");
            AddVariable("inst", "Asset.Object");
            AddVariable("chargeeffectid", "Asset.Object");
            AddVariable("dashcloudid", "Asset.Object");
            AddVariable("crazyruneffectid", "Asset.Object");
            AddVariable("superslameffectid", "Asset.Object");
            AddVariable("speedlineseffectid", "Asset.Object");
            AddVariable("ratpowerup", "Asset.Object");
            AddVariable("pl", "Asset.Object");
            AddVariable("dragonactor", "Asset.Object");
            AddVariable("anarchist", "Asset.Object");
            AddVariable("_checker", "Asset.Object");
            AddVariable("baddieID", "Asset.Object");
            AddVariable("baddieid", "Asset.Object");
            #endregion
            #region Sprites
            AddVariable("gameframe_caption_icon", "Asset.Sprite");
            AddVariable("bpal", "Asset.Sprite");
            AddVariable("vstitle", "Asset.Sprite");
            AddVariable("bg", "Asset.Sprite");
            AddVariable("bg2", "Asset.Sprite");
            AddVariable("bg3", "Asset.Sprite");
            AddVariable("playersprshadow", "Asset.Sprite");
            AddVariable("bosssprshadow", "Asset.Sprite");
            AddVariable("portrait1_idle", "Asset.Sprite");
            AddVariable("portrait1_hurt", "Asset.Sprite");
            AddVariable("portrait2_idle", "Asset.Sprite");
            AddVariable("portrait2_hurt", "Asset.Sprite");
            AddVariable("boss_palette", "Asset.Sprite");
            AddVariable("panicspr", "Asset.Sprite");
            AddVariable("bossarr", "Asset.Sprite");
            AddVariable("palettetexture", "Asset.Sprite");
            AddVariable("switchstart", "Asset.Sprite");
            AddVariable("switchend", "Asset.Sprite");
            AddVariable("_hurt", "Asset.Sprite");
            AddVariable("_dead", "Asset.Sprite");
            AddVariable("storedspriteindex", "Asset.Sprite");
            AddVariable("icon", "Asset.Sprite");
            AddVariable("spridle", "Asset.Sprite");
            AddVariable("sprgot", "Asset.Sprite");
            AddVariable("landspr", "Asset.Sprite");
            AddVariable("idlespr", "Asset.Sprite");
            AddVariable("fallspr", "Asset.Sprite");
            AddVariable("stunfallspr", "Asset.Sprite");
            AddVariable("walkspr", "Asset.Sprite");
            AddVariable("turnspr", "Asset.Sprite");
            AddVariable("recoveryspr", "Asset.Sprite");
            AddVariable("grabbedspr", "Asset.Sprite");
            AddVariable("scaredspr", "Asset.Sprite");
            AddVariable("ragespr", "Asset.Sprite");
            AddVariable("spr_dead", "Asset.Sprite");
            AddVariable("spr_palette", "Asset.Sprite");
            AddVariable("tube_spr", "Asset.Sprite");
            AddVariable("spr_intro", "Asset.Sprite");
            AddVariable("spr_introidle", "Asset.Sprite");
            AddVariable("sprite", "Asset.Sprite");
            AddVariable("divisionjustforplayersprites", "Asset.Sprite");
            AddVariable("spr_move", "Asset.Sprite");
            AddVariable("spr_crawl", "Asset.Sprite");
            AddVariable("spr_hurt", "Asset.Sprite");
            AddVariable("spr_jump", "Asset.Sprite");
            AddVariable("spr_jump2", "Asset.Sprite");
            AddVariable("spr_fall", "Asset.Sprite");
            AddVariable("spr_fall2", "Asset.Sprite");
            AddVariable("spr_crouch", "Asset.Sprite");
            AddVariable("spr_crouchjump", "Asset.Sprite");
            AddVariable("spr_crouchfall", "Asset.Sprite");
            AddVariable("spr_couchstart", "Asset.Sprite");
            AddVariable("spr_bump", "Asset.Sprite");
            AddVariable("spr_land", "Asset.Sprite");
            AddVariable("spr_land2", "Asset.Sprite");
            AddVariable("spr_lookdoor", "Asset.Sprite");
            AddVariable("spr_walkfront", "Asset.Sprite");
            AddVariable("spr_victory", "Asset.Sprite");
            AddVariable("spr_Ladder", "Asset.Sprite");
            AddVariable("spr_laddermove", "Asset.Sprite");
            AddVariable("spr_ladderdown", "Asset.Sprite");
            AddVariable("spr_keyget", "Asset.Sprite");
            AddVariable("spr_crouchslip", "Asset.Sprite");
            AddVariable("spr_pistolshot", "Asset.Sprite");
            AddVariable("spr_pistolwalk", "Asset.Sprite");
            AddVariable("spr_longjump", "Asset.Sprite");
            AddVariable("spr_longjumpend", "Asset.Sprite");
            AddVariable("spr_breakdance", "Asset.Sprite");
            AddVariable("spr_machslideboostfall", "Asset.Sprite");
            AddVariable("spr_mach3boostfall", "Asset.Sprite");
            AddVariable("spr_mrpinch", "Asset.Sprite");
            AddVariable("spr_rampjump", "Asset.Sprite");
            AddVariable("spr_mach1", "Asset.Sprite");
            AddVariable("spr_mach", "Asset.Sprite");
            AddVariable("spr_secondjump1", "Asset.Sprite");
            AddVariable("spr_secondjump2", "Asset.Sprite");
            AddVariable("spr_machslidestart", "Asset.Sprite");
            AddVariable("spr_machslide", "Asset.Sprite");
            AddVariable("spr_machslideend", "Asset.Sprite");
            AddVariable("spr_machslideboost", "Asset.Sprite");
            AddVariable("spr_catched", "Asset.Sprite");
            AddVariable("spr_punch", "Asset.Sprite");
            AddVariable("spr_backkick", "Asset.Sprite");
            AddVariable("spr_shoulder", "Asset.Sprite");
            AddVariable("spr_uppunch", "Asset.Sprite");
            AddVariable("spr_stomp", "Asset.Sprite");
            AddVariable("spr_stompprep", "Asset.Sprite");
            AddVariable("spr_crouchslide", "Asset.Sprite");
            AddVariable("spr_climbwall", "Asset.Sprite");
            AddVariable("spr_grab", "Asset.Sprite");
            AddVariable("spr_mach2jump", "Asset.Sprite");
            AddVariable("spr_Timesup", "Asset.Sprite");
            AddVariable("spr_deathstart", "Asset.Sprite");
            AddVariable("spr_deathend", "Asset.Sprite");
            AddVariable("spr_machpunch1", "Asset.Sprite");
            AddVariable("spr_machpunch2", "Asset.Sprite");
            AddVariable("spr_hurtjump", "Asset.Sprite");
            AddVariable("spr_entergate", "Asset.Sprite");
            AddVariable("spr_gottreasure", "Asset.Sprite");
            AddVariable("spr_bossintro", "Asset.Sprite");
            AddVariable("spr_hurtidle", "Asset.Sprite");
            AddVariable("spr_hurtwalk", "Asset.Sprite");
            AddVariable("spr_suplexmash1", "Asset.Sprite");
            AddVariable("spr_suplexmash2", "Asset.Sprite");
            AddVariable("spr_suplexmash3", "Asset.Sprite");
            AddVariable("spr_suplexmash4", "Asset.Sprite");
            AddVariable("spr_tackle", "Asset.Sprite");
            AddVariable("spr_airdash1", "Asset.Sprite");
            AddVariable("spr_airdash2", "Asset.Sprite");
            AddVariable("spr_idle1", "Asset.Sprite");
            AddVariable("spr_idle2", "Asset.Sprite");
            AddVariable("spr_idle3", "Asset.Sprite");
            AddVariable("spr_idle4", "Asset.Sprite");
            AddVariable("spr_idle5", "Asset.Sprite");
            AddVariable("spr_idle6", "Asset.Sprite");
            AddVariable("spr_wallsplat", "Asset.Sprite");
            AddVariable("spr_piledriver", "Asset.Sprite");
            AddVariable("spr_piledriverland", "Asset.Sprite");
            AddVariable("spr_charge", "Asset.Sprite");
            AddVariable("spr_mach3jump", "Asset.Sprite");
            AddVariable("spr_mach4", "Asset.Sprite");
            AddVariable("spr_machclimbwall", "Asset.Sprite");
            AddVariable("spr_dive", "Asset.Sprite");
            AddVariable("spr_machroll", "Asset.Sprite");
            AddVariable("spr_hitwall", "Asset.Sprite");
            AddVariable("spr_superjumpland", "Asset.Sprite");
            AddVariable("spr_walljumpstart", "Asset.Sprite");
            AddVariable("spr_superjumpprep", "Asset.Sprite");
            AddVariable("spr_superjump", "Asset.Sprite");
            AddVariable("spr_superjumppreplight", "Asset.Sprite");
            AddVariable("spr_superjumpright", "Asset.Sprite");
            AddVariable("spr_superjumpleft", "Asset.Sprite");
            AddVariable("spr_machfreefall", "Asset.Sprite");
            AddVariable("spr_mach3hit", "Asset.Sprite");
            AddVariable("spr_knightpepwalk", "Asset.Sprite");
            AddVariable("spr_knightpepjump", "Asset.Sprite");
            AddVariable("spr_knightpepfall", "Asset.Sprite");
            AddVariable("spr_knightpepidle", "Asset.Sprite");
            AddVariable("spr_knightpepjumpstart", "Asset.Sprite");
            AddVariable("spr_knightpepthunder", "Asset.Sprite");
            AddVariable("spr_knightpepland", "Asset.Sprite");
            AddVariable("spr_knightpepdownslope", "Asset.Sprite");
            AddVariable("spr_knightpepstart", "Asset.Sprite");
            AddVariable("spr_knightpepcharge", "Asset.Sprite");
            AddVariable("spr_knightpepdoublejump", "Asset.Sprite");
            AddVariable("spr_knightpepfly", "Asset.Sprite");
            AddVariable("spr_knightpepdowntrust", "Asset.Sprite");
            AddVariable("spr_knightpepupslope", "Asset.Sprite");
            AddVariable("spr_knightpepbump", "Asset.Sprite");
            AddVariable("spr_bodyslamfall", "Asset.Sprite");
            AddVariable("spr_bodyslamstart", "Asset.Sprite");
            AddVariable("spr_bodyslamland", "Asset.Sprite");
            AddVariable("spr_crazyrun", "Asset.Sprite");
            AddVariable("spr_bombpeprun", "Asset.Sprite");
            AddVariable("spr_bombpepintro", "Asset.Sprite");
            AddVariable("spr_bombpeprunabouttoexplode", "Asset.Sprite");
            AddVariable("spr_bombpepend", "Asset.Sprite");
            AddVariable("spr_jetpackstart2", "Asset.Sprite");
            AddVariable("spr_fireass", "Asset.Sprite");
            AddVariable("spr_fireassground", "Asset.Sprite");
            AddVariable("spr_fireassend", "Asset.Sprite");
            AddVariable("spr_tumblestart", "Asset.Sprite");
            AddVariable("spr_tumbleend", "Asset.Sprite");
            AddVariable("spr_tumble", "Asset.Sprite");
            AddVariable("spr_stunned", "Asset.Sprite");
            AddVariable("spr_clown", "Asset.Sprite");
            AddVariable("spr_clownbump", "Asset.Sprite");
            AddVariable("spr_clowncrouch", "Asset.Sprite");
            AddVariable("spr_clownfall", "Asset.Sprite");
            AddVariable("spr_clownjump", "Asset.Sprite");
            AddVariable("spr_clownwallclimb", "Asset.Sprite");
            AddVariable("spr_downpizzabox", "Asset.Sprite");
            AddVariable("spr_uppizzabox", "Asset.Sprite");
            AddVariable("spr_slipnslide", "Asset.Sprite");
            AddVariable("spr_mach3boost", "Asset.Sprite");
            AddVariable("spr_facehurtup", "Asset.Sprite");
            AddVariable("spr_facehurt", "Asset.Sprite");
            AddVariable("spr_walljumpend", "Asset.Sprite");
            AddVariable("spr_suplexdash", "Asset.Sprite");
            AddVariable("spr_suplexdashjumpstart", "Asset.Sprite");
            AddVariable("spr_suplexdashjump", "Asset.Sprite");
            AddVariable("spr_shotgunsuplexdash", "Asset.Sprite");
            AddVariable("spr_rollgetup", "Asset.Sprite");
            AddVariable("spr_swingding", "Asset.Sprite");
            AddVariable("spr_swingdingend", "Asset.Sprite");
            AddVariable("spr_haulingjump", "Asset.Sprite");
            AddVariable("spr_haulingidle", "Asset.Sprite");
            AddVariable("spr_haulingwalk", "Asset.Sprite");
            AddVariable("spr_haulingstart", "Asset.Sprite");
            AddVariable("spr_haulingfall", "Asset.Sprite");
            AddVariable("spr_haulingland", "Asset.Sprite");
            AddVariable("spr_uppercutfinishingblow", "Asset.Sprite");
            AddVariable("spr_finishingblow1", "Asset.Sprite");
            AddVariable("spr_finishingblow2", "Asset.Sprite");
            AddVariable("spr_finishingblow3", "Asset.Sprite");
            AddVariable("spr_finishingblow4", "Asset.Sprite");
            AddVariable("spr_finishingblow5", "Asset.Sprite");
            AddVariable("spr_winding", "Asset.Sprite");
            AddVariable("spr_3hpwalk", "Asset.Sprite");
            AddVariable("spr_3hpidle", "Asset.Sprite");
            AddVariable("spr_panic", "Asset.Sprite");
            AddVariable("spr_facestomp", "Asset.Sprite");
            AddVariable("spr_freefall", "Asset.Sprite");
            AddVariable("spr_shotgunsuplex", "Asset.Sprite");
            AddVariable("spr_pushback1", "Asset.Sprite");
            AddVariable("spr_pushback2", "Asset.Sprite");
            AddVariable("spr_throw", "Asset.Sprite");
            AddVariable("spr_run", "Asset.Sprite");
            AddVariable("spr_shotgunidle", "Asset.Sprite");
            AddVariable("spr_sworddash", "Asset.Sprite");
            AddVariable("spr", "Asset.Sprite");
            AddVariable("expressionsprite", "Asset.Sprite");
            AddVariable("_spr", "Asset.Sprite");
            AddVariable("attackdash", "Asset.Sprite");
            AddVariable("airattackdash", "Asset.Sprite");
            AddVariable("airattackdashstart", "Asset.Sprite");
            AddVariable("tauntstoredsprite", "Asset.Sprite");
            AddVariable("movespr", "Asset.Sprite");
            AddVariable("spr_joystick", "Asset.Sprite");
            AddVariable("t", "Asset.Sprite");
            AddVariable("spr_attack", "Asset.Sprite");
            AddVariable("spr_hidden", "Asset.Sprite");
            AddVariable("spr_idle", "Asset.Sprite");
            AddVariable("stunspr", "Asset.Sprite");
            AddVariable("bgsprite", "Asset.Sprite");
            AddVariable("boss_hpsprite", "Asset.Sprite");
            AddVariable("tvsprite", "Asset.Sprite");
            AddVariable("particlespr", "Asset.Sprite");
            AddVariable("heatmeterspr", "Asset.Sprite");
            AddVariable("heatmetersprfill", "Asset.Sprite");
            AddVariable("heatmetersprpalette", "Asset.Sprite");
            AddVariable("pizzascorespr", "Asset.Sprite");
            AddVariable("rankshudspr", "Asset.Sprite");
            AddVariable("rankshudsprfill", "Asset.Sprite");
            AddVariable("tv_combobubble", "Asset.Sprite");
            AddVariable("tv_combobubblefill", "Asset.Sprite");
            AddVariable("tv_escapeG", "Asset.Sprite");
            AddVariable("tv_happyG", "Asset.Sprite");
            AddVariable("tv_hurtG", "Asset.Sprite");
            AddVariable("tv_idleG", "Asset.Sprite");
            AddVariable("tv_barrel", "Asset.Sprite");
            AddVariable("tv_bombpep", "Asset.Sprite");
            AddVariable("tv_boxxedpep", "Asset.Sprite");
            AddVariable("tv_cheeseball", "Asset.Sprite");
            AddVariable("tv_cheesepep", "Asset.Sprite");
            AddVariable("tv_clown", "Asset.Sprite");
            AddVariable("tv_exprcollect", "Asset.Sprite");
            AddVariable("tv_exprcombo", "Asset.Sprite");
            AddVariable("tv_exprheat", "Asset.Sprite");
            AddVariable("tv_exprhurt", "Asset.Sprite");
            AddVariable("tv_exprmach3", "Asset.Sprite");
            AddVariable("tv_exprmach4", "Asset.Sprite");
            AddVariable("tv_exprpanic", "Asset.Sprite");
            AddVariable("tv_fireass", "Asset.Sprite");
            AddVariable("tv_firemouth", "Asset.Sprite");
            AddVariable("tv_ghost", "Asset.Sprite");
            AddVariable("tv_golf", "Asset.Sprite");
            AddVariable("tv_idle", "Asset.Sprite");
            AddVariable("tv_idlesecret", "Asset.Sprite");
            AddVariable("tv_idleanim1", "Asset.Sprite");
            AddVariable("tv_idleanim2", "Asset.Sprite");
            AddVariable("tv_knight", "Asset.Sprite");
            AddVariable("tv_mort", "Asset.Sprite");
            AddVariable("tv_rocket", "Asset.Sprite");
            AddVariable("tv_scaredjump", "Asset.Sprite");
            AddVariable("tv_shotgun", "Asset.Sprite");
            AddVariable("tv_squished", "Asset.Sprite");
            AddVariable("tv_tumble", "Asset.Sprite");
            AddVariable("tv_weenie", "Asset.Sprite");
            AddVariable("tv_off", "Asset.Sprite");
            AddVariable("tv_open", "Asset.Sprite");
            AddVariable("tv_whitenoise", "Asset.Sprite");
            AddVariable("tv_exprhurt1", "Asset.Sprite");
            AddVariable("tv_exprhurt2", "Asset.Sprite");
            AddVariable("tv_exprhurt3", "Asset.Sprite");
            AddVariable("tv_exprhurt4", "Asset.Sprite");
            AddVariable("tv_exprhurt5", "Asset.Sprite");
            AddVariable("tv_exprhurt6", "Asset.Sprite");
            AddVariable("tv_exprhurt7", "Asset.Sprite");
            AddVariable("tv_exprhurt8", "Asset.Sprite");
            AddVariable("tv_exprhurt9", "Asset.Sprite");
            AddVariable("tv_exprhurt10", "Asset.Sprite");
            AddVariable("vstitleplayer", "Asset.Sprite");
            AddVariable("texture", "Asset.Sprite");
            AddVariable("player_hpsprite", "Asset.Sprite");
            AddVariable("chasespr", "Asset.Sprite");
            AddVariable("throwspr", "Asset.Sprite");
            AddVariable("transitionspr", "Asset.Sprite");
            AddVariable("angryspr", "Asset.Sprite");
            AddVariable("upspr", "Asset.Sprite");
            AddVariable("downspr", "Asset.Sprite");
            AddVariable("deadspr", "Asset.Sprite");
            AddVariable("spr_pose", "Asset.Sprite");
            AddVariable("spr_run", "Asset.Sprite");
            AddVariable("spr_intro", "Asset.Sprite");
            AddVariable("spr_idle_strongcold", "Asset.Sprite");
            AddVariable("spr_run_strongcold", "Asset.Sprite");
            AddVariable("spr_intro_strongcold", "Asset.Sprite");
            AddVariable("prevsprite", "Asset.Sprite");
            AddVariable("title_sprite", "Asset.Sprite");
            AddVariable("titlecard_sprite", "Asset.Sprite");
            AddVariable("playersprite", "Asset.Sprite");
            AddVariable("peppino_sprite", "Asset.Sprite");
            AddVariable("toppin_sprite", "Asset.Sprite");
            AddVariable("bell_sprite", "Asset.Sprite");
            AddVariable("portrait1_sprite", "Asset.Sprite");
            AddVariable("portrait2_sprite", "Asset.Sprite");
            AddVariable("pizzaface_sprite", "Asset.Sprite");
            AddVariable("johnface_sprite", "Asset.Sprite");
            AddVariable("bubblespr", "Asset.Sprite");
            AddVariable("noise_sprite", "Asset.Sprite");
            AddVariable("noisesprite", "Asset.Sprite");
            AddVariable("iconspr", "Asset.Sprite");
            AddVariable("hand_sprite", "Asset.Sprite");
            AddVariable("canon_sprite", "Asset.Sprite");
            AddVariable("captain_sprite", "Asset.Sprite");
            AddVariable("door_sprite", "Asset.Sprite");
            AddVariable("throw_sprite", "Asset.Sprite");
            AddVariable("_sprite", "Asset.Sprite");
            AddVariable("gate_sprite", "Asset.Sprite");
            AddVariable("taunt_spr", "Asset.Sprite");
            AddVariable("rank_spr", "Asset.Sprite");
            AddVariable("spr_happy", "Asset.Sprite");
            AddVariable("tauntspr", "Asset.Sprite");
            AddVariable("activatespr", "Asset.Sprite");
            AddVariable("dancespr", "Asset.Sprite");
            AddVariable("smilespr", "Asset.Sprite");
            AddVariable("item_sprite", "Asset.Sprite");
            AddVariable("spr_arrow", "Asset.Sprite");
            AddVariable("bg_spr", "Asset.Sprite");
            AddVariable("playerspr", "Asset.Sprite");
            AddVariable("particlespr", "Asset.Sprite");
            AddVariable("bomblit_spr", "Asset.Sprite");
            AddVariable("defeatplayerspr", "Asset.Sprite");
            AddVariable("bumpspr", "Asset.Sprite");
            AddVariable("bubble_spr", "Asset.Sprite");
            AddVariable("transspr", "Asset.Sprite");
            AddVariable("confirmspr", "Asset.Sprite");
            AddVariable("selectedspr", "Asset.Sprite");
            AddVariable("clipspr", "Asset.Sprite");
            AddVariable("signspr", "Asset.Sprite");
            AddVariable("bouncespr", "Asset.Sprite");
            AddVariable("parryspr", "Asset.Sprite");
            AddVariable("background_spr", "Asset.Sprite");
            AddVariable("achievement_spr", "Asset.Sprite");
            AddVariable("visible_spr", "Asset.Sprite");
            AddVariable("treasurespr", "Asset.Sprite");
            AddVariable("shootspr", "Asset.Sprite");
            AddVariable("gerome_spr", "Asset.Sprite");
            AddVariable("timerspr", "Asset.Sprite");
            AddVariable("spitcheesespr", "Asset.Sprite");
            AddVariable("hitceillingspr", "Asset.Sprite");
            AddVariable("hitwallspr", "Asset.Sprite");
            AddVariable("stunfalltransspr", "Asset.Sprite");
            AddVariable("rollingspr", "Asset.Sprite");
            AddVariable("flyingspr", "Asset.Sprite");
            AddVariable("hitspr", "Asset.Sprite");
            AddVariable("stunlandspr", "Asset.Sprite");
            AddVariable("stompedspr", "Asset.Sprite");
            AddVariable("handsprite", "Asset.Sprite");
            AddVariable("player_sprite", "Asset.Sprite");
            AddVariable("spr_sign", "Asset.Sprite");
            AddVariable("spr_helicopter", "Asset.Sprite");
            AddVariable("spr_name", "Asset.Sprite");
            AddVariable("spr_air", "Asset.Sprite");
            AddVariable("spr_animatronic", "Asset.Sprite");
            AddVariable("spr_pal", "Asset.Sprite");
            AddVariable("spr_hand", "Asset.Sprite");
            AddVariable("spr_left", "Asset.Sprite");
            AddVariable("spr_right", "Asset.Sprite");
            AddVariable("spr_content_dead", "Asset.Sprite");
            AddVariable("spr_gamepadbuttons", "Asset.Sprite");
            #endregion
            #region Colors
            AddVariable("color", "Constant.Color");
            AddVariable("textcolor", "Constant.Color");
            AddVariable("bc", "Constant.Color");
            AddVariable("tc", "Constant.Color");
            AddVariable("gameframe_blend", "Constant.Color");
            AddVariable("c1", "Constant.Color");
            AddVariable("c2", "Constant.Color");
            AddVariable("c_player", "Constant.Color");
            #endregion
            // Tilesets
            AddVariable("ts", "Asset.Tileset");
            
            // Gamepad Constants
            AddVariable("_select", "Constant.GamepadButton");
            AddVariable("_back", "Constant.GamepadButton");
            AddVariable("_face", "Constant.GamepadButton");

            // Arrays
            AddVariable("objectlist", "Array<Asset.Object>");
            AddVariable("object_arr", "Array<Asset.Object>");
            AddVariable("objdark_arr", "Array<Asset.Object>");
            AddVariable("content_arr", "Array<Asset.Object>");
            AddVariable("spawnpool", "Array<Asset.Object>");
            AddVariable("spawn_arr", "Array<Asset.Object>");
            AddVariable("dark_arr", "Array<Asset.Object>");
            AddVariable("flash_arr", "Array<Asset.Object>");
            AddVariable("collision_list", "Array<Asset.Object>");
            AddVariable("levels", "Array<Asset.Room>");
            AddVariable("room_arr", "Array<Asset.Room>");
            AddVariable("treasure_arr", "Array<Asset.Sprite>");
            #endregion
            #region Functions
            // Functions with Optional Arguments
            AddFunction("gml_Script_scr_boss_genericintro", [null, null, "Asset.Sprite"], null, 1);
            AddFunction("gml_Script_palette_unlock", [null, null, null, "Asset.Sprite" ], null, 2);
            AddFunction("gml_Script_scr_sound", ["Asset.Sound"], null, 2);
            AddFunction("gml_Script_scr_soundeffect", ["Asset.Sound"], null, 2);
            AddFunction("gml_Script_scr_music", ["Asset.Sound"], null, 2);
            AddFunction("gml_Script_create_debris", [null, null, "Asset.Sprite"], null, 1);
            AddFunction("gml_Script_add_music", ["Asset.Room", null, null, null], null, 1);
            AddFunction("gml_Script_scr_pauseicon_add", ["Asset.Sprite"], null, 3);
            AddFunction("gml_Script_tv_do_expression", ["Asset.Sprite"], "Bool", 2);
            AddFunction("gml_Script_create_collect", [null, null, "Asset.Sprite"], null, 1);

            // Simple Functions (From UTMTCE)
            AddFunction("gml_Script_randomize_animations", ["Array<Asset.Sprite>"]);
            AddFunction("gml_Script_instance_create_unique", [null, null, "Asset.Object"]);
            AddFunction("gml_Script_instance_nearest_random", ["Asset.Object", null]);
            AddFunction("instance_place_list", [null, null, "Asset.Object", null, "Bool"]);
            AddFunction("gml_Script_draw_enemy", [null, null, "Constant.Color"]);
            AddFunction("gml_Script_create_afterimage", [null, null, "Asset.Sprite", null]);
            AddFunction("gml_Script_create_mach2effect", [null, null, "Asset.Sprite", null, null]);
            AddFunction("gml_Script_create_heatattack_afterimage", [null, null, "Asset.Sprite", null, null]);
            AddFunction("gml_Script_create_firemouth_afterimage", [null, null, "Asset.Sprite", null, null]);
            AddFunction("gml_Script_create_blue_afterimage", [null, null, "Asset.Sprite", null, null]);
            AddFunction("gml_Script_create_red_afterimage", [null, null, "Asset.Sprite", null, null]);
            AddFunction("gml_Script_create_blur_afterimage", [null, null, "Asset.Sprite", null, null]);
            AddFunction("gml_Script_pal_swap_init_system", ["Asset.Shader"]);
            AddFunction("gml_Script_pal_swap_init_system_fix", ["Asset.Shader"]);
            AddFunction("gml_Script_pal_swap_set", ["Asset.Sprite", null, null]);
            AddFunction("gml_Script_pattern_set", [null, "Asset.Sprite", null, null, null, null]);
            AddFunction("gml_Script_scr_room_goto", ["Asset.Room"]);
            AddFunction("gml_Script_hub_state", ["Asset.Room", null, null]);
            AddFunction("gml_Script_draw_background_tiled", ["Asset.Sprite", null, null, null]);
            AddFunction("gml_Script_scr_draw_granny_texture", [null, null, null, null, null, null, "Asset.Sprite", "Asset.Sprite"]);
            AddFunction("gml_Script_object_get_depth", ["Asset.Object"]);
            AddFunction("gml_Script_scr_bosscontroller_particle_anim", ["Asset.Sprite", null, null, null, null, "Asset.Sprite", null]);
            AddFunction("gml_Script_scr_bosscontroller_particle_hp", ["Asset.Sprite", null, null, null, null, "Asset.Sprite", null, "Asset.Sprite"]);
            AddFunction("gml_Script_scr_bosscontroller_draw_health", ["Asset.Sprite", null, null, null, null, null, null, null, null, null, null, "Asset.Sprite", null, "Asset.Sprite"]);
            AddFunction("gml_Script_boss_update_pizzaheadKO", ["Asset.Sprite", "Asset.Sprite"]);
            AddFunction("gml_Script_scr_pizzaface_p3_do_player_attack", ["Asset.Object"]);
            AddFunction("gml_Script_scr_boss_do_hurt_phase2", ["Asset.Object"]);
            AddFunction("gml_Script_check_slope", ["Asset.Object"]);
            AddFunction("gml_Script_check_slope_player", ["Asset.Object"]);
            AddFunction("gml_Script_try_solid", [null, null, "Asset.Object", null]);
            AddFunction("gml_Script_add_rank_achievements", [null, null, "Asset.Sprite", null, null]);
            AddFunction("gml_Script_add_boss_achievements", [null, "Asset.Room", "Asset.Sprite", null]);
            AddFunction("gml_Script_achievement_unlock", [null, null, "Asset.Sprite", null]);
            AddFunction("gml_Script_scr_monsterinvestigate", [null, "Asset.Sprite", "Asset.Sprite"]);
            AddFunction("gml_Script_timedgate_add_objects", ["Asset.Object", null]);
            AddFunction("gml_Script_randomize_animations", ["Array<Asset.Sprite>"]);
            AddFunction("gml_Script_tdp_draw_text_color", [null, null, null, "Constant.Color", "Constant.Color", "Constant.Color", "Constant.Color", null]);
            AddFunction("gml_Script_lang_draw_sprite", ["Asset.Sprite", null, null, null]);
            AddFunction("layer_background_change", [null, "Asset.Sprite"]);
            AddFunction("layer_background_sprite", [null, "Asset.Sprite"]);
            #endregion

            #region Misc Enums

            #region Particle Enums
            if (data.Code.ByName("gml_Object_obj_particlesystem_Create_0") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("enum_start", i++);
                EnumSet.TryAdd("cloudeffect", i++);
                EnumSet.TryAdd("crazyrunothereffect", i++);
                EnumSet.TryAdd("highjumpcloud1", i++);
                EnumSet.TryAdd("highjumpcloud2", i++);
                EnumSet.TryAdd("jumpdust", i++);
                EnumSet.TryAdd("balloonpop", i++);
                EnumSet.TryAdd("shotgunimpact", i++);
                EnumSet.TryAdd("impact", i++);
                EnumSet.TryAdd("genericpoofeffect", i++);
                EnumSet.TryAdd("keyparticles", i++);
                EnumSet.TryAdd("teleporteffect", i++);
                EnumSet.TryAdd("landcloud", i++);
                EnumSet.TryAdd("ratmountballooncloud", i++);
                EnumSet.TryAdd("groundpoundeffect", i++);
                EnumSet.TryAdd("noisegrounddash", i++);
                EnumSet.TryAdd("bubblepop", i++);
                EnumSet.TryAdd("enum_length", i++);

                AddFunction("gml_Script_declare_particle", ["Enum.particle", "Asset.Sprite", null, null]);
                AddFunction("gml_Script_particle_set_scale", ["Enum.particle", null, null]);
                AddFunction("gml_Script_create_particle", [null, null, "Enum.particle"], null, 1);

                AddEnum("particle", EnumSet);
            }
            #endregion
            #region Notification Enums
            if (data.Code.ByName("gml_Script_notification_push") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("bodyslam_start", i++);
                EnumSet.TryAdd("bodyslam_end", i++);
                EnumSet.TryAdd("generic_killed", i++);
                EnumSet.TryAdd("room_enemiesdead", i++);
                EnumSet.TryAdd("enemy_parried", i++);
                EnumSet.TryAdd("level_finished", i++);
                EnumSet.TryAdd("mortcube_destroyed", i++);
                EnumSet.TryAdd("hurt", i++);
                EnumSet.TryAdd("fell_into_pit", i++);
                EnumSet.TryAdd("beer_knocked", i++);
                EnumSet.TryAdd("touched_timedgate", i++);
                EnumSet.TryAdd("flush_done", i++);
                EnumSet.TryAdd("baddie_killed_projectile", i++);
                EnumSet.TryAdd("treasureguy_uncovered", i++);
                EnumSet.TryAdd("special_destroyable_destroyed", i++);
                EnumSet.TryAdd("custom_destructibles_destroyed", i++);
                EnumSet.TryAdd("pizzaball_shot", i++);
                EnumSet.TryAdd("pizzaball_kill", i++);
                EnumSet.TryAdd("pizzaball_goal", i++);
                EnumSet.TryAdd("brickball_start", i++);
                EnumSet.TryAdd("john_destroyed", i++);
                EnumSet.TryAdd("brickball_kill", i++);
                EnumSet.TryAdd("pigcitizen_taunt", i++);
                EnumSet.TryAdd("pizzaboy_killed", i++);
                EnumSet.TryAdd("touched_mrpinch", i++);
                EnumSet.TryAdd("priest_touched", i++);
                EnumSet.TryAdd("secret_entered", i++);
                EnumSet.TryAdd("secret_exited", i++);
                EnumSet.TryAdd("iceblock_bird_freed", i++);
                EnumSet.TryAdd("monster_killed", i++);
                EnumSet.TryAdd("monster_activated", i++);
                EnumSet.TryAdd("jumpscared", i++);
                EnumSet.TryAdd("knightpep_bumped", i++);
                EnumSet.TryAdd("cheeseblock_destroyed", i++);
                EnumSet.TryAdd("rat_destroyed_with_baddie", i++);
                EnumSet.TryAdd("rattumble_destroyed", i++);
                EnumSet.TryAdd("rat_destroyed", i++);
                EnumSet.TryAdd("touched_lava", i++);
                EnumSet.TryAdd("touched_cow", i++);
                EnumSet.TryAdd("touched_cow_once", i++);
                EnumSet.TryAdd("touched_gravesurf_once", i++);
                EnumSet.TryAdd("touched_ghostfollow", i++);
                EnumSet.TryAdd("ghost_end", i++);
                EnumSet.TryAdd("superjump_end", i++);
                EnumSet.TryAdd("shotgun_shot", i++);
                EnumSet.TryAdd("shotgun_shot_end", i++);
                EnumSet.TryAdd("destroyable_destroyed", i++);
                EnumSet.TryAdd("bazooka_explosion", i++);
                EnumSet.TryAdd("wartimer_finished", i++);
                EnumSet.TryAdd("totem_reactivated", i++);
                EnumSet.TryAdd("boss_defeated", i++);
                EnumSet.TryAdd("combo_end", i++);
                EnumSet.TryAdd("achievement_unlocked", i++);
                EnumSet.TryAdd("crouched_in_poo", i++);
                EnumSet.TryAdd("game_beaten", i++);
                EnumSet.TryAdd("taunted", i++);
                EnumSet.TryAdd("john_resurrected", i++);
                EnumSet.TryAdd("knight_obtained", i++);
                EnumSet.TryAdd("mooney_unlocked", i++);
                EnumSet.TryAdd("unknown59", i++);
                EnumSet.TryAdd("pumpkin_gotten", i++);
                EnumSet.TryAdd("pumpkindoor_entered", i++);
                EnumSet.TryAdd("trickytreat_failed", i++);
                EnumSet.TryAdd("trickytreat_door_entered", i++);
                EnumSet.TryAdd("tornadoattack_end", i++);
                EnumSet.TryAdd("gate_taunted", i++);
                EnumSet.TryAdd("noisebomb_wasted", i++);
                EnumSet.TryAdd("got_endingrank", i++);
                EnumSet.TryAdd("breakdance_start", i++);
                EnumSet.TryAdd("touched_banana", i++);
                EnumSet.TryAdd("level_finished_pizzaface", i++);
                EnumSet.TryAdd("player_antigrav", i++);
                EnumSet.TryAdd("ptg_seen", i++);
                EnumSet.TryAdd("touched_granny", i++);

                AddFunction("gml_Script_notification_push", ["Enum.notifications", null]);

                AddEnum("notifications", EnumSet);
            }
            #endregion
            #region Holiday Enums
            if (data.Code.ByName("gml_Script_is_holiday") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("normal", i++);
                EnumSet.TryAdd("halloween", i++);

                AddFunction("gml_Script_is_holiday", ["Enum.holidays"]);

                AddEnum("holidays", EnumSet);
            }
            #endregion
            #region TV Prompt Enums
            if (data.Code.ByName("gml_Script_tv_push_prompt") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("start", i++);
                EnumSet.TryAdd("highprio", i++);
                EnumSet.TryAdd("normal", i++);

                AddFunction("gml_Script_tv_push_prompt", [null, "Enum.tv_prompttypes", null, null]);
                AddArray("Enum.tv_prompttypes");

                AddEnum("tv_prompttypes", EnumSet);
            }
            #endregion
            #region Text Enums
            if (data.Code.ByName("gml_Script_scr_draw_text_arr") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("none", i++);
                EnumSet.TryAdd("shake", i++);
                EnumSet.TryAdd("wave", i++);

                AddFunction("gml_Script_scr_draw_text_arr", [null, null, null, "Constant.Color"], null, 3);

                AddEnum("text_effects", EnumSet);
            }
            #endregion
            #region Menu ID Enums
            if (data.Code.ByName("gml_Script_menu_goto") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("categories", i++);
                EnumSet.TryAdd("audio", i++);
                EnumSet.TryAdd("video", i++);
                EnumSet.TryAdd("window", i++);
                EnumSet.TryAdd("resolution", i++);
                EnumSet.TryAdd("unknown5", i++);
                EnumSet.TryAdd("game", i++);
                EnumSet.TryAdd("controls", i++);
                EnumSet.TryAdd("controller", i++);
                EnumSet.TryAdd("keyboard", i++);
                EnumSet.TryAdd("deadzones", i++);
                EnumSet.TryAdd("last", i++);

                AddFunction("gml_Script_menu_goto", ["Enum.MenuIDs"]);

                AddEnum("MenuIDs", EnumSet);
            }
            #endregion
            #region Editor Enums
            if (data.Code.ByName("gml_Script_editor_set_state") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("init", i++);
                EnumSet.TryAdd("instance_edit", i++);
                EnumSet.TryAdd("unknown2", i++);
                EnumSet.TryAdd("resize_room", i++);
                EnumSet.TryAdd("save_level", i++);
                EnumSet.TryAdd("load_level", i++);

                AddFunction("gml_Script_editor_set_state", ["Enum.EditorState"]);

                AddEnum("EditorState", EnumSet);
            }
            #endregion
            #region AfterImage Enums
            if (data.Code.ByName("gml_Script_particle_set_scale") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("normal", i++);
                EnumSet.TryAdd("mach3effect", i++);
                EnumSet.TryAdd("heatattack", i++);
                EnumSet.TryAdd("firemouth", i++);
                EnumSet.TryAdd("blue", i++);
                EnumSet.TryAdd("blur", i++);
                EnumSet.TryAdd("red", i++);
                EnumSet.TryAdd("red_alt", i++);
                EnumSet.TryAdd("noise", i++);
                EnumSet.TryAdd("last", i++);

                AddVariable("identifier", "Enum.AfterimageType");

                AddEnum("AfterimageType", EnumSet);
            }
            #endregion
            #region TDP Input Enums
            if (data.Code.ByName("gml_Script_tdp_get_icon") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("keyboard", i++);
                EnumSet.TryAdd("gamepad_button", i++);
                EnumSet.TryAdd("gamepad_axis", i++);

                // WHAT THE FUCK IS THIS
                AddFunction("gml_Script_anon_tdp_input_key_gml_GlobalScript_tdp_input_classes_316_tdp_input_key_gml_GlobalScript_tdp_input_classes",
                    ["Enum.TDPInputActionType", null], null, 1);

                AddFunction("gml_Script_tdp_input_action", ["Enum.TDPInputActionType", "Constant.GamepadButton"], null, 1);
                AddFunction("gml_Script_tdp_action", ["Enum.TDPInputActionType", "Constant.GamepadButton"], null, 1);
                AddFunction("has_value", ["Enum.TDPInputActionType", null, null], null, 1);

                AddEnum("TDPInputActionType", EnumSet);
            }
            #endregion
            #region Menu Anchor Enums
            if (data.Code.ByName("gml_Script_create_menu_fixed") != null)
            {
                int i = 0;
                Dictionary<string, int> EnumSet = [];

                EnumSet.TryAdd("center", i++);
                EnumSet.TryAdd("left", i++);

                AddFunction("gml_Script_create_menu_fixed", ["Enum.MenuIDs", "Enum.MenuAnchor", null, null], null, 1);

                AddEnum("MenuAnchor", EnumSet);
            }
            #endregion

            #endregion

            // Init these Arrays
            AddArray("Asset.Room");
            AddArray("Asset.Object");
            AddArray("Asset.Sprite");

            #region Write JSON Files
            JsonSerializerOptions jsonOptions = new() { WriteIndented = true };
            string GameFileName = $"{data.GeneralInfo.Name}".Replace("\"", "");
            string GameDispName = $"{data.GeneralInfo.DisplayName}".Replace("\"", "");

            #region Definitions JSON
            var MainJSONStruct = new
            {
                // Enum Only Branch
                Types = new
                {
                    Enums = EnumEntries,
                    Constants = new { },
                    General = ArrayEntries
                },
                // Other Shit Branch
                GlobalNames = new
                {
                    Variables = VarEntries,
                    FunctionArguments = FuncEntries,
                    FunctionReturn = new { }
                },
                // Shit just for the Template
                CodeEntryNames = new { }
            };

            // Convert the parent object to a JSON string
            string jsonString = JsonSerializer.Serialize(MainJSONStruct, jsonOptions)
                .Replace("\\u003C", "<").Replace("\\u003E", ">");// idk man

            // Write main JSON File
            File.WriteAllText($"{Program.GetExecutableDirectory()}/GameSpecificData/Underanalyzer/{GameFileName}.json", jsonString);
            #endregion
            #region Loader JSON
            var LoaderJSONStruct = new
            {
                LoadOrder = 1,
                Conditions = new[]
                {
                    new
                    {
                        ConditionKind = "DisplayName.Regex",
                        Value = $"(?i)^{GameDispName}"
                    }
                },
                UnderanalyzerFilename = GameFileName + ".json"
            };

            // Write Loader JSON
            File.WriteAllText($"{Program.GetExecutableDirectory()}/GameSpecificData/Definitions/{GameFileName}_loader.json", JsonSerializer.Serialize(LoaderJSONStruct, jsonOptions));
            #endregion

            #endregion

            // Notify User that it's done
            Application.Current.MainWindow.ShowMessage("Pizza Tower JSON File made\n\nTo apply the generated JSON File to the Decompiler, please restart the program");

            // Clean Up
            FuncEntries = [];
            VarEntries = [];
            EnumEntries = [];
            ArrayEntries = [];
        }

        // Detects PT state names (now without instuction bullshit)
        public static void FindStateNames(UndertaleCode UCode, string switchstate, string[] statePrefix)
        {
            if (UCode is null) return;

            // sorry but i dont like making vars for one time uses 
            string[] CodeLines = 
                new DecompileContext(
                    new GlobalDecompileContext(data),
                    UCode,
                    new DecompileSettings()
                    {
                        // force UnknownEnums
                        UnknownEnumName = "UnknownEnum",
                        UnknownEnumValuePattern = "Value_{0}",
                        CreateEnumDeclarations = false,
                        AllowLeftoverDataOnStack = true
                    }
                )
                .DecompileToString() // get decompiled code
                .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None); // split it into lines

            // if code is useless, just leave
            if (CodeLines.Length < 6) return;

            // make dictionary for this enum
            Dictionary<string, int> enumset_found = [];

            #region Find State Names in Code
            for (int i = 0; i < CodeLines.Length - 1; i++)
            {
                string line = CodeLines[i].Trim();

                // check only in the switch state
                if (line.StartsWith($"switch ({switchstate})"))
                    continue;

                // start checking for states
                if (line.StartsWith("case "))
                {
                    var caseMatch = Regex.Match(line, @"case\s+(?:UnknownEnum\.Value_)?(\d+):");
                    if (!caseMatch.Success)
                        continue;

                    // get state value as integer
                    int stateValue = int.Parse(caseMatch.Groups[1].Value);

                    // Look for the function call on the next line
                    string nextLine = CodeLines[i + 1].Trim();
                    foreach (var prefix in statePrefix)
                    {
                        if (nextLine.StartsWith(prefix))
                        {
                            var suffixMatch = Regex.Match(nextLine, $@"{Regex.Escape(prefix)}(\w+)\s*\(");
                            if (suffixMatch.Success)
                            {
                                // STATE!!!
                                string suffix = suffixMatch.Groups[1].Value;

                                // fix throw and parry
                                suffix = suffix switch
                                { 
                                    "throw" or "parry" => $"{suffix}ing",
                                    _ => suffix
                                };

                                // Add to dictionary
                                enumset_found.TryAdd(suffix, stateValue);
                            }
                            break;
                        }
                    }
                }
            }
            #endregion
            #region Add Enum to JSON
            if (enumset_found.Count > 0)
            {
                // merge existing entry
                if (EnumEntries.TryGetValue($"Enum.{switchstate}s", out dynamic EnumObj))
                {
                    Dictionary<string, int> EnumSet = EnumObj.Values as Dictionary<string, int>;
                    foreach (var kvp in enumset_found)
                    {
                        if (!EnumSet.ContainsKey(kvp.Key))// stop overwrite
                            EnumSet[kvp.Key] = kvp.Value; // add
                    }
                    // remove old enum
                    EnumEntries.Remove($"Enum.{switchstate}s");
                    // add new merged enum
                    AddEnum($"{switchstate}s", EnumSet);
                }
                // make new if it doesn't exist
                else
                    AddEnum($"{switchstate}s", enumset_found);
            }

            // ONLY Add if the FindStateNames Function actually found pt player states
            if (enumset_found.Count > 0 && switchstate == "state")
            {
                // Variables that should == Enum
                AddVariable("state", "Enum.states");
                AddVariable("_state", "Enum.states");
                AddVariable("prevstate", "Enum.states");
                AddVariable("_prevstate", "Enum.states");
                AddVariable("substate", "Enum.states");
                AddVariable("arenastate", "Enum.states");
                AddVariable("player_state", "Enum.states");
                AddVariable("tauntstoredstate", "Enum.states");
                AddVariable("taunt_storedstate", "Enum.states");
                AddVariable("storedstate", "Enum.states");
                AddVariable("chosenstate", "Enum.states");
                AddVariable("superattackstate", "Enum.states");
                AddVariable("text_state", "Enum.states");
                AddVariable("ministate", "Enum.states");
                AddVariable("dropstate", "Enum.states");
                AddVariable("verticalstate", "Enum.states");
                AddVariable("walkstate", "Enum.states");
                AddVariable("hitstate", "Enum.states");
                AddVariable("toppin_state", "Enum.states");
                AddVariable("bossintrostate", "Enum.states");
                AddVariable("introstate", "Enum.states");
                AddVariable("fadeoutstate", "Enum.states");
                AddVariable("supergrabstate", "Enum.states");
                AddVariable("startstate", "Enum.states");
                AddVariable("atstate", "Enum.states");
                AddVariable("attack_pool", "Array<Enum.states>");
                AddVariable("transformation", "Enum.states");

                // Function Arguments
                AddFunction("gml_Script_vigilante_cancel_attack", ["Enum.states", null]);
                AddFunction("gml_Script_scr_bombshoot", ["Enum.states"]);

                // Apply States to Arrays as well
                AddArray("Enum.states");
            }
            #endregion
        }
    }
}