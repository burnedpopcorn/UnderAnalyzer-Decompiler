/*
    Ultimate_GMS2_Decompiler_v3
        The Ultimate GameMaker Studio 2 Decompiler

    New Features by burnedpopcorn180
    Original Decompiler made by crystallizedsparkle

    Originally used 0.0.1-prerelease as a base
    and added some shit from 0.0.4prerelease and the latest public release 
    (commit hash 2240548beeeae69204cb391095cd5b26bb5446f7 is of time of writing)
    https://github.com/crystallizedsparkle/Gamemaker-LTS-Decompiler/

    This Script is Compatible with Both My UnderAnalyzer Decompiler
    and Bleeding Edge UTMT 0.7.0.0+

    List of New Features I added
        - Replaced YAML Config with an Advanced GUI
        - Added ability to select individual assets to decompile
        - Added ability to decompile as a GameMaker Importable Package (YYMPS) rather than a full GameMaker Project
        - Added ability to include UnknownEnum Declaration into GlobalInit Script
        - Added ability to copy external datafiles into the project (before official implimentation, and a bit better)
        - Added High Quality Icon Extraction for Project and Windows Build Icon
        - Made Progress Bar display Asset Name currently being decompiled

    Included LICENSE file applies ONLY to crystallizedsparkle's code
    Any of my own changes are not under this LICENSE file, and can be freely used by anyone
    (although credit would be nice)
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Dynamic;
using System.Linq;
using System.Security;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using NAudio.Wave;
using NAudio.Vorbis;
using Underanalyzer;
using Underanalyzer.Decompiler;
using UndertaleModLib.Util;
using Underanalyzer.Decompiler.AST;
using Underanalyzer.Decompiler.GameSpecific;
using Underanalyzer.Decompiler.ControlFlow;
using ImageMagick;

// new ui
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Input;

// checks
EnsureDataLoaded();
if (!Data.IsVersionAtLeast(2, 3))
{
    if (!ScriptQuestion("This Script is not intended for games made with GameMaker versions below GMS2.3\n(Consider using the GMS1 Decompiler instead)\n\n Continue Anyways?"))
        return;
}
if (Data.ToolInfo.DecompilerSettings.CreateEnumDeclarations == true)
{
	if (!ScriptQuestion("The 'Create Enum Declarations' Setting is ENABLED\nDecompiling with this Enabled will almost certainly break your decompilation\n\nContinue Anyways?"))
		return;
}

#region options.ini Parser

// for the options.ini file
public static class IniParser
{
    private static dynamic? GetValueType(string value)
    {
        if (value.StartsWith('\"')) // string
            return value;
        else if (value == "True" || value == "False") // boolean
            return Convert.ToBoolean((value == "True"));
        else if (value.Contains('.') && Single.TryParse(value, out float outputFloat)) // double/float
            return outputFloat;
        else if (int.TryParse(value, out int result)) // int
            return result;
        else
            return value; // give up and return a string
    }

    public static Dictionary<string, Dictionary<string, dynamic>> ParseToDictionary(string filePath)
    {
        // obtain the data from the INI
        string[] iniData = null;

        if (File.Exists(filePath))
            iniData = File.ReadAllLines(filePath);
        else
            return null;

        string? section = null;
        Dictionary<string, Dictionary<string, dynamic>> output = new();
        foreach (string line in iniData)
        {
            // get a new section
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = Regex.Replace(line, @"[\[\]]", "");
                output[section] = new Dictionary<string, dynamic>();
            }
            else if (section is not null && line.Contains('='))
            {
                string[] splitLine = line.Split('=');
                string key = splitLine[0];
                string value = splitLine[1];

                dynamic typedValue = GetValueType(value);

                output[section][key] = typedValue;
            }
        }
        return output;
    }
}

#endregion

#region Classes

#region Asset Type Enums
// the color enum ripped from GM
public enum eColour : uint
{
    Red = 4278190335U,
    Green = 4278255360U,
    Blue = 4294901760U,
    Cyan = 4294967040U,
    Purple = 4294902015U,
    Yellow = 4278255615U,
    Orange = 4278232575U,
    White = 4294967295U,
    LightGray = 4290822336U,
    LightBlue = 4294944000U,
    Gray = 4286611584U,
    DarkGray = 4282400832U,
    Black = 4278190080U,
    DarkRed = 4278190208U,
    DarkGreen = 4278222848U,
    DarkBlue = 4286578688U,
    DarkCyan = 4286611456U,
    DarkPurple = 4286578816U,
    DarkYellow = 4278222976U,
    NOALPHA_Red = 16711680U,
    NOALPHA_Green = 65280U,
    NOALPHA_Blue = 16711680U,
    NOALPHA_Cyan = 16776960U,
    NOALPHA_Purple = 16711935U,
    NOALPHA_Yellow = 65535U,
    NOALPHA_White = 16777215U,
    NOALPHA_LightGray = 12632256U,
    NOALPHA_Gray = 8421504U,
    NOALPHA_DarkGray = 4210752U,
    NOALPHA_Black = 0U,
    NOALPHA_DarkRed = 128U,
    NOALPHA_DarkGreen = 32768U,
    NOALPHA_DarkBlue = 8388608U,
    NOALPHA_DarkCyan = 8421376U,
    NOALPHA_DarkPurple = 8388736U,
    NOALPHA_DarkYellow = 32896U,
    HALFALPHA_White = 2164260863U,
    SlateGrey = 4283977031U
}
// asset types
public enum GMAssetType
{
    None = -1,
    Room = 0,
    Sprite = 1,
    Object = 2,
    Script = 3,
    Sound = 4,
    AudioGroup = 5,
    TileSet = 6,
    Note = 7,
    TextureGroup = 8,
    Font = 9,
    Sequence = 10,
    Shader = 11,
    Extension = 12,
    Path = 13,
    AnimationCurve = 14,
    Timeline = 15,
}
// the origin enum ripped from GM
public enum eOrigin
{
    TopLeft,
    TopCentre,
    TopRight,
    MiddleLeft,
    MiddleCentre,
    MiddleRight,
    BottomLeft,
    BottomCentre,
    BottomRight,
    Custom
}

#endregion

#region Internal Data Classes
// really hacky way to get enums
public class MacroData
{
    public MacroTypes Types { get; set; } = new();
    public class MacroTypes
    {
        public Dictionary<string, EnumData> Enums { get; set; } = new();
    }
}

public class EnumData
{
    public EnumData(string name, Dictionary<string, long>? values)
    {
        this.Name = name;
        if (values is not null)
            this.Values = values;
    }
    public Dictionary<string, long> Values { get; set; } = new();
    public string Name { get; set; }
}

public class RunnerData
{
    public RunnerData(string filePath)
    {
        if (filePath == String.Empty || filePath == null)
            return;

        name = Path.GetFileNameWithoutExtension(filePath);
        path = filePath;
        iconData = null;
        var runnerInfo = FileVersionInfo.GetVersionInfo(filePath);

        if (runnerInfo is null)
            return;

        version = runnerInfo.FileVersion;
        companyName = runnerInfo.CompanyName;
        productName = runnerInfo.ProductName;
        copyright = runnerInfo.LegalCopyright;
        description = runnerInfo.FileDescription;
    }
    public string name { get; set; } = "decompiledGame";
    public string path { get; set; } = "";
    public Icon? iconData { get; set; } = null;
    public string version { get; set; } = "";
    public string companyName { get; set; } = "";
    public string productName { get; set; } = "";
    public string copyright { get; set; } = "";
    public string description { get; set; } = "";
}

public class ImageAssetData
{
    public ImageAssetData(UndertaleTexturePageItem image, string filePath, string imageName)
    {
        this.image = image;
        this.filePath = filePath;
        this.imageName = imageName;
    }

    public ImageAssetData(MagickImage image, string filePath, string imageName)
    {
        this.image = image;
        this.filePath = filePath;
        this.imageName = imageName;
    }
    // either UndertaleTexturePageItem or MagickImage
    public dynamic image { get; set; }
    public string filePath { get; set; }
    public string imageName { get; set; }
    public void Dump(TextureWorker tw)
    {
        if (image is null) return;

        tw.ExportAsPNG(image, filePath + imageName, null, true);
    }
}

#endregion

#region Common Classes

public class GMResource
{
    public GMResource()
    {
        resourceType = base.GetType().Name;
    }
    public string resourceType { get; set; }
    public string resourceVersion { get; set; } = "1.0";
    // ignore these conditions when they're null
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AssetReference? parent { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[] tags { get; set; }
}

public class AssetReference
{
    public AssetReference(string name, GMAssetType type)
    {
        this.name = name;
        path = CreateFilePath(name, type);
    }
    // overload method
    public AssetReference()
    {

    }
    public string name { get; set; }
    public string path { get; set; }
}

#endregion

#region GMProject Class

public class GMProject : GMResource
{
    public GMProject(string name)
    {
        resourceVersion = "1.6";
        this.name = name;
    }

    public ConcurrentQueue<Resource> resources { get; set; } = new();
    public List<AssetReference> Options { get; set; } = new();
    public int defaultScriptType { get; set; } = 1;
    public bool isEcma { get; set; } = false;
    public RoomOrderNode[] RoomOrderNodes { get; set; } = new RoomOrderNode[0];
    public GMFolder[] Folders { get; set; } = CreateDefaultFolders();
    public GMAudioGroup[] AudioGroups { get; set; } = new GMAudioGroup[0];
    public List<GMTextureGroup> TextureGroups { get; set; } = new();
    public List<GMIncludedFile> IncludedFiles { get; set; } = new();
    public ProjectMetaData MetaData { get; set; } = new();

    public class ProjectMetaData
    {
        public string IDEVersion { get; set; } = "2022.0.3.85"; // the IDE version this script was made for
    }

    public class Resource
    {
        public AssetReference id { get; set; }
        public int order { get; set; }
        // type of resource
        [JsonIgnore]
        public GMAssetType type { get; set; } = GMAssetType.None;
    }

    public class BuildConfig
    {
        string name { get; set; } = "Default";
        BuildConfig[] children { get; set; } = new BuildConfig[0];
    }

    public class RoomOrderNode
    {
        public RoomOrderNode(string name)
        {
            roomId = new AssetReference(name, GMAssetType.Room);
        }
        public AssetReference roomId { get; set; }
    }

    public class GMAudioGroup : GMResource
    {
        public GMAudioGroup(string name)
        {
            resourceVersion = "1.3";
            this.name = name;
        }
        public long targets { get; set; } = -1L;
    }

    public class GMTextureGroup : GMResource
    {
        public GMTextureGroup(string name)
        {
            resourceVersion = "1.3";
            this.name = name;
        }
        public bool isScaled { get; set; } = true;
        public string compressFormat { get; set; }
        public string loadType { get; set; } = "default";
        public string directory { get; set; } = String.Empty;
        public bool autocrop { get; set; } = true;
        public int border { get; set; } = 2;
        public int mipsToGenerate { get; set; } = 0;
        public AssetReference? groupParent { get; set; } = null;
        public long targets { get; set; } = -1L;
    }

    public class GMIncludedFile : GMResource
    {
        public GMIncludedFile(string name)
        {
            this.name = name;
        }
        public long CopyToMask { get; set; } = -1L;
        public string filePath { get; set; } = "datafiles";
    }

    public class GMFolder : GMResource
    {
        public GMFolder(string name, string folderPath)
        {
            this.name = name;
            this.folderPath = folderPath;
        }
        public string folderPath { get; set; }
        public int order { get; set; }
    }

    static GMFolder[] CreateDefaultFolders()
    {
        // lazy solution, not planning to do much with folders so it doesnt matter.
        int currentOrder = 0;
        return new GMFolder[]
        {
            new GMFolder("Sprites", "folders/Sprites.yy") { order = ++currentOrder },
            new GMFolder("Tile Sets", "folders/Tile Sets.yy") { order = ++currentOrder },
            new GMFolder("Sounds", "folders/Sounds.yy") { order = ++currentOrder },
            new GMFolder("Paths", "folders/Paths.yy") { order = ++currentOrder },
            new GMFolder("Scripts", "folders/Scripts.yy") { order = ++currentOrder },
            new GMFolder("Shaders", "folders/Shaders.yy") { order = ++currentOrder },
            new GMFolder("Fonts", "folders/Fonts.yy") { order = ++currentOrder },
            new GMFolder("Timelines", "folders/Timelines.yy") { order = ++currentOrder },
            new GMFolder("Objects", "folders/Objects.yy") { order = ++currentOrder },
            new GMFolder("Rooms", "folders/Rooms.yy") { order = ++currentOrder },
            new GMFolder("Sequences", "folders/Sequences.yy") { order = ++currentOrder },
            new GMFolder("Animation Curves", "folders/Animation Curves.yy") { order = ++currentOrder },
            new GMFolder("Notes", "folders/Notes.yy") { order = ++currentOrder },
            new GMFolder("Extensions", "folders/Extensions.yy") { order = ++currentOrder },
            new GMFolder("DecompilerGenerated", "folders/DecompilerGenerated.yy") { order = ++currentOrder }, // for things like tile data & gml_pragma.
            new GMFolder("GeneratedTileSprites", "folders/DecompilerGenerated/GeneratedTileSprites.yy") { order = ++currentOrder }
        };

    }
}

#endregion

#region GMObject Class

public class GMObject : GMResource
{
    public GMObject(string name)
    {
        this.name = name;

        parent = GetParentFolder(GMAssetType.Object);
    }
    public AssetReference? spriteId { get; set; } = null;
    public bool solid { get; set; } = false;
    public bool visible { get; set; } = true;
    public bool managed { get; set; } = true;
    public AssetReference? spriteMaskId { get; set; } = null;
    public bool persistent { get; set; } = false;
    public AssetReference? parentObjectId { get; set; } = null;
    public bool physicsObject { get; set; } = false;
    public bool physicsSensor { get; set; } = false;
    public int physicsShape { get; set; }
    public int physicsGroup { get; set; }
    public float physicsDensity { get; set; } = 0.5f;
    public float physicsRestitution { get; set; } = 0.1f;
    public float physicsLinearDamping { get; set; } = 0.1f;
    public float physicsAngularDamping { get; set; } = 0.1f;
    public float physicsFriction { get; set; } = 0.2f;
    public bool physicsStartAwake { get; set; } = true;
    public bool physicsKinematic { get; set; } = false;
    public GMPoint[] physicsShapePoints { get; set; } = new GMPoint[0];
    public List<GMEvent> eventList { get; set; } = new();
    public List<GMObjectProperty> properties { get; set; } = new();
    public List<GMOverriddenProperty> overriddenProperties { get; set; } = new();
}

public class GMEvent : GMResource
{
    public GMEvent()
    {
        name = String.Empty;
    }
    public bool isDnD { get; set; } = false;
    public int eventNum { get; set; }
    public int eventType { get; set; }
    public AssetReference? collisionObjectId { get; set; } = null;
}

public class GMObjectProperty : GMResource
{
    public GMObjectProperty(string name)
    {
        this.name = name;
    }
    public int varType { get; set; }
    public string value { get; set; }
    public bool rangeEnabled { get; set; } = false;
    public float rangeMin { get; set; } = 0f;
    public float rangeMax { get; set; } = 0f;
    public string[] listItems { get; set; } = new string[0];
    public bool multiselect { get; set; } = false;
    public string[] filters { get; set; } = new string[0];
}

public class GMOverriddenProperty : GMResource
{
    public AssetReference propertyId { get; set; }
    public AssetReference objectId { get; set; }
    public string value { get; set; }
}

#endregion

#region GMScript Class

public class GMScript : GMResource
{
    public GMScript(string name)
    {
        this.name = name;

        parent = GetParentFolder(GMAssetType.Script);
    }
    public bool isDnd { get; set; } = false;
    public bool isCompatibility { get; set; } = false;
}

#endregion

#region GMSound Class

public class GMSound : GMResource
{
    public GMSound(string name)
    {
        this.name = name;

        parent = GetParentFolder(GMAssetType.Sound);
    }
    public int conversionMode { get; set; }
    public int compression { get; set; }
    public float volume { get; set; }
    public bool preload { get; set; }
    public int bitRate { get; set; } = 128; // cant obtain original value afaik
    public int sampleRate { get; set; } = 44100;
    public int type { get; set; } = 0;
    public int bitDepth { get; set; } = 1; // cant obtain original value afaik
    public AssetReference audioGroupId { get; set; }
    public string soundFile { get; set; }
    public float duration { get; set; } = 0f;
    public AssetReference parent { get; set; }
}

#endregion

#region GMRoom Class

public class GMRoom : GMResource
{
    public GMRoom(string roomName)
    {
        parent = GetParentFolder(GMAssetType.Room);

        name = roomName;
    }

    public bool isDnd { get; set; } = false;
    public float volume { get; set; } = 1f;
    public AssetReference? parentRoom { get; set; } = null;
    public GMRView[] views { get; set; } = new GMRView[0];
    public List<dynamic> layers { get; set; } = new();
    public bool inheritLayers { get; set; } = false;
    public string creationCodeFile { get; set; } = String.Empty;
    public bool inheritCode { get; set; } = false;
    public List<AssetReference> instanceCreationOrder { get; set; } = new();
    public bool inheritCreationOrder { get; set; }
    public AssetReference? sequenceId { get; set; } = null;
    public GMRoomSettings roomSettings { get; set; } = new GMRoomSettings();
    public GMRoomViewSettings viewSettings { get; set; } = new GMRoomViewSettings();
    public GMRoomPhysicsSettings physicsSettings { get; set; } = new GMRoomPhysicsSettings();
    public AssetReference parent { get; set; }
    public class GMRAsset : GMResource
    {
        public float x { get; set; }
        public float y { get; set; }
        public AssetReference spriteId { get; set; }
        public float headPosition { get; set; }
        public float rotation { get; set; }
        public float scaleX { get; set; }
        public float scaleY { get; set; }
        public float animationSpeed { get; set; }
        public uint colour { get; set; }
        public AssetReference? inheritedItemId { get; set; } = null;
        public bool frozen { get; set; }
        public bool ignore { get; set; }
        public bool inheritItemSettings { get; set; }
    }



    public class GMRSpriteGraphic : GMRAsset
    {
        public GMRSpriteGraphic()
        {
            this.name = name;
        }

        public float headPosition { get; set; }
        public float rotation { get; set; }
        public float scaleX { get; set; }
        public float scaleY { get; set; }
        public float animationSpeed { get; set; }
    }

    public class GMRLayerBase : GMResource
    {
        public GMRLayerBase(string name)
        {
            this.name = name;
        }
        public GMRLayerBase()
        {

        }
        public bool visible { get; set; } = true;
        public float depth { get; set; } = 0;
        public bool userdefinedDepth { get; set; } = false;
        public bool inheritLayerDepth { get; set; } = false;
        public bool inheritLayerSettings { get; set; } = false;
        //public bool inheritVisibility { get; set; } = true;
        //public bool inheritSubLayers { get; set; } = false;
        public double gridX { get; set; } = 32;
        public double gridY { get; set; } = 32;
        public List<GMRLayerBase> layers { get; set; } = new();
        public bool hierarchyFrozen { get; set; } = false;
        public bool effectEnabled { get; set; } = true;
        public string? effectType { get; set; } = null;
        public GMREffectProperty[] properties { get; set; } = new GMREffectProperty[0];
    }

    public class GMREffectProperty
    {
        public int type { get; set; } = 0;
        public string name { get; set; }
        public string value { get; set; }
    }

    public class GMREffectLayer : GMRLayerBase
    {
        public GMREffectLayer(string name)
        {
            this.name = name;
        }
        // its basically the layer base
    }

    public class GMRAssetLayer : GMRLayerBase
    {
        public GMRAssetLayer(string name)
        {
            this.name = name;
        }

        public List<dynamic> assets { get; set; } = new();

        public class GMRGraphic : GMRAsset
        {
            public GMRGraphic(string name)
            {
                this.name = name;
            }
            public uint w { get; set; }
            public uint h { get; set; }
            public int u0 { get; set; }
            public int v0 { get; set; }
            public int u1 { get; set; }
            public int v1 { get; set; }
        }
    }

    public class GMRBackgroundLayer : GMRLayerBase
    {
        public GMRBackgroundLayer(string name)
        {
            this.name = name;
        }
        public AssetReference? spriteId { get; set; } = null;
        public uint colour { get; set; }
        public float x { get; set; }
        public float y { get; set; }
        public bool htiled { get; set; }
        public bool vtiled { get; set; }
        public float hspeed { get; set; }
        public float vspeed { get; set; }
        public bool stretch { get; set; }
        public float animationFPS { get; set; }
        public int animationSpeedType { get; set; }
        public bool userdefinedAnimFPS { get; set; }
    }

    public class GMRPathLayer : GMRLayerBase
    {
        public GMRPathLayer(string name)
        {
            this.name = name;
        }
        public AssetReference? pathId { get; set; } = null;
        public uint colour { get; set; }
    }

    public class GMRTileLayer : GMRLayerBase
    {
        public GMRTileLayer(string name)
        {
            resourceVersion = "1.1";
            this.name = name;
        }
        public AssetReference? tilesetId { get; set; }
        // offset
        public float x { get; set; }
        public float y { get; set; }
        public GMRTileData tiles { get; set; }

        public class GMRTileData
        {
            //public int TileDataFormat { get; set; } = 1; // unknown
            public int SerialiseWidth { get; set; }
            public int SerialiseHeight { get; set; }
            // uint because thats what it is in gamemaker.
            public List<uint> TileSerialiseData { get; set; } = new();
        }
    }


    public class GMRInstanceLayer : GMRLayerBase
    {
        public GMRInstanceLayer(string name)
        {
            this.name = name;
        }
        public List<GMRInstance> instances { get; set; } = new();

        public class GMRInstance : GMResource
        {
            public GMRInstance(string name)
            {
                this.name = name;
            }
            public List<GMOverriddenProperty> properties { get; set; } = new();
            public bool isDnd { get; set; }
            public AssetReference objectId { get; set; }
            public bool inheritCode { get; set; }
            public bool hasCreationCode { get; set; }
            public uint colour { get; set; }
            public float rotation { get; set; }
            public float scaleX { get; set; }
            public float scaleY { get; set; }
            public float imageSpeed { get; set; }
            public int imageIndex { get; set; }
            public AssetReference? inheritedItemId { get; set; } = null;
            public bool frozen { get; set; }
            public bool ignore { get; set; }
            public bool inheritItemSettings { get; set; }
            public float x { get; set; }
            public float y { get; set; }
        }
    }

    public class GMRoomPhysicsSettings
    {
        public bool inheritPhysicsSettings { get; set; } = false;
        public bool PhysicsWorld { get; set; } = false;
        public float PhysicsWorldGravityX { get; set; } = 0f;
        public float PhysicsWorldGravityY { get; set; } = 10f;
        public float PhysicsWorldPixToMetres { get; set; } = 0.1f;
    }
    public class GMRoomViewSettings
    {
        public bool inheritViewSettings { get; set; } = false;
        public bool enableViews { get; set; } = false;
        public bool clearViewBackground { get; set; } = false;
        public bool clearDisplayBuffer { get; set; } = true;
    }
    public class GMRView
    {
        public bool inherit { get; set; } = false;
        public bool visible { get; set; } = false;
        public int xview { get; set; } = 0;
        public int yview { get; set; } = 0;
        public int wview { get; set; } = 1366;
        public int hview { get; set; } = 768;
        public int xport { get; set; } = 0;
        public int yport { get; set; } = 0;
        public int wport { get; set; } = 1366;
        public int hport { get; set; } = 768;
        public uint hborder { get; set; } = 32;
        public uint vborder { get; set; } = 32;
        public int hspeed { get; set; } = -1;
        public int vspeed { get; set; } = -1;
        public AssetReference? objectId { get; set; } = null;
    }
    public class GMRoomSettings
    {
        public bool inheritRoomSettings { get; set; } = false;
        public uint Width { get; set; } = 1366;
        public uint Height { get; set; } = 768;
        public bool persistent { get; set; } = false;
    }
}

#endregion

#region GMAnimCurve Class

public class GMPoint
{
    public GMPoint(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
    public float x { get; set; } = 0f;
    public float y { get; set; } = 0f;
}

public class GMAnimCurve : GMResource
{
    public int function { get; set; }
    public List<GMAnimCurveChannel> channels { get; set; } = new();
    public AssetReference parent { get; set; }
    public GMAnimCurve(string name)
    {
        resourceVersion = "1.2";
        this.name = name;
    }
    public class GMAnimCurveChannel : GMResource
    {
        public GMAnimCurveChannel(string name)
        {
            this.name = name;
        }
        public uint colour { get; set; } = 4290799884;
        public bool visible { get; set; } = true;
        public List<GMAnimCurvePoint> points { get; set; } = new();

    }
    public class GMAnimCurvePoint : GMPoint
    {
        public GMAnimCurvePoint(float x, float y) : base(x, y)
        {
        }
        public float th0 { get; set; }
        public float th1 { get; set; }
        public float tv0 { get; set; }
        public float tv1 { get; set; }
    }
}

#endregion

#region GMSprite Class

public class GMSprite : GMResource
{
    public GMSprite(string name)
    {
        this.name = name;
    }

    public int bboxMode { get; set; } = 0;
    public int collisionKind { get; set; } = 1;
    public int type { get; set; } = 0;
    public eOrigin origin { get; set; } = 0;
    public bool preMultiplyAlpha { get; set; } = false;
    public bool edgeFiltering { get; set; } = false;
    public int collisionTolerance { get; set; } = 0;
    public float swfPrecision { get; set; } = 2.525f;
    public int bbox_left { get; set; }
    public int bbox_right { get; set; }
    public int bbox_top { get; set; }
    public int bbox_bottom { get; set; }
    public bool HTile { get; set; }
    public bool VTile { get; set; }
    public bool For3D { get; set; }
    public bool DynamicTexturePage { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public AssetReference textureGroupId { get; set; }
    public uint[]? swatchColours { get; set; } = null;
    public int gridX { get; set; }
    public int gridY { get; set; }
    public List<GMSpriteFrame> frames { get; set; } = new();
    public GMSequence sequence { get; set; }
    public List<GMImageLayer> layers { get; set; } = new();
    public GMNineSliceData? nineSlice { get; set; }
    public AssetReference parent { get; set; }

    public class GMSpriteFrame : GMResource
    {
        public GMSpriteFrame(string frameGuid)
        {
            resourceVersion = "1.1";
            name = frameGuid;
        }
    }
    public class GMImageLayer : GMResource
    {
        public GMImageLayer(string name)
        {
            this.name = name;
        }
        public bool visible { get; set; } = true;
        public bool isLocked { get; set; }
        public int blendMode { get; set; } = 0;
        public float opacity { get; set; } = 100f;
        public string displayName { get; set; } = "default";
    }
    public class GMNineSliceData : GMResource
    {
        public int left { get; set; }
        public int top { get; set; }
        public int right { get; set; }
        public int bottom { get; set; }
        public uint[] guideColour { get; set; } = new uint[] { 4294902015U, 4294902015U, 4294902015U, 4294902015U, 4294902015U };
        public uint highlightColour { get; set; } = 1728023040U;
        public int highlightStyle { get; set; }
        public bool enabled { get; set; }
        public int[] tileMode { get; set; }
    }
}

#endregion

#region GMSequence Class

public class GMSequence : GMResource
{
    public GMSequence(string name)
    {
        resourceVersion = "1.4";
        this.name = name;
    }
    public int timeUnits { get; set; } = 1;
    public int playback { get; set; } = 1;
    public float playbackSpeed { get; set; } = 30f;
    public int playbackSpeedType { get; set; } = 0;
    public bool autoRecord { get; set; } = true;
    public float volume { get; set; } = 1f;
    public float length { get; set; } = 1f;
    public KeyframeStore<MessageEventKeyframe> events { get; set; } = new();
    public KeyframeStore<MomentsEventKeyframe> moments { get; set; } = new();
    public List<dynamic> tracks { get; set; } = new();
    public GMPoint? visibleRange { get; set; } = null;
    public bool lockOrigin { get; set; } = false;
    public bool showBackdrop { get; set; } = true;
    public bool showBackdropImage { get; set; } = false;
    public string backdropImagePath { get; set; } = String.Empty;
    public float backdropImageOpacity { get; set; } = 0.5f;
    public int backdropWidth { get; set; } = 1366;
    public int backdropHeight { get; set; } = 768;
    public float backdropXOffset { get; set; } = 0f;
    public float backdropYOffset { get; set; } = 0f;
    public int xorigin { get; set; } = 0;
    public int yorigin { get; set; } = 0;
    public Dictionary<string, string> eventToFunction { get; set; } = new();
    public AssetReference eventStubScript { get; set; }
    public AssetReference spriteId { get; set; }
}

public class GMBaseTrack : GMResource
{
    public uint trackColour { get; set; } = 0U;
    public bool inheritsTrackColour { get; set; } = true;
    public int builtinName { get; set; } = -1;
    public int traits { get; set; }
    public int interpolation { get; set; } = 1;
    public List<dynamic> tracks { get; set; } = new();
    public List<GMEvent> events { get; set; } = new();
    public bool isCreationTrack { get; set; }
    public string[] modifiers { get; set; } = new string[0]; // IDE considers this a dictionary??? weird.
}

public class GMGraphicTrack : GMBaseTrack
{
    public KeyframeStore<AssetSpriteKeyframe> keyframes { get; set; } = new();
}

public class GMTextTrack : GMBaseTrack
{
    public KeyframeStore<AssetTextKeyframe> keyframes { get; set; } = new();
}

public class AssetTextKeyframe : AssetKeyframe
{
    public string? Text { get; set; } = null;
    public bool Wrap { get; set; }
    public int Alignment { get; set; } = 0;
}

public class GMSpriteFramesTrack : GMBaseTrack
{
    public AssetReference spriteId { get; set; }
    public KeyframeStore<SpriteFrameKeyframe> keyframes { get; set; } = new KeyframeStore<SpriteFrameKeyframe>();
    public string name { get; set; } = "frames";
}

public class GMGroupTrack : GMBaseTrack
{

}

public class GMClipMaskTrack : GMBaseTrack
{

}
public class GMClipMask_Mask : GMBaseTrack
{

}
public class GMClipMask_Subject : GMBaseTrack
{

}

public class GMRealTrack : GMBaseTrack
{
    public KeyframeStore<RealKeyframe> keyframes { get; set; } = new();
}

public class GMColourTrack : GMBaseTrack
{
    public KeyframeStore<ColourKeyframe> keyframes { get; set; } = new();
}

public class GMAudioTrack : GMBaseTrack
{
    public KeyframeStore<AudioKeyframe> keyframes { get; set; } = new();
}

public class GMInstanceTrack : GMBaseTrack
{
    public KeyframeStore<AssetInstanceKeyframe> keyframes { get; set; } = new();
}

public class GMSequenceTrack : GMBaseTrack
{
    public KeyframeStore<AssetSequenceKeyframe> keyframes { get; set; } = new();
}

public class AssetSequenceKeyframe : AssetKeyframe
{

}

public class AssetInstanceKeyframe : AssetKeyframe
{

}

public class AssetSpriteKeyframe : AssetKeyframe
{

}
public class AnimCurveKeyframe : GMResource
{
    // yeah you can put anim curves inside of sequences (scary!!!)
    public AssetReference AnimCurveId { get; set; }
    public GMAnimCurve EmbeddedAnimCurve { get; set; }
}

public class RealKeyframe : AnimCurveKeyframe
{
    public float RealValue { get; set; }
}

public class ColourKeyframe : AnimCurveKeyframe
{
    public uint Colour { get; set; }
}

public class AudioKeyframe : AssetKeyframe
{
    public int Mode { get; set; }
}

public class KeyframeStore<T> : GMResource
{
    public string resourceType { get { return $"KeyframeStore<{typeof(T).Name}>"; } }
    public List<Keyframe<T>> Keyframes { get; set; } = new();
}

public class Keyframe<T> : GMResource
{
    public Guid id { get; set; } = Guid.NewGuid();
    public float Key { get; set; } = 0f;
    public float Length { get; set; } = 1f;
    public string resourceType { get { return $"Keyframe<{typeof(T).Name}>"; } }
    public bool Stretch { get; set; } = false;
    public bool Disabled { get; set; } = false;
    public bool IsCreationKey { get; set; } = false;
    public Dictionary<string, T> Channels { get; set; } = new();
}

public class AssetKeyframe : GMResource
{
    public AssetReference Id { get; set; }
}

public class SpriteFrameKeyframe : AssetKeyframe
{

}
public class MessageEventKeyframe : GMResource
{
    public string[] Events { get; set; } = new string[0];
}

public class MomentsEventKeyframe : GMResource
{
    public List<string> Events { get; set; } = new();
}

#endregion

#region GMNote Class

public class GMNotes : GMResource
{
    public GMNotes(string name)
    {
        resourceVersion = "1.1";
        this.name = name;
        parent = GetParentFolder(GMAssetType.Note);
    }
}

#endregion

#region GMFont Class

public class GMFont : GMResource
{
    public class GMGlyph
    {
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }
        public int character { get; set; }
        public int shift { get; set; }
        public int offset { get; set; }
    }

    public class GMKerningPair
    {
        public int first { get; set; }
        public int second { get; set; }
        public int amount { get; set; } = -1;
    }

    public class GMFontRange
    {
        public int lower { get; set; }
        public int upper { get; set; }
    }

    public GMFont(string name)
    {
        this.name = name;
    }
    public int hinting { get; set; }
    public int glyphOperations { get; set; }
    public int interpreter { get; set; }
    public int pointRounding { get; set; }
    public int applyKerning { get; set; }
    public string fontName { get; set; }
    public string styleName { get; set; }
    public float size { get; set; } = 12f;
    public bool bold { get; set; }
    public bool italic { get; set; }
    public int charset { get; set; }
    public int AntiAlias { get; set; } = 1;
    public int first { get; set; }
    public int last { get; set; }
    public string sampleText { get; set; } = "abcdef ABCDEF\n0123456789 .,<>\"'&!?\nthe quick brown fox jumps over the lazy dog\nTHE QUICK BROWN FOX JUMPS OVER THE LAZY DOG\nDefault character: ▯ (9647)";
    public bool includeTTF { get; set; }
    public string TTFName { get; set; }
    public AssetReference textureGroupId { get; set; }
    public int ascenderOffset { get; set; }
    public int ascender { get; set; }
    public int lineHeight { get; set; }
    public Dictionary<int, GMGlyph> glyphs { get; set; } = new();
    public List<GMKerningPair> kerningPairs { get; set; } = new();
    public List<GMFontRange> ranges { get; set; } = new();
    public bool regenerateBitmap { get; set; }
    public bool canGenerateBitmap { get; set; }
    public bool maintainGms1Font { get; set; }
}


#endregion

#region GMShader Class

public class GMShader : GMResource
{
    public GMShader(string name)
    {
        this.name = name;
    }
    public int type { get; set; } = 1; // GLSL-ES, GLSL, HLSL-11, PSSL
    public AssetReference parent { get; set; }
}

#endregion

#region GMExtension Class

public class GMExtension : GMResource
{
    public GMExtension(string name)
    {
        this.name = name;
        resourceVersion = "1.2";
    }
    public class GMExtensionOption : GMResource
    {
        public GMExtensionOption(string name)
        {
            this.name = name;
        }
        public AssetReference? extensionId { get; set; }
        public Guid guid { get; set; } = Guid.NewGuid();
        public string displayName { get; set; }
        public string[] listItems { get; set; } = new string[0]; // make sure its created in the first place
        public string description { get; set; }
        public string defaultValue { get; set; } = "0";
        public bool exportToINI { get; set; }
        public bool hidden { get; set; }
        public int optType { get; set; } = 1; // default to 1
    }
    public class GMExtensionConstant : GMResource
    {
        public string value { get; set; } = String.Empty;
        public bool hidden { get; set; }
    }
    public class GMProxyFile : GMResource
    {
        public int TargetMask { get; set; }
    }
    public class GMExtensionFile : GMResource
    {
        public string filename { get; set; }
        public string origname { get; set; } = String.Empty;
        public string init { get; set; }
        public string final { get; set; }
        public int kind { get; set; }
        public bool uncompress { get; set; } = false;
        public List<GMExtensionFunction> functions { get; set; } = new();
        public GMExtensionConstant[] constants { get; set; } = new GMExtensionConstant[0];
        public GMProxyFile[] ProxyFiles { get; set; } = new GMProxyFile[0];
        public int copyToTargets { get; set; } = -1;
        public bool usesRunnerInterface { get; set; } = false;
        public string[] order { get; set; } = new string[0];
    }
    public class GMExtensionFrameworkEntry : GMResource
    {
        public bool weakReference { get; set; }
        public int embed { get; set; }
    }
    public class GMExtensionFunction : GMResource
    {
        public GMExtensionFunction(string name)
        {
            this.name = name;
        }
        public int argCount { get; set; }
        public int[] args { get; set; } = new int[0];
        public string documentation { get; set; } = String.Empty;
        public string externalName { get; set; }
        public string help { get; set; } = String.Empty;
        public bool hidden { get; set; } = false;
        public int kind { get; set; }
        public int returnType { get; set; }
    }
    public string extensionVersion { get; set; } = "0.0.1";
    public int copyToTargets { get; set; } = -1;
    public bool androidProps { get; set; }
    public bool iosProps { get; set; }
    public bool tvosProps { get; set; }
    public bool html5Props { get; set; }
    public string optionsFile { get; set; }
    public List<GMExtensionOption> options { get; set; } = new();
    public bool exportToGame { get; set; } = true;
    public int supportedTargets { get; set; } = -1;
    public string packageId { get; set; } = String.Empty;
    public string productId { get; set; } = String.Empty;
    public string author { get; set; } = String.Empty;
    public DateTime date { get; set; } = DateTime.Now;
    public string license { get; set; } = String.Empty;
    public string description { get; set; } = String.Empty;
    public string helpfile { get; set; } = String.Empty;
    public string installdir { get; set; } = String.Empty;
    public List<GMExtensionFile> files { get; set; } = new();
    public string HTML5CodeInjection { get; set; } = String.Empty;
    public string classname { get; set; } = String.Empty;
    // tvos stuff can be nullable for some reason
    public string? tvosclassname { get; set; } = null;
    public string? tvosdelegatename { get; set; } = null;
    public string iosDelegateName { get; set; } = String.Empty;
    public string androidClassName { get; set; } = String.Empty;
    public string sourceDir { get; set; } = String.Empty;
    public string androidSourceDir { get; set; } = String.Empty;
    public string macSourceDir { get; set; } = String.Empty;
    public string macCompilerFlags { get; set; } = String.Empty;
    public string tvosMacCompilerFlags { get; set; } = String.Empty;
    public string macLinkerFlags { get; set; } = String.Empty;
    public string tvosMacLinkerFlags { get; set; } = String.Empty;
    public string iosPlistInject { get; set; } = String.Empty;
    public string tvosPlistInject { get; set; } = String.Empty;
    public string androidInject { get; set; } = String.Empty;
    public string androidManifestInject { get; set; } = String.Empty;
    public string androidActivityInject { get; set; } = String.Empty;
    public string gradleInject { get; set; } = String.Empty;
    public string androidCodeInjection { get; set; } = String.Empty;
    public bool hasConvertedCodeInjection { get; set; } = false;
    public string ioscodeinjection { get; set; } = String.Empty;
    public string tvoscodeinjection { get; set; } = String.Empty;
    public GMExtensionFrameworkEntry[] iosSystemFrameworkEntries { get; set; } = new GMExtensionFrameworkEntry[0];
    public GMExtensionFrameworkEntry[] tvosSystemFrameworkEntries { get; set; } = new GMExtensionFrameworkEntry[0];
    public GMExtensionFrameworkEntry[] iosThirdPartyFrameworkEntries { get; set; } = new GMExtensionFrameworkEntry[0];
    public GMExtensionFrameworkEntry[] tvosThirdPartyFrameworkEntries { get; set; } = new GMExtensionFrameworkEntry[0];
    public string[] IncludedResources { get; set; } = new string[0];
    public string[] androidPermissions { get; set; } = new string[0];
    public string iosCocoaPods { get; set; } = String.Empty;
    public string tvosCocoaPods { get; set; } = String.Empty;
    public string iosCocoaPodDependencies { get; set; } = String.Empty;
    public string tvosCocoaPodDependencies { get; set; } = String.Empty;
}


#endregion

#region GMPath Class

public class GMPath : GMResource
{
    public GMPath(string name)
    {
        this.name = name;
    }

    public int kind { get; set; }
    public int precision { get; set; } // 1-8
    public bool closed { get; set; }
    public GMPathPoint[] points { get; set; } = new GMPathPoint[0];
    public AssetReference parent { get; set; }


    public class GMPathPoint : GMPoint
    {
        public GMPathPoint(float x, float y) : base(x, y)
        {
        }
        public float speed { get; set; } = 100f;
    }
}

#endregion

#region GMTileSet Class

public class GMTileSet : GMResource
{
    public GMTileSet(string name)
    {
        this.name = name;
    }

    public AssetReference? spriteId { get; set; }
    public int tileWidth { get; set; }
    public int tileHeight { get; set; }
    public int tilexoff { get; set; }
    public int tileyoff { get; set; }
    public int tilehsep { get; set; }
    public int tilevsep { get; set; }
    public int out_tilehborder { get; set; }
    public int out_tilevborder { get; set; }
    public bool spriteNoExport { get; set; } = true;
    public AssetReference textureGroupId { get; set; }
    public int out_columns { get; set; }
    public int tile_count { get; set; }
    public GMAutoTileSet[] autoTileSets { get; set; } = new GMAutoTileSet[0];
    public List<GMTileAnimation> tileAnimationFrames { get; set; } = new();
    public float tileAnimationSpeed { get; set; } = 15f;
    public TileAnimation tileAnimation { get; set; }
    public MacroPageTiles macroPageTiles { get; set; } = new();
    public AssetReference parent { get; set; }

    public class GMAutoTileSet : GMResource
    {
        public string name { get; set; } = "autotile_1";
        public List<int> tiles { get; set; } = new();
        public bool closed_edge { get; set; }
    }

    public class GMTileAnimation : GMResource
    {
        public GMTileAnimation(int index)
        {
            name = $"animation_{index}";
        }
        public List<uint> frames { get; set; }
    }
    // the tile data
    public class TileAnimation
    {
        public uint[] frameData { get; set; } = new uint[0];
        public int SerialiseFrameCount { get; set; } = 1;
    }

    public class MacroPageTiles
    {
        public int SerialiseWidth { get; set; } = 0;
        public int SerialiseHeight { get; set; } = 0;
        public uint[] TileSerialiseData { get; set; } = new uint[0];
    }
}

#endregion

#region GMOptions Class

public class GMMainOptions : GMResource
{
    public GMMainOptions()
    {
        name = "Main";
        resourceVersion = "1.4";
    }
    public Guid option_gameguid { get; set; } = Guid.NewGuid();
    public string option_gameid { get; set; } = "0";
    public int option_game_speed { get; set; } = 60;
    public bool option_mips_for_3d_textures { get; set; }
    // https://www.reddit.com/r/pathofexile/comments/mse8lm/for_people_wondering_about_4294967295_it_is_the/
    public uint option_draw_colour { get; set; } = uint.MaxValue;
    public byte option_window_colour { get; set; } = byte.MaxValue;
    public string option_steam_app_id { get; set; } = "0";
    public bool option_collision_compatibility { get; set; }
    public bool option_copy_on_write_enabled { get; set; }
    public bool option_spine_licence { get; set; }
    public string option_template_image { get; set; } = "${base_options_dir}/main/template_image.png";
    public string option_template_icon { get; set; } = "${base_options_dir}/main/template_icon.png";
    public string? option_template_description { get; set; } = null;
}

public class GMWindowsOptions : GMResource
{
    public GMWindowsOptions()
    {
        name = "Windows";
        resourceVersion = "1.1";
    }
    public string option_windows_display_name { get; set; } = "Created with GameMaker";
    public string option_windows_executable_name { get; set; } = "${project_name}.exe";
    public string option_windows_version { get; set; } = "1.0.0.0";
    public string option_windows_company_info { get; set; } = "YoYo Games Ltd";
    public string option_windows_product_info { get; set; } = "Created with GameMaker";
    public string option_windows_copyright_info { get; set; } = String.Empty;
    public string option_windows_description_info { get; set; } = "A GameMaker Game";
    public bool option_windows_display_cursor { get; set; } = true;
    public string option_windows_icon { get; set; } = "icons/icon.ico";
    public int option_windows_save_location { get; set; } = 0;
    public string option_windows_splash_screen { get; set; } = "${base_options_dir}/windows/splash/splash.png";
    public bool option_windows_use_splash { get; set; }
    public bool option_windows_start_fullscreen { get; set; }
    public bool option_windows_allow_fullscreen_switching { get; set; }
    public bool option_windows_interpolate_pixels { get; set; }
    public bool option_windows_vsync { get; set; }
    public bool option_windows_resize_window { get; set; }
    public bool option_windows_borderless { get; set; }
    public int option_windows_scale { get; set; } = 0;
    public bool option_windows_copy_exe_to_dest { get; set; }
    public int option_windows_sleep_margin { get; set; } = 10;
    public string option_windows_texture_page { get; set; } = "2048x2048";
    public string option_windows_installer_finished { get; set; } = "${base_options_dir}/windows/installer/finished.bmp";
    public string option_windows_installer_header { get; set; } = "${base_options_dir}/windows/installer/header.bmp";
    public string option_windows_license { get; set; } = "${base_options_dir}/windows/installer/license.txt";
    public string option_windows_nsis_file { get; set; } = "${base_options_dir}/windows/installer/nsis_script.nsi";
    public bool option_windows_enable_steam { get; set; }
    public bool option_windows_disable_sandbox { get; set; }
    public bool option_windows_steam_use_alternative_launcher { get; set; }
}

#endregion

public class ObjectProperty
{
    public string ObjName { get; set; }
    public KeyValuePair<string, string> Prop { get; set; }
    public ObjectProperty(string objName, KeyValuePair<string, string> prop)
    {
        ObjName = objName;
        Prop = prop;
    }
}

#region  GMTimeline Class

public class GMTimeline : GMResource
{
    public GMTimeline(string name)
    {
        this.name = name;
    }
    public List<GMMoment> momentList { get; set; } = new();

    public class GMMoment : GMResource
    {
        public uint moment { get; set; }
        public GMEvent evnt { get; set; }
    }
}

#endregion
#endregion

#region Main Variables
public var jsonOptions = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = true };

// assetName | groupName
public Dictionary<string, string> texGroupStuff = new();

public List<string> errorList = new();
public List<string> logList = new();

public ConcurrentBag<ImageAssetData> imagesToDump = new();
public Dictionary<string, List<string>> extensionGML = new();
// the folder of the data.win
public string rootDir = Path.GetDirectoryName(FilePath) + "\\";
// for macro stuff!
public string definitionDir = $"{Program.GetExecutableDirectory()}\\GameSpecificData\\Definitions\\";
public string macroDir = $"{Program.GetExecutableDirectory()}\\GameSpecificData\\Underanalyzer\\";
// the folder the project is exported to
public string scriptDir = $"{rootDir}Exported_Project\\";
// delete old decompiling attempt
if (Directory.Exists(scriptDir))
    Directory.Delete(scriptDir, true);

public string runnerFile = GetRunnerFile(rootDir);

// create Exported_Project folder
Directory.CreateDirectory(scriptDir);
public StreamWriter logFile = File.AppendText(scriptDir + "script.log");
// for the decompiler
GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

// progress bar
public int r_num = 0;

#endregion

#region Icon shit
public IMagickImage ToMagickImage(Bitmap bitmap)
{
    using (var memoryStream = new MemoryStream())
    {
        // save Bitmap to MemoryStream
        bitmap.Save(memoryStream, ImageFormat.Png);

        // reset memory stream position
        memoryStream.Seek(0, SeekOrigin.Begin);

        // create a MagickImage from MemoryStream
        IMagickImage magickImage = new MagickImage(memoryStream);

        return magickImage;
    }
}

// thanks https://stackoverflow.com/questions/17830853/how-can-i-load-a-program-icon-in-c-sharp
#region Extract Icon Functions
internal static class ExtractIcon
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Unicode)]
    [SuppressUnmanagedCodeSecurity]
    internal delegate bool ENUMRESNAMEPROC(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LockResource(IntPtr hResData);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [SuppressUnmanagedCodeSecurity]
    public static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, ENUMRESNAMEPROC lpEnumFunc, IntPtr lParam);


    private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    private readonly static IntPtr RT_ICON = (IntPtr)3;
    private readonly static IntPtr RT_GROUP_ICON = (IntPtr)14;

    public static Icon ExtractIconFromExecutable(string path)
    {
        IntPtr hModule = LoadLibraryEx(path, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);
        var tmpData = new List<byte[]>();

        ENUMRESNAMEPROC callback = (h, t, name, l) =>
        {
            var dir = GetDataFromResource(hModule, RT_GROUP_ICON, name);

            // Calculate the size of an entire .icon file.

            int count = BitConverter.ToUInt16(dir, 4);  // GRPICONDIR.idCount
            int len = 6 + 16 * count;                   // sizeof(ICONDIR) + sizeof(ICONDIRENTRY) * count
            for (int i = 0; i < count; ++i)
                len += BitConverter.ToInt32(dir, 6 + 14 * i + 8);   // GRPICONDIRENTRY.dwBytesInRes

            using (var dst = new BinaryWriter(new MemoryStream(len)))
            {
                // Copy GRPICONDIR to ICONDIR.

                dst.Write(dir, 0, 6);

                int picOffset = 6 + 16 * count; // sizeof(ICONDIR) + sizeof(ICONDIRENTRY) * count

                for (int i = 0; i < count; ++i)
                {
                    // Load the picture.

                    ushort id = BitConverter.ToUInt16(dir, 6 + 14 * i + 12);    // GRPICONDIRENTRY.nID
                    var pic = GetDataFromResource(hModule, RT_ICON, (IntPtr)id);

                    // Copy GRPICONDIRENTRY to ICONDIRENTRY.

                    dst.Seek(6 + 16 * i, 0);

                    dst.Write(dir, 6 + 14 * i, 8);  // First 8bytes are identical.
                    dst.Write(pic.Length);          // ICONDIRENTRY.dwBytesInRes
                    dst.Write(picOffset);           // ICONDIRENTRY.dwImageOffset

                    // Copy a picture.

                    dst.Seek(picOffset, 0);
                    dst.Write(pic, 0, pic.Length);

                    picOffset += pic.Length;
                }

                tmpData.Add(((MemoryStream)dst.BaseStream).ToArray());
            }
            return true;
        };
        EnumResourceNames(hModule, RT_GROUP_ICON, callback, IntPtr.Zero);
        byte[][] iconData = tmpData.ToArray();
        using (var ms = new MemoryStream(iconData[0]))
        {
            return new Icon(ms);
        }
    }
    private static byte[] GetDataFromResource(IntPtr hModule, IntPtr type, IntPtr name)
    {
        // Load the binary data from the specified resource.

        IntPtr hResInfo = FindResource(hModule, name, type);

        IntPtr hResData = LoadResource(hModule, hResInfo);

        IntPtr pResData = LockResource(hResData);

        uint size = SizeofResource(hModule, hResInfo);

        byte[] buf = new byte[size];
        Marshal.Copy(pResData, buf, 0, buf.Length);

        return buf;
    }
}
#endregion

// get icon
public IMagickImage mainoptionimg, winoptionimg;
if (!YYMPS)
{
    try
    {
        Icon ExeIcon = ExtractIcon.ExtractIconFromExecutable(runnerFile);

        // for main option
        mainoptionimg = ToMagickImage(ExeIcon.ToBitmap());
        // resize image
        var mainsize = new MagickGeometry(172, 172);
        mainsize.IgnoreAspectRatio = false; // maintain the aspect ratio
        mainoptionimg.FilterType = FilterType.Point; // stop interpolation
        mainoptionimg.Resize(mainsize);

        // for windows icon
        winoptionimg = ToMagickImage(ExeIcon.ToBitmap());
        // resize image
        var winsize = new MagickGeometry(256, 256);
        winsize.IgnoreAspectRatio = false; // maintain the aspect ratio
        winoptionimg.FilterType = FilterType.Point; // stop interpolation
        winoptionimg.Resize(winsize);
    }
    catch (Exception e)
    {
        mainoptionimg = null;
        winoptionimg = null;
    }
}
#endregion

#region Main UI (kinda shit, less now)

public bool DUMP, OBJT, ROOM, EXTN, SCPT, TMLN, SOND, SHDR, PATH, ACRV, SEQN, FONT, SPRT, BGND, LOG, YYMPS, ENUM, ADDFILES, FIXAUDIO, FIXTILE, GENROOM;
public bool CSTM_Enable = false;
public List<string> CSTM = new List<string>();
public int cpu_usage = 70;

#region Main Window stuffs
public class MainWindow : Window
{
    public bool DUMP, OBJT, ROOM, EXTN, SCPT, TMLN, SOND, SHDR, PATH, ACRV, SEQN, FONT, SPRT, BGND, LOG, YYMPS, ENUM, ADDFILES, FIXAUDIO, FIXTILE, GENROOM;
	public int cpu_usage = 70;
	public bool CSTM_Enable = false;
	public List<string> CSTM = new List<string>();
	
	private AssetPickerWindow pickerWindow;

	public MainWindow(UndertaleData _data, string scriptDir, bool isDark)
	{
		Title = "Ultimate_GMS2_Decompiler_v3";
		// remove OS title bar
		WindowStyle = WindowStyle.None;
		AllowsTransparency = false;
		ResizeMode = ResizeMode.NoResize;
		SizeToContent = SizeToContent.WidthAndHeight;
		WindowStartupLocation = WindowStartupLocation.CenterScreen;
		
		// Theme
		var lightgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
		var darkgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));
		var BGgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 23, 23));
        var BGwhite = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));

        var BasicWhite = System.Windows.Media.Brushes.White;
		var BasicBlack = System.Windows.Media.Brushes.Black;
		
		Background = isDark ? BGgrey : BGwhite;
		Foreground = isDark ? BasicWhite : BasicBlack;//text
		
		var mainPanel = new StackPanel { Margin = new Thickness(8) };
		var tooltip = new ToolTip();

        #region New Titlebar
        var titleBar = new DockPanel
		{
			Height = 30,
			Background = isDark ? darkgrey : lightgrey,
		};

		var titleText = new TextBlock
		{
			Text = Title,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(10, 0, 0, 0),
			Foreground = Foreground,
		};

		var closeButton = new Button
		{
			Content = "X",
			Width = 40,
			Height = 30,
			Background = System.Windows.Media.Brushes.Transparent,
			Foreground = Foreground,
			BorderBrush = System.Windows.Media.Brushes.Transparent,
			HorizontalAlignment = HorizontalAlignment.Right,
			Padding = new Thickness(0),
			FontWeight = FontWeights.Bold,
		};

		closeButton.Click += (s, e) => this.Close();

        // Enable window dragging via title bar
        titleBar.MouseLeftButtonDown += (s, e) =>
		{
			if (e.ButtonState == MouseButtonState.Pressed)
				DragMove();
		};

		titleBar.Children.Add(titleText);
		DockPanel.SetDock(closeButton, Dock.Right);
		titleBar.Children.Add(closeButton);
		mainPanel.Children.Insert(0, titleBar);
        #endregion

        // Back to sanity kinda
        mainPanel.Children.Add(new TextBlock
		{
			Text = "Original Script by crystallizedsparkle\n\tImproved by burnedpopcorn180",
			Margin = new Thickness(0, 20, 0, 8)
		});

		// Resources section
		mainPanel.Children.Add(new TextBlock
		{
			Text = "Select Resources to Dump:",
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0, 8, 0, 4)
		});

		var resourceGrid = new UniformGrid { Columns = 6 };
		var _PRJT = CreateCheckBox(isDark, "Project File", true, false);
		var _OBJT = CreateCheckBox(isDark, "Objects", true);
		var _ROOM = CreateCheckBox(isDark, "Rooms", true);
		var _EXTN = CreateCheckBox(isDark, "Extensions", true);
		var _SCPT = CreateCheckBox(isDark, "Scripts", true);
		var _TMLN = CreateCheckBox(isDark, "Timelines", true);
		var _SOND = CreateCheckBox(isDark, "Sounds", true);
		var _SHDR = CreateCheckBox(isDark, "Shaders", true);
		var _PATH = CreateCheckBox(isDark, "Paths", true);

		var _ACRV = CreateCheckBox(isDark, "Anim. Curves", true);
		if (_data.AnimationCurves == null)
		{
			_ACRV.IsEnabled = false;
			_ACRV.IsChecked = false;
			_ACRV.Content = "ACRV (2.3+)";
		}

		var _SEQN = CreateCheckBox(isDark, "Sequences", true);
		if (_data.Sequences == null)
		{
			_SEQN.IsEnabled = false;
			_SEQN.IsChecked = false;
			_SEQN.Content = "SEQN (2.3+)";
		}

		var _FONT = CreateCheckBox(isDark, "Fonts", true);
		var _SPRT = CreateCheckBox(isDark, "Sprites", true);
		var _BGND = CreateCheckBox(isDark, "Tilesets", true);

		resourceGrid.Children.Add(_PRJT);
		resourceGrid.Children.Add(_OBJT);
		resourceGrid.Children.Add(_ROOM);
		resourceGrid.Children.Add(_EXTN);
		resourceGrid.Children.Add(_SCPT);
		resourceGrid.Children.Add(_TMLN);
		resourceGrid.Children.Add(_SOND);
		resourceGrid.Children.Add(_SHDR);
		resourceGrid.Children.Add(_PATH);
		resourceGrid.Children.Add(_ACRV);
		resourceGrid.Children.Add(_SEQN);
		resourceGrid.Children.Add(_FONT);
		resourceGrid.Children.Add(_SPRT);
		resourceGrid.Children.Add(_BGND);

		mainPanel.Children.Add(resourceGrid);
		
		#region AssetPicker Shit
		var centerContainer = new Grid
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 10, 0, 10)
		};
		centerContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		centerContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		// CheckBox + Label
		var leftStack = new StackPanel
		{
			Orientation = Orientation.Vertical,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0, 0, 16, 0) // space between left and right
		};

		var _CSTM = new CheckBox
		{
			Content = "Pick Assets",
			IsChecked = false,
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 0, 0, 4),
			
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack
		};

		var CSTMLabel = new TextBlock
		{
			Text = "",
			Foreground = Foreground,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		leftStack.Children.Add(_CSTM);
		leftStack.Children.Add(CSTMLabel);
		Grid.SetColumn(leftStack, 0);
		centerContainer.Children.Add(leftStack);

		// Right side: Button
		var pickAssetsButton = new Button
		{
			Content = "Pick Individual Assets...",
			Width = 180,
			Height = 30,
			Margin = new Thickness(0),
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			IsEnabled = false,
			
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack,
			BorderBrush = isDark ? BGgrey : lightgrey
		};

		pickAssetsButton.Click += (s, e) =>
		{
			pickerWindow = new AssetPickerWindow(_data, isDark);
			pickerWindow.Owner = this;
			pickerWindow.ShowDialog();

			// Save
			CSTM = pickerWindow.CSTM.ToList();
			CSTMLabel.Text = $"Assets Selected: {CSTM.Count}";
		};

		Grid.SetColumn(pickAssetsButton, 1);
		centerContainer.Children.Add(pickAssetsButton);

		// Add to main layout
		mainPanel.Children.Add(centerContainer);

		// bulk
		var resourceCheckboxes = new List<CheckBox>
		{
			_OBJT, _ROOM, _EXTN, _SCPT, _TMLN, _SOND, _SHDR, _PATH,
			_ACRV, _SEQN, _FONT, _SPRT, _BGND
		};
		// For Pick Assets CHECKBOX
		_CSTM.Checked += (s, e) =>
		{
			// disable all resource checkboxes
			resourceCheckboxes.ForEach(cb => { cb.IsChecked = false; cb.IsEnabled = false; });

			// Enable Pick Assets button
			pickAssetsButton.IsEnabled = true;
			
			// because why not
			CSTMLabel.Text = $"Assets Selected: 0";
		};

		_CSTM.Unchecked += (s, e) =>
		{
			// enable all resource checkboxes
			resourceCheckboxes.ForEach(cb => cb.IsEnabled = true);

			// Disable the Pick Assets button
			pickAssetsButton.IsEnabled = false;

			// Handle specific checkboxes based on data availability
			_SEQN.IsEnabled = _data.Sequences != null;
			_SEQN.IsChecked = _data.Sequences != null;

			_ACRV.IsEnabled = _data.AnimationCurves != null;
			_ACRV.IsChecked = _data.AnimationCurves != null;

			// Check all the enabled checkboxes
			resourceCheckboxes.Where(cb => cb.IsEnabled).ToList().ForEach(cb => cb.IsChecked = true);
			
			// WIPE
			CSTMLabel.Text = "";
			CSTM.Clear();
		};
        #endregion

        // Settings section
        mainPanel.Children.Add(new TextBlock
		{
			Text = "Decompiler Settings",
			FontWeight = FontWeights.Bold,
			Margin = new Thickness(0, 12, 0, 4)
		});

		var settingsGrid = new UniformGrid { Columns = 4 };

		var _LOG = CreateCheckBox(isDark, "Log Assets");
		_LOG.ToolTip = "Logs every Asset that gets decompiled\nMostly for Debugging, will clog up the logs if enabled.";

		var _YYMPS = CreateCheckBox(isDark, "Export as YYMPS");
		_YYMPS.ToolTip = "Exports decompiled resources\nas a GameMaker Importable Package.";

		var _ENUM = CreateCheckBox(isDark, "Bitwise Enums");
		_ENUM.ToolTip = "Turns Unknown Enums into Bitwise Operations.\n\nExample:\nUnknownEnum.Value_1 -> (1 << 0)";

		var _ADDFILES = CreateCheckBox(isDark, "Add Datafiles", true);
		_ADDFILES.ToolTip = "Attempts to automatically add included datafiles\nMight be inaccurate and might miss some files";

		var _FIXA = CreateCheckBox(isDark, "Fix Audio");
		_FIXA.ToolTip = "If a .wav file is labelled a .mp3, this setting will label it back to .wav.";

		var _FIXT = CreateCheckBox(isDark, "Fix Tilesets");
		_FIXT.ToolTip = "Fixes Tileset Separation, slower due to image processing.";

		var _GENROOM = CreateCheckBox(isDark, "Generate Room Name");
		_GENROOM.ToolTip = "Simulates GameMaker asset naming behavior.";

		settingsGrid.Children.Add(_LOG);
		settingsGrid.Children.Add(_YYMPS);
		settingsGrid.Children.Add(_ENUM);
		settingsGrid.Children.Add(_ADDFILES);
		settingsGrid.Children.Add(_FIXA);
		settingsGrid.Children.Add(_FIXT);
		settingsGrid.Children.Add(_GENROOM);

		mainPanel.Children.Add(settingsGrid);

        #region CPU Controls
        var cpuLabel = new Label
		{
			Content = $"CPU Usage: {cpu_usage}%",
			HorizontalAlignment = HorizontalAlignment.Center,
			Margin = new Thickness(0, 10, 0, 0)
		};

		var cpuSlider = new Slider
		{
			Minimum = 1,
			Maximum = 100,
			Value = cpu_usage,
			Width = 200,
			Margin = new Thickness(0, 0, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Center,
			
			Foreground = isDark ? BasicWhite : BasicBlack
		};

		cpuSlider.ValueChanged += (s, e) =>
		{
			cpu_usage = (int)cpuSlider.Value;
			cpuLabel.Content = $"CPU Usage: {cpu_usage}%";
		};

		var cpuPanel = new StackPanel
		{
			Orientation = Orientation.Vertical,
			HorizontalAlignment = HorizontalAlignment.Center
		};

		cpuPanel.Children.Add(cpuLabel);
		cpuPanel.Children.Add(cpuSlider);
		mainPanel.Children.Add(cpuPanel);
        #endregion

        // OK Button
        var OKBT = new Button
		{
			Content = Directory.Exists($"{scriptDir}") ? "Overwrite Dump" : "Start Dump",
			Height = 48,
			Margin = new Thickness(0, 10, 0, 0),
			
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack,
			BorderBrush = isDark ? BGgrey : lightgrey
		};
		OKBT.Click += (o, s) =>
		{
			DUMP = true;

			OBJT = _OBJT.IsChecked == true;
			ROOM = _ROOM.IsChecked == true;
			EXTN = _EXTN.IsChecked == true;
			SCPT = _SCPT.IsChecked == true;
			TMLN = _TMLN.IsChecked == true;
			SOND = _SOND.IsChecked == true;
			SHDR = _SHDR.IsChecked == true;
			PATH = _PATH.IsChecked == true;
			ACRV = _ACRV.IsChecked == true;
			SEQN = _SEQN.IsChecked == true;
			FONT = _FONT.IsChecked == true;
			SPRT = _SPRT.IsChecked == true;
			BGND = _BGND.IsChecked == true;

			LOG = _LOG.IsChecked == true;
			YYMPS = _YYMPS.IsChecked == true;
			ENUM = _ENUM.IsChecked == true;
			ADDFILES = _ADDFILES.IsChecked == true;
			FIXAUDIO = _FIXA.IsChecked == true;
			FIXTILE = _FIXT.IsChecked == true;
			GENROOM = _GENROOM.IsChecked == true;
			
			CSTM_Enable = _CSTM.IsChecked == true;

			Close();
		};

		mainPanel.Children.Add(OKBT);

		//no scroll bar
		Content = mainPanel;
	}

	private CheckBox CreateCheckBox(bool isDark, string content, bool isChecked = false, bool? enabled = true)
	{
		var lightgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
		var darkgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));
		var BasicWhite = System.Windows.Media.Brushes.White;
		var BasicBlack = System.Windows.Media.Brushes.Black;
		
		return new CheckBox
		{
			Content = content,
			IsChecked = isChecked,
			IsEnabled = enabled ?? true,
			Margin = new Thickness(4),
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack
		};
	}
}
#endregion
#region Asset Picker stuffs
public class AssetPickerWindow : Window
{
    private TreeView treeView;
    private ListBox listBox;
    private TextBox searchTreeBox, searchListBox;
	public List<string> CSTM = new List<string>();

    public AssetPickerWindow(UndertaleData Data, bool isDark)
    {
        Title = "Asset Picker";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

		// Themes
        var lightgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
        var darkgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 45, 48));
		var BGgrey = new SolidColorBrush(System.Windows.Media.Color.FromRgb(23, 23, 23));
        var BGwhite = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 230, 230));

        var BasicWhite = System.Windows.Media.Brushes.White;
		var BasicBlack = System.Windows.Media.Brushes.Black;

        Background = isDark ? BGgrey : BGwhite;
        Foreground = isDark ? BasicWhite : BasicBlack;

        #region title bar
        var titleBar = new DockPanel
        {
            Height = 30,
			LastChildFill = true,
            Background = isDark ? darkgrey : lightgrey
        };

        var titleText = new TextBlock
        {
            Text = Title,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            Foreground = Foreground,
        };

        var closeButton = new Button
        {
            Content = "X",
            Width = 40,
            Height = 30,
            Background = System.Windows.Media.Brushes.Transparent,
            Foreground = Foreground,
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            FontWeight = FontWeights.Bold,
			HorizontalAlignment = HorizontalAlignment.Right
        };
        closeButton.Click += (s, e) => Close();
        titleBar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

        titleBar.Children.Add(titleText);
        DockPanel.SetDock(closeButton, Dock.Right);
        titleBar.Children.Add(closeButton);

        var layout = new StackPanel();
        layout.Children.Add(titleBar);
        #endregion

        var grid = new Grid
        {
            Margin = new Thickness(10)
        };

        for (int i = 0; i < 3; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        for (int i = 0; i < 3; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Search box for tree
        searchTreeBox = new TextBox { Width = 200, Margin = new Thickness(0, 0, 8, 4) };
        searchTreeBox.TextChanged += (s, e) => UpdateTree(searchTreeBox.Text, Data);
        Grid.SetRow(searchTreeBox, 0);
        Grid.SetColumn(searchTreeBox, 0);
        grid.Children.Add(searchTreeBox);

        // TreeView
        treeView = new TreeView { Height = 400, Width = 200 };
        treeView.MouseDoubleClick += (s, e) =>
        {
            if (treeView.SelectedItem is TreeViewItem item && item.Parent is TreeViewItem)
            {
                if (!CSTM.Contains(item.Header.ToString()))
                    CSTM.Add(item.Header.ToString());
                UpdateList(searchListBox.Text);
            }
        };
        Grid.SetRow(treeView, 1);
        Grid.SetColumn(treeView, 0);
        grid.Children.Add(treeView);

        #region Arrow Buttons
        var buttonPanel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(8, 0, 8, 0) };

        var addButton = new Button 
		{ 
			Content = "->", 
			Width = 40, 
			Height = 32, 
			Margin = new Thickness(0, 4, 0, 4),
			
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack,
			BorderBrush = isDark ? BGgrey : lightgrey
		};
        addButton.Click += (s, e) =>
        {
            if (treeView.SelectedItem is TreeViewItem item && item.Parent is TreeViewItem)
            {
                if (!CSTM.Contains(item.Header.ToString()))
                    CSTM.Add(item.Header.ToString());
                UpdateList(searchListBox.Text);
            }
        };
        buttonPanel.Children.Add(addButton);

        var removeButton = new Button 
		{ 
			Content = "<-", 
			Width = 40, 
			Height = 32,
			
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack,
			BorderBrush = isDark ? BGgrey : lightgrey
		};
        removeButton.Click += (s, e) =>
        {
            if (listBox.SelectedItem != null)
            {
                CSTM.RemoveAll(r => r == listBox.SelectedItem.ToString());
                UpdateList(searchListBox.Text);
            }
        };
        buttonPanel.Children.Add(removeButton);

        Grid.SetRow(buttonPanel, 1);
        Grid.SetColumn(buttonPanel, 1);
        grid.Children.Add(buttonPanel);
        #endregion

        // Search box for list
        searchListBox = new TextBox { Width = 200, Margin = new Thickness(0, 0, 0, 4) };
        searchListBox.TextChanged += (s, e) => UpdateList(searchListBox.Text);
        Grid.SetRow(searchListBox, 0);
        Grid.SetColumn(searchListBox, 2);
        grid.Children.Add(searchListBox);

        // ListBox
        listBox = new ListBox 
		{ 
			Width = 200, 
			Height = 400,
			
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack
		};
        listBox.MouseDoubleClick += (s, e) =>
        {
            if (listBox.SelectedItem != null)
            {
                CSTM.RemoveAll(r => r == listBox.SelectedItem.ToString());
                UpdateList(searchListBox.Text);
            }
        };
        Grid.SetRow(listBox, 1);
        Grid.SetColumn(listBox, 2);
        grid.Children.Add(listBox);

        // OK Button
        var okButton = new Button
        {
            Content = "OK",
            Width = 100,
            Height = 32,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
			
			Background = isDark ? darkgrey : lightgrey,
			Foreground = isDark ? BasicWhite : BasicBlack,
			BorderBrush = isDark ? BGgrey : lightgrey
        };
        okButton.Click += (s, e) => Close();
		
        Grid.SetRow(okButton, 2);
        Grid.SetColumnSpan(okButton, 3);
        grid.Children.Add(okButton);

        layout.Children.Add(grid);
        Content = layout;

        BuildInitialTree(Data);
        UpdateList("");
    }

    private void BuildInitialTree(UndertaleData Data)
    {
        treeView.Items.Clear();
        var root = new TreeViewItem { Header = "Data", IsExpanded = true };

        var categories = new[]
        {
            "Sounds", "Sprites", "Tilesets", "Paths", "Scripts", "Shaders", "Fonts",
            "Timelines", "Game objects", "Rooms", "Extensions"
        };
        foreach (var c in categories)
            root.Items.Add(new TreeViewItem { Header = c });

        if (Data.Sequences != null)
            root.Items.Add(new TreeViewItem { Header = "Sequences" });
        if (Data.AnimationCurves != null)
            root.Items.Add(new TreeViewItem { Header = "Curves" });

        treeView.Items.Add(root);
        UpdateTree("", Data);
    }

    private void UpdateTree(string search, UndertaleData Data)
    {
        foreach (TreeViewItem cat in ((TreeViewItem)treeView.Items[0]).Items)
        {
            cat.Items.Clear();
            var name = cat.Header.ToString();

            IEnumerable<string> items = name switch
            {
                "Sounds" => Data.Sounds.Select(s => s.Name.Content),
                "Sprites" => Data.Sprites.Select(s => s.Name.Content),
                "Tilesets" => Data.Backgrounds.Select(s => s.Name.Content),
                "Paths" => Data.Paths.Select(s => s.Name.Content),
                /*"Scripts" => Data.Scripts.Select(s => s.Name.Content),*/
                // filters out non-scripts
                "Scripts" => Data.Code.Select(s => s.Name.Content)
                .Where(n => n.StartsWith("gml_GlobalScript_"))
                .Select(n => n.Substring("gml_GlobalScript_".Length)),

                "Shaders" => Data.Shaders.Select(s => s.Name.Content),
                "Fonts" => Data.Fonts.Select(s => s.Name.Content),
                "Timelines" => Data.Timelines.Select(s => s.Name.Content),
                "Game objects" => Data.GameObjects.Select(s => s.Name.Content),
                "Rooms" => Data.Rooms.Select(s => s.Name.Content),
                "Extensions" => Data.Extensions.Select(s => s.Name.Content),
                "Sequences" => Data.Sequences?.Select(s => s.Name.Content) ?? Enumerable.Empty<string>(),
                "Curves" => Data.AnimationCurves?.Select(s => s.Name.Content) ?? Enumerable.Empty<string>(),
                _ => Enumerable.Empty<string>()
            };

            foreach (var item in items)
            {
                if (item.Contains(search, StringComparison.OrdinalIgnoreCase))
                    cat.Items.Add(new TreeViewItem { Header = item });
            }
        }
    }

    private void UpdateList(string search)
    {
        listBox.Items.Clear();
        foreach (var item in CSTM)
        {
            if (item.Contains(search, StringComparison.OrdinalIgnoreCase))
                listBox.Items.Add(item);
        }
    }
}
#endregion

// thank god this works
// the other solution was such a hack
bool isDarkEnabled = SettingsWindow.EnableDarkMode;

// open main window
var window = new MainWindow(Data, scriptDir, isDarkEnabled);
window.ShowDialog();

#region save values
DUMP = window.DUMP;

OBJT = window.OBJT;
ROOM = window.ROOM;
EXTN = window.EXTN;
SCPT = window.SCPT;
TMLN = window.TMLN;
SOND = window.SOND;
SHDR = window.SHDR;
PATH = window.PATH;
ACRV = window.ACRV;
SEQN = window.SEQN;
FONT = window.FONT;
SPRT = window.SPRT;
BGND = window.BGND;

LOG = window.LOG;
YYMPS = window.YYMPS;
ENUM = window.ENUM;
ADDFILES = window.ADDFILES;
FIXAUDIO = window.FIXAUDIO;
FIXTILE = window.FIXTILE;
GENROOM = window.GENROOM;

cpu_usage = window.cpu_usage;

CSTM_Enable = window.CSTM_Enable;
CSTM = window.CSTM;
#endregion

// if exit
if (!DUMP)
    return;

#endregion

#region Datafile Copier
async Task CopyDataFiles()
{
    if (ADDFILES)
    {
        // just in case
        Directory.CreateDirectory(scriptDir + "datafiles\\");

        // Get all files and subdirectories
        foreach (var file in Directory.GetFiles(rootDir, "*", SearchOption.AllDirectories))
        {
            // Skip these files                                                        //also get rid of sounds because yeah
            if (new[] { ".dll", ".exe", ".ini", ".win", ".unx", ".droid", ".ios", ".dat", ".mp3", ".ogg", ".wav" }.Contains(Path.GetExtension(file).ToLower()))
                continue;

            string relativePath = Path.GetRelativePath(rootDir, file);
            string destinationFile = Path.Combine(scriptDir + "datafiles\\", relativePath);

            // Skip "Exported_Project" or "Export_YYMPS" directories and files within them
            string dirName = Path.GetDirectoryName(file);
            if (!dirName.Contains("Exported_Project") && !dirName.Contains("Export_YYMPS"))
            {
                // Ensure it exists
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

                // Copy the file
                File.Copy(file, destinationFile, true);

                // add to yyp
                int folderpos = destinationFile.IndexOf("datafiles");
                string trimmedfolder = destinationFile.Substring(folderpos);
                finalExport.IncludedFiles.Add(new GMProject.GMIncludedFile(Path.GetFileName(destinationFile))
                {
                    filePath = Path.GetDirectoryName(trimmedfolder).Replace("\\", "/")
                });
            }
        }
    }
}
#endregion

#region Useful Tools

// this dictionary holds all the names of the assets
public static readonly Dictionary<GMAssetType, string> assetTypes = new Dictionary<GMAssetType, string>
{
{ GMAssetType.None, "" },
{ GMAssetType.Room, "Room" },
{ GMAssetType.Sprite, "Sprite" },
{ GMAssetType.Object, "Object" },
{ GMAssetType.Script, "Script" },
{ GMAssetType.Sound, "Sound" },
{ GMAssetType.AudioGroup, "AudioGroup" },
{ GMAssetType.TileSet, "Tile Set" },
{ GMAssetType.Note, "Note" },
{ GMAssetType.TextureGroup, "TextureGroup" },
{ GMAssetType.Font, "Font" },
{ GMAssetType.Sequence, "Sequence" },
{ GMAssetType.Shader, "Shader" },
{ GMAssetType.Extension, "Extension" },
{ GMAssetType.Path, "Path" },
{ GMAssetType.AnimationCurve, "Animation Curve" },
{ GMAssetType.Timeline, "Timeline" },
};

string IdToHex(uint id)
{
    if (!GENROOM)
        return id.ToString();

    // gamemaker IDE does it kinda like this
    Random rand = new((int)id);
    return rand.Next().ToString("X");
}

/// <summary>
/// returns a random colour
/// </summary>
/// <returns>uint</returns>
uint GetRandomColour()
{
    // this turns the color enum into array and use the random class to choose a random one
    Array values = Enum.GetValues(typeof(eColour));
    Random random = new Random();
    return (uint)values.GetValue(random.Next(values.Length));
}
/// <summary>
/// Returns the path of the runner.
/// </summary>
/// <param name="fileDir"></param>
/// <returns>file path</returns>
string GetRunnerFile(string fileDir)
{
    // get all exe files in the directory
    string[] files = Directory.GetFiles(fileDir, "*exe");
    // loop through each file and check.
    foreach (string file in files)
    {
        string lastLine = File.ReadAllLines(file).Last();
        // these always appear in the last line of the actual gamemaker runner.
        if (lastLine.Contains($"name=\"YoYoGames.GameMaker.Runner\"") || lastLine.Contains("GameMaker C++ Core Runner."))
            return file;
    }
    bool doSearch = ScriptQuestion("The runner was not found! Would you like to point me to it please?");

    while (doSearch)
    {
		// damn, new ui made me do it
        System.Windows.Forms.OpenFileDialog fileDialog = new()
        {
            Title = "Take me to your game executable.......",
            InitialDirectory = rootDir,
            Filter = "Executable Files (*.exe)|*.exe",
        };
        if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            string lastLine = File.ReadAllLines(fileDialog.FileName).Last();
            // these always appear in the last line of the actual gamemaker runner.
            if (lastLine.Contains($"name=\"YoYoGames.GameMaker.Runner\"") || lastLine.Contains("GameMaker C++ Core Runner."))
                return fileDialog.FileName;
            else
                doSearch = ScriptQuestion("Thats not the runner! The runner is the game executable that loads the data.win file! Would you like to try again?");
        }
    }
    return String.Empty;
}

public Mutex mut = new Mutex();
/// <summary>
/// adds a string to the log file.
/// </summary>
/// <param name="message"></param>
public void PushToLog(string message)
{
    mut.WaitOne();
    logFile.Flush();
    logFile.WriteLine($"{message}");
    mut.ReleaseMutex();
}
/// <summary>
/// creates a new <c>GMProject.Resource</c> inside of the exported project
/// </summary>
/// <param name="assetType"></param>
/// <param name="assetName"></param>
/// <param name="assetOrder"></param>
public void CreateProjectResource(GMAssetType assetType, string assetName, int assetOrder)
{
    finalExport.resources.Enqueue(new GMProject.Resource()
    {
        id = new AssetReference(assetName, assetType),
        order = assetOrder,
        type = assetType
    });
}
/// <summary>
/// creates a file path.
/// e.g: $"{assetname}s/{assetname}/{assetname}.yy"
/// </summary>
/// <param name="assetName"></param>
/// <param name="type"></param>
public static string CreateFilePath(string assetName, GMAssetType type)
{
    string asset = assetTypes[type].ToLower();
    // dumb switch statement
    switch (asset)
    {
        case "animation curve":
            asset = "animcurve";
            break;
        case "tile set":
            asset = "tileset";
            break;
    }
    return $"{asset}s/{assetName}/{assetName}.yy";
}
/// <summary>
/// Creates a new GMNote in the project.
/// </summary>
/// <param name="noteName"></param>
/// <param name="folderName"></param>
/// <param name="noteText"></param>
public void CreateNote(string noteName = "Note1", string folderName = "Notes", string noteText = "")
{
    string assetDir = $"{scriptDir}notes\\{noteName}\\";

    GMNotes note = new(noteName);
    note.parent.name = folderName;
    note.parent.path = $"folders/{folderName}.yy";

    CreateProjectResource(GMAssetType.Note, noteName, noteIndex++);
    Directory.CreateDirectory(assetDir);
    File.WriteAllText($"{assetDir}\\{noteName}.yy", JsonSerializer.Serialize(note, jsonOptions));
    File.WriteAllText($"{assetDir}\\{noteName}.txt", noteText);
}
/// <summary>
/// Trims a certain part of the shader code to remove interal yyg stuff.
/// </summary>
/// <param name="input"></param>
/// <returns>trimmedShader</returns>
public static string TrimShader(this string input)
{
    var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .SkipWhile(line => !line.Contains("#define _YY_GLSL"))
                    .Skip(1); // skip matching line

    return String.Join("\n", lines);
}

// heavily referenced from quantum
/// <summary>
/// translates pre-create code into object properties.
/// </summary>
/// <param name="eventList"></param>
public List<GMObjectProperty> CreateObjectProperties(UndertalePointerList<UndertaleGameObject.Event> eventList)
{
    //Still regex BULLSHIT
    Regex assignmentRegex = new Regex(
        @"^(?:self\.)?(\w+)\s*=\s*(.+?);?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ECMAScript
    );

    List<GMObjectProperty> propList = new();
    if (eventList is null)
        return propList;

    foreach (UndertaleGameObject.Event e in eventList)
    {
        foreach (UndertaleGameObject.EventAction action in e.Actions)
        {
            UndertaleCode code = action.CodeId;
            if (code is null)
                continue;

            string dumpedCode = DumpCode(code, new DecompileSettings
            {
                UseSemicolon = false,
                AllowLeftoverDataOnStack = true
            });

            foreach (Match match in assignmentRegex.Matches(dumpedCode))
            {
                string name = match.Groups[1].Value;
                string rawValue = match.Groups[2].Value.Trim();

                GMObjectProperty prop = new(name)
                {
                    name = name,
                    value = rawValue
                };

                // String
                if (rawValue.StartsWith("\"") && rawValue.EndsWith("\""))
                {
                    prop.varType = 2;
                }
                // Boolean
                else if (rawValue == "true" || rawValue == "false")
                {
                    prop.varType = 3;
                }
                // Decimal
                else if (Regex.IsMatch(rawValue, @"^\d+\.\d+$"))
                {
                    prop.varType = 0;
                }
                // Integer or Color
                else if (Regex.IsMatch(rawValue, @"^\d+$"))
                {
                    if (UInt32.TryParse(rawValue, out uint colorVal) && colorVal >= 0xFF000000 && colorVal <= 0xFFFFFFFF)
                    {
                        prop.varType = 7; // Color
                        prop.value = $"${colorVal:X8}";
                    }
                    else
                    {
                        prop.varType = 1; // Integer
                    }
                }
                // Expression
                else if (Regex.IsMatch(rawValue, @"[=!<>+\-*/%&|()]"))
                {
                    prop.varType = 4;
                }
                /*
                // Asset (Todo due it's really sucks)
                else if (!rawValue.Contains("\"") && !Regex.IsMatch(rawValue, @"\W") && !char.IsDigit(rawValue[0]))
                {
                    prop.varType = 5;
                    prop.resource = new()
                    {
                        name = rawValue,
                        path = $"scripts/{rawValue}/{rawValue}.yy"
                    };
                }
                */
                else
                {
                    prop.varType = 4; // Fallback to Expression
                }

                propList.Add(prop);
            }
        }
    }

    return propList;
}


/// <summary>
/// returns a folder directory for an asset inside of an <c>AssetReference</c>. e.g: $"folders/{foldername}.yy"
/// </summary>
/// <param name="type"></param>
public static AssetReference GetParentFolder(GMAssetType type)
{
    string assetName = assetTypes[type] + "s";
    return new AssetReference(assetName, GMAssetType.None)
    {
        name = assetName,
        path = $"folders/{assetName}.yy"
    };
}
/// <summary>
/// e.g: $"folders/{folderPath}{folderName}.yy"
/// </summary>
/// <param name="folderName"></param>
public static AssetReference GetFolderReference(string folderName, string folderPath = "")
{
    return new AssetReference()
    {
        name = folderName,
        path = $"folders/{folderPath}{folderName}.yy"
    };
}

/// <summary>
/// returns an <c>AssetReference</c> based on the name of the texture group.
/// </summary>
/// <param name="name"></param>
public AssetReference GetTextureGroup(string name)
{
    string texGroup = "default";
    if (texGroupStuff.ContainsKey(name))
        texGroup = texGroupStuff[name];

    return new AssetReference()
    {
        name = texGroup,
        path = $"texturegroups/{texGroup}"
    };
}
/// <summary>
/// makes all variable declarations on one line.
/// </summary>
/// <param name="s"></param>
public static string FixVariableDeclarations(this string s)
{
    /*
    This is a little algorithm to make sure all variable declarations are on one line.
    First we check for what I call an "anchor line", basically an anchor line is
    a line with "=" in it. which we will start a new line on.
    everything that isnt an anchor line will be taken and put into the same line as the anchor.
    */

    StringBuilder result = new StringBuilder();
    string[] lines = s.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    bool isAnchorLine = false;

    foreach (string line in lines)
    {
        if (line.Contains("="))
        {
            // make sure we dont add a new line at the start
            if (result.Length > 0)
            {
                result.AppendLine();
            }
            result.Append(line);
            isAnchorLine = true;
        }
        else if (isAnchorLine)
        {
            // if its an anchor line just append
            result.Append(line);
        }
        else
        {
            if (result.Length > 0)
            {
                result.AppendLine();
            }
            result.Append(line);
        }
    }
    return result.ToString();
}

/// <summary>
/// turns properties of an object into a <c>Dictionary</c>
/// </summary>
/// <param name="codeInput"></param>
public static Dictionary<string, string> ObjectPropertiesToDictionary(this string codeInput)
{
    Dictionary<string, string> objectProperties = new();

    // this assumes that you already ran FixVariableDeclarations
    string[] lines = codeInput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToArray();

    foreach (string line in lines)
    {
        string[] kvp = line.Split("=").ToArray();
        // if the property exists, add it.
        if (!objectProperties.ContainsKey(kvp[0].Trim()))
            objectProperties.Add(kvp[0].Trim(), kvp[1].Trim());
    }

    return objectProperties;
}
/// <summary>
/// decompiles any <c>UndertaleCode</c>
/// </summary>
/// <param name="code"></param>
/// <param name="set"></param>
/// <returns>code</returns>
string? DumpCode(UndertaleCode code, IDecompileSettings? set = null)
{
    if (code is not null)
    {
        try
        {
            DecompileContext context = new DecompileContext(globalDecompileContext, code, (set is not null ? set : decompilerSettings));
            string dumpedCode = context.DecompileToString();

            // enum replacement.
            if (ENUM)
            {
                // crystal didn't account for this, so i'll do it i guess
                var unknownName = SettingsWindow.DecompilerSettings.UnknownEnumName;
                var unknownVal = SettingsWindow.DecompilerSettings.UnknownEnumValuePattern;

                //@"UnknownEnum\.Value_(m?)(\d+)"
                dumpedCode = Regex.Replace(dumpedCode, $@"{unknownName}\.{unknownVal.Replace("{0}", "")}(m?)(\d+)", match =>
                {
                    string sign = match.Groups[1].Value == "m" ? "-" : "";
                    string number = match.Groups[2].Value;
                    return $"({sign}{number} << 0)";
                });

                // remove "gml_Script_" from things
                dumpedCode = Regex.Replace(dumpedCode, "gml_Script_", "");
            }
            // generated room stuff idk
            if (GENROOM)
            {
                // if it has "graphic_" or "inst_"
                dumpedCode = Regex.Replace(dumpedCode, @"(graphic_|inst_)(\d+)", match =>
                {
                    string prefix = match.Groups[1].Value;
                    uint number = uint.Parse(match.Groups[2].Value);
                    string hexValue = IdToHex(number); // convert number
                    return $"{prefix}{hexValue}";
                });
            }
            foreach (IDecompileWarning error in context.Warnings)
                errorList.Add($"{error.CodeEntryName} | {error.Message}");

            // logspam the line
            if (LOG)
                PushToLog($"'{code.Name.Content}' successfully decompiled.");

            return dumpedCode;
        }
        catch (Exception e)
        {
            errorList.Add($"{code.Name.Content} | Failed to decompile.");
        }
    }
    return null;
}

/// <summary>
/// obtains the <c>tags</c> variable from any asset
/// </summary>
/// <param name="asset"></param>
string[]? GetTags(dynamic asset)
{
    // that one obscure gamemaker feature that nobody uses
    if (Data.Tags is null) return null;

    // get the id
    var assetTagId = UndertaleTags.GetAssetTagID(Data, asset);
    string[] obtainedTags = null;

    if (Data.Tags.AssetTags.ContainsKey(assetTagId))
    {
        // cast it into an enumerable for use.
        var tagList = (IEnumerable<UndertaleString>)Data.Tags.AssetTags[assetTagId];
        // add all the tags to the list.
        obtainedTags = tagList.Select(t => t.Content).ToArray();
    }

    return obtainedTags;
}

string GetTexturePageSize()
{
    int[] sizes = new int[6];
    int[] types = [256, 512, 1024, 2048, 4096, 8192];
    Dictionary<string, int> appearances = new();
    if (Data.EmbeddedTextures.Count == 0)
        return "2048x2048";

    foreach (UndertaleEmbeddedTexture page in Data.EmbeddedTextures)
    {
        for (int i = 0; i < sizes.Length; i++)
        {
            if (page.TextureData.Width == types[i] && page.TextureData.Height == types[i])
            {
                string sizeStr = $"{types[i].ToString()}x{types[i].ToString()}";
                if (appearances.ContainsKey(sizeStr))
                    appearances[sizeStr]++;
                else
                    appearances[sizeStr] = 1;
            }
        }
    }

    if (appearances.Count == 0)
        return "2048x2048";

    KeyValuePair<string, int> mostAppeared = appearances.Aggregate((l, r) => l.Value > r.Value ? l : r);
    return mostAppeared.Key;
}

#endregion

#region Main Resource Dumpers

void DumpScript(UndertaleScript s, int index)
{
    string scriptName = s.Name.Content;
    string assetDir = $"{scriptDir}scripts\\{scriptName}\\";

    // that one script
    if (scriptName == "_effect_windblown_particles_script" || scriptName == "_effect_blend_script")
        return;

    string? dumpedCode = DumpCode(s.Code);
    dumpedCode = (dumpedCode is null ? "" : dumpedCode);

    // fallback to empty function for when the script is empty just in case if its called.
    if (dumpedCode == String.Empty)
        dumpedCode = "function " + scriptName + "()\n{\n\n}";

    Directory.CreateDirectory(assetDir);
    GMScript dumpedScript = new(scriptName)
    {
        isCompatibility = !Data.IsVersionAtLeast(2, 3),
        tags = GetTags(s)
    };
    File.WriteAllText($"{assetDir}{scriptName}.yy", JsonSerializer.Serialize(dumpedScript, jsonOptions));
    File.WriteAllText($"{assetDir}{scriptName}.gml", dumpedCode);

    CreateProjectResource(GMAssetType.Script, scriptName, index);

    IncrementProgressParallel();
}

async Task DumpScripts()
{
    var watch = Stopwatch.StartNew();
    if (SCPT || CSTM_Enable)
    {
        PushToLog("Dumping Scripts...");
        await Task.Run(() => Parallel.ForEach(scriptsToDump, parallelOptions, (scr, state, index) =>
        {
            if (scr is null)
            {
                r_num++;
                return;
            }
            if (SCPT || (CSTM_Enable && CSTM.Contains(scr.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Script: {scr.Name.Content}", r_num++, toDump);

                if (LOG)
                    PushToLog($"Dumping script '{scr.Name.Content}'...");
                DumpScript(scr, (int)index);
                if (LOG)
                    PushToLog($"Script '{scr.Name.Content}' successfully dumped.");
            }
        }));
    }

    watch.Stop();
    PushToLog($"Scripts complete! Took {watch.ElapsedMilliseconds} ms");
}

void DumpObject(UndertaleGameObject o, int index)
{
    string objectName = o.Name.Content;
    string assetDir = $"{scriptDir}objects\\{objectName}\\";

    Directory.CreateDirectory(assetDir);

    GMObject dumpedObject = new(objectName)
    {
        spriteId = (o.Sprite is null ? null : new AssetReference(o.Sprite.Name.Content, GMAssetType.Sprite)),
        solid = o.Solid,
        visible = o.Visible,
        managed = o.Managed,
        spriteMaskId = (o.TextureMaskId is null ? null : new AssetReference(o.TextureMaskId.Name.Content, GMAssetType.Sprite)),
        persistent = o.Persistent,
        parentObjectId = (o.ParentId is null ? null : new AssetReference(o.ParentId.Name.Content, GMAssetType.Object)),
        // physics stuff
        physicsObject = o.UsesPhysics,
        physicsSensor = o.IsSensor,
        physicsShape = (int)o.CollisionShape,
        physicsGroup = (int)o.Group,
        physicsDensity = o.Density,
        physicsRestitution = o.Restitution,
        physicsLinearDamping = o.LinearDamping,
        physicsAngularDamping = o.AngularDamping,
        physicsFriction = o.Friction,
        physicsStartAwake = o.Awake,
        physicsKinematic = o.Kinematic,
        physicsShapePoints = o.PhysicsVertices.Select(p => new GMPoint(p.X, p.Y)).ToArray(),
        tags = GetTags(o)
    };
    // events (also referenced from quantum)
    for (int i = 0; i < o.Events.Count; i++)
    {
        var eventList = o.Events[i];
        var parentEventList = o.ParentId?.Events[i];



        if (i == (int)EventType.PreCreate)
        {
            // get current properties
            List<GMObjectProperty> props = CreateObjectProperties(eventList);
            List<GMObjectProperty> parentProps = CreateObjectProperties(parentEventList);

            // these are added to the object later.
            List<GMOverriddenProperty> overriddenprops = new();
            List<GMObjectProperty> finalprops = new();

            // Iterate through the properties of the current object
            for (int l = 0; l < props.Count; l++)
            {
                GMObjectProperty prop = props[l];

                // if the object has no parent, add all properties
                if (o.ParentId is null)
                {
                    finalprops.Add(prop);
                }
                else // Object has a parent
                {

                    bool isOverridden = false;
                    UndertaleGameObject parObject = o;

                    while (parObject.ParentId is not null)
                    {
                        parObject = parObject.ParentId;
                        List<GMObjectProperty> currentParentProps = CreateObjectProperties(parObject.Events[(int)EventType.PreCreate]);

                        // Check if the parent has a property with the same name
                        GMObjectProperty currentParentProp = currentParentProps.FirstOrDefault(p => p.name == prop.name);

                        if (currentParentProp is not null)
                        {
                            // If the parent has the property and the value is different, it's overridden
                            if (prop.value != currentParentProp.value)
                            {
                                overriddenprops.Add(new GMOverriddenProperty()
                                {
                                    value = prop.value,
                                    propertyId = new AssetReference(parObject.Name.Content, GMAssetType.Object)
                                    {
                                        name = prop.name
                                    },
                                    objectId = new AssetReference(parObject.Name.Content, GMAssetType.Object)
                                });
                                isOverridden = true;
                                break;
                            }
                        }
                    }



                    // If the property is not overridden, add it to the final properties
                    if (!isOverridden)
                    {
                        finalprops.Add(prop);
                    }
                }
            }


            // Assign the final properties and overridden properties to the dumped object
            dumpedObject.properties = finalprops;
            dumpedObject.overriddenProperties = overriddenprops;
            continue;
        }

        foreach (UndertaleGameObject.Event ev in eventList)
        {
            AssetReference collisionReference = null;
            int subType = (int)ev.EventSubtype;
            // if collision event
            if (i == (int)EventType.Collision)
            {
                // set subType to 0 because its a collision event
                subType = 0;
                UndertaleGameObject collisionObject = Data.GameObjects[(int)ev.EventSubtype];
                if (collisionObject is not null)
                    collisionReference = new AssetReference(collisionObject.Name.Content, GMAssetType.Object);
            }
            dumpedObject.eventList.Add(new GMEvent()
            {
                eventType = i,
                eventNum = subType,
                collisionObjectId = collisionReference,
            });

            if (ev.Actions.Count > 0)
            {
                var action = ev.Actions[0];
                UndertaleCode code = action.CodeId;
                string subTypeString = subType.ToString();
                if (i == (int)EventType.Collision)
                    subTypeString = Data.GameObjects[(int)ev.EventSubtype].Name.Content;

                string fileName = $"{((EventType)i).ToString()}_{subTypeString}";
                string dumpedCode = DumpCode(code);
                if (dumpedCode is not null)
                    File.WriteAllText($"{scriptDir}objects\\{objectName}\\{fileName}.gml", dumpedCode);
            }
        }
    }

    File.WriteAllText($"{assetDir}{objectName}.yy", JsonSerializer.Serialize(dumpedObject, jsonOptions));

    CreateProjectResource(GMAssetType.Object, objectName, index);

    IncrementProgressParallel();
}

async Task DumpObjects()
{
    if (OBJT || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Objects...");
        await Task.Run(() => Parallel.ForEach(Data.GameObjects, parallelOptions, (obj, state, index) =>
        {
            if (obj is null)
            {
                r_num++;
                return;
            }

            if (OBJT || (CSTM_Enable && CSTM.Contains(obj.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Object: {obj.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping object '{obj.Name.Content}'...");
                DumpObject(obj, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Object '{obj.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Objects complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

public void DumpSound(UndertaleSound s, int index)
{
    string soundName = s.Name.Content;
    string assetDir = $"{scriptDir}sounds\\{soundName}\\";
    // get the last '/' in the file path.
    int slashIndex = s.File.Content.LastIndexOf('/');
    // dumb shit for external sound paths
    string dumpedSoundPath = assetDir + (slashIndex != -1 ? s.File.Content.Substring(slashIndex, s.File.Content.Length - slashIndex) : s.File.Content);
    string soundPath = rootDir + s.File.Content.Replace('/', '\\');

    Directory.CreateDirectory(assetDir);

    bool isExternal = File.Exists(rootDir + s.File.Content);

    GMSound dumpedSound = new GMSound(soundName)
    {
        volume = s.Volume,
        preload = s.Preload,
        tags = GetTags(s)
    };

    // compression
    if (s.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsEmbedded)) dumpedSound.compression = 0;
    else if (s.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsCompressed)) dumpedSound.compression = 1;
    else if (s.Flags.HasFlag(UndertaleSound.AudioEntryFlags.IsDecompressedOnLoad)) dumpedSound.compression = 2;
    else if (isExternal) dumpedSound.compression = 3;

    // handle audiogroups
    string audioGroupPath = $"{rootDir}audiogroup{s.GroupID}.dat";
    var audioGroupElement = Data.AudioGroups.ElementAtOrDefault(s.GroupID);
    string audioGroupName = (audioGroupElement is null ? "audiogroup_default" : audioGroupElement.Name.Content);

    dumpedSound.audioGroupId = new AssetReference()
    {
        name = audioGroupName,
        path = $"audiogroups/{audioGroupName}"
    };

    // declare these for checking later, trimmed to 3 for ID3 checking
    byte[] wavSignature = new byte[] { (byte)'R', (byte)'I', (byte)'F' };
    byte[] oggSignature = new byte[] { (byte)'O', (byte)'g', (byte)'g' };
    byte[] mp3Signature = new byte[] { (byte)'I', (byte)'D', (byte)'3' };
    byte[] wmaSignature = new byte[] { (byte)0x30, (byte)0x26, (byte)0xB2 };
    // TODO: WMA support?

    byte[] fileData;
    // if not using audiogroup_default and is an external audiogroup
    if (s.GroupID != 0 && File.Exists(audioGroupPath))
    {
        // referenced from ExportAllSounds.csx
        try
        {
            UndertaleData data = null;
            // read the audio group
            using (var stream = new FileStream(audioGroupPath, FileMode.Open, FileAccess.Read))
                data = UndertaleIO.Read(stream);

            fileData = data.EmbeddedAudio[s.AudioID].Data;
            File.WriteAllBytes(dumpedSoundPath, data.EmbeddedAudio[s.AudioID].Data);
        }
        catch (Exception e)
        {
            errorList.Add($"{soundName} | An error occured while trying to load {Data.AudioGroups[s.GroupID].Name.Content}, {e}");
            IncrementProgressParallel();
            return;
        }
    }
    else if (s.AudioFile is not null)
        File.WriteAllBytes(dumpedSoundPath, s.AudioFile.Data);
    else if (isExternal)
        File.Copy(soundPath, dumpedSoundPath);

    if (File.Exists(dumpedSoundPath))
        fileData = File.ReadAllBytes(dumpedSoundPath);
    else
    {
        errorList.Add($"{soundName} | File: \"{dumpedSoundPath}\" does not exist.");
        IncrementProgressParallel();
        return;
    }

    // this array is to get the first 3 bytes of the file
    byte[] fileSignature = new byte[3];

    // copy the first 3 bytes into this array
    Array.Copy(fileData, 0, fileSignature, 0, 3);

    // set these for future operations
    WaveStream ws = null;
    string fileExt = String.Empty;

    // run through every common file type
    if (fileSignature.SequenceEqual(wavSignature)) fileExt = "wav";
    else if (fileSignature.SequenceEqual(oggSignature)) fileExt = "ogg";
    else if (fileSignature.SequenceEqual(mp3Signature)) fileExt = "mp3";
    else if (fileSignature.SequenceEqual(wmaSignature)) fileExt = "wma";
    else
    {
        errorList.Add($"{soundName} | Unable to fetch format.");
        return;
    }
    if (FIXAUDIO)
    {
        // rename files without extension
        if (dumpedSoundPath.IndexOf(fileExt, 0, StringComparison.OrdinalIgnoreCase) == -1 && dumpedSoundPath.EndsWith($".{fileExt}"))
        {
            string newPath = Path.GetFileNameWithoutExtension(dumpedSoundPath) + $".{fileExt}";
            File.Move(dumpedSoundPath, newPath);
            dumpedSoundPath = newPath; // update it
        }
    }

    switch (fileExt)
    {
        case "wav":
            ws = new WaveFileReader(dumpedSoundPath);
            break;

        case "ogg":
            ws = new VorbisWaveReader(dumpedSoundPath);
            break;

        case "mp3":
            ws = new Mp3FileReader(dumpedSoundPath);
            break;

        case "wma":
            errorList.Add($"{soundName} | WMA format not supported.");
            return;

    }
    // set the sound file name in the yy file
    dumpedSound.soundFile = (s.File is not null) ? Path.GetFileName(dumpedSoundPath) : String.Empty;

    if (ws is not null)
    {
        TimeSpan len = ws.TotalTime;
        double hours = len.TotalHours * 3600; // hours to seconds
        double minutes = len.TotalMinutes * 60; // minutes to seconds
        double seconds = len.TotalSeconds;
        double milliseconds = len.TotalMilliseconds / 1000; // ms to seconds

        dumpedSound.duration = (float)(hours + minutes + seconds + milliseconds) / 4; // them all together (and divided by 4 for some reason)

        // get sample rate from wavestream
        dumpedSound.sampleRate = ws.WaveFormat.SampleRate;

        // 3d sounds dont seem to decompile correctly with this method, find a more consistent way later.
        dumpedSound.type = ws.WaveFormat.Channels - 1;
    }

    File.WriteAllText($"{assetDir}{soundName}.yy", JsonSerializer.Serialize(dumpedSound, jsonOptions));

    CreateProjectResource(GMAssetType.Sound, soundName, index);

    IncrementProgressParallel();
}

async Task DumpSounds()
{
    if (SOND || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Sounds...");
        await Task.Run(() => Parallel.ForEach(Data.Sounds, parallelOptions, (snd, state, index) =>
        {
            if (snd is null)
            {
                r_num++;
                return;
            }
            if (SOND || (CSTM_Enable && CSTM.Contains(snd.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Sound: {snd.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping sound '{snd.Name.Content}'...");
                DumpSound(snd, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Sound '{snd.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Sounds complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpRoom(UndertaleRoom r, int index)
{
    string roomName = r.Name.Content;
    string assetDir = $"{scriptDir}rooms\\{roomName}\\";
    // create the dumpedRoom folder
    Directory.CreateDirectory(assetDir);
    // construct the object
    GMRoom dumpedRoom = new GMRoom(roomName)
    {
        views = r.Views.Select(v => new GMRoom.GMRView
        {
            // inherit doesnt exist, probably not compiled.
            visible = v.Enabled,
            xview = v.ViewX,
            yview = v.ViewY,
            wview = v.ViewWidth,
            hview = v.ViewHeight,
            xport = v.PortX,
            yport = v.PortY,
            wport = v.PortWidth,
            hport = v.PortHeight,
            hborder = v.BorderX,
            vborder = v.BorderY,
            hspeed = v.SpeedX,
            vspeed = v.SpeedY,
            objectId = (v.ObjectId is null) ? null : new AssetReference(v.ObjectId.Name.Content, GMAssetType.Object)
        }).ToArray(),
        tags = GetTags(r)
    };

    #region layer handling
    // layers are the hardest part of the room decompiler.

    // loop through each layer
    for (int i = 0; i < r.Layers.Count; i++)
    {
        UndertaleRoom.Layer layer = r.Layers[i];
        // this is the end result layer
        GMRoom.GMRLayerBase dumpedLayer = new();

        switch (layer.LayerType)
        {
            case UndertaleRoom.LayerType.Path:
                {
                    dumpedLayer = new GMRoom.GMRPathLayer(layer.LayerName.Content);
                    break;
                }
            case UndertaleRoom.LayerType.Background:
                {
                    dumpedLayer = new GMRoom.GMRBackgroundLayer(layer.LayerName.Content)
                    {
                        spriteId = (layer.BackgroundData.Sprite is null) ? null : new AssetReference(layer.BackgroundData.Sprite.Name.Content, GMAssetType.Sprite),
                        colour = layer.BackgroundData.Color,
                        x = layer.XOffset,
                        y = layer.YOffset,
                        htiled = layer.BackgroundData.TiledHorizontally,
                        vtiled = layer.BackgroundData.TiledVertically,
                        hspeed = layer.HSpeed,
                        vspeed = layer.VSpeed,
                        stretch = layer.BackgroundData.Stretch,
                        animationFPS = layer.BackgroundData.AnimationSpeed,
                        animationSpeedType = (int)layer.BackgroundData.AnimationSpeedType,
                        userdefinedAnimFPS = true
                    };
                    break;
                }
            case UndertaleRoom.LayerType.Instances:
                {
                    GMRoom.GMRInstanceLayer newLayer = new(layer.LayerName.Content);
                    foreach (UndertaleRoom.GameObject inst in layer.InstancesData.Instances)
                    {
                        // create the name of the instance, it will be used for creation code file names and the instance name itself
                        string instanceName = $"inst_{IdToHex(inst.InstanceID)}";
                        // code dump
                        if (inst.CreationCode is not null)
                        {
                            string file_path = $"{assetDir}\\InstanceCreationCode_{instanceName}.gml";
                            string? dumpedCode = DumpCode(inst.CreationCode);
                            if (dumpedCode is not null)
                                File.WriteAllText(file_path, dumpedCode);
                        }

                        List<GMOverriddenProperty> newProperties = new();
                        if (inst.PreCreateCode is not null)
                        {
                            List<ObjectProperty> propData = new();
                            void ObtainPropertyData(UndertaleGameObject o)
                            {
                                StringBuilder sb = new();
                                foreach (var ev in o.Events[(int)EventType.PreCreate])
                                {
                                    string objectProperty = DumpCode(ev.Actions[0].CodeId, new DecompileSettings
                                    {
                                        IndentString = "",
                                        MacroDeclarationsAtTop = false,
                                        CreateEnumDeclarations = false,
                                        UseSemicolon = false,
                                        AllowLeftoverDataOnStack = true
                                    });
                                    sb.Append(objectProperty);
                                }
                                Dictionary<string, string> objectProperties = sb.ToString().Replace("event_inherited()", "").FixVariableDeclarations().ObjectPropertiesToDictionary();

                                if (o.ParentId is not null)
                                    ObtainPropertyData(o.ParentId);

                                foreach (KeyValuePair<string, string> kvp in objectProperties)
                                {
                                    if (!propData.Any(p => p.Prop.Key == kvp.Key))
                                        propData.Add(new ObjectProperty(o.Name.Content, kvp));
                                }
                            }

                            ObtainPropertyData(inst.ObjectDefinition);

                            Dictionary<string, string> currentProperties = DumpCode(inst.PreCreateCode, new DecompileSettings
                            {
                                IndentString = "",
                                MacroDeclarationsAtTop = false,
                                CreateEnumDeclarations = false,
                                UseSemicolon = false,
                                AllowLeftoverDataOnStack = true
                            }).FixVariableDeclarations().ObjectPropertiesToDictionary();

                            foreach (KeyValuePair<string, string> kvp in currentProperties)
                            {
                                // get a match
                                ObjectProperty matchingProperty = propData.FirstOrDefault(op => op.Prop.Key == kvp.Key);

                                if (matchingProperty is null)
                                    continue;

                                // add property
                                newProperties.Add(new GMOverriddenProperty()
                                {
                                    value = kvp.Value,
                                    propertyId = new AssetReference(matchingProperty.ObjName, GMAssetType.Object)
                                    {
                                        name = kvp.Key.Replace("self.","")
                                    },
                                    objectId = new AssetReference(matchingProperty.ObjName, GMAssetType.Object),
                                });
                            }
                        }

                        // construct the instance
                        newLayer.instances.Add(new GMRoom.GMRInstanceLayer.GMRInstance(instanceName)
                        {
                            isDnd = false,
                            objectId = (inst.ObjectDefinition is null) ? null : new AssetReference(inst.ObjectDefinition.Name.Content, GMAssetType.Object),
                            inheritCode = false,
                            hasCreationCode = (inst.CreationCode is not null),
                            colour = inst.Color,
                            rotation = inst.Rotation,
                            scaleX = inst.ScaleX,
                            scaleY = inst.ScaleY,
                            imageSpeed = inst.ImageSpeed,
                            imageIndex = inst.ImageIndex,
                            inheritedItemId = null,
                            frozen = false, // doesnt seem to be implemented.
                            ignore = false, // same here?
                            inheritItemSettings = false, // again?
                            x = inst.X,
                            y = inst.Y,
                            properties = newProperties
                        });

                        // add an entry to instanceCreationOrder
                        dumpedRoom.instanceCreationOrder.Add(new AssetReference(r.Name.Content, GMAssetType.Room)
                        {
                            name = instanceName
                        });
                    }
                    // push to end result
                    dumpedLayer = newLayer;
                    break;
                }
            case UndertaleRoom.LayerType.Assets:
                {
                    GMRoom.GMRAssetLayer newLayer = new(layer.LayerName.Content);
                    // legacy tile stuff
                    foreach (UndertaleRoom.Tile tileAsset in layer.AssetsData.LegacyTiles)
                    {
                        newLayer.assets.Add(new GMRoom.GMRAssetLayer.GMRGraphic($"graphic_{IdToHex(tileAsset.InstanceID)}")
                        {
                            spriteId = (tileAsset.ObjectDefinition is null) ? null : new AssetReference(tileAsset.ObjectDefinition.Name.Content, GMAssetType.Sprite),
                            x = tileAsset.X,
                            y = tileAsset.Y,
                            w = tileAsset.Width,
                            h = tileAsset.Height,
                            u0 = tileAsset.SourceX,
                            v0 = tileAsset.SourceY,
                            u1 = tileAsset.SourceX + (int)tileAsset.Width,
                            v1 = tileAsset.SourceY + (int)tileAsset.Height,
                            colour = tileAsset.Color
                        });
                    }

                    // normal assets
                    foreach (UndertaleRoom.SpriteInstance spriteAsset in layer.AssetsData.Sprites)
                    {
                        if (spriteAsset.Sprite is null || spriteAsset is null)
                            continue;

                        newLayer.assets.Add(new GMRoom.GMRSpriteGraphic
                        {
                            name = spriteAsset.Name.Content,
                            x = spriteAsset.X,
                            y = spriteAsset.Y,
                            spriteId = new AssetReference(spriteAsset.Sprite.Name.Content, GMAssetType.Sprite),
                            headPosition = spriteAsset.FrameIndex,
                            rotation = spriteAsset.Rotation,
                            scaleX = spriteAsset.ScaleX,
                            scaleY = spriteAsset.ScaleY,
                            animationSpeed = spriteAsset.AnimationSpeed,
                            colour = spriteAsset.Color,
                            inheritedItemId = null, // probably not compiled
                            frozen = false, // oh god this again
                            ignore = false,
                            inheritItemSettings = false,
                        });
                    }

                    // push to end result
                    dumpedLayer = newLayer;
                    break;
                }
            case UndertaleRoom.LayerType.Tiles:
                {
                    GMRoom.GMRTileLayer newLayer = new(layer.LayerName.Content);
                    // construct tile data, itll be for the tileset handling below
                    GMRoom.GMRTileLayer.GMRTileData tileData = new()
                    {
                        SerialiseWidth = (int)layer.TilesData.TilesX,
                        SerialiseHeight = (int)layer.TilesData.TilesY,
                    };

                    newLayer.tilesetId = (layer.TilesData.Background is null) ? null : new AssetReference(layer.TilesData.Background.Name.Content, GMAssetType.TileSet);
                    newLayer.x = layer.XOffset;
                    newLayer.y = layer.YOffset;
                    newLayer.tiles = tileData;

                    // obtain tile data
                    tileData.TileSerialiseData.AddRange(layer.TilesData.TileData.SelectMany(row => row));

                    dumpedLayer = newLayer;
                    break;
                }
            case UndertaleRoom.LayerType.Effect:
                {
                    // afaik the same as the base
                    dumpedLayer = new GMRoom.GMREffectLayer(layer.LayerName.Content);
                    break;
                }
            default:
                {
                    dumpedLayer = new GMRoom.GMRLayerBase(layer.LayerName.Content);
                    break;
                }
        }
        // fetch these from the 'layer' variable
        dumpedLayer.visible = layer.IsVisible;
        dumpedLayer.depth = layer.LayerDepth;
        dumpedLayer.effectEnabled = layer.EffectEnabled;
        // this made me get stuck for like an hour I didnt even know you could declare nullable things like this it feels wrong
        dumpedLayer.effectType = layer.EffectType?.Content;

        dumpedLayer.properties = layer.EffectProperties.Select(p => new GMRoom.GMREffectProperty
        {
            name = p.Name.Content,
            type = (int)p.Kind,
            value = p.Value.Content
        }).ToArray();

        bool isFirstLayer = (i == 0 && layer.LayerDepth == 0);
        bool isAlignedWithPrevious = (i != 0 && r.Layers[i - 1].LayerDepth == layer.LayerDepth + 100);
        //bool isAlignedWithNext = (i != r.Layers.Count && r.Layers[i + 1].LayerDepth == layer.LayerDepth - 100);
        dumpedLayer.userdefinedDepth = (isFirstLayer || isAlignedWithPrevious);

        dumpedLayer.inheritLayerDepth = false;
        dumpedLayer.inheritLayerSettings = false;
        //dumpedLayer.inheritVisibility = true;
        //dumpedLayer.inheritSubLayers = false;
        dumpedLayer.hierarchyFrozen = false;

        dumpedLayer.gridX = r.GridWidth;
        dumpedLayer.gridY = r.GridHeight;

        dumpedRoom.layers.Add(dumpedLayer);
    }
    #endregion

    // decompile roomCC
    if (r.CreationCodeId is not null)
    {
        dumpedRoom.creationCodeFile = $"rooms/{r.Name.Content}/RoomCreationCode.gml";

        string file_path = $"{assetDir}\\RoomCreationCode.gml";
        File.WriteAllText(file_path, DumpCode(r.CreationCodeId));
    }
    // settings stuff
    dumpedRoom.roomSettings = new GMRoom.GMRoomSettings
    {
        inheritRoomSettings = false,
        Width = r.Width,
        Height = r.Height,
        persistent = r.Persistent
    };
    dumpedRoom.viewSettings = new GMRoom.GMRoomViewSettings
    {
        inheritViewSettings = false,
        clearDisplayBuffer = r.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.DoNotClearDisplayBuffer),
        clearViewBackground = r.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.ShowColor),
        enableViews = r.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.EnableViews)
    };
    dumpedRoom.physicsSettings = new GMRoom.GMRoomPhysicsSettings
    {
        inheritPhysicsSettings = false,
        PhysicsWorld = r.World,
        PhysicsWorldGravityX = r.GravityX,
        PhysicsWorldGravityY = r.GravityY,
        PhysicsWorldPixToMetres = r.MetersPerPixel
    };

    // turn object into json
    string json_string = JsonSerializer.Serialize(dumpedRoom, jsonOptions);
    File.WriteAllText($"{assetDir}\\{r.Name.Content}.yy", json_string);

    CreateProjectResource(GMAssetType.Room, roomName, index);

    IncrementProgressParallel();
}

async Task DumpRooms()
{
    if (ROOM || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Rooms...");
        await Task.Run(() => Parallel.ForEach(Data.Rooms, parallelOptions, (rm, state, index) =>
        {
            if (rm is null)
            {
                r_num++;
                return;
            }
            if (ROOM || (CSTM_Enable && CSTM.Contains(rm.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Room: {rm.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping room '{rm.Name.Content}'...");
                DumpRoom(rm, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Room '{rm.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        // room order nodes
        finalExport.RoomOrderNodes = Data.GeneralInfo.RoomOrder.Select(r => new GMProject.RoomOrderNode(r.Resource.Name.Content)).ToArray();

        watch.Stop();
        PushToLog($"Rooms complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpSprite(UndertaleSprite s, int index)
{
    bool exportFrames = true;
    string spriteName = s.Name.Content;
    string assetDir = $"{scriptDir}sprites\\{spriteName}\\";
    string layersPath = assetDir + "layers\\";
    string layerId = Guid.NewGuid().ToString();

    // kill gamemakers internal sprites
    if (spriteName.StartsWith("_filter_") || spriteName.StartsWith("_effect_")) return;

    Directory.CreateDirectory(assetDir);
    Directory.CreateDirectory(layersPath);

    GMSprite dumpedSprite = new(spriteName)
    {
        bboxMode = (int)s.BBoxMode,
        preMultiplyAlpha = s.Transparent,
        edgeFiltering = s.Smooth,
        bbox_left = s.MarginLeft,
        bbox_right = s.MarginRight,
        bbox_bottom = s.MarginBottom,
        bbox_top = s.MarginTop,
        width = (int)s.Width,
        height = (int)s.Height,
        // taken from quantum (thanks)
        collisionKind = s.SepMasks switch
        {
            UndertaleSprite.SepMaskType.AxisAlignedRect => 1,
            UndertaleSprite.SepMaskType.Precise => 0,
            UndertaleSprite.SepMaskType.RotatedRect => 5,
        },
        nineSlice = s.V3NineSlice is null ? null : new GMSprite.GMNineSliceData
        {
            enabled = s.V3NineSlice.Enabled,
            left = s.V3NineSlice.Left,
            right = s.V3NineSlice.Right,
            top = s.V3NineSlice.Top,
            bottom = s.V3NineSlice.Bottom,
            tileMode = s.V3NineSlice.TileModes.Select(e => (int)e).ToArray()
        },
        parent = GetParentFolder(GMAssetType.Sprite),
        tags = GetTags(s)
    };

    if (s.V2Sequence is not null)
        dumpedSprite.sequence = SequenceDumper(s.V2Sequence, s);
    else
    {
        dumpedSprite.sequence = new GMSequence(spriteName)
        {
            length = s.Textures.Count,
            xorigin = s.OriginX,
            yorigin = s.OriginY,
            playbackSpeed = s.GMS2PlaybackSpeed,
            playbackSpeedType = (int)s.GMS2PlaybackSpeedType,
            spriteId = new AssetReference(spriteName, GMAssetType.Sprite),
        };
    }
    // precise per frame checking
    if (dumpedSprite.collisionKind == 0 && s.CollisionMasks.Count > 1)
        dumpedSprite.collisionKind = 4;

    // origin calculations
    int originX = s.OriginX;
    int originY = s.OriginY;
    int width = (int)s.Width;
    int height = (int)s.Height;
    eOrigin o = eOrigin.Custom;

    if (originX == 0 && originY == 0)
        o = eOrigin.TopLeft;
    else if (originX == width / 2 && originY == 0)
        o = eOrigin.TopCentre;
    else if (originX == width && originY == 0)
        o = eOrigin.TopRight;
    else if (originX == 0 && originY == width / 2)
        o = eOrigin.MiddleLeft;
    else if (originX == width / 2 && originY == height / 2)
        o = eOrigin.MiddleCentre;
    else if (originX == width && originY == height / 2)
        o = eOrigin.MiddleRight;
    else if (originX == 0 && originY == height)
        o = eOrigin.BottomLeft;
    else if (originX == width / 2 && originY == height)
        o = eOrigin.BottomCentre;
    else if (originX == width && originY == height)
        o = eOrigin.BottomRight;

    dumpedSprite.origin = o;

    // if there is at least 1 frame
    if (s.Textures.Count > 0 && s.Textures[0] is not null)
    {
        // thank you for the idea melia!!
        if (s.Textures[0].Texture?.TexturePage.TextureData.Width == s.Width && s.Textures[0].Texture?.TexturePage.TextureData.Height == s.Height)
            dumpedSprite.For3D = true;
        // create the layer
        dumpedSprite.layers.Add(new GMSprite.GMImageLayer(layerId));
    }
    else
        exportFrames = false;

    AssetReference texGroup = GetTextureGroup(spriteName);
    // another check for For3D
    if (texGroup.name.StartsWith("__YY__")) dumpedSprite.For3D = true;
    else dumpedSprite.textureGroupId = texGroup;

    GMSpriteFramesTrack framesTrack = new();

    for (int i = 0; i < s.Textures.Count; i++)
    {
        UndertaleSprite.TextureEntry tex = s.Textures[i];

        string frameGUID = Guid.NewGuid().ToString();

        // create the directory for the frame
        Directory.CreateDirectory(layersPath + frameGUID);

        if (exportFrames)
        {
            using (TextureWorker tw = new TextureWorker())
            {
                switch ((int)s.SSpriteType)
                {
                    case 0: // raster
                        dumpedSprite.frames.Add(new GMSprite.GMSpriteFrame(frameGUID));
                        break;
                    case 1: // vector
                        errorList.Add($"{dumpedSprite.name} | SWF sprites are not implemented, set to blank image.");
                        dumpedSprite.frames.Add(new GMSprite.GMSpriteFrame(frameGUID));
                        break;
                    case 2: // SPINE
                        errorList.Add($"{dumpedSprite.name} | SPINE sprites are not implemented, set to blank image.");
                        dumpedSprite.frames.Add(new GMSprite.GMSpriteFrame(frameGUID));
                        break;
                }
            }

            imagesToDump.Add(new ImageAssetData(tex.Texture, assetDir, frameGUID + ".png"));
            imagesToDump.Add(new ImageAssetData(tex.Texture, $"{layersPath}{frameGUID}\\", layerId + ".png"));

        }


        Keyframe<SpriteFrameKeyframe> currentKeyframe = new()
        {
            Length = 1f,
            Key = (float)i,
        };

        currentKeyframe.Channels.Add("0", new SpriteFrameKeyframe
        {
            Id = new AssetReference(spriteName, GMAssetType.Sprite)
            {
                name = frameGUID
            },
            name = String.Empty
        });

        framesTrack.keyframes.Keyframes.Add(currentKeyframe);
    }

    if (s.V2Sequence is not null)
    {
        // fix sequence tracks
        for (int i = 0; i < dumpedSprite.frames.Count; i++)
        {
            var frameName = dumpedSprite.frames[i].name;
            // sprites should only have one track
            dumpedSprite.sequence.tracks[0].keyframes.Keyframes[i].Channels["0"].Id.name = frameName;
        }
    }
    else
        dumpedSprite.sequence.tracks.Add(framesTrack);

    File.WriteAllText($"{assetDir}{spriteName}.yy", JsonSerializer.Serialize(dumpedSprite, jsonOptions));
    CreateProjectResource(GMAssetType.Sprite, spriteName, index);

    IncrementProgressParallel();
}

async Task DumpSprites()
{
    if (SPRT || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Sprites...");
        await Task.Run(() => Parallel.ForEach(Data.Sprites, parallelOptions, (spr, state, index) =>
        {
            if (spr is null)
            {
                r_num++;
                return;
            }
            if (SPRT || (CSTM_Enable && CSTM.Contains(spr.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Sprite: {spr.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping sprite '{spr.Name.Content}'...");
                DumpSprite(spr, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Sprite '{spr.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Sprites complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpFont(UndertaleFont f, int index)
{
    string fontName = f.Name.Content;
    string assetDir = $"{scriptDir}fonts\\{fontName}\\";

    Directory.CreateDirectory(assetDir);

    GMFont dumpedFont = new(fontName)
    {
        size = f.EmSize,
        fontName = f.DisplayName.Content,
        bold = f.Bold,
        italic = f.Italic,
        first = (int)f.RangeStart,
        charset = f.Charset,
        AntiAlias = (int)f.AntiAliasing,
        last = (int)f.RangeEnd,
        ascender = (int)f.Ascender,
        lineHeight = (int)f.LineHeight,
        ascenderOffset = (int)f.AscenderOffset,
        maintainGms1Font = true,
        parent = GetParentFolder(GMAssetType.Font),
        textureGroupId = GetTextureGroup(fontName),
        tags = GetTags(f)
    };

    // style name
    dumpedFont.styleName = f.Bold
        ? (f.Italic ? "Bold Italic" : "Bold")
        : (f.Italic ? "Italic" : "Regular");

    // glyph
    dumpedFont.glyphs = f.Glyphs.ToDictionary(g => (int)g.Character, g => new GMFont.GMGlyph
    {
        character = (int)g.Character,
        x = (int)g.SourceX,
        y = (int)g.SourceY,
        w = (int)g.SourceWidth,
        h = (int)g.SourceHeight,
        offset = (int)g.Offset,
        shift = (int)g.Shift,
    });


    // range (heavily referenced quantum)
    GMFont.GMFontRange fontRange = null;
    for (int i = (int)f.RangeStart; i <= f.RangeEnd; i++)
    {
        if (dumpedFont.glyphs.ContainsKey(i))
        {
            if (fontRange is not null) dumpedFont.ranges.Add(fontRange);
            fontRange = null;
        }
        else
        {
            if (fontRange is null)
                fontRange = new GMFont.GMFontRange() { lower = i };

            fontRange.upper = i;
        }
    }

    if (fontRange is not null)
        dumpedFont.ranges.Add(fontRange);

    /*
    using (TextureWorker t = new TextureWorker())
    {
        t.ExportAsPNG(, $"{assetDir}{fontName}.png");
    };
    */
    imagesToDump.Add(new ImageAssetData(f.Texture, assetDir, fontName + ".png"));

    File.WriteAllText($"{assetDir}{fontName}.yy", JsonSerializer.Serialize(dumpedFont, jsonOptions));

    CreateProjectResource(GMAssetType.Font, fontName, index);

    IncrementProgressParallel();
}

async Task DumpFonts()
{
    if (FONT || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Fonts...");
        await Task.Run(() => Parallel.ForEach(Data.Fonts, parallelOptions, (fnt, state, index) =>
        {
            if (fnt is null)
            {
                r_num++;
                return;
            }
            if (FONT || (CSTM_Enable && CSTM.Contains(fnt.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Font: {fnt.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping font '{fnt.Name.Content}'...");
                DumpFont(fnt, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Font '{fnt.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Fonts complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

GMSequence SequenceDumper(UndertaleSequence s, UndertaleSprite spr = null)
{
    GMSequence dumpedSequence = new(s.Name.Content);
    dumpedSequence.playback = (int)s.Playback;
    dumpedSequence.playbackSpeed = s.PlaybackSpeed;
    dumpedSequence.playbackSpeedType = (int)s.PlaybackSpeedType;
    dumpedSequence.length = s.Length;
    dumpedSequence.xorigin = s.OriginX;
    dumpedSequence.yorigin = s.OriginY;
    dumpedSequence.volume = s.Volume;

    if (spr is not null)
        dumpedSequence.parent = GetParentFolder(GMAssetType.Sprite);
    else
    {
        dumpedSequence.parent = GetParentFolder(GMAssetType.Sequence);
        dumpedSequence.tags = GetTags(s);
    }

    // broadcast messages!
    foreach (UndertaleSequence.Keyframe<UndertaleSequence.BroadcastMessage> broadcastMessage in s.BroadcastMessages)
    {
        Keyframe<MessageEventKeyframe> currentKeyframe = new()
        {
            Key = broadcastMessage.Key,
            Length = broadcastMessage.Length,
            Stretch = broadcastMessage.Stretch,
            Disabled = broadcastMessage.Disabled
        };
        foreach (var channel in broadcastMessage.Channels)
        {
            currentKeyframe.Channels.Add(channel.Channel.ToString(), new MessageEventKeyframe() { Events = channel.Value.Messages.Select(message => message.Content).ToArray() });
        }
        dumpedSequence.events.Keyframes.Add(currentKeyframe);
    }


    // moment!!
    string evstubscript = String.Empty;
    if (s.Moments is not null)
    {
        foreach (UndertaleSequence.Keyframe<UndertaleSequence.Moment> moment in s.Moments)
        {
            Keyframe<MomentsEventKeyframe> currentKeyframe = new()
            {
                Key = moment.Key,
                Length = moment.Length,
                Stretch = moment.Stretch,
                Disabled = moment.Disabled
            };
            foreach (var channel in moment.Channels)
            {
                UndertaleString? currentEvent = null;
                MomentsEventKeyframe mom = new();
                if (channel.Value.Events.Count > 1)
                    PushToLog("more than one moment in a single keyframe! report this!");
                else if (channel.Value.Events.Count < 1)
                    currentEvent = channel.Value.Events[0];
                // if it exists
                if (currentEvent is not null)
                {
                    UndertaleScript scriptData = Data.Scripts.ByName(currentEvent.Content);
                    // if the script was found
                    if (scriptData is not null)
                        evstubscript = Regex.Replace(scriptData.Code.ParentEntry.Name.Content, "gml_Script_|gml_GlobalScript_", "");

                    mom.Events.Add(Regex.Replace(currentEvent.Content, "gml_Script_|gml_GlobalScript_", ""));
                    currentKeyframe.Channels.Add(channel.Channel.ToString(), mom);
                }
                dumpedSequence.moments.Keyframes.Add(currentKeyframe);
            }
        }
    }

    // set the stubscript
    if (evstubscript != String.Empty)
        dumpedSequence.eventStubScript = new AssetReference(evstubscript, GMAssetType.Script);

    // eventToFunction stuff
    dumpedSequence.eventToFunction = s.FunctionIDs.ToDictionary(e => e.ID.ToString(), f => Regex.Replace(f.FunctionName.Content, "gml_Script_|gml_GlobalScript_", ""));

    // need to make this a function because tracks can appear recursively!
    // for some reason the recursed tracks are in a normal list instead of an UndertaleSimpleList??
    List<dynamic> DumpTracks(ICollection<UndertaleSequence.Track> tracks, uint parentColour = 69U) // any number that isnt in the color enum lol
    {
        List<dynamic> dumpedTracks = new();

        foreach (UndertaleSequence.Track track in tracks)
        {
            uint colour = parentColour != 69U ? parentColour : GetRandomColour();

            GMBaseTrack currentTrack = new GMBaseTrack();

            // some notes:
            // theres just a switch statement in UTMT with all of the track types lol!
            // for some reason keyframes have a list variable inside of it containing the ACTUAL data.

            switch (track.ModelName.Content) // get the name of the track type
            {
                case "GMAudioTrack":
                    {
                        currentTrack = new GMAudioTrack();
                        var keyframes = ((UndertaleSequence.AudioKeyframes)track.Keyframes).List;

                        foreach (var keyframe in keyframes)
                        {
                            ((GMAudioTrack)currentTrack).keyframes.Keyframes.Add(new Keyframe<AudioKeyframe>()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled,
                                Channels = keyframe.Channels.ToDictionary(k => k.Channel.ToString(), k => new AudioKeyframe()
                                {
                                    Mode = k.Value.Mode,
                                    Id = new AssetReference(k.Value.Resource.Resource.Name.Content, GMAssetType.Sound)
                                })
                            });
                        }

                        break;
                    }
                case "GMInstanceTrack":
                    {
                        currentTrack = new GMInstanceTrack();
                        var keyframes = ((UndertaleSequence.InstanceKeyframes)track.Keyframes).List;

                        foreach (var keyframe in keyframes)
                        {
                            // if the asset is gone.
                            if (keyframe.Channels.Any(r => r.Value.Resource.Resource is null)) break;

                            ((GMInstanceTrack)currentTrack).keyframes.Keyframes.Add(new Keyframe<AssetInstanceKeyframe>()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled,
                                Channels = keyframe.Channels.ToDictionary(k => k.Channel.ToString(), k => new AssetInstanceKeyframe()
                                {
                                    Id = new AssetReference(k.Value.Resource.Resource.Name.Content, GMAssetType.Object)
                                })
                            });

                        }

                        break;
                    }
                case "GMSpriteFramesTrack":
                    {
                        if (spr is null)
                            errorList.Add($"{s.Name.Content} | Track type '{track.ModelName.Content}' inside normal sequence?");

                        currentTrack = new GMSpriteFramesTrack();
                        var keyframes = ((UndertaleSequence.SpriteFramesKeyframes)track.Keyframes).List;

                        for (int i = 0; i < keyframes.Count; i++)
                        {
                            var keyframe = keyframes[i];

                            (currentTrack as GMSpriteFramesTrack).keyframes.Keyframes.Add(new Keyframe<SpriteFrameKeyframe>()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled,
                                Channels = keyframe.Channels.ToDictionary(k => k.Channel.ToString(), k => new SpriteFrameKeyframe()
                                {
                                    Id = new AssetReference(spr.Name.Content, GMAssetType.Sprite)
                                })
                            });
                        }

                        break;
                    }
                case "GMGraphicTrack":
                    {
                        currentTrack = new GMGraphicTrack();
                        var keyframes = ((UndertaleSequence.GraphicKeyframes)track.Keyframes).List;

                        foreach (var keyframe in keyframes)
                        {
                            if (keyframe.Channels.Any(r => r.Value.Resource.Resource is null)) break;

                            (currentTrack as GMGraphicTrack).keyframes.Keyframes.Add(new Keyframe<AssetSpriteKeyframe>()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled,
                                Channels = keyframe.Channels.ToDictionary(k => k.Channel.ToString(), k => new AssetSpriteKeyframe()
                                {
                                    Id = new AssetReference(k.Value.Resource.Resource.Name.Content, GMAssetType.Sprite)
                                })
                            });
                        }

                        break;
                    }
                case "GMSequenceTrack":
                    {
                        currentTrack = new GMSequenceTrack();
                        var keyframes = ((UndertaleSequence.SequenceKeyframes)track.Keyframes).List;

                        foreach (var keyframe in keyframes)
                        {
                            if (keyframe.Channels.Any(r => r.Value.Resource.Resource is null)) break;

                            ((GMSequenceTrack)currentTrack).keyframes.Keyframes.Add(new Keyframe<AssetSequenceKeyframe>()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled,
                                Channels = keyframe.Channels.ToDictionary(k => k.Channel.ToString(), k => new AssetSequenceKeyframe()
                                {
                                    Id = new AssetReference(k.Value.Resource.Resource.Name.Content, GMAssetType.Sequence)
                                })
                            });
                        }

                        break;
                    }
                case "GMRealTrack":
                    {
                        currentTrack = new GMRealTrack();
                        var keyframes = ((UndertaleSequence.RealKeyframes)track.Keyframes).List;

                        foreach (var keyframe in keyframes)
                        {
                            Keyframe<RealKeyframe> currentKeyframe = new()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled,
                                name = String.Empty
                            };

                            foreach (var channel in keyframe.Channels)
                            {
                                // set the value lol
                                RealKeyframe value = new()
                                {
                                    RealValue = channel.Value.Value,
                                    AnimCurveId = (channel.Value.AssetAnimCurve is not null && channel.Value.AssetAnimCurve.Resource is not null ? new AssetReference(channel.Value.AssetAnimCurve.Resource.Name.Content, GMAssetType.AnimationCurve) : null),
                                };
                                // embedded anim curve handling
                                if (channel.Value.IsCurveEmbedded && channel.Value.EmbeddedAnimCurve is not null)
                                {
                                    UndertaleAnimationCurve c = channel.Value.EmbeddedAnimCurve;

                                    string curveName = char.ToUpper(track.Name.Content[0]) + track.Name.Content.Substring(1);

                                    GMAnimCurve dumpedCurve = new(curveName)
                                    {
                                        function = (int)c.GraphType,
                                        tags = GetTags(c)
                                    };

                                    foreach (UndertaleAnimationCurve.Channel curveChannel in c.Channels)
                                    {
                                        GMAnimCurve.GMAnimCurveChannel dumpedChannel = new(curveChannel.Name.Content);

                                        foreach (UndertaleAnimationCurve.Channel.Point point in curveChannel.Points)
                                        {
                                            GMAnimCurve.GMAnimCurvePoint dumpedPoint = new(point.X, point.Value)
                                            {
                                                th0 = point.BezierX0,
                                                th1 = point.BezierX1,
                                                tv0 = point.BezierY0,
                                                tv1 = point.BezierY1
                                            };
                                            dumpedChannel.points.Add(dumpedPoint);
                                        }
                                        dumpedCurve.channels.Add(dumpedChannel);
                                    }
                                    value.EmbeddedAnimCurve = dumpedCurve;
                                }


                                currentKeyframe.Channels.Add(channel.Channel.ToString(), value);
                            }
                            ((GMRealTrack)currentTrack).keyframes.Keyframes.Add(currentKeyframe);
                        }

                        break;
                    }
                case "GMColourTrack":
                    {
                        currentTrack = new GMColourTrack();
                        var keyframes = ((UndertaleSequence.IntKeyframes)track.Keyframes).List;

                        foreach (var keyframe in keyframes)
                        {
                            Keyframe<ColourKeyframe> currentKeyframe = new()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled
                            };

                            foreach (var channel in keyframe.Channels)
                            {
                                // set the value lol
                                ColourKeyframe value = new()
                                {
                                    // cast into uint
                                    Colour = unchecked((uint)channel.Value.Value),
                                    AnimCurveId = (channel.Value.AssetAnimCurve is not null && channel.Value.AssetAnimCurve.Resource is not null ? new AssetReference(channel.Value.AssetAnimCurve.Resource.Name.Content, GMAssetType.AnimationCurve) : null)
                                };

                                // embedded anim curve handling
                                if (channel.Value.IsCurveEmbedded && channel.Value.EmbeddedAnimCurve is not null)
                                {
                                    UndertaleAnimationCurve c = channel.Value.EmbeddedAnimCurve;

                                    string curveName = char.ToUpper(track.Name.Content[0]) + track.Name.Content.Substring(1);

                                    GMAnimCurve dumpedCurve = new(curveName)
                                    {
                                        function = (int)c.GraphType,
                                        tags = GetTags(c)
                                    };

                                    foreach (UndertaleAnimationCurve.Channel curveChannel in c.Channels)
                                    {
                                        GMAnimCurve.GMAnimCurveChannel dumpedChannel = new(curveChannel.Name.Content);

                                        foreach (UndertaleAnimationCurve.Channel.Point point in curveChannel.Points)
                                        {
                                            GMAnimCurve.GMAnimCurvePoint dumpedPoint = new(point.X, point.Value)
                                            {
                                                th0 = point.BezierX0,
                                                th1 = point.BezierX1,
                                                tv0 = point.BezierY0,
                                                tv1 = point.BezierY1
                                            };
                                            dumpedChannel.points.Add(dumpedPoint);
                                        }
                                        dumpedCurve.channels.Add(dumpedChannel);
                                    }
                                    value.EmbeddedAnimCurve = dumpedCurve;
                                }

                                currentKeyframe.Channels.Add(channel.Channel.ToString(), value);
                            }
                            ((GMColourTrack)currentTrack).keyframes.Keyframes.Add(currentKeyframe);
                        }

                        break;
                    }
                case "GMTextTrack":
                    {
                        currentTrack = new GMTextTrack();
                        var keyframes = ((UndertaleSequence.TextKeyframes)track.Keyframes).List;

                        foreach (var keyframe in keyframes)
                        {
                            Keyframe<AssetTextKeyframe> currentKeyframe = new()
                            {
                                Key = keyframe.Key,
                                Length = keyframe.Length,
                                Stretch = keyframe.Stretch,
                                Disabled = keyframe.Disabled,
                                Channels = keyframe.Channels.ToDictionary(k => k.Channel.ToString(), k => new AssetTextKeyframe()
                                {
                                    Text = k.Value.Text.Content,
                                    Wrap = k.Value.Wrap,
                                    // idk about alignment lol
                                    Id = new AssetReference(Data.Fonts[k.Value.FontIndex].Name.Content, GMAssetType.Font)
                                })
                            };
                            ((GMTextTrack)currentTrack).keyframes.Keyframes.Add(currentKeyframe);
                        }
                        break;
                    }
                // lets do the ones with no data afaik.
                case "GMGroupTrack":
                    currentTrack = new GMGroupTrack()
                    {
                        builtinName = 0
                    };
                    break;
                // clip mask tracks
                case "GMClipMaskTrack":
                    currentTrack = new GMClipMaskTrack()
                    {
                        builtinName = 11
                    };
                    break;
                case "GMClipMask_Mask":
                    currentTrack = new GMClipMask_Mask()
                    {
                        builtinName = 12
                    };
                    break;
                case "GMClipMask_Subject":
                    currentTrack = new GMClipMask_Subject()
                    {
                        builtinName = 13
                    };
                    break;
                default:
                    errorList.Add($"{s.Name.Content} | Track type '{track.ModelName.Content}' unimplemented. Defaulting to GMRealTrack");
                    currentTrack = new GMRealTrack();
                    break;
            }


            // TODO: VISIBILITY
            currentTrack.trackColour = colour;
            currentTrack.isCreationTrack = track.IsCreationTrack;
            if (currentTrack.builtinName == -1)
                currentTrack.builtinName = (int)track.BuiltinName;
            currentTrack.name = track.Name.Content;

            currentTrack.tracks = DumpTracks(track.Tracks, colour);


            dumpedTracks.Add(currentTrack);
        }
        return dumpedTracks;
    }

    // start the recursion!
    dumpedSequence.tracks = DumpTracks(s.Tracks);

    return dumpedSequence;
}

void DumpSequence(UndertaleSequence s, int index)
{
    string sequenceName = s.Name.Content;
    string assetDir = $"{scriptDir}sequences\\{sequenceName}\\";

    Directory.CreateDirectory(assetDir);

    GMSequence dumpedSequence = SequenceDumper(s);

    File.WriteAllText($"{assetDir}{sequenceName}.yy", JsonSerializer.Serialize(dumpedSequence, jsonOptions));

    CreateProjectResource(GMAssetType.Sequence, sequenceName, index);

    IncrementProgressParallel();
}

async Task DumpSequences()
{
    if (SEQN || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Sequences...");
        await Task.Run(() => Parallel.ForEach(Data.Sequences, (seq, state, index) =>
        {
            if (seq is null)
            {
                r_num++;
                return;
            }
            if (SEQN || (CSTM_Enable && CSTM.Contains(seq.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Sequence: {seq.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping '{seq.Name.Content}'...");
                DumpSequence(seq, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Sequence '{seq.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Sequences complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpShader(UndertaleShader s, int index)
{
    string shaderName = s.Name.Content;
    string assetDir = $"{scriptDir}shaders\\{shaderName}\\";

    // kill gamemaker internal shaders
    if (shaderName.StartsWith("__yy") || shaderName.StartsWith("_filter_"))
        return;

    Directory.CreateDirectory(assetDir);

    string vertexPath = $"{assetDir}{shaderName}.vsh";
    string fragmentPath = $"{assetDir}{shaderName}.fsh";

    GMShader dumpedShader = new(shaderName)
    {
        parent = GetParentFolder(GMAssetType.Shader),
        type = s.Type switch
        {
            UndertaleShader.ShaderType.GLSL_ES => 1,
            UndertaleShader.ShaderType.GLSL => 2,
            // idk about HLSL_9
            UndertaleShader.ShaderType.HLSL11 => 3,
            UndertaleShader.ShaderType.PSSL => 4,
            // I believe the rest are GMS1
        },
        tags = GetTags(s)
    };

    string vertexSrc = s.GLSL_ES_Vertex.Content.TrimShader();
    string fragmentSrc = s.GLSL_ES_Fragment.Content.TrimShader();

    File.WriteAllText(vertexPath, vertexSrc);
    File.WriteAllText(fragmentPath, fragmentSrc);
    File.WriteAllText($"{assetDir}{shaderName}.yy", JsonSerializer.Serialize(dumpedShader, jsonOptions));

    CreateProjectResource(GMAssetType.Shader, shaderName, index);

    IncrementProgressParallel();
}

async Task DumpShaders()
{
    if (SHDR || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Shaders...");
        await Task.Run(() => Parallel.ForEach(Data.Shaders, parallelOptions, (shd, state, index) =>
        {
            if (shd is null)
            {
                r_num++;
                return;
            }

            if (SHDR || (CSTM_Enable && CSTM.Contains(shd.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Shader: {shd.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping shader '{shd.Name.Content}'...");
                DumpShader(shd, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Shader '{shd.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Shaders complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpExtension(UndertaleExtension e, int index)
{
    string extensionName = e.Name.Content;
    string assetDir = $"{scriptDir}extensions\\{extensionName}\\";

    Directory.CreateDirectory(assetDir);

    GMExtension dumpedExtension = new(extensionName)
    {
        classname = e.ClassName.Content,
        parent = GetParentFolder(GMAssetType.Extension)
    };

    foreach (UndertaleExtensionFile extFile in e.Files)
    {
        string fileName = extFile.Filename.Content;

        GMExtension.GMExtensionFile dumpedFile = new()
        {
            filename = fileName,
            // not sure about origname
            init = extFile.InitScript.Content,
            final = extFile.CleanupScript.Content,
            kind = (int)extFile.Kind
        };

        switch ((int)extFile.Kind)
        {
            case 2: // GML
                string code = String.Empty;
                if (extensionGML.ContainsKey(extensionName))
                {
                    code = String.Join('\n', extensionGML[extensionName]);
                    for (int i = 0; i < extensionGML[extensionName].Count; i++)
                    {
                        string firstLine = extensionGML[extensionName][i].Split('\n')[0];
                        int lineIndex = "#define ".Length;
                        string funcName = firstLine.Substring(lineIndex, firstLine.Length - lineIndex);

                        // arg count
                        Regex regex = new Regex(@"argument(\d+)");

                        int argCount = regex.Matches(extensionGML[extensionName][i])
                            .Cast<Match>()
                            .Select(m => int.Parse(m.Groups[1].Value))
                            .DefaultIfEmpty(0) // Handle case where no matches are found
                            .Max();

                        if (extensionGML[extensionName][i].Contains("argument["))
                            argCount = -1;

                        dumpedFile.functions.Add(
                            new GMExtension.GMExtensionFunction(funcName)
                            {
                                externalName = funcName,
                                kind = 11, // taken from gameframe, might not be right.
                                returnType = 2, // asl taken from gameframe
                                argCount = argCount,
                                args = new int[0]
                            });
                    }
                    extensionGML[extensionName].Clear();
                }


                File.WriteAllText(assetDir + fileName, code);
                break;
            case 1:
            case 3:
            case 4:
            case 5:
                // copy if the file exists.
                if (File.Exists(rootDir + fileName))
                    File.Copy(rootDir + fileName, assetDir + fileName);
                else
                    PushToLog($"File: '{rootDir + fileName}' does not exist");

                foreach (UndertaleExtensionFunction fn in extFile.Functions)
                {
                    dumpedFile.functions.Add(
                        new GMExtension.GMExtensionFunction(fn.Name.Content)
                        {
                            externalName = fn.ExtName.Content,
                            kind = (int)fn.Kind,
                            returnType = (int)fn.RetType,
                            argCount = fn.Arguments.Count,
                            args = fn.Arguments.Select(f => (int)f.Type).ToArray(),
                        });
                }
                break;
        }

        dumpedExtension.files.Add(dumpedFile);
    }

    foreach (UndertaleExtensionOption opt in e.Options)
    {
        string optionName = opt.Name.Content;
        GMExtension.GMExtensionOption dumpedOption = new(optionName)
        {
            displayName = optionName,
            optType = (int)opt.Kind,
        };
        if (iniData.ContainsKey(extensionName) && iniData[extensionName].ContainsKey(optionName))
        {
            dynamic iniValue = iniData[extensionName][optionName];
            dumpedOption.exportToINI = true;
            dumpedOption.defaultValue = iniValue.ToString();
        }

        dumpedExtension.options.Add(dumpedOption);
    }

    File.WriteAllText($"{assetDir}{extensionName}.yy", JsonSerializer.Serialize(dumpedExtension, jsonOptions));

    CreateProjectResource(GMAssetType.Extension, extensionName, index);

    IncrementProgressParallel();
}

async Task DumpExtensions()
{
    if (EXTN || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Extensions...");
        await Task.Run(() => Parallel.ForEach(Data.Extensions, parallelOptions, (ext, state, index) =>
        {
            if (ext is null)
            {
                r_num++;
                return;
            }
            if (EXTN || (CSTM_Enable && CSTM.Contains(ext.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Extension: {ext.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping extension '{ext.Name.Content}'...");
                DumpExtension(ext, (int)index);
                if (LOG)
                    PushToLog($"Extension '{ext.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));

        #region gml extension

        if (!extensionGML.ContainsKey("DecompiledGMLExtension"))
            return;

        string extensionName = "DecompiledExtension";
        string assetDir = $"{scriptDir}extensions\\{extensionName}\\";

        Directory.CreateDirectory(assetDir);
        // create gml func extension
        GMExtension gmlExtension = new(extensionName)
        {
            classname = extensionName,
            parent = new AssetReference(extensionName, GMAssetType.Extension)
            {
                name = "DecompilerGenerated",
                path = "folders/DecompilerGenerated.yy"
            }
        };
        GMExtension.GMExtensionFile extensionFile = new()
        {
            filename = extensionName + "GML"
        };
        foreach (string func in extensionGML["DecompiledGMLExtension"])
        {
            string firstLine = func.Split('\n')[0];
            int index = "#define ".Length;
            string funcName = firstLine.Substring(index, firstLine.Length - index);

            if (funcName.Contains("init"))
                extensionFile.init = funcName;
            // arg count
            Regex regex = new Regex(@"argument(\d+)");

            int argCount = regex.Matches(func)
                .Cast<Match>()
                .Select(m => int.Parse(m.Groups[1].Value))
                .DefaultIfEmpty(0) // Handle case where no matches are found
                .Max();

            if (func.Contains("argument["))
                argCount = -1;

            extensionFile.functions.Add(
                new GMExtension.GMExtensionFunction(funcName)
                {
                    externalName = funcName,
                    kind = 11, // taken from gameframe, might not be right.
                    returnType = 2, // asl taken from gameframe
                    argCount = argCount,
                    args = new int[0]
                });
        }
        gmlExtension.files.Add(extensionFile);

        File.WriteAllText($"{assetDir}{extensionName}.yy", JsonSerializer.Serialize(gmlExtension, jsonOptions));
        File.WriteAllText($"{assetDir}{extensionName}GML.gml", String.Join('\n', extensionGML["DecompiledGMLExtension"]));

        CreateProjectResource(GMAssetType.Extension, extensionName, Data.Extensions.Count + 1);

        IncrementProgressParallel();

        #endregion

        watch.Stop();
        PushToLog($"Extensions complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpPath(UndertalePath p, int index)
{
    string pathName = p.Name.Content;
    string assetDir = $"{scriptDir}paths\\{pathName}\\";

    Directory.CreateDirectory(assetDir);

    GMPath dumpedPath = new(pathName)
    {
        kind = Convert.ToInt32(p.IsSmooth),
        precision = (int)p.Precision,
        closed = p.IsClosed,
        parent = GetParentFolder(GMAssetType.Path),
        tags = GetTags(p)
    };

    dumpedPath.points = p.Points.Select(point => new GMPath.GMPathPoint(point.X, point.Y) { speed = point.Speed }).ToArray();

    File.WriteAllText($"{assetDir}{pathName}.yy", JsonSerializer.Serialize(dumpedPath, jsonOptions));

    CreateProjectResource(GMAssetType.Path, pathName, index);

    IncrementProgressParallel();
}

async Task DumpPaths()
{
    if (PATH || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Paths...");
        await Task.Run(() => Parallel.ForEach(Data.Paths, parallelOptions, (pth, state, index) =>
        {
            if (pth is null)
            {
                r_num++;
                return;
            }
            if (PATH || (CSTM_Enable && CSTM.Contains(pth.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Path: {pth.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping path '{pth.Name.Content}'...");
                DumpPath(pth, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Path '{pth.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Paths complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpAnimCurve(UndertaleAnimationCurve c, int index)
{
    string curveName = c.Name.Content;
    string assetDir = $"{scriptDir}animcurves\\{curveName}\\";

    Directory.CreateDirectory(assetDir);

    GMAnimCurve dumpedCurve = new(curveName)
    {
        function = (int)c.GraphType,
        parent = GetParentFolder(GMAssetType.AnimationCurve),
        tags = GetTags(c)
    };

    foreach (UndertaleAnimationCurve.Channel channel in c.Channels)
    {
        GMAnimCurve.GMAnimCurveChannel dumpedChannel = new(channel.Name.Content);

        dumpedChannel.points = channel.Points.Select(p => new GMAnimCurve.GMAnimCurvePoint(p.X, p.Value)
        {
            th0 = p.BezierX0,
            th1 = p.BezierX1,
            tv0 = p.BezierY0,
            tv1 = p.BezierY1
        }).ToList();
        dumpedCurve.channels.Add(dumpedChannel);
    }

    File.WriteAllText($"{assetDir}{curveName}.yy", JsonSerializer.Serialize(dumpedCurve, jsonOptions));

    CreateProjectResource(GMAssetType.AnimationCurve, curveName, index);

    IncrementProgressParallel();
}

async Task DumpAnimCurves()
{
    if (ACRV || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog($"Dumping Animation Curves...");
        await Task.Run(() => Parallel.ForEach(Data.AnimationCurves, parallelOptions, (cur, state, index) =>
        {
            if (cur is null)
            {
                r_num++;
                return;
            }
            if (ACRV || (CSTM_Enable && CSTM.Contains(cur.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Animation Curve: {cur.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping animation curve '{cur.Name.Content}'...");
                DumpAnimCurve(cur, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Animation curve '{cur.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Animation Curves complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpTileSet(UndertaleBackground t, int index)
{
    string tilesetName = t.Name.Content;
    string spriteName = "_decompiled_" + tilesetName;
    string assetDir = $"{scriptDir}tilesets\\{tilesetName}\\";
    string spriteassetDir = $"{scriptDir}sprites\\{spriteName}\\";
    string layersPath = spriteassetDir + "layers\\";

    Directory.CreateDirectory(spriteassetDir);
    Directory.CreateDirectory(assetDir);
    Directory.CreateDirectory(layersPath);

    GMTileSet dumpedTileset = new(tilesetName)
    {
        // copied this chunk from quantum
        tileWidth = (int)t.GMS2TileWidth,
        tileHeight = (int)t.GMS2TileHeight,
        tileAnimation = new GMTileSet.TileAnimation(),
        out_tilehborder = (int)t.GMS2OutputBorderX,
        out_tilevborder = (int)t.GMS2OutputBorderY,
        spriteNoExport = true,
        out_columns = (int)t.GMS2TileColumns,
        tile_count = (int)t.GMS2TileCount,
        parent = GetParentFolder(GMAssetType.TileSet),
        spriteId = (t.Texture is null ? null : new AssetReference(spriteName, GMAssetType.Sprite)),
        textureGroupId = GetTextureGroup(t.Name.Content),
        tags = GetTags(t)
    };

    if (FIXTILE)
    {
        dumpedTileset.tilexoff = 0;
        dumpedTileset.tileyoff = 0;
        dumpedTileset.tilehsep = 0;
        dumpedTileset.tilevsep = 0;
    }
    else
    {
        dumpedTileset.tilexoff = (int)t.GMS2OutputBorderX;
        dumpedTileset.tileyoff = (int)t.GMS2OutputBorderY;
        dumpedTileset.tilehsep = (int)t.GMS2OutputBorderX * 2;
        dumpedTileset.tilevsep = (int)t.GMS2OutputBorderY * 2;
    }

    dumpedTileset.tileAnimation.frameData = t.GMS2TileIds.Select(t => t.ID).ToArray();

    dumpedTileset.tileAnimation.SerialiseFrameCount = (int)t.GMS2ItemsPerTileCount;
    dumpedTileset.tileAnimationSpeed = 1 / ((float)t.GMS2FrameLength / 1000000);

    // tile animation
    List<uint> IDStorage = new List<uint>(); // temp storage for frame IDs
    List<uint> ignoredIds = new List<uint>();
    foreach (var tile in t.GMS2TileIds)
    {
        if (dumpedTileset.tileAnimation.SerialiseFrameCount > 1)
        {
            IDStorage.Add(tile.ID);

            if (dumpedTileset.tileAnimation.SerialiseFrameCount <= IDStorage.Count)
            {
                bool hasDistinctFrames = IDStorage.Distinct().Count() > 1;
                bool isNotIgnored = !ignoredIds.Any(ignoreId => IDStorage.Contains(ignoreId));

                if (hasDistinctFrames && isNotIgnored)
                {
                    // Found an animation
                    dumpedTileset.tileAnimationFrames.Add(new GMTileSet.GMTileAnimation(dumpedTileset.tileAnimationFrames.Count + 1)
                    {
                        frames = new List<uint>(IDStorage)
                    });

                    ignoredIds.AddRange(IDStorage);
                }

                IDStorage.Clear();
            }
        }
    }



    if (t.Texture is not null)
    {
        MagickImage finalResult = null;
        if (FIXTILE)
        {
            TextureWorker worker = new(); // wont let me use 'using'
            // obtain the image for the background and seperate the image into a list of tiles.
            var image = worker.GetTextureFor(t.Texture, tilesetName);
            // dump the tileset early because why not.
            TextureWorker.SaveImageToFile(image, $"{assetDir}output_tileset.png");

            worker.Dispose();
            var tiledImage = image.CropToTiles(
                (uint)(dumpedTileset.tileWidth + ((int)t.GMS2OutputBorderX * 2)),
                (uint)(dumpedTileset.tileHeight + ((int)t.GMS2OutputBorderY * 2))
            ).ToList();

            // set the geometry to the tileset dimensions
            var geometry = new MagickGeometry((uint)dumpedTileset.tileWidth, (uint)dumpedTileset.tileHeight);
            //remove checkerboard
            var settings = new MagickReadSettings
            {
                BackgroundColor = MagickColors.Transparent,
                Width = (uint)dumpedTileset.tileWidth,
                Height = (uint)dumpedTileset.tileHeight
            };
            tiledImage[0] = new MagickImage("xc:none", settings);
            // iterate through each tile, fixing the padding by setting the tileset to the correct dimensions.
            for (int i = 0; i <= tiledImage.Count - 1; i++)
            {
                tiledImage[i].Extent(geometry, Gravity.Center, MagickColors.Transparent);
            }

            using var exportedImage = new MagickImageCollection();
            // construct the image by each tile.
            foreach (var tile in tiledImage)
            {
                exportedImage.Add(tile);
            }

            MontageSettings ms = new MontageSettings()
            {
                Geometry = geometry,
                TileGeometry = new MagickGeometry((uint)dumpedTileset.out_columns, 0),
                BackgroundColor = MagickColors.None,
                Gravity = Gravity.Center
            };

            // save the image to a file when complete.
            using (var result = exportedImage.Montage(ms))
            {
                finalResult = (MagickImage)result.Clone();
            }

            image.Dispose();
            exportedImage.Dispose();
        }



        GMSprite dumpedSprite = new(spriteName)
        {
            origin = 0, // dont need to obtain it for tilesets
            preMultiplyAlpha = t.Transparent,
            edgeFiltering = t.Smooth,
            bbox_left = 0,
            bbox_right = (finalResult is not null ? (int)finalResult.Width : (int)t.Texture.TargetWidth) - 1,
            bbox_bottom = (finalResult is not null ? (int)finalResult.Height : (int)t.Texture.TargetWidth) - 1,
            bbox_top = 0,
            width = (finalResult is not null ? (int)finalResult.Width : (int)t.Texture.TargetWidth),
            height = (finalResult is not null ? (int)finalResult.Height : (int)t.Texture.TargetHeight),
            sequence = new GMSequence(spriteName)
            {
                length = 1,
                xorigin = 0,
                yorigin = 0,
                playbackSpeed = 1,
                playbackSpeedType = 1,
                spriteId = new AssetReference(spriteName, GMAssetType.Sprite)
            },
            parent = GetFolderReference("GeneratedTileSprites", "DecompilerGenerated/"),
            textureGroupId = dumpedTileset.textureGroupId
        };

        GMSpriteFramesTrack framesTrack = new()
        {
            builtinName = 0
        };

        string frameGUID = Guid.NewGuid().ToString();
        string layerGUID = Guid.NewGuid().ToString();

        dumpedSprite.layers.Add(new GMSprite.GMImageLayer(layerGUID));

        Directory.CreateDirectory(layersPath + frameGUID);

        dumpedSprite.frames.Add(new GMSprite.GMSpriteFrame(frameGUID));

        Keyframe<SpriteFrameKeyframe> currentKeyframe = new()
        {
            Length = 1f,
            Key = (float)0,
        };

        currentKeyframe.Channels.Add("0", new SpriteFrameKeyframe
        {
            Id = new AssetReference(spriteName, GMAssetType.Sprite)
            {
                name = frameGUID
            },
            name = String.Empty
        });

        framesTrack.keyframes.Keyframes.Add(currentKeyframe);
        dumpedSprite.sequence.tracks.Add(framesTrack);
        if (FIXTILE)
        {
            TextureWorker.SaveImageToFile(finalResult, $"{spriteassetDir}{frameGUID}.png");
            File.Copy($"{spriteassetDir}{frameGUID}.png", $"{layersPath}{frameGUID}\\{layerGUID}.png");
            // cleanup
            finalResult.Dispose();
        }
        else
        {
            imagesToDump.Add(new ImageAssetData(t.Texture, spriteassetDir, frameGUID + ".png"));
            imagesToDump.Add(new ImageAssetData(t.Texture, $"{layersPath}{frameGUID}\\", layerGUID + ".png"));
            imagesToDump.Add(new ImageAssetData(t.Texture, assetDir, "output_tileset.png"));
        }

        File.WriteAllText($"{spriteassetDir}\\{spriteName}.yy", JsonSerializer.Serialize(dumpedSprite, jsonOptions));

        CreateProjectResource(GMAssetType.Sprite, spriteName, Data.Sprites.Count + index);

        IncrementProgressParallel();

    }


    File.WriteAllText($"{assetDir}\\{tilesetName}.yy", JsonSerializer.Serialize(dumpedTileset, jsonOptions));

    CreateProjectResource(GMAssetType.TileSet, tilesetName, index);

    IncrementProgressParallel();
}

async Task DumpTileSets()
{
    if (BGND || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog($"Dumping Tilesets...");
        await Task.Run(() => Parallel.ForEach(Data.Backgrounds, parallelOptions, (ts, state, index) =>
        {
            if (ts is null)
            {
                r_num++;
                return;
            }
            if (BGND || (CSTM_Enable && CSTM.Contains(ts.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Tileset: {ts.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping tileset '{ts.Name.Content}'...");
                DumpTileSet(ts, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Tileset '{ts.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Tilesets complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpTimeline(UndertaleTimeline t, int index)
{
    string timelineName = t.Name.Content;
    string assetDir = $"{scriptDir}timelines\\{timelineName}\\";
    string finalCode = String.Empty;

    Directory.CreateDirectory(assetDir);

    GMTimeline dumpedTimeline = new(timelineName)
    {
        parent = GetParentFolder(GMAssetType.Timeline),
        tags = GetTags(t)
    };

    foreach (UndertaleTimeline.UndertaleTimelineMoment moment in t.Moments)
    {
        GMTimeline.GMMoment currentMoment = new()
        {
            moment = moment.Step,
            evnt = new GMEvent()
            {
                eventNum = (int)moment.Step
            }
        };
        dumpedTimeline.momentList.Add(currentMoment);

        foreach (UndertaleGameObject.EventAction ev in moment.Event)
            finalCode = DumpCode(ev.CodeId);

        if (finalCode != String.Empty)
            File.WriteAllText(assetDir + $"moment_{moment.Step}.gml", finalCode);
    }

    File.WriteAllText($"{assetDir}\\{timelineName}.yy", JsonSerializer.Serialize(dumpedTimeline, jsonOptions));

    CreateProjectResource(GMAssetType.Timeline, timelineName, Data.Timelines.Count + index);

    IncrementProgressParallel();

}

async Task DumpTimelines()
{
    if (TMLN || CSTM_Enable)
    {
        var watch = Stopwatch.StartNew();
        PushToLog("Dumping Timelines...");
        await Task.Run(() => Parallel.ForEach(Data.Timelines, parallelOptions, (tl, state, index) =>
        {
            if (tl is null)
            {
                r_num++;
                return;
            }
            if (TMLN || (CSTM_Enable && CSTM.Contains(tl.Name.Content)))
            {
                SetProgressBar(null, $"Exporting Timeline: {tl.Name.Content}", r_num++, toDump);

                var assetWatch = Stopwatch.StartNew();
                if (LOG) 
                    PushToLog($"Dumping timeline '{tl.Name.Content}'...");
                DumpTimeline(tl, (int)index);
                assetWatch.Stop();
                if (LOG)
                    PushToLog($"Timelines '{tl.Name.Content}' successfully dumped in {assetWatch.ElapsedMilliseconds} ms.");
            }
        }));
        watch.Stop();
        PushToLog($"Timelines complete! Took {watch.ElapsedMilliseconds} ms");
    }
    else
        return;
}

void DumpTexGroup(UndertaleTextureGroupInfo t)
{
    if (t.Name.Content.ToLower().StartsWith("__yy__") || t.Name.Content.ToLower().StartsWith("_yy_"))
        return;

    string texGroupName = t.Name.Content;
    GMProject.GMTextureGroup dumpedTexGroup = new(texGroupName);


    string lType = "default";
    // LoadType is an enum
    if ((int)t.LoadType != 0) // if external
    {
        dumpedTexGroup.directory = t.Directory.Content;
        lType = "dynamicpages";
    }
    dumpedTexGroup.loadType = lType;

    string compressionFormat = "png";

    if (t.TexturePages.Count > 0)
    {
        var texPage = t.TexturePages[0];

        // compression
        if (texPage.Resource.TextureData.FormatQOI) compressionFormat = "qoi";
        else if (texPage.Resource.TextureData.FormatBZ2) compressionFormat = "bz2";

        dumpedTexGroup.isScaled = Convert.ToBoolean(texPage.Resource.Scaled);
        dumpedTexGroup.mipsToGenerate = (int)texPage.Resource.GeneratedMips;
    }

    dumpedTexGroup.compressFormat = compressionFormat;

    // add these to the dictionary. the texture group stores the assets so all we need to do is fetch the name of them and reference them.
    foreach (var asset in t.Sprites)
		if (asset?.Resource?.Name.Content != null)
			texGroupStuff.Add(asset.Resource.Name.Content, dumpedTexGroup.name);

    foreach (var asset in t.Tilesets)
		if (asset?.Resource?.Name.Content != null)
			texGroupStuff.Add(asset.Resource.Name.Content, dumpedTexGroup.name);

    foreach (var asset in t.Fonts)
		if (asset?.Resource?.Name.Content != null)
			texGroupStuff.Add(asset.Resource.Name.Content, dumpedTexGroup.name);

    finalExport.TextureGroups.Add(dumpedTexGroup);
}

async Task DumpTexGroups()
{
    var watch = Stopwatch.StartNew();
    foreach (UndertaleTextureGroupInfo tg in Data.TextureGroupInfo)
    {
        DumpTexGroup(tg);
        if (LOG)
            PushToLog($"'{tg.Name.Content}' successfully dumped.");
    }
    watch.Stop();
    PushToLog($"Texture Groups complete! Took {watch.ElapsedMilliseconds} ms");
}

async Task DumpAudioGroups()
{
    var watch = Stopwatch.StartNew();
    finalExport.AudioGroups = Data.AudioGroups.Select(ag => new GMProject.GMAudioGroup(ag?.Name?.Content)).ToArray();
    watch.Stop();
    PushToLog($"Audio Groups complete! Took {watch.ElapsedMilliseconds} ms");
}

async Task DumpTextures()
{
    var watch = Stopwatch.StartNew();
    using (TextureWorker tw = new())
    {
        await Task.Run(() => Parallel.ForEach(imagesToDump, parallelOptions, imageData =>
        {
            imageData.Dump(tw);
			
			// get filename
			var imgfilepath = Path.GetFileNameWithoutExtension(imageData.filePath.TrimEnd(Path.DirectorySeparatorChar));
            // check if its garbage text (technically a GUID, but whatever)
            var isGUID = Guid.TryParse(imgfilepath, out _);

            // only update progress bar text if its an actual readable name
            if (!isGUID)
                SetProgressBar(null, $"Dumping Texture: {imgfilepath}", imgsdumped++, imagesToDump.Count);
            else
                imgsdumped++;
        }));
    }
    watch.Stop();
    PushToLog($"All Textures complete! Took {watch.ElapsedMilliseconds} ms");
}

void DumpOptions()
{
    // we're only doing main and windows, cant really test all others.

    string mainOptionsDirectory = $"{scriptDir}options\\main\\";
    string windowsOptionsDirectory = $"{scriptDir}options\\windows\\";
    var info = Data.GeneralInfo;
    var optionInfo = Data.Options;
    // lets start with main.
    Directory.CreateDirectory(mainOptionsDirectory);

    GMMainOptions dumpedMainOptions = new()
    {
        option_gameid = info.GameID.ToString(),
        option_game_speed = (int)info.GMS2FPS,
        option_window_colour = (byte)optionInfo.WindowColor,
        option_steam_app_id = info.SteamAppID.ToString(),
        // at the beginning of 2022, yyg updated the collision system, I dont know the exact runtime, so im just going to assume 2022.1.
        option_collision_compatibility = optionInfo.Info.HasFlag(UndertaleOptions.OptionsFlags.FastCollisionCompatibility) || Data.IsVersionAtLeast(2022, 1),
        option_copy_on_write_enabled = optionInfo.Info.HasFlag(UndertaleOptions.OptionsFlags.EnableCopyOnWrite),
    };

    // now lets go to windows options.
    Directory.CreateDirectory(windowsOptionsDirectory);

    GMWindowsOptions dumpedWindowsOptions = new()
    {
        option_windows_display_name = info.DisplayName.Content,
        option_windows_executable_name = info.Name.Content == rData.name ? "{project_name}.exe" : $"{rData.name}.exe",
        option_windows_version = rData.version,
        option_windows_company_info = rData.companyName,
        option_windows_product_info = rData.productName,
        option_windows_copyright_info = rData.copyright,
        option_windows_description_info = rData.description,
        option_windows_display_cursor = optionInfo.Info.HasFlag(UndertaleOptions.OptionsFlags.ShowCursor),
        option_windows_save_location = Convert.ToInt32(info.Info.HasFlag(UndertaleGeneralInfo.InfoFlags.LocalDataEnabled)),
        option_windows_start_fullscreen = optionInfo.Info.HasFlag(UndertaleOptions.OptionsFlags.FullScreen),
        option_windows_allow_fullscreen_switching = optionInfo.Info.HasFlag(UndertaleOptions.OptionsFlags.ScreenKey),
        option_windows_interpolate_pixels = optionInfo.Info.HasFlag(UndertaleOptions.OptionsFlags.InterpolatePixels),
        option_windows_vsync = info.Info.HasFlag(UndertaleGeneralInfo.InfoFlags.SyncVertex1), // theres like 3 of these SyncVertex flags, I'm just going to do the first one.
        option_windows_resize_window = info.Info.HasFlag(UndertaleGeneralInfo.InfoFlags.Sizeable),
        option_windows_borderless = info.Info.HasFlag(UndertaleGeneralInfo.InfoFlags.BorderlessWindow),
        option_windows_scale = Convert.ToInt32(info.Info.HasFlag(UndertaleGeneralInfo.InfoFlags.Scale)),
        option_windows_texture_page = GetTexturePageSize(),
        option_windows_enable_steam = info.Info.HasFlag(UndertaleGeneralInfo.InfoFlags.SteamEnabled),
        option_windows_disable_sandbox = optionInfo.Info.HasFlag(UndertaleOptions.OptionsFlags.DisableSandbox)
    };

    // constants in the data file.
    foreach (UndertaleOptions.Constant con in Data.Options.Constants)
    {
        if (con.Name.Content.Contains("SleepMargin"))
            dumpedWindowsOptions.option_windows_sleep_margin = Int32.Parse(con.Value.Content);

        if (con.Name.Content.Contains("DrawColour"))
            dumpedMainOptions.option_draw_colour = UInt32.Parse(con.Value.Content);
    }

    // icon handling
    if (!YYMPS)
    {
        string iconsDir = windowsOptionsDirectory + "icons\\";
        Directory.CreateDirectory(iconsDir);

        // Icons
        if (mainoptionimg != null)
            mainoptionimg.Write(mainOptionsDirectory + "template_icon.png"); // 172x172 Icon for GM UI
        if (winoptionimg != null)
            winoptionimg.Write(iconsDir + "icon.ico"); // 256x256 Windows Icon
    }

    // splash screen handling
    string splashScreenPath = $"{rootDir}splash.png";
    if (File.Exists(splashScreenPath))
    {
        dumpedWindowsOptions.option_windows_use_splash = true;
        dumpedWindowsOptions.option_windows_splash_screen = "splash/splash.png";

        Directory.CreateDirectory(windowsOptionsDirectory + "splash");
        File.Copy(splashScreenPath, windowsOptionsDirectory + dumpedWindowsOptions.option_windows_splash_screen);
    }

    File.WriteAllText(mainOptionsDirectory + "options_main.yy", JsonSerializer.Serialize(dumpedMainOptions, jsonOptions));
    File.WriteAllText(windowsOptionsDirectory + "options_windows.yy", JsonSerializer.Serialize(dumpedWindowsOptions, jsonOptions));

    finalExport.Options.Add(new AssetReference { name = dumpedMainOptions.name, path = $"options/main/options_main.yy" });
    finalExport.Options.Add(new AssetReference { name = dumpedWindowsOptions.name, path = $"options/windows/options_windows.yy" });
}

#endregion

#region Program
// scuffed CPU usage limiter
double usageLimit = Math.Clamp((float)cpu_usage, 0f, 100f);
int processorCount = Environment.ProcessorCount;
int threadsToUse = Math.Clamp((int)Math.Floor(processorCount * (usageLimit / 100)), 1, processorCount);
ParallelOptions parallelOptions = new()
{
    MaxDegreeOfParallelism = threadsToUse
};

// obtain info from the runner
RunnerData rData = new(runnerFile);

GMProject finalExport = new GMProject(Data.GeneralInfo.Name.Content)
{
    isEcma = (Data.GeneralInfo.Info.HasFlag(UndertaleGeneralInfo.InfoFlags.JavaScriptMode))
};

finalExport.MetaData.IDEVersion = $"{Data.GeneralInfo.Major}.{Data.GeneralInfo.Minor}.{Data.GeneralInfo.Release}.{Data.GeneralInfo.Build}";

// parse the options.ini file, some extension options export to it.
var iniData = IniParser.ParseToDictionary(rootDir + "options.ini");

// get amount of assets to dump
public int toDump =
  // account for custom pick
  ((CSTM.Count > 0 && CSTM_Enable) ? CSTM.Count :
  // else if normal
  (OBJT ? Data.GameObjects.Count : 0) +
   (SOND ? Data.Sounds.Count : 0) +
    (ROOM ? Data.Rooms.Count : 0) +
     (SPRT ? Data.Sprites.Count : 0) +
      (FONT ? Data.Fonts.Count : 0) +
       (SHDR ? Data.Shaders.Count : 0) +
        (EXTN ? Data.Extensions.Count : 0) +
         (PATH ? Data.Paths.Count : 0) +
          (ACRV ? Data.AnimationCurves.Count : 0) +
           (BGND ? (Data.Backgrounds.Count * 2) : 0) +
            (SEQN ? Data.Sequences.Count : 0) +
             (TMLN ? Data.Timelines.Count : 0));

SetUMTConsoleText("Initializing...");

// doing this before main operation because its needed
await DumpTexGroups();
// might aswell do this as well
await DumpAudioGroups();
// just because I dont consider it a real asset.
if (!YYMPS)
    DumpOptions();

// for DumpScripts & the progress bar
public List<UndertaleScript> scriptsToDump = new();
foreach (UndertaleScript scr in Data.Scripts)
{
    if (scr is null || (scr.Code?.ParentEntry is not null || (scr.Code is null && scr.Name.Content.StartsWith("gml_Script")) || scr.Name.Content.StartsWith("gml_Room")))
        continue;
    else if (scr.Name.Content.StartsWith("gml_Script"))
    {
        // a common naming convention for extension functions are to have the name of the extension in the function, lets find that.
        string functionNameId = scr.Name.Content.Replace("gml_Script_", "");
        int index = functionNameId.IndexOf('_');
        string correctExtension = "DecompiledGMLExtension";
        foreach (UndertaleExtension ext in Data.Extensions)
        {
            string extName = ext.Name.Content.ToLower();
            if (extName.Contains(functionNameId.ToLower().Substring(0, index)))
            {
                correctExtension = extName;
                break;
            }
        }
        if (!extensionGML.ContainsKey(correctExtension))
            extensionGML[correctExtension] = new List<string>();
        extensionGML[correctExtension].Add($"#define {functionNameId}\n{DumpCode(scr.Code, new DecompileSettings() { UnknownArgumentNamePattern = "argument{0}", AllowLeftoverDataOnStack = true })}");
        continue;
    }
    scriptsToDump.Add(scr);
}
toDump += (SCPT ? scriptsToDump.Count : 0);

SetProgressBar(null, "Exporting Assets...", 0, toDump);
StartProgressBarUpdater();
SetUMTConsoleText("Running Decompiler...");

var totalTime = Stopwatch.StartNew();

await Task.WhenAll(
    CopyDataFiles(),

    DumpScripts(),
    DumpObjects(),
    DumpSounds(),
    DumpRooms(),
    DumpSprites(),
    DumpFonts(),
    DumpShaders(),
    DumpExtensions(),
    DumpPaths(),
    DumpAnimCurves(),
    DumpTileSets(),
    DumpSequences(),
    DumpTimelines()
);

await StopProgressBarUpdater();
HideProgressBar();

public int imgsdumped = 0;
if (imagesToDump.Count > 0)
{
    SetProgressBar(null, "Dumping Textures...", imgsdumped, imagesToDump.Count);
    StartProgressBarUpdater();

    await DumpTextures();

    await StopProgressBarUpdater();
    HideProgressBar();
}
public int noteIndex = 0; // for the order
string readMeMessage =
$@"A Decompilation of {Data.GeneralInfo.DisplayName.Content}

Original GameMaker Version: {Data.GeneralInfo.Major}.{Data.GeneralInfo.Minor}.{Data.GeneralInfo.Release}.{Data.GeneralInfo.Build}

--------------------------------------------------------
Project Decompiled by Ultimate_GMS2_Decompiler_v3.csx
	Improved by burnedpopcorn180
		Original Version by crystallizedsparkle
";
// create the readme
if (!YYMPS)
    CreateNote("README", "DecompilerGenerated", readMeMessage);

// Custom Stuff
#region Extract Asset Order Note

// Asset Order NOTE
string asset_text = "Generated by Ultimate_GMS2_Decompiler_v3.csx";

asset_text += ("\n\nThis is a List of All Asset IDs");
asset_text += ("\nas the Decompiler often has to use an Asset's ID");
asset_text += ("\nbecause it can only GUESS what is an Asset and what is just a Number");

asset_text += ("\n\nAssets Found:");

asset_text += ("\n\nSprites: " + Data.Sprites.Count);
asset_text += ("\nObjects: " + Data.GameObjects.Count);
asset_text += ("\nRooms: " + Data.Rooms.Count);
asset_text += ("\nSounds: " + Data.Sounds.Count);
asset_text += ("\nBackgrounds: " + Data.Backgrounds.Count);
asset_text += ("\nShaders: " + Data.Shaders.Count);
asset_text += ("\nFonts: " + Data.Fonts.Count);
asset_text += ("\nPaths: " + Data.Paths.Count);
asset_text += ("\nTimelines: " + Data.Timelines.Count);
asset_text += ("\nScripts: " + Data.Scripts.Count);
asset_text += ("\nExtensions: " + Data.Extensions.Count);
asset_text += ("\n\n");

// Write Sprites.
asset_text += ("\n--------------------- SPRITES ---------------------");
if (Data.Sprites.Count > 0)
{
    var resourcecount = 0;
    foreach (var sprite in Data.Sprites)
    {
        if (sprite is null)
            continue;            

        asset_text += ("\n" + resourcecount + " - " + sprite.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Sprites could be Found");
}
else if (Data.Sprites.Count == 0)
    asset_text += ("\nNo Sprites could be Found");
// Write Objects.
asset_text += ("\n--------------------- OBJECTS ---------------------");
if (Data.GameObjects.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleGameObject gameObject in Data.GameObjects)
    {
        //but, why?
        if (gameObject is null)
            continue;

        asset_text += ("\n" + resourcecount + " - " + gameObject.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Objects could be Found");
}
else if (Data.GameObjects.Count == 0)
    asset_text += ("\nNo Objects could be Found");
// Write Rooms.
asset_text += ("\n---------------------- ROOMS ----------------------");
if (Data.Rooms.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleRoom room in Data.Rooms)
    {
        if (room is null)
            continue;

        asset_text += ("\n" + resourcecount + " - " + room.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Rooms could be Found");
}
else if (Data.Rooms.Count == 0)
    asset_text += ("\nNo Rooms could be Found");
// Write Sounds.
asset_text += ("\n--------------------- SOUNDS ---------------------");
if (Data.Sounds.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleSound sound in Data.Sounds)
    {
        if (sound is null)
            continue;
        asset_text += ("\n" + resourcecount + " - " + sound.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Sounds could be Found");
}
else if (Data.Sounds.Count == 0)
    asset_text += ("\nNo Sounds could be Found");
// Write Backgrounds.
asset_text += ("\n------------------- BACKGROUNDS -------------------");
if (Data.Backgrounds.Count > 0)
{
    var resourcecount = 0;
    foreach (var background in Data.Backgrounds)
    {
        if (background is null)
            continue;

        asset_text += ("\n" + resourcecount + " - " + background.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Backgrounds could be Found");
}
else if (Data.Backgrounds.Count == 0)
    asset_text += ("\nNo Backgrounds could be Found");
// Write Shaders.
asset_text += ("\n--------------------- SHADERS ---------------------");
if (Data.Shaders.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleShader shader in Data.Shaders)
    {
        if (shader is null)
            continue;

        asset_text += ("\n" + resourcecount + " - " + shader.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Shaders could be Found");
}
else if (Data.Shaders.Count == 0)
    asset_text += ("\nNo Shaders could be Found");
// Write Fonts.
asset_text += ("\n---------------------- FONTS ----------------------");
if (Data.Fonts.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleFont font in Data.Fonts)
    {
        if (font is null)
            continue;

        asset_text += ("\n" + resourcecount + " - " + font.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Fonts could be Found");
}
else if (Data.Fonts.Count == 0)
    asset_text += ("\nNo Fonts could be Found");
// Write Paths.
asset_text += ("\n---------------------- PATHS ----------------------");
if (Data.Paths.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertalePath path in Data.Paths)
    {
        if (path is null)
            continue;

        asset_text += ("\n" + resourcecount + " - " + path.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Paths could be Found");
}
else if (Data.Paths.Count == 0)
    asset_text += ("\nNo Paths could be Found");
// Write Timelines.
asset_text += ("\n-------------------- TIMELINES --------------------");
if (Data.Timelines.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleTimeline timeline in Data.Timelines)
    {
        if (timeline is null)
            continue;

        asset_text += ("\n" + resourcecount + " - " + timeline.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Timelines could be Found");
}
else if (Data.Timelines.Count == 0)
    asset_text += ("\nNo Timelines could be Found");
// Write Scripts.
asset_text += ("\n--------------------- SCRIPTS ---------------------");
if (Data.Scripts.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleScript script in Data.Scripts)
    {
        if (script is null)
            continue;
        asset_text += ("\n" + resourcecount + " - " + script.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Scripts could be Found");
}
else if (Data.Scripts.Count == 0)
    asset_text += ("\nNo Scripts could be Found");
// Write Extensions.
asset_text += ("\n-------------------- EXTENSIONS --------------------");
if (Data.Extensions.Count > 0)
{
    var resourcecount = 0;
    foreach (UndertaleExtension extension in Data.Extensions)
    {
        if (extension is null)
            continue;
        asset_text += ("\n" + resourcecount + " - " + extension.Name.Content);
        ++resourcecount;
    }
    if (resourcecount == 0)
        asset_text += ("\nNo Extensions could be Found");
}
else if (Data.Extensions.Count == 0)
    asset_text += ("\nNo Extensions could be Found");

// make it
if (!YYMPS)
    CreateNote("Asset_Order", "DecompilerGenerated", asset_text);

#endregion
#region Create GlobalInit Script

#region UnknownEnum Extraction Stuff

DecompileSettings dSettings = new DecompileSettings();
dSettings.MacroDeclarationsAtTop = true;
dSettings.CreateEnumDeclarations = true;
string enumName = Data.ToolInfo.DecompilerSettings.UnknownEnumName;
dSettings.UnknownEnumName = enumName;
dSettings.UnknownEnumValuePattern = Data.ToolInfo.DecompilerSettings.UnknownEnumValuePattern;
HashSet<long> values = new HashSet<long>();
List<UndertaleCode> enumtoDump = new();

// search for all UnknownEnum values
async Task DumpEnum()
{
    if (Data.GlobalFunctions is null)
        await Task.Run(() => GlobalDecompileContext.BuildGlobalFunctionCache(Data));

    await Task.Run(() => Parallel.ForEach(enumtoDump, DumpEnums));
}

// Check all Code Entries for Unknown Enums
void DumpEnums(UndertaleCode code)
{
    if (code is not null)
    {
        try
        {
            if (code != null)
            {
                var context = new DecompileContext(globalDecompileContext, code, dSettings);
                BlockNode rootBlock = (BlockNode)context.DecompileToAST();
                foreach (IStatementNode stmt in rootBlock.Children)
                    if (stmt is EnumDeclNode decl && decl.Enum.Name == enumName)
                        foreach (GMEnumValue val in decl.Enum.Values)
                            values.Add(val.Value);
            }
        }
        catch
        { }
    }
}
#endregion

if (SCPT
    // add anyways if any rooms, objects, or scripts were decompiled using asset picker
    // kinda hacky, but not really, and idc
    || Directory.Exists(scriptDir + "scripts")
    || Directory.Exists(scriptDir + "rooms")
    || Directory.Exists(scriptDir + "objects")
)
{
    GMScript globalInitScript = new("_GLOBAL_INIT")
    {
        parent = GetFolderReference("DecompilerGenerated")
    };
    string assetDir = $"{scriptDir}scripts\\{globalInitScript.name}\\";
    string globalInitCode = $"// Generated by Ultimate_GMS2_Decompiler_v3.csx\n";

    #region gml_pragma shit
    foreach (UndertaleGlobalInit g in Data.GlobalInitScripts)
    {
        if (scriptsToDump.Any(s => s.Code == g.Code))
            continue;

        string dumpedCode = DumpCode(g.Code, new DecompileSettings
        {
            MacroDeclarationsAtTop = false,
            CreateEnumDeclarations = false,
            UseSemicolon = false,
            AllowLeftoverDataOnStack = true
        });
        dumpedCode = (dumpedCode is null ? "" : dumpedCode);
        // from quantum
        dumpedCode = dumpedCode.Replace("'", "'+\"'\"+@'").TrimEnd();
        globalInitCode += $"gml_pragma(\"global\", @'{dumpedCode}');\n";
    }
    Directory.CreateDirectory(assetDir);
    #endregion
    #region Extract Enums from JSON files
    globalInitCode += "\n// GameSpecificData Enums\n";
    string[] defs = Directory.GetFiles(definitionDir);

    foreach (string def in defs)
    {
        GameSpecificResolver.GameSpecificDefinition currentDef = JsonSerializer.Deserialize<GameSpecificResolver.GameSpecificDefinition>(File.ReadAllText(def), new JsonSerializerOptions() { AllowTrailingCommas = true });

        foreach (GameSpecificResolver.GameSpecificCondition condition in currentDef.Conditions)
        {
            if ((condition.ConditionKind == "DisplayName.Regex" && Regex.IsMatch((Data?.GeneralInfo?.DisplayName?.Content != null ? Data?.GeneralInfo?.DisplayName?.Content : ""), condition.Value)) || condition.ConditionKind == "Always")
            {
                string macroPath = $"{macroDir}{currentDef.UnderanalyzerFilename}";
                if (File.Exists(macroPath))
                {
                    MacroData macro = JsonSerializer.Deserialize<MacroData>(File.ReadAllText(macroPath), new JsonSerializerOptions() { AllowTrailingCommas = true });
                    foreach (KeyValuePair<string, EnumData> kvp in macro.Types.Enums)
                    {
                        // builtin enums
                        if (kvp.Value.Name == "AudioEffectType" || kvp.Value.Name == "AudioLFOType")
                            continue;
                        // add the enum line
                        globalInitCode += $"enum {kvp.Value.Name} \n{{\n";
                        foreach (KeyValuePair<string, long> currentEnum in kvp.Value.Values)
                        {
                            globalInitCode += $"\t{currentEnum.Key} = {currentEnum.Value},\n";
                        }
                        globalInitCode += $"}}\n\n";
                    }
                }
            }

        }
    }
    #endregion
    #region Extract UnknownEnums
    // if bitwise enums are to be used, don't do it at all
    if (!ENUM)
    {
        globalInitCode += "// Generic Enum Declaration\n";

        foreach (UndertaleCode _code in Data.Code)
        {
            if (_code.ParentEntry is null)
                enumtoDump.Add(_code);
        }

        // search for UnknownEnum Values
        await DumpEnum();

        #region Proper Ordering
        List<long> sorted = new List<long>();
        try
        {
            // https://github.com/UnderminersTeam/Underanalyzer/blob/main/Underanalyzer/Decompiler/AST/Nodes/EnumDeclNode.cs
            sorted = new List<long>(values);
            sorted.Sort((value1sort, value2sort) => Math.Sign(value1sort - value2sort));
        }
        // this sometimes fails, idk why, so ask user to retry
        // because this usually fixes itself after one try
        catch
        {
            int attempts = 0;
            bool tryagain = ScriptQuestion("UnknownEnum Extraction failed!\nTry again?");

            while (tryagain)
            {
                try
                {
                    // clean and try again
                    values = new HashSet<long>();
                    await DumpEnum();
                    sorted = new List<long>(values);
                    sorted.Sort((value1sort, value2sort) => Math.Sign(value1sort - value2sort));

                    // if successful, leave while loop and proceed
                    tryagain = false;
                }
                // if not, ask again
                catch
                {
                    attempts++;
                    tryagain = ScriptQuestion($"UnknownEnum Extraction failed!\nTry again?\n\nAttempt {attempts}");
                }
            }
        }
        #endregion

        // Adding Unknown Enums to the script
        globalInitCode += "enum " + enumName + " \n{\n";

        long expectedValue = 0;
        foreach (long val in sorted)
        {
            string name = string.Format(dSettings.UnknownEnumValuePattern, val.ToString().Replace("-", "m"));
            // if in order, ex: 1, 2, 3
            if (val == expectedValue)
            {
                globalInitCode += "\t" + name + ",\n";
                if (expectedValue != long.MaxValue)
                    expectedValue++;
            }
            // else if not in order, like: 1, 2, 5
            // then make it = 5 to account for it
            else
            {
                globalInitCode += "\t" + name + " = " + val.ToString() + ",\n";
                if (expectedValue != long.MaxValue)
                    expectedValue = val + 1;
                else
                    expectedValue = val;
            }
        }
        globalInitCode += "}";
    }
    #endregion

    File.WriteAllText($"{assetDir}{globalInitScript.name}.yy", JsonSerializer.Serialize(globalInitScript, jsonOptions));
    File.WriteAllText($"{assetDir}{globalInitScript.name}.gml", globalInitCode);

    CreateProjectResource(GMAssetType.Script, "_GLOBAL_INIT", Data.Scripts.Count + 1);

    PushToLog("Created GlobalInit Script.");
}

#endregion

// order all of the resources correctly
finalExport.resources = new ConcurrentQueue<GMProject.Resource>(finalExport.resources.OrderBy(asset => asset.order));
string yypStr = JsonSerializer.Serialize(finalExport, jsonOptions);

File.WriteAllText($"{scriptDir}{finalExport.name}.yyp", yypStr);

totalTime.Stop();
PushToLog($"All assets complete! Took {totalTime.ElapsedMilliseconds} ms");

// YYMPS Packages are literally just normal GameMaker Projects
// that are compressed as a ZIP file with the .yymps file extension
// and with some additional metadata in the .yyp and an extra metadata.json
// to tell gamemaker that its a package rather than a full project
// so yeah
#region YYMPS Maker
if (YYMPS)
{
    #region metadata.json
    // i dont give a shit
    // but i do, i must do this
    string metadatastring = $@"
    {{
    ""package_id"": ""Package"",
    ""display_name"": ""Package"",
     ""version"": ""1.0.0"",
     ""package_type"": ""asset"",
     ""ide_version"": ""{Data.GeneralInfo.Major}.{Data.GeneralInfo.Minor}.{Data.GeneralInfo.Release}.{Data.GeneralInfo.Build}""
}}";
    File.WriteAllText($"{scriptDir}metadata.json", metadatastring);
    #endregion
    #region YYP Metadata adding
    // Read yyp
    string jsonContent = File.ReadAllText($"{scriptDir}{finalExport.name}.yyp");

    // parse it
    JObject jsonObject = JObject.Parse(jsonContent);
    JObject metaData = (JObject)jsonObject["MetaData"];

    // Add new properties or modify existing ones
    metaData["PackageType"] = "Asset";
    metaData["PackageName"] = "Package";
    metaData["PackageID"] = "Package";
    metaData["PackagePublisher"] = "Package";
    metaData["PackageVersion"] = "1.0.0";

    string modifiedJson = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
    File.WriteAllText($"{scriptDir}{finalExport.name}.yyp", modifiedJson);
    #endregion

    // Make final YYMPS directory
    string yympsfolder = $"{rootDir}/Export_YYMPS/";
    Directory.CreateDirectory(yympsfolder);

    // YYMPS Compression
    string yymps = yympsfolder + $"{finalExport.name}.yymps";

    // if previous yymps exists with the same name, DELETE IT OFF THE FACE OF THE EARTH
    if (File.Exists(yymps))
        File.Delete(yymps);

    // the main event i guess
    async Task CreateYYMPS()
    {
        // Compress to YYMPS
        ZipFile.CreateFromDirectory(scriptDir, yymps);

        // delete the directory
        Directory.Delete(scriptDir, true);

        // Wait until the directory is fully deleted
        // because bad stuffs happen if the script finished and its not completely deleted
        while (Directory.Exists(scriptDir))
            { await Task.Delay(1000); } // wait 1000 ms before next check
    }

    // to wait for yymps creation to fully finish
    await CreateYYMPS();
}
#endregion
else
{
    // add logs if not a yymps
    if (errorList.Count > 0)
        File.WriteAllLines(scriptDir + "errors.log", errorList);
}

ScriptMessage($"Script done with {errorList.Count} error{(errorList.Count == 1 ? "" : "s")}!" + (!YYMPS ? "\n\nDouble Check that all necessary files/folders are in the 'datafiles' directory!" : ""));

Process.Start("explorer.exe", (!YYMPS ? scriptDir : $"{rootDir}Export_YYMPS\\"));

GC.Collect();

#endregion
