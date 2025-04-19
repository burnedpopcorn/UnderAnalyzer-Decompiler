/*
    BetterExportSpritesAsGIF.csx
		by burnedpopcorn180, with parts based off of ExportSpritesAsGIFDLL.csx by CST1229

    Exports Sprites as a GIF, but without an external DLL, and with a handy dandy UI
	older versions of UTMT won't work, because of new TextureWorker and ImageMagick
 */

// For UI
using System.Drawing;
using System.Windows.Forms;

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using ImageMagick;

EnsureDataLoaded();
TextureWorker worker = new TextureWorker();

ScriptMessage("Please Select an Output Directory");
string folder = PromptChooseDirectory();
if (folder == null)
    return;
folder = Path.GetDirectoryName(folder);

List<string> Selected_Sprites = new List<string>();
public bool okbt_pressed = false;
public bool custom_fpsENABLED = true;
public int custom_fps = 24;

private int currentFrameIndex = 0;

#region Main UI

Form MAINFORM = new Form()
{
    AutoSize = true,
    Text = "Sprite Selector",
    MaximizeBox = false,
    MinimizeBox = false,
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedDialog,
};

// tree view
var treeView = new TreeView()
{
    Location = new System.Drawing.Point(4, 24 + 4 + 4),
    Height = 400,
    Width = 200,
};

treeView.BeginUpdate();
treeView.Nodes.Add("Sprites").Expand();

UpdateTree(treeView, Data, "");
treeView.EndUpdate();

// search bar
var searchbar = new TextBox()
{
    Location = new System.Drawing.Point(4, 4),
    Width = 200,
};
searchbar.TextChanged += (o, s) =>
{
    treeView.BeginUpdate();
    UpdateTree(treeView, Data, searchbar.Text);
    treeView.EndUpdate();
};

// list box
var listbox = new ListBox()
{
    Location = new System.Drawing.Point(300, 24 + 4 + 4),
    Height = 400,
    Width = 200,
    HorizontalScrollbar = true
};
UpdateList(listbox, "");

// search bar 2
var searchbar2 = new TextBox()
{
    Location = new System.Drawing.Point(300, 4),
    Width = 200,
};
searchbar2.TextChanged += (o, s) =>
{
    listbox.BeginUpdate();
    UpdateList(listbox, searchbar2.Text);
    listbox.EndUpdate();
};

// double click left side
treeView.NodeMouseDoubleClick += (s, e) =>
{
    if (!Selected_Sprites.Contains(e.Node.Text) && e.Node.Level == 1)
        Selected_Sprites.Add(e.Node.Text);
    UpdateList(listbox, searchbar2.Text);
};
listbox.DoubleClick += (s, e) =>
{
    Selected_Sprites.RemoveAll(r => r == listbox.SelectedItem);
    UpdateList(listbox, searchbar2.Text);
};

// ok
var OKBT = new Button()
{
    Location = new System.Drawing.Point(215, 180),
    Text = "OK",
    Height = 32
};
OKBT.Click += (o, s) => {
    okbt_pressed = true;
    MAINFORM.Close();
};
MAINFORM.Controls.Add(OKBT);

// >
var ADBT = new Button()
{
    Location = new System.Drawing.Point(215, 140),
    Text = "->",
    Height = 32
};
ADBT.Click += (o, s) =>
{
    if (!Selected_Sprites.Contains(treeView.SelectedNode.Text))
        Selected_Sprites.Add(treeView.SelectedNode.Text);
    UpdateList(listbox, searchbar2.Text);
};
MAINFORM.Controls.Add(ADBT);

// <
var RMBT = new Button()
{
    Location = new System.Drawing.Point(215, 220),
    Text = "<-",
    Height = 32
};
RMBT.Click += (o, s) =>
{
    Selected_Sprites.RemoveAll(r => r == listbox.SelectedItem);
    UpdateList(listbox, searchbar2.Text);
};
MAINFORM.Controls.Add(RMBT);

// add them
MAINFORM.Controls.Add(treeView);
MAINFORM.Controls.Add(searchbar);
MAINFORM.Controls.Add(listbox);
MAINFORM.Controls.Add(searchbar2);

void UpdateList(ListBox list, string search)
{
    list.Items.Clear();
    foreach (var i in Selected_Sprites)
    {
        if (i.Contains(search))
            list.Items.Add(i);
    }
}
void UpdateTree(TreeView tree, UndertaleData data, string search)
{
    foreach (TreeNode i in tree.Nodes)
        i.Nodes.Clear();

    // sprites
    foreach (var i in data.Sprites)
    {
        if (i.Name.Content.Contains(search))
            tree.Nodes[0].Nodes.Add(new TreeNode(i.Name.Content));
    }
}

// FPS Stuff
var fpsTrackBar = new TrackBar()
{
    Minimum = 1, // Minimum FPS
    Maximum = 60, // Maximum FPS
    TickFrequency = 1,
    Value = custom_fps, // Set initial value to 24
    Location = new Point(150, 500),
    Width = 200,
};
fpsTrackBar.Scroll += FpsTrackBar_Scroll;

var fpsLabel = new Label()
{
    Text = $"FPS: {custom_fps}",
    Location = new Point(230, 475),
    Width = 100,
};

void FpsTrackBar_Scroll(object o, EventArgs e)
{
    custom_fps = fpsTrackBar.Value;
    fpsLabel.Text = $"FPS: {custom_fps}";
}
MAINFORM.Controls.Add(fpsTrackBar);
MAINFORM.Controls.Add(fpsLabel);

// Create Checkbox
var FPSBox = new CheckBox()
{
    Text = "Use In-Game FPS",
    Location = new Point(200, 440),
    AutoSize = true,
};

// CheckBox to use Custom FPS or In-Game FPS
FPSBox.CheckedChanged += (o, e) =>
{
    custom_fpsENABLED = FPSBox.Checked;

    // Enable/Disable other stuff
    fpsTrackBar.Enabled = !custom_fpsENABLED;
    fpsLabel.Enabled = !custom_fpsENABLED;
};
MAINFORM.Controls.Add(FPSBox);

// Initialize
MAINFORM.AcceptButton = OKBT;
MAINFORM.ShowDialog();

#endregion

// if OK Button was pressed, continue
// else if X button was pressed, exit
if (okbt_pressed)
    await ExtractSprites(folder, Selected_Sprites);
else
    return;

private async Task ExtractSprites(string folder, List<string> Selected_Sprites)
{
    //TextureWorker worker = new TextureWorker();
    try
    {
        IList<UndertaleSprite> sprites = Data.Sprites;
        if (Selected_Sprites.Count > 0)
        {
            // init list with all desired sprites to be exported
            sprites = new List<UndertaleSprite> { };
            foreach (UndertaleSprite sprite in Data.Sprites)
            {
                if (Selected_Sprites.Contains(sprite.Name.Content))
                {
                    // if sprite exists, add to list that will be exported
                    sprites.Add(sprite);
                }
            }
        }

        SetProgressBar(null, "Exporting Sprites to GIF...", 0, sprites.Count);
        StartProgressBarUpdater();
		
		// run the actual exporting function
        await Task.Run(() => Parallel.ForEach(sprites, (sprite) => {
            IncrementProgressParallel();
            ExtractSprite(sprite, folder, worker);
        }));
		
        await StopProgressBarUpdater();
        HideProgressBar();

        ScriptMessage($"Sprite GIFs exported to: \n{folder}");
    }
    catch (Exception e)
    {
        throw;
    }
    finally
    {
		// for newer UTMT/UA Releases
		worker.Dispose();
    }
}

private void ExtractSprite(UndertaleSprite sprite, string folder, TextureWorker worker)
{
    // List to hold the frames
    List<MagickImage> frames = new List<MagickImage>();

    // For every frame in sprite
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        // Create a new blank frame
		// fucking why 0.7.0.0
		var settings = new MagickReadSettings
        {
            BackgroundColor = MagickColors.Transparent,
            Width = (uint)sprite.Textures[i].Texture.BoundingWidth,
            Height = (uint)sprite.Textures[i].Texture.BoundingHeight
        };
        MagickImage frame = new MagickImage("xc:none", settings);
        
        // Get texture for this frame
        IMagickImage<byte> image = worker.GetTextureFor(sprite.Textures[i].Texture, sprite.Name.Content, true);
        MagickImage spriteFrame = new MagickImage(image);
        frame.Composite(spriteFrame, CompositeOperator.Over);

        // Set the playback speed
        if (!custom_fpsENABLED)
            // If using In-Game Time
            frame.AnimationDelay = (uint)(100 / sprite.GMS2PlaybackSpeed);
        else
            // If using Custom Time
            frame.AnimationDelay = (uint)(100 / custom_fps);

        // Add the frame to the list
        frames.Add(frame);
    }

    // Create the GIF from the frames
    using (MagickImageCollection collection = new MagickImageCollection())
    {
        // Add all frames
        foreach (var frame in frames)
        {
            collection.Add(frame);
			
			// very important as it stops smearing/ghosting
			frame.GifDisposeMethod = GifDisposeMethod.Background;
        }
        
        // Save GIF
        collection.Write(folder + "/" + sprite.Name.Content + ".gif");
    }
}