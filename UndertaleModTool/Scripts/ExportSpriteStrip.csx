/*
	ExportSpriteStrip.csx
		by burnedpopcorn180, with parts based off of ExportSpritesAsGIFDLL.csx by CST1229
	
    Exports Sprite Frames into a Single Sprite Strip
	older versions of UTMT won't work, because of new TextureWorker and ImageMagick
 */

using System.Drawing;
using System.Windows.Forms;

using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;
using ImageMagick;

EnsureDataLoaded();

ScriptMessage("Please Select an Output Directory");
string folder = PromptChooseDirectory();
if (folder == null)
    return;
folder = Path.GetDirectoryName(folder);

List<string> Selected_Sprites = new List<string>();
public bool okbt_pressed = false;

#region UI

var form2 = new Form()
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
    form2.Close();
};
form2.Controls.Add(OKBT);

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
form2.Controls.Add(ADBT);

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
form2.Controls.Add(RMBT);

// add them
form2.Controls.Add(treeView);
form2.Controls.Add(searchbar);
form2.Controls.Add(listbox);
form2.Controls.Add(searchbar2);

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
form2.AcceptButton = OKBT;
form2.ShowDialog();

#endregion

// if OK Button was pressed, continue
// else if X button was pressed, exit
if (okbt_pressed)
    await ExtractSprites(folder, Selected_Sprites);
else
    return;

private async Task ExtractSprites(string folder, List<string> Selected_Sprites)
{
    TextureWorker worker = new TextureWorker();
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

        SetProgressBar(null, "Exporting Sprites to Sprite Strips...", 0, sprites.Count);
        StartProgressBarUpdater();
		
		// run the actual exporting function
        await Task.Run(() => Parallel.ForEach(sprites, (sprite) => {
            IncrementProgressParallel();
            ExtractSprite(sprite, folder, worker);
        }));
		
        await StopProgressBarUpdater();
        HideProgressBar();
		
		ScriptMessage($"Sprite Strips exported to: \n{folder}");
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
	// get initial frame width
    int initWidth = (int)sprite.Textures[0].Texture.BoundingWidth;
    
    // get the final width of the sprite strip
    int totalWidth = initWidth * sprite.Textures.Count;  // original frame times the amount of all frames
	
	// get the final height of the sprite strip
    int totalHeight = (int)sprite.Textures[0].Texture.BoundingHeight; // this doesn't/shouldn't change
    
	// make image
    MagickImage spriteStrip = new MagickImage(MagickColors.Transparent, totalWidth, totalHeight);
    
    // add all frames
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        // get texture for this frame
        IMagickImage<byte> image = worker.GetTextureFor(sprite.Textures[i].Texture, sprite.Name.Content, true);
        MagickImage spriteFrame = new MagickImage(image);
        
        // set the position of this frame
        int xOffset = i * initWidth;
		// add Frame to PNG
        spriteStrip.Composite(spriteFrame, xOffset, 0, CompositeOperator.Over);
    }

    // Save as PNG
    string spriteStripPath = $"{folder}/{sprite.Name.Content}_strip{sprite.Textures.Count}.png";
    spriteStrip.Write(spriteStripPath);
}