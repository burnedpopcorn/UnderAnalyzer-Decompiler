/*
    Ultimate_GMS1_Decompiler_v4.csx
        Improved by burnedpopcorn180

            Original Script by cubeww
            Fixed Version by CST1229

    This Script is Compatible with Both My UnderAnalyzer Decompiler
    and Bleeding Edge UTMT 0.8.4.1+

    Ultimate_GMS1_Decompiler_v4 Changes:
        - Added support for Extension Extraction
        - Deleted a single function because OF COURSE

    Ultimate_GMS1_Decompiler_v3 Changes:
        - Added support for Options and Icon Extraction
        - Added support for Automatically Importing Datafiles
        - Cleaned Up all UI code
        - Fixed Shader Trimming

    Ultimate_GMS1_Decompiler_v2 Changes:
        - Rewrote UI to look better and use Dark Mode
        - Script now decompiles all asset types asynchronously
        - Cleaned up previous code
		
    Ultimate_GMS1_Decompiler Changes:
        - UI has been added, with the ability to select specific resource types to decompile
        - Added support for decompiling Shaders
        - Added ability to log all code entries that failed to decompile to a text file
*/

#region Usings
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using ImageMagick;
// new ui
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Input;
// Basic UTMT stuff
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using Underanalyzer.Decompiler;
using Underanalyzer;
using UndertaleModTool;

// just to shorten options code
using OptFlags = UndertaleModLib.Models.UndertaleOptions.OptionsFlags;
using InfoFlags = UndertaleModLib.Models.UndertaleGeneralInfo.InfoFlags;
#endregion

#region Init

#region Checks
// make sure a data.win is loaded
EnsureDataLoaded();

// if GMS2 game
if (Data.IsVersionAtLeast(2, 3) || Data.GeneralInfo.BytecodeVersion > 16)
{
    string Question = 
@"This Script is not intended for games made with newer GameMaker versions
(Consider using the GMS2 Decompiler instead)

Continue Anyways?";

    if (!ScriptQuestion(Question))
        return;
}
// if GM version older than GameMaker: Studio 1.4
if (Data.GeneralInfo.BytecodeVersion < 16)
{
    string Question =
@"This Script is not intended for games made with GameMaker versions older than GM:S 1.4
Continuing to decompile with this script can still work, but can cause some weird issues in some instances

Continue Anyways?";

    if (!ScriptQuestion(Question))
        return;
}
#endregion

string GameName = Data.GeneralInfo.Name.Content.Replace(@"""", ""); //Name == "Project" -> Project
int progress = 0;
string projFolder = GetFolder(FilePath) + GameName + ".gmx" + Path.DirectorySeparatorChar;

// Main Project File
XDocument ProjectGMX = new(
    GMXDeclaration(),
    new XElement("assets",
        // add config shit
        new XElement("Configs", new XAttribute("name", "configs"),
            new XElement("Config", "Configs\\Default")
        )
    )
);
XElement ProjectGMXAssets = ProjectGMX.Element("assets");

// for error log
List<string> errLog = new();

// UnderAnalyzer shit
GlobalDecompileContext decompileContext = new(Data);
IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

#region Get Runner Data
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

public IMagickImage? Convert_Icon(Bitmap _icon, uint _width, uint _height)
{
    try
    {
        // create new IMagick image
        IMagickImage img = null;

        // Convert bitmap to IMagickImage
        using (var memoryStream = new MemoryStream())
        {
            // save Bitmap to MemoryStream
            _icon.Save(memoryStream, ImageFormat.Png);

            // reset memory stream position
            memoryStream.Seek(0, SeekOrigin.Begin);

            // create a MagickImage from MemoryStream
            img = new MagickImage(memoryStream);
        }

        // stop interpolation
        img.FilterType = FilterType.Point;

        // set size
        var size = new MagickGeometry(_width, _height);
        // maintain the aspect ratio
        size.IgnoreAspectRatio = false;
        // resize image
        img.Resize(size);

        return img;
    }
    catch (Exception)
    {
        // if it fails, just return null
        return null;
    }
}
#endregion

// Get Runner Data
string Runner = GetFolder(FilePath) + GameName + ".exe";
string rName = "", rVersion = "", 
        rCompany = "", rProduct = "", 
        rCopyright = "", rDescription = "";
// Icons
public IMagickImage? WinIcon = null, BigIcon = null;

if (File.Exists(Runner))
{
    var rInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(Runner);

    rName = Path.GetFileNameWithoutExtension(Runner);
    rVersion = rInfo.FileVersion;
    rCompany = rInfo.CompanyName;
    rProduct = rInfo.ProductName;
    rCopyright = rInfo.LegalCopyright;
    rDescription = rInfo.FileDescription;

    // Icon
    Bitmap ExeIcon = ExtractIcon.ExtractIconFromExecutable(Runner).ToBitmap();
    WinIcon = Convert_Icon(ExeIcon, 48, 48); // 48x48
    BigIcon = Convert_Icon(ExeIcon, 256, 256); // 256x256
}
#endregion

// gotta redo this
//         extName             scrName, maxArgs
Dictionary<string, Dictionary<string, int>> DumpedExtGMLScripts = new();
// extName, GMLCode
Dictionary<string, string> DumpedExtGMLCode = new();

// Image stuffs
class Images
{
    public Images(dynamic image, string filepath, string name, int? frame)
    {
        this.Image = image;
        this.FilePath = filepath;
        this.Name = name;
        this.Frame = frame;
    }
    public dynamic Image { get; set; }
    public string FilePath { get; set; }
    public string Name { get; set; }
    public int? Frame { get; set; }
}
List<Images> SavedImages = [];

#endregion

#region Main UI

public static class UISettings
{
    public static bool DUMP, // If user chose to go through with decompiling
        // main resources user wants to dump
        OBJT, ROOM, SCPT, TMLN, SOND, SHDR, EXTN, PATH, FONT, SPRT, BGND;
}

#region Theme Class
public static class Theme
{
    // If Dark Mode
    public static bool IsDark = SettingsWindow.EnableDarkMode;

    // Individual Colors
    public static SolidColorBrush LightGrey = new(System.Windows.Media.Color.FromRgb(245, 245, 245));
    public static SolidColorBrush DarkGrey = new(System.Windows.Media.Color.FromRgb(45, 45, 48));
    public static SolidColorBrush BG_Grey = new(System.Windows.Media.Color.FromRgb(23, 23, 23));
    public static SolidColorBrush BG_White = new(System.Windows.Media.Color.FromRgb(230, 230, 230));

    // Simple Colors
    public static SolidColorBrush BasicWhite = System.Windows.Media.Brushes.White;
    public static SolidColorBrush BasicBlack = System.Windows.Media.Brushes.Black;
    public static SolidColorBrush Transparent = System.Windows.Media.Brushes.Transparent;

    // Main Colors
    public static SolidColorBrush WindowBackground = IsDark ? BG_Grey : BG_White;
    public static SolidColorBrush WindowForeground = IsDark ? BasicWhite : BasicBlack;
    public static SolidColorBrush ElementBackground = IsDark ? DarkGrey : LightGrey;
    public static SolidColorBrush ButtonBrush = IsDark ? BG_Grey : LightGrey;
}
#endregion

#region Main Window stuffs
public class UIWindow : Window
{
    public UIWindow()
    {
        Title = "Ultimate_GMS1_Decompiler_v4";
        // remove OS title bar
        WindowStyle = WindowStyle.None;
        AllowsTransparency = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        Background = Theme.WindowBackground;
        Foreground = Theme.WindowForeground;

        StackPanel mainPanel = new() { Margin = new Thickness(8) };
        ToolTip tooltip = new();

        #region New Titlebar
        DockPanel titleBar = new()
        {
            Height = 30,
            Background = Theme.ElementBackground,
        };

        TextBlock titleText = new()
        {
            Text = Title,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0),
            Foreground = Foreground,
        };

        Button closeButton = new()
        {
            Content = "X",
            Width = 40,
            Height = 30,
            Background = Theme.Transparent,
            Foreground = Foreground,
            BorderBrush = Theme.Transparent,
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
            Text = "Welcome to Ultimate_GMS1_Decompiler_v4!\n\nSelect the parts you want to be included in the project, or just press \"Start Dump\" to do a full Export",
            Margin = new Thickness(0, 20, 0, 8)
        });

        // Resources section
        mainPanel.Children.Add(new TextBlock
        {
            Text = "Select Resources to Dump:",
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 8, 0, 4)
        });

        UniformGrid resourceGrid = new() { Columns = 6 };
        CheckBox _OBJT = CreateCheckBox("Objects", true);
        CheckBox _ROOM = CreateCheckBox("Rooms", true);
        CheckBox _SCPT = CreateCheckBox("Scripts", true);
        CheckBox _TMLN = CreateCheckBox("Timelines", true);
        CheckBox _SOND = CreateCheckBox("Sounds", true);
        CheckBox _SHDR = CreateCheckBox("Shaders", true);
        CheckBox _EXTN = CreateCheckBox("Extensions", true);
        CheckBox _PATH = CreateCheckBox("Paths", true);
        CheckBox _FONT = CreateCheckBox("Fonts", true);
        CheckBox _SPRT = CreateCheckBox("Sprites", true);
        CheckBox _BGND = CreateCheckBox("Tilesets", true);

        resourceGrid.Children.Add(_OBJT);
        resourceGrid.Children.Add(_ROOM);
        resourceGrid.Children.Add(_SCPT);
        resourceGrid.Children.Add(_TMLN);
        resourceGrid.Children.Add(_SOND);
        resourceGrid.Children.Add(_SHDR);
        resourceGrid.Children.Add(_EXTN);
        resourceGrid.Children.Add(_PATH);
        resourceGrid.Children.Add(_FONT);
        resourceGrid.Children.Add(_SPRT);
        resourceGrid.Children.Add(_BGND);

        mainPanel.Children.Add(resourceGrid);

        // OK Button
        Button OKBT = new()
        {
            Content = "Start Dump",
            Height = 48,
            Margin = new Thickness(0, 10, 0, 0),

            Background = Theme.ElementBackground,
            Foreground = Theme.WindowForeground,
            BorderBrush = Theme.ButtonBrush
        };
        OKBT.Click += (o, s) =>
        {
            UISettings.DUMP = true;

            UISettings.OBJT = _OBJT.IsChecked == true;
            UISettings.ROOM = _ROOM.IsChecked == true;
            UISettings.SCPT = _SCPT.IsChecked == true;
            UISettings.TMLN = _TMLN.IsChecked == true;
            UISettings.SOND = _SOND.IsChecked == true;
            UISettings.SHDR = _SHDR.IsChecked == true;
            UISettings.EXTN = _EXTN.IsChecked == true;
            UISettings.PATH = _PATH.IsChecked == true;
            UISettings.FONT = _FONT.IsChecked == true;
            UISettings.SPRT = _SPRT.IsChecked == true;
            UISettings.BGND = _BGND.IsChecked == true;

            Close();
        };

        mainPanel.Children.Add(OKBT);

        //no scroll bar
        Content = mainPanel;
    }

    private CheckBox CreateCheckBox(string content, bool isChecked = false, bool enabled = true)
    {
        return new CheckBox
        {
            Content = content,
            IsChecked = isChecked,
            IsEnabled = enabled,
            Margin = new Thickness(4),
            Background = Theme.ElementBackground,
            Foreground = Theme.WindowForeground
        };
    }
}
#endregion

// open main window
new UIWindow().ShowDialog();

// if exit
if (!UISettings.DUMP)
{
    GC.Collect();
    return;
}

#endregion

#region Helper Functions
string GetFolder(string path) => Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;

// In the GMX file, -1 is true and 0 is false.
string BoolToString(bool value) => value ? "-1" : "0";

string DecompileCode(UndertaleCode codeId, string assettype = "", string assetname = "")
    => codeId != null 
        ? new DecompileContext(decompileContext, codeId, decompilerSettings).DecompileToString() 
        : (assettype != "" ? AddtoLog(assettype, assetname) : "");

// If a code entry is null
string AddtoLog(string assettype, string assetname)
{
    errLog.Add($"{assettype}:   {assetname}");
    return "/*\nDECOMPILER FAILED!\n\n";
}

string TrimShader(string? sCode, string functionName)
{
    if (sCode is null) return "";

    // this fuck ass regex man
    // this finds the position of a basic GM function that is always compiled with the shader
    string pattern = @"\w+\s+" + functionName + @"\s*\([^)]*\)\s*\{" +
                     @"(?>[^{}]+|(?<open>\{)|(?<-open>\}))*" +
                     @"(?(open)(?!))\}";

    // find pattern
    Match match = Regex.Match(sCode, pattern, RegexOptions.Singleline);
    return
        match.Success
        ? sCode.Substring(match.Index + match.Length).TrimStart() // remove basic GM function and all the others above it
        : sCode;
}

XComment GMXDeclaration() => new("This Document is generated by GameMaker, if you edit it by hand then you do so at your own risk!");

string GMXToString(XDocument GMX) => GMX.ToString() + "\n";// Linux: "\n", Windows: "\r\n";

// works for at least bytecode 16 games
// earlier bytecode versions means that it probably wasn't originally a GMS1.4 game
string EstimateTexPageSize()
{
    if (Data.EmbeddedTextures.Count == 0) return "2048";

    // all normal Texture Page sizes
    List<int> TexPageSizes = new() { 256, 512, 1024, 2048, 4096, 8192 };
    // counts the amount of appearences values of all possible sizes
    Dictionary<string, int> SizesFound = TexPageSizes.ToDictionary(size => $"{size}", size => 0);

    foreach (UndertaleEmbeddedTexture TexPage in Data.EmbeddedTextures)
    {
        int Width = TexPage.TextureData.Width;
        int Height = TexPage.TextureData.Height;

        if (TexPageSizes.Contains(Width) && TexPageSizes.Contains(Height))
            SizesFound[$"{(Width > Height ? Width : Height)}"]++; // get biggest one just in case
    }

    // find most common size used
    KeyValuePair<string, int> OrderedSizes = SizesFound.Aggregate((l, r) => l.Value > r.Value ? l : r);
    return OrderedSizes.Value != 0 ? OrderedSizes.Key : "2048";
}

#endregion

#region Start Dumping
// check and account for old decomp attempt
if (Directory.Exists(projFolder))
    Directory.Delete(projFolder, true);

Directory.CreateDirectory(projFolder);

// Find Amount of Assets that will be extracted
int GetResourceNum(int AssetCount, bool isEnabled) => isEnabled ? AssetCount : 0;
int resourceNum =
    GetResourceNum(Data.Sprites.Count, UISettings.SPRT) +
    GetResourceNum(Data.Backgrounds.Count, UISettings.BGND) +
    GetResourceNum(Data.GameObjects.Count, UISettings.OBJT) +
    GetResourceNum(Data.Rooms.Count, UISettings.ROOM) +
    GetResourceNum(Data.Sounds.Count, UISettings.SOND) +
    GetResourceNum(Data.Scripts.Count, UISettings.SCPT) +
    GetResourceNum(Data.Fonts.Count, UISettings.FONT) +
    GetResourceNum(Data.Paths.Count, UISettings.PATH) +
    GetResourceNum(Data.Timelines.Count, UISettings.TMLN) +
    GetResourceNum(Data.Shaders.Count, UISettings.SHDR) +
    GetResourceNum(Data.Extensions.Count, UISettings.EXTN);

// Check Extension GMLs
CheckExtensionGML();

#region Export Main Assets
SetProgressBar(null, "Exporting Assets...", 0, resourceNum);
StartProgressBarUpdater();
SetUMTConsoleText("Starting Decompiler...");

// Export Resources
await Task.WhenAll(
    ExportResources(Data.Sprites, ((a) => ExportSprite(a)), UISettings.SPRT, "/sprites/images"),
    ExportResources(Data.Backgrounds, ((a) => ExportBackground(a)), UISettings.BGND, "/background/images"),
    ExportResources(Data.GameObjects, ((a) => ExportGameObject(a)), UISettings.OBJT, "/objects"),
    ExportResources(Data.Rooms, ((a) => ExportRoom(a)), UISettings.ROOM, "/rooms"),
    ExportResources(Data.Sounds, ((a) => ExportSound(a)), UISettings.SOND, "/sound/audio"),
    ExportResources(Data.Scripts, ((a) => ExportScript(a)), UISettings.SCPT, "/scripts"),
    ExportResources(Data.Fonts, ((a) => ExportFont(a)), UISettings.FONT, "/fonts"),
    ExportResources(Data.Paths, ((a) => ExportPath(a)), UISettings.PATH, "/paths"),
    ExportResources(Data.Timelines, ((a) => ExportTimeline(a)), UISettings.TMLN, "/timelines"),
    ExportResources(Data.Shaders, ((a) => ExportShader(a)), UISettings.SHDR, "/shaders"),
    ExportResources(Data.Extensions, ((a) => ExportExtension(a)), UISettings.EXTN, "/extensions"),
    ExportConfig()
);

await StopProgressBarUpdater();
HideProgressBar();
#endregion
#region Export Texture Files
if (SavedImages.Count > 0)
{
    SetProgressBar(null, "Exporting Texture Files...", 0, SavedImages.Count);
    StartProgressBarUpdater();

    int ImagesDumped = 0;
    using (TextureWorker tWorker = new())
    {
        await Task.Run(() => Parallel.ForEach(SavedImages, Image =>
        {
            SetProgressBar(null, 
                $"Exporting Texture File: {Image.Name} {(Image.Frame != null ? $"(Frame: {Image.Frame})" : "")}", 
                ImagesDumped++, SavedImages.Count
            );
            tWorker.ExportAsPNG(Image.Image, Image.FilePath, null, true);
        }));
    }

    await StopProgressBarUpdater();
    HideProgressBar();
}
#endregion

// Generate project file
AddDatafiles(ProjectGMXAssets, GetFolder(FilePath));
AddExtensionsToProjectGMX();
File.WriteAllText($"{projFolder}{GameName}.project.gmx", GMXToString(ProjectGMX));

#endregion
#region Dump Finished
if (errLog.Count > 0) // If Errors were Encountered during decompilation
{
    File.WriteAllLinesAsync(projFolder + "Error_Log.txt", errLog);
    ScriptMessage($"Done with {errLog.Count} error{(errLog.Count == 1 ? "" : "s")}.\n" + projFolder + "\n\nError_Log.txt can be found in the Decompiled Project with a list of all code entries that failed to decompile");
}
else // If there weren't any errors found
    ScriptMessage("Done with No Errors Encountered!");

// open export folder in file explorer
System.Diagnostics.Process.Start("explorer.exe", projFolder);
#endregion

#region Main Export Functions

async Task ExportResources(dynamic AssetChunk, Action<dynamic> ExportFunc, bool isEnabled, string Dir)
{
    if (!isEnabled) return;

    Directory.CreateDirectory($"{projFolder}{Dir}");
    await Task.Run(() => Parallel.ForEach(AssetChunk, ExportFunc));
}

#region Sprites
void ExportSprite(UndertaleSprite s)
{
    UpdateProgressBar(null, $"Exporting Sprite: {s.Name.Content}", progress++, resourceNum);

    // Save the sprite GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("sprite",
            new XElement("type", "0"),
            new XElement("xorig", $"{s.OriginX}"),
            new XElement("yorigin", $"{s.OriginY}"),
            // If SepMasks == precise, set to 0 to avoid shape issues
            new XElement("colkind", s.SepMasks == UndertaleSprite.SepMaskType.Precise ? "0" : $"{s.BBoxMode}"),
            new XElement("coltolerance", "0"),
            new XElement("sepmasks", $"{s.SepMasks:D}"),
            new XElement("bboxmode", $"{s.BBoxMode}"),
            new XElement("bbox_left", $"{s.MarginLeft}"),
            new XElement("bbox_right", $"{s.MarginRight}"),
            new XElement("bbox_top", $"{s.MarginTop}"),
            new XElement("bbox_bottom", $"{s.MarginBottom}"),
            new XElement("HTile", "0"),
            new XElement("VTile", "0"),
            new XElement("TextureGroups",
                new XElement("TextureGroup0", "0")
            ),
            new XElement("For3D", "0"),
            new XElement("width", $"{s.Width}"),
            new XElement("height", $"{s.Height}"),
            new XElement("frames")
        )
    );

    for (int i = 0; i < s.Textures.Count; i++)
    {
        if (s.Textures[i]?.Texture != null)
        {
            gmx.Element("sprite").Element("frames").Add(
                new XElement(
                    "frame",
                    new XAttribute("index", $"{i}"),
                    $"images\\{s.Name.Content}_{i}.png"
                )
            );

            // Save sprite textures
            SavedImages.Add(
                new Images(
                    s.Textures[i].Texture, 
                    $"{projFolder}/sprites/images/{s.Name.Content}_{i}.png", 
                    s.Name.Content, 
                    i
                )
            );
        }
    }

    AddAssetToProjectGMX(s.Name.Content, "sprites");
    File.WriteAllText($"{projFolder}/sprites/{s.Name.Content}.sprite.gmx", GMXToString(gmx));
}
#endregion
#region Backgrounds
void ExportBackground(UndertaleBackground b)
{
    UpdateProgressBar(null, $"Exporting Background: {b.Name.Content}", progress++, resourceNum);

    string Width = b.Texture == null ? "0" : $"{b.Texture.BoundingWidth}";
    string Height = b.Texture == null ? "0" : $"{b.Texture.BoundingHeight}";

    // Save the backgound GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("background",
            new XElement("istileset", "-1"),
            new XElement("tilewidth", Width),
            new XElement("tileheight", Height),
            new XElement("tilexoff", "0"),
            new XElement("tileyoff", "0"),
            new XElement("tilehsep", "0"),
            new XElement("tilevsep", "0"),
            new XElement("HTile", "-1"),
            new XElement("VTile", "-1"),
            new XElement("TextureGroups",
                new XElement("TextureGroup0", "0")
            ),
            new XElement("For3D", "0"),
            new XElement("width", Width),
            new XElement("height", Height),
            new XElement("data", $"images\\{b.Name.Content}.png")
        )
    );

    // Save background images
    if (b.Texture != null)
    {
        SavedImages.Add(
            new Images(
                b.Texture,
                $"{projFolder}/background/images/{b.Name.Content}.png",
                b.Name.Content,
                null
            )
        );
    }

    AddAssetToProjectGMX(b.Name.Content, "backgrounds");
    File.WriteAllText($"{projFolder}/background/{b.Name.Content}.background.gmx", GMXToString(gmx));
}
#endregion
#region Objects
void ExportGameObject(UndertaleGameObject o)
{
    UpdateProgressBar(null, $"Exporting Object: {o.Name.Content}", progress++, resourceNum);

    // Save the object GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("object",
            new XElement("spriteName", o.Sprite is null ? "<undefined>" : o.Sprite.Name.Content),
            new XElement("solid", BoolToString(o.Solid)),
            new XElement("visible", BoolToString(o.Visible)),
            new XElement("depth", $"{o.Depth}"),
            new XElement("persistent", BoolToString(o.Persistent)),
            new XElement("parentName", o.ParentId is null ? "<undefined>" : o.ParentId.Name.Content),
            new XElement("maskName", o.TextureMaskId is null ? "<undefined>" : o.TextureMaskId.Name.Content),
            new XElement("events"),

            //Physics
            new XElement("PhysicsObject", BoolToString(o.UsesPhysics)),
            new XElement("PhysicsObjectSensor", BoolToString(o.IsSensor)),
            new XElement("PhysicsObjectShape", (uint)o.CollisionShape),
            new XElement("PhysicsObjectDensity", o.Density),
            new XElement("PhysicsObjectRestitution", o.Restitution),
            new XElement("PhysicsObjectGroup", o.Group),
            new XElement("PhysicsObjectLinearDamping", o.LinearDamping),
            new XElement("PhysicsObjectAngularDamping", o.AngularDamping),
            new XElement("PhysicsObjectFriction", o.Friction),
            new XElement("PhysicsObjectAwake", BoolToString(o.Awake)),
            new XElement("PhysicsObjectKinematic", BoolToString(o.Kinematic)),
            new XElement("PhysicsShapePoints")
        )
    );

    // Loop through PhysicsShapePoints List
    for (int _point = 0; _point < o.PhysicsVertices.Count; _point++)
    {
        var _x = o.PhysicsVertices[_point].X;
        var _y = o.PhysicsVertices[_point].Y;

        var physicsPointsNode = gmx.Element("object").Element("PhysicsShapePoints");
        physicsPointsNode.Add(new XElement("points", $"{_x},{_y}"));
    }

    // Traversing the event type list
    for (int i = 0; i < o.Events.Count; i++)
    {
        // Determine if an event is empty
        if (o.Events[i].Count == 0) 
            continue;

        // Traversing event list
        foreach (var j in o.Events[i])
        {
            var eventsNode = gmx.Element("object").Element("events");

            XElement eventNode = new("event",
                new XAttribute("eventtype", $"{i}")
            );

            eventNode.Add(
                j.EventSubtype == 4 // To get the actual name of the collision object when the event type is a collision event
                ? new XAttribute("ename", Data.GameObjects[(int)j.EventSubtype].Name.Content)
                : new XAttribute("enumb", $"{j.EventSubtype}") // Get the sub-event number directly if not
                );

            // Save action
            XElement actionNode = new("action");

            // Traversing the action list
            foreach (var k in j.Actions)
            {
                actionNode.Add(
                    new XElement("libid", $"{k.LibID}"),
                    new XElement("id", "603"),// set to 603 so its always code, because that's all we get
                    new XElement("kind", $"{k.Kind}"),
                    new XElement("userelative", BoolToString(k.UseRelative)),
                    new XElement("isquestion", BoolToString(k.IsQuestion)),
                    new XElement("useapplyto", BoolToString(k.UseApplyTo)),
                    new XElement("exetype", $"{k.ExeType}"),
                    new XElement("functionname", k.ActionName.Content),
                    new XElement("codestring", ""),
                    new XElement("whoName", "self"),
                    new XElement("relative", BoolToString(k.Relative)),
                    new XElement("isnot", BoolToString(k.IsNot)),
                    new XElement("arguments",
                        new XElement("argument",
                            new XElement("kind", "1"),
                            new XElement("string", DecompileCode(k.CodeId, "OBJECT", o.Name.Content))
                        )
                    )
                );
            }
            eventNode.Add(actionNode);
            eventsNode.Add(eventNode);
        }
    }

    AddAssetToProjectGMX(o.Name.Content, "objects");
    File.WriteAllText($"{projFolder}/objects/{o.Name.Content}.object.gmx", GMXToString(gmx));
}
#endregion
#region Rooms
void ExportRoom(UndertaleRoom r)
{
    UpdateProgressBar(null, $"Exporting Room: {r.Name.Content}", progress++, resourceNum);

    // Save the room GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("room",
            new XElement("caption", r.Caption.Content),
            new XElement("width", $"{r.Width}"),
            new XElement("height", $"{r.Height}"),
            new XElement("vsnap", $"{r.GridHeight}"),
            new XElement("hsnap", $"{r.GridWidth}"),
            new XElement("isometric", "0"),
            new XElement("speed", $"{r.Speed}"),
            new XElement("persistent", BoolToString(r.Persistent)),
            new XElement("colour", $"{(r.BackgroundColor ^ 0xFF000000)}"),// remove alpha (background color doesn't have alpha)
            new XElement("showcolour", BoolToString(r.DrawBackgroundColor)),
            new XElement("code", DecompileCode(r.CreationCodeId)),
            new XElement("enableViews", BoolToString(r.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.EnableViews))),
            new XElement("clearViewBackground", BoolToString(r.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.ClearViewBackground))),
            new XElement("clearDisplayBuffer", BoolToString(r.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.DoNotClearDisplayBuffer))),//added back cuz yeah
            new XElement("makerSettings",
                new XElement("isSet", 0),
                new XElement("w", 1024),
                new XElement("h", 600),
                new XElement("showGrid", 0),
                new XElement("showObjects", -1),
                new XElement("showTiles", -1),
                new XElement("showBackgrounds", -1),
                new XElement("showForegrounds", -1),
                new XElement("showViews", 0),
                new XElement("deleteUnderlyingObj", 0),
                new XElement("deleteUnderlyingTiles", -1),
                new XElement("page", 1),
                new XElement("xoffset", 0),
                new XElement("yoffset", 0)
            )
        )
    );

    #region Room Backgrounds
    XElement backgroundsNode = new("backgrounds");
    foreach (var i in r.Backgrounds)
    {
        XElement backgroundNode = new("background",
            new XAttribute("visible", BoolToString(i.Enabled)),
            new XAttribute("foreground", BoolToString(i.Foreground)),
            new XAttribute("name", i.BackgroundDefinition is null ? "" : i.BackgroundDefinition.Name.Content),
            new XAttribute("x", $"{i.X}"),
            new XAttribute("y", $"{i.Y}"),
            new XAttribute("htiled", BoolToString(i.TiledHorizontally)),
            new XAttribute("vtiled", BoolToString(i.TiledVertically)),
            new XAttribute("hspeed", $"{i.SpeedX}"),
            new XAttribute("vspeed", $"{i.SpeedY}"),
            new XAttribute("stretch", BoolToString(i.Stretch))
        );
        backgroundsNode.Add(backgroundNode);
    }
    gmx.Element("room").Add(backgroundsNode);
    #endregion
    #region Room Views
    XElement viewsNode = new("views");
    foreach (var i in r.Views)
    {
        XElement viewNode = new("view",
            new XAttribute("visible", BoolToString(i.Enabled)),
            new XAttribute("objName", i.ObjectId is null ? "<undefined>" : i.ObjectId.Name.Content),
            new XAttribute("xview", $"{i.ViewX}"),
            new XAttribute("yview", $"{i.ViewY}"),
            new XAttribute("wview", $"{i.ViewWidth}"),
            new XAttribute("hview", $"{i.ViewHeight}"),
            new XAttribute("xport", $"{i.PortX}"),
            new XAttribute("yport", $"{i.PortY}"),
            new XAttribute("wport", $"{i.PortWidth}"),
            new XAttribute("hport", $"{i.PortHeight}"),
            new XAttribute("hborder", $"{i.BorderX}"),
            new XAttribute("vborder", $"{i.BorderY}"),
            new XAttribute("hspeed", $"{i.SpeedX}"),
            new XAttribute("vspeed", $"{i.SpeedY}")
        );
        viewsNode.Add(viewNode);
    }
    gmx.Element("room").Add(viewsNode);
    #endregion
    #region Room Instances
    XElement instancesNode = new("instances");
    foreach (var i in r.GameObjects)
    {
        XElement instanceNode = new("instance",
            new XAttribute("objName", i.ObjectDefinition.Name.Content),
            new XAttribute("x", $"{i.X}"),
            new XAttribute("y", $"{i.Y}"),
            new XAttribute("name", $"inst_{i.InstanceID:X}"),
            new XAttribute("locked", "0"),
            new XAttribute("code", DecompileCode(i.CreationCode)),
            new XAttribute("scaleX", $"{i.ScaleX}"),
            new XAttribute("scaleY", $"{i.ScaleY}"),
            new XAttribute("colour", $"{i.Color}"),
            new XAttribute("rotation", $"{i.Rotation}")
        );
        instancesNode.Add(instanceNode);
    }
    gmx.Element("room").Add(instancesNode);
    #endregion
    #region Room Tiles
    XElement tilesNode = new("tiles");
    foreach (var i in r.Tiles)
    {
        XElement tileNode = new("tile",
            new XAttribute("bgName", i.BackgroundDefinition is null ? "" : i.BackgroundDefinition.Name.Content),
            new XAttribute("x", $"{i.X}"),
            new XAttribute("y", $"{i.Y}"),
            new XAttribute("w", $"{i.Width}"),
            new XAttribute("h", $"{i.Height}"),
            new XAttribute("xo", $"{i.SourceX}"),
            new XAttribute("yo", $"{i.SourceY}"),
            new XAttribute("id", $"{i.InstanceID}"),
            new XAttribute("name", $"inst_{i.InstanceID:X}"),
            new XAttribute("depth", $"{i.TileDepth}"),
            new XAttribute("locked", "0"),
            new XAttribute("colour", $"{i.Color}"),
            new XAttribute("scaleX", $"{i.ScaleX}"),
            new XAttribute("scaleY", $"{i.ScaleY}")
        );
        tilesNode.Add(tileNode);
    }
    gmx.Element("room").Add(tilesNode);
    #endregion
    #region Room Physics
    gmx.Element("room").Add(
        new XElement("PhysicsWorld", BoolToString(r.World)),
        new XElement("PhysicsWorldTop", r.Top),
        new XElement("PhysicsWorldLeft", r.Left),
        new XElement("PhysicsWorldRight", r.Right),
        new XElement("PhysicsWorldBottom", r.Bottom),
        new XElement("PhysicsWorldGravityX", r.GravityX),
        new XElement("PhysicsWorldGravityY", r.GravityY),
        new XElement("PhysicsWorldPixToMeters", r.MetersPerPixel)
    );
    #endregion

    AddAssetToProjectGMX(r.Name.Content, "rooms");
    File.WriteAllText($"{projFolder}/rooms/{r.Name.Content}.room.gmx", GMXToString(gmx));
}
#endregion
#region Sounds
void ExportSound(UndertaleSound s)
{
    UpdateProgressBar(null, $"Exporting Sound: {s.Name.Content}", progress++, resourceNum);

    string sExt = Path.GetExtension(s.File.Content);

    // Save the sound GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("sound",
            new XElement("kind", sExt == ".ogg" ? "3" : "0"),
            new XElement("extension", sExt),
            new XElement("origname", $"sound\\audio\\{s.File.Content}"),
            new XElement("effects", $"{s.Effects}"),
            new XElement("volume",
                new XElement("volume", $"{s.Volume}")
            ),
            new XElement("pan", "0"),
            new XElement("bitRates",
                new XElement("bitRate", "192")
            ),
            new XElement("sampleRates",
                new XElement("sampleRate", "44100")
            ),
            new XElement("types",
                new XElement("type", "1")
            ),
            new XElement("bitDepths",
                new XElement("bitDepth", "16")
            ),
            new XElement("preload", "-1"),
            new XElement("data", Path.GetFileName(s.File.Content)),
            new XElement("compressed", sExt == ".ogg" ? "1" : "0"),
            new XElement("streamed", sExt == ".ogg" ? "1" : "0"),
            new XElement("uncompressOnLoad", "0"),
            new XElement("audioGroup", "0")
        )
    );

    AddAssetToProjectGMX(s.Name.Content, "sounds");
    File.WriteAllText($"{projFolder}/sound/{s.Name.Content}.sound.gmx", GMXToString(gmx));

    // Save sound files
    string sPath = $"{projFolder}/sound/audio/{s.File.Content}";
    string sExtPath = $"{Path.GetDirectoryName(FilePath)}\\{s.File.Content}";

    if (s.AudioFile != null) // if sound is internal, write it
        File.WriteAllBytes(sPath, s.AudioFile.Data);
    else if (File.Exists(sExtPath)) // if sound file is external, copy it
        File.Copy(sExtPath, sPath, true);
}
#endregion
#region Scripts
void ExportScript(UndertaleScript s)
{
    UpdateProgressBar(null, $"Exporting Script: {s.Name.Content}", progress++, resourceNum);

    foreach (string extName in DumpedExtGMLScripts.Keys)
        if (DumpedExtGMLScripts[extName].ContainsKey(s.Name.Content))
            return;

    AddAssetToProjectGMX(s.Name.Content, "scripts", ".gml");

    // Save code to GML file
    File.WriteAllText(
        $"{projFolder}/scripts/{s.Name.Content}.gml",
        DecompileCode(s.Code, "SCRIPT", s.Name.Content)
    );
}
#endregion
#region Fonts
void ExportFont(UndertaleFont f)
{
    UpdateProgressBar(null, $"Exporting Font: {f.Name.Content}", progress++, resourceNum);

    // Save the font GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("font",
            new XElement("name", f.Name.Content),
            new XElement("size", $"{f.EmSize}"),
            new XElement("bold", BoolToString(f.Bold)),
            new XElement("renderhq", "-1"),
            new XElement("italic", BoolToString(f.Italic)),
            new XElement("charset", $"{f.Charset}"),
            new XElement("aa", $"{f.AntiAliasing}"),
            new XElement("includeTTF", "0"),
            new XElement("TTFName", ""),
            new XElement("texgroups",
                new XElement("texgroup", "0")
            ),
            new XElement("ranges",
                new XElement("range0", $"{f.RangeStart},{f.RangeEnd}")
            ),
            new XElement("glyphs"),
            new XElement("kerningPairs"),
            new XElement("image", $"{f.Name.Content}.png")
        )
    );

    #region Glyphs
    XElement glyphsNode = gmx.Element("font").Element("glyphs");
    foreach (var i in f.Glyphs)
    {
        XElement glyphNode = new("glyph");
        glyphNode.Add(new XAttribute("character", $"{i.Character}"));
        glyphNode.Add(new XAttribute("x", $"{i.SourceX}"));
        glyphNode.Add(new XAttribute("y", $"{i.SourceY}"));
        glyphNode.Add(new XAttribute("w", $"{i.SourceWidth}"));
        glyphNode.Add(new XAttribute("h", $"{i.SourceHeight}"));
        glyphNode.Add(new XAttribute("shift", $"{i.Shift}"));
        glyphNode.Add(new XAttribute("offset", $"{i.Offset}"));
        glyphsNode.Add(glyphNode);
    }
    #endregion

    // Save font textures
    SavedImages.Add(
        new Images(
            f.Texture,
            $"{projFolder}/fonts/{f.Name.Content}.png",
            f.Name.Content,
            null
        )
    );

    AddAssetToProjectGMX(f.Name.Content, "fonts");
    File.WriteAllText($"{projFolder}/fonts/{f.Name.Content}.font.gmx", GMXToString(gmx));
}
#endregion
#region Paths
void ExportPath(UndertalePath p)
{
    UpdateProgressBar(null, $"Exporting Path: {p.Name.Content}", progress++, resourceNum);

    // Save the path GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("path",
            new XElement("kind", "0"),
            new XElement("closed", BoolToString(p.IsClosed)),
            new XElement("precision", $"{p.Precision}"),
            new XElement("backroom", "-1"),
            new XElement("hsnap", "16"),
            new XElement("vsnap", "16"),
            new XElement("points")
        )
    );

    // add points
    foreach (var i in p.Points)
        gmx.Element("path").Element("points").Add(new XElement("point", $"{i.X},{i.Y},{i.Speed}"));

    AddAssetToProjectGMX(p.Name.Content, "paths");
    File.WriteAllText($"{projFolder}/paths/{p.Name.Content}.path.gmx", GMXToString(gmx));
}
#endregion
#region Timelines
void ExportTimeline(UndertaleTimeline t)
{
    UpdateProgressBar(null, $"Exporting Timeline: {t.Name.Content}", progress++, resourceNum);

    // Save the timeline GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("timeline")
    );

    foreach (var i in t.Moments)
    {
        XElement entryNode = new("entry");
        entryNode.Add(new XElement("step", i.Step));
        entryNode.Add(new XElement("event"));
        foreach (var j in i.Event)
        {
            entryNode.Element("event").Add(
                new XElement("action",
                    new XElement("libid", $"{j.LibID}"),
                    new XElement("id", "603"),// set to always use code
                    new XElement("kind", $"{j.Kind}"),
                    new XElement("userelative", BoolToString(j.UseRelative)),
                    new XElement("isquestion", BoolToString(j.IsQuestion)),
                    new XElement("useapplyto", BoolToString(j.UseApplyTo)),
                    new XElement("exetype", $"{j.ExeType}"),
                    new XElement("functionname", j.ActionName.Content),
                    new XElement("codestring", ""),
                    new XElement("whoName", "self"),
                    new XElement("relative", BoolToString(j.Relative)),
                    new XElement("isnot", BoolToString(j.IsNot)),
                    new XElement("arguments",
                        new XElement("argument",
                            new XElement("kind", "1"),
                            new XElement("string", DecompileCode(j.CodeId, "TIMELINE", t.Name.Content))
                        )
                    )
                )
            );
        }
        gmx.Element("timeline").Add(entryNode);
    }

    AddAssetToProjectGMX(t.Name.Content, "timelines");
    File.WriteAllText($"{projFolder}/timelines/{t.Name.Content}.timeline.gmx", GMXToString(gmx));
}
#endregion
#region Shaders
void ExportShader(UndertaleShader s)
{
    UpdateProgressBar(null, $"Exporting Shader: {s.Name.Content}", progress++, resourceNum);

    // add gamemaker marker between them since they share the same file
    string ShaderFile =
        TrimShader(s.GLSL_ES_Vertex?.Content, "DoLighting") // Vertex Shader Code (cutoff stuff higher then vec4 DoLighting)
        + "//######################_==_YOYO_SHADER_MARKER_==_######################@~" +
        TrimShader(s.GLSL_ES_Fragment?.Content, "DoFog"); // Fragment Shader Code (cutoff stuff higher then void DoFog)

    AddAssetToProjectGMX(s.Name.Content, "shaders", ".shader");
    File.WriteAllText($"{projFolder}/shaders/{s.Name.Content}.shader", ShaderFile);
}
#endregion
#region Extensions
void ExportExtension(UndertaleExtension e)
{
    UpdateProgressBar(null, $"Exporting Extension: {e.Name.Content}", progress++, resourceNum);

    // Save the extension GMX
    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("extension",
            new XElement("name", e.Name.Content),
            new XElement("version", e.Version?.Content ?? ""),
            new XElement("classname", e.ClassName?.Content ?? ""),
            new XElement("files")
        )
    );

    var Xfiles = gmx.Element("extension").Element("files");

    foreach (UndertaleExtensionFile extFile in e.Files)
    {
        var XCurrentFile = new XElement("file",
            new XElement("filename", extFile.Filename.Content),
            new XElement("origname", "extensions\\" + extFile.Filename.Content),// maybe not this
            new XElement("init", extFile.InitScript.Content),
            new XElement("final", extFile.CleanupScript.Content),
            new XElement("kind", (int)extFile.Kind),
            new XElement("functions")
        );
        var Xfunctions = XCurrentFile.Element("functions");

        string newfilepath = $"{projFolder}/extensions/{e.Name.Content}";
        Directory.CreateDirectory(newfilepath);

        switch ((int)extFile.Kind)
        {
            case 2:  // GML

                if (!DumpedExtGMLCode.ContainsKey(e.Name.Content) || !DumpedExtGMLScripts.ContainsKey(e.Name.Content))
                    break;

                string ExtGMLCode = DumpedExtGMLCode[e.Name.Content];

                foreach (string GMLEntryName in DumpedExtGMLScripts[e.Name.Content].Keys)
                {
                    int maxArgs = DumpedExtGMLScripts[e.Name.Content][GMLEntryName];

                    // construct new func element
                    var Xfunc = new XElement("function",
                        new XElement("name", GMLEntryName),
                        new XElement("externalName", GMLEntryName),
                        new XElement("kind", 2),
                        new XElement("returnType", 2), // default to Double, since idk how to find out
                        new XElement("argCount", maxArgs),
                        new XElement("args")
                    );

                    if (maxArgs > 0)
                    {
                        // add all args (arg, type)
                        var Xargs = Xfunc.Element("args");
                        for (var i = 0; i < maxArgs; i++)
                            Xargs.Add(new XElement("arg", 2)); // default to Double AGAIN
                    }

                    // add element to functions list
                    Xfunctions.Add(Xfunc);
                }

                // copy GML code
                if (ExtGMLCode != string.Empty)
                    File.WriteAllText($"{newfilepath}/{e.Name.Content}.gml", ExtGMLCode);

                break;

            // DLL is 1, but this works as well
            default: // include the other ones just in case

                foreach (UndertaleExtensionFunction func in extFile.Functions)
                {
                    // construct new func element
                    var Xfunc = new XElement("function",
                        new XElement("name", func.Name.Content),
                        new XElement("externalName", func.ExtName.Content),
                        new XElement("kind", (int)func.Kind),
                        new XElement("returnType", (int)func.RetType),
                        new XElement("argCount", func.Arguments.Count),
                        new XElement("args")
                    );

                    // add all args (arg, type)
                    var Xargs = Xfunc.Element("args");
                    foreach (var arg in func.Arguments)
                        Xargs.Add(new XElement("arg", (int)arg.Type));

                    // add element to functions list
                    Xfunctions.Add(Xfunc);
                }

                // copy DLL file
                var compiledfilepath = $"{Path.GetDirectoryName(FilePath)}\\{extFile.Filename.Content}";
                if (File.Exists(compiledfilepath))
                    File.Copy(compiledfilepath, $"{newfilepath}/{extFile.Filename.Content}", true);

                break;
        }

        // add current file
        Xfiles.Add(XCurrentFile);
    }

    File.WriteAllText($"{projFolder}/extensions/{e.Name.Content}.extension.gmx", GMXToString(gmx));
}

void CheckExtensionGML()
{
    foreach (UndertaleExtension ext in Data.Extensions)
    {
        // weed out useless extensions
        if (ext.Files.Count == 0) continue;
        bool hasGML = false;
        foreach (UndertaleExtensionFile extFile in ext.Files)
            if ((int)extFile.Kind == 2)
                hasGML = true;

        if (!hasGML) continue;

        // start shit
        string DumpedGMLCode = string.Empty;
        foreach (UndertaleScript scr in Data.Scripts)
        {
            if (scr.Code == null) continue;

            // name check
            // see if the names are similar
            string scrName = scr.Name.Content;
            int index = scrName.IndexOf('_');
            string extName = ext.Name.Content.ToLower();
            // leave early if name check fails
            if (index < 0 || !extName.Contains(scrName.ToLower().Substring(0, index))) continue;

            // return check
            // because most extension code has a return at the last line
            string GMLCode = DecompileCode(scr.Code);

            string lastLine = GMLCode.TrimEnd(); //default just in case its one line
            int lastNewLine = GMLCode.TrimEnd().LastIndexOf('\n');
            if (lastNewLine > 0) // get last line
                lastLine = GMLCode.TrimEnd().Substring(lastNewLine + 1);

            // add shit if it passes return check
            // (we already checked for name similarity above)
            if (lastLine.Contains("return"))
            {
                // add function to global code string
                DumpedGMLCode += $"#define {scrName}\n{GMLCode}";

                // get the max amount of arguments using normal args (argument0)
                int maxArgs = Regex.Matches(GMLCode, @"argument(\d+)")
                    .Cast<Match>().Select(m => int.Parse(m.Groups[1].Value))
                    .DefaultIfEmpty(0).Max();

                // if argument[0], use -1 instead
                if (GMLCode.Contains("argument[0]")) maxArgs = -1;
                else maxArgs++; // to make argument0 = 1, argument1 = 2...

                if (!DumpedExtGMLScripts.ContainsKey(ext.Name.Content))
                    DumpedExtGMLScripts[ext.Name.Content] = new Dictionary<string, int>();
                DumpedExtGMLScripts[ext.Name.Content][scrName] = maxArgs;
            }
        }

        if (DumpedGMLCode != string.Empty)
            DumpedExtGMLCode[ext.Name.Content] = DumpedGMLCode;
    }
}
#endregion

#region Config Options
async Task ExportConfig()
{
    #region Setup shit
    // universal func to get both types of flags
    bool HasFlag(dynamic Flag)
    { 
        if (Flag is OptFlags)
            return Data.Options.Info.HasFlag(Flag);
        if (Flag is InfoFlags)
            return Data.GeneralInfo.Info.HasFlag(Flag);

        return false;
    }

    // same thing as above, but returns int values
    int HasFlagAsInt(dynamic Flag) => HasFlag(Flag) ? 1 : 0;

    string ConfigDir = $"{projFolder}/Configs/Default/windows";
    Directory.CreateDirectory(ConfigDir);
    #endregion

    XDocument gmx = new(
        GMXDeclaration(),
        new XElement("Config",
            new XElement("Options",
                // Options (Flags)
                new XElement("option_fullscreen", HasFlag(OptFlags.FullScreen)),
                new XElement("option_interpolate", HasFlag(OptFlags.InterpolatePixels)),
                new XElement("option_use_new_audio", HasFlag(OptFlags.UseNewAudio)),
                new XElement("option_noborder", HasFlag(OptFlags.NoBorder)),
                new XElement("option_showcursor", HasFlag(OptFlags.ShowCursor)),
                new XElement("option_sizeable", HasFlag(OptFlags.Sizeable)),
                new XElement("option_stayontop", HasFlag(OptFlags.StayOnTop)),
                new XElement("option_changeresolution", HasFlag(OptFlags.ChangeResolution)),
                new XElement("option_nobuttons", HasFlag(OptFlags.NoButtons)),
                new XElement("option_screenkey", HasFlag(OptFlags.ScreenKey)),
                new XElement("option_helpkey", HasFlag(OptFlags.HelpKey)),
                new XElement("option_quitkey", HasFlag(OptFlags.QuitKey)),
                new XElement("option_savekey", HasFlag(OptFlags.SaveKey)),
                new XElement("option_screenshotkey", HasFlag(OptFlags.ScreenShotKey)),
                new XElement("option_closeesc", HasFlag(OptFlags.CloseSec)),
                new XElement("option_freeze", HasFlag(OptFlags.Freeze)),
                new XElement("option_showprogress", HasFlagAsInt(OptFlags.ShowProgress)),
                new XElement("option_loadtransparent", HasFlag(OptFlags.LoadTransparent)),
                new XElement("option_scaleprogress", HasFlag(OptFlags.ScaleProgress)),
                new XElement("option_displayerrors", HasFlag(OptFlags.DisplayErrors)),
                new XElement("option_writeerrors", HasFlag(OptFlags.WriteErrors)),
                new XElement("option_aborterrors", HasFlag(OptFlags.AbortErrors)),
                new XElement("option_variableerrors", HasFlag(OptFlags.VariableErrors)),
                new XElement("option_psvita_fronttouch", BoolToString(HasFlag(OptFlags.UseFrontTouch))),//PS Vita shit
                new XElement("option_psvita_reartouch", BoolToString(HasFlag(OptFlags.UseRearTouch))),//uses -1 and 0 for some fucking reason
                new XElement("option_use_fast_collision", HasFlag(OptFlags.UseFastCollision)),
                new XElement("option_fast_collision_compatibility", HasFlag(OptFlags.FastCollisionCompatibility)),
                // Options (REAL SHIT)
                new XElement("option_scale", Data.Options.Scale),
                new XElement("option_windowcolor", $"${Data.Options.WindowColor:X8}"), // uint as "$AABBGGRR"
                new XElement("option_colordepth", Data.Options.ColorDepth),
                new XElement("option_resolution", Data.Options.Resolution),
                new XElement("option_frequency", Data.Options.Frequency),
                new XElement("option_sync_vertex", Data.Options.VertexSync),
                new XElement("option_loadalpha", Data.Options.LoadAlpha),

                // Info
                new XElement("option_display_name", Data.GeneralInfo.DisplayName.Content),
                new XElement("option_gameid", Data.GeneralInfo.GameID),
                new XElement("option_borderless", HasFlag(InfoFlags.BorderlessWindow)),
                new XElement("option_windows_save_location", HasFlagAsInt(InfoFlags.UseAppDataSaveLocation)),
                new XElement("option_windows_texture_page", EstimateTexPageSize())
            )
        )
    );

    // for appending shit
    var OptionsNode = gmx.Element("Config").Element("Options");

    // Runner Data and shit
    if (File.Exists(Runner))
    {
        OptionsNode.Add(
            new XElement("option_windows_company_info", rCompany),
            new XElement("option_windows_copyright_info", rCopyright),
            new XElement("option_windows_description_info", rDescription),
            new XElement("option_windows_product_info", rProduct),
            new XElement("option_windows_game_icon", "runner_icon.ico")
        );
        if (WinIcon != null) WinIcon.Write($"{ConfigDir}/runner_icon.ico");
        if (BigIcon != null) BigIcon.Write($"{ConfigDir}/Runner_Icon_256.ico");
    }

    // splash screen handling
    if (File.Exists($"{projFolder}/splash.png"))
    {
        OptionsNode.Add(
            new XElement("option_windows_splash_screen", "Configs\\Default\\windows\\splash.png"),
            new XElement("option_windows_use_splash", 1)
        );
        File.Copy($"{projFolder}/splash.png", $"{ConfigDir}/splash.png");
    }
    else
        OptionsNode.Add(new XElement("option_windows_use_splash", 0));

    // add steam id if enabled
    if (HasFlag(InfoFlags.SteamEnabled))
    {
        OptionsNode.Add(
            new XElement("option_windows_enable_steam", true),
            new XElement("option_windows_steam_app_id", Data.GeneralInfo.SteamAppID)
        );
    }

    // constants and shit
    foreach (UndertaleOptions.Constant con in Data.Options.Constants)
    {
        string conStr = con.Name.Content;
        var conVal = con.Value.Content;

        if (conStr.Contains("SleepMargin"))
            OptionsNode.Add(new XElement("option_windows_sleep_margin", Int32.Parse(conVal)));

        // TODO - This exists, but idk the XML element name
        //if (conStr.Contains("DrawColour"))
        //    OptionsNode.Add(new XElement("option_draw_colour", UInt32.Parse(conVal)));
    }

    // i gotta man
    OptionsNode.Add(new XElement("option_information",
        $@"This is a Decompilation of {Data.GeneralInfo.DisplayName.Content}

--------------------------------------------------------
Project Decompiled by Ultimate_GMS1_Decompiler_v4.csx
	Improved by burnedpopcorn180
		Original Version by cubeww and CST1229"));

    File.WriteAllText($"{projFolder}/Configs/Default.config.gmx", GMXToString(gmx));
}
#endregion
#region DataFiles
void AddDatafiles(XElement element, string filepath)
{
    bool FileIsBlacklisted(string file) => new[] { ".dll", ".exe", ".ini", ".win", ".unx", ".droid", ".ios", ".dat", ".mp3", ".ogg", ".wav" }.Contains(Path.GetExtension(file).ToLower());

    #region Setup

    // get relative directory name for xml
    string outputPath = Path.Combine($"{projFolder}/datafiles", Path.GetRelativePath(GetFolder(FilePath), filepath));
    var dirName = new DirectoryInfo(outputPath).Name;

    // Skip export folder
    if (dirName == $"{GameName}.gmx") return;

    // start adding folder to xml
    element.Add(
        new XElement("datafiles", 
            new XAttribute("name", dirName)// name of folder
        )
    );
    // save element pointer for files
    var DatafileXML = element.Element("datafiles");
    #endregion

    // Copy and Record all Files
    int TotalFiles = 0;
    foreach (var file in Directory.GetFiles(filepath))
    {
        #region Copy Files

        if (FileIsBlacklisted(file)) continue;

        string relativePath = Path.GetRelativePath(GetFolder(FilePath), file);
        string destinationFile = Path.Combine(projFolder + "/datafiles", relativePath);

        // Skip export folder
        if (Path.GetDirectoryName(file).Contains($"{GameName}.gmx")) continue;

        // add file
        TotalFiles++;

        // Ensure it exists
        Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));

        // Copy the file
        File.Copy(file, destinationFile, true);

        #endregion
        #region Add Files to XML

        string FileName = Path.GetFileName(file);
        long FileSize = new FileInfo(file).Length;

        DatafileXML.Add(
            new XElement("datafile",
                new XElement("name", FileName),
                new XElement("exists", -1),
                new XElement("size", FileSize),
                new XElement("exportAction", 2),
                new XElement("exportDir", ""), //empty cuz yeah
                new XElement("overwrite", 0),
                new XElement("freeData", -1),
                new XElement("removeEnd", 0),
                new XElement("store", 0),
                // im skipping config options, fuck off
                new XElement("filename", FileName) // basically just copy of name
            )
        );

        #endregion
    }
    DatafileXML.Add(new XAttribute("number", TotalFiles));

    // Search Sub-Directories
    foreach (var dir in Directory.GetDirectories(filepath))
    {
        // Skip export folder
        if (Path.GetDirectoryName(dir).Contains($"{GameName}.gmx")) continue;

        // recursive function no way
        AddDatafiles(DatafileXML, dir);
    }
}
#endregion

#endregion

#region Project GMX Functions
void AddAssetToProjectGMX(string assetName, string elementName, string fileExtension = "")
{
    // idk man
    string resourceName = elementName.TrimEnd('s');
    string attributeName = elementName switch
    {
        "sounds" or "backgrounds" => resourceName,
        _ => elementName,
    };

    // check if main category exists in project gmx first
    XElement resourcesNode = 
        ProjectGMXAssets.Element(elementName) is null // check if main category exists in project gmx first
        ? new XElement(elementName, new XAttribute("name", attributeName)) // if not, create new
        : ProjectGMXAssets.Element(elementName); // else get current list

    // add asset
    resourcesNode.Add(new XElement(resourceName, $"{attributeName}\\{assetName}{fileExtension}"));

    // add entire category if its hasn't yet
    if (ProjectGMXAssets.Element(elementName) is null)
        ProjectGMXAssets.Add(resourcesNode);
}

void AddExtensionsToProjectGMX()
{
    if (Data.Extensions.Count > 0)
    {
        XElement extXML = new("NewExtensions");
        int extindex = 0;

        foreach (UndertaleExtension e in Data.Extensions)
        {
            extXML.Add(new XElement("extension", new XAttribute("index", extindex), $"extensions\\{e.Name.Content}"));
            extindex++;
        }

        ProjectGMXAssets.Add(extXML);
    }
}
#endregion