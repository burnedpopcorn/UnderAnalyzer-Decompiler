/*
    Exports Sprites as a GIF, but without an external DLL

    Script made by burnedpopcorn180, with parts based off of ExportSpritesAsGIFDLL.csx by CST1229
	
	older versions of UTMT won't work, because of new TextureWorker and ImageMagick
 */

using System.Drawing;
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

string filter = SimpleTextInput("Filter Sprites", "String that the sprite names must start with (or leave blank to export all):", "", false);
await ExtractSprites(folder, filter);
ScriptMessage($"Sprite GIFs exported to: {folder}");

private async Task ExtractSprites(string folder, string prefix)
{
    TextureWorker worker = new TextureWorker();
    try
    {
        IList<UndertaleSprite> sprites = Data.Sprites;
        if (prefix != "")
        {
			// init list with all desired sprites to be exported
            sprites = new List<UndertaleSprite> { };
            foreach (UndertaleSprite sprite in Data.Sprites)
            {
                if (sprite.Name.Content.StartsWith(prefix))
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
        MagickImage frame = new MagickImage(MagickColors.Transparent, 
            (int)sprite.Textures[i].Texture.BoundingWidth, 
            (int)sprite.Textures[i].Texture.BoundingHeight);
        
        // Get texture for this frame
        IMagickImage<byte> image = worker.GetTextureFor(sprite.Textures[i].Texture, sprite.Name.Content, true);
        MagickImage spriteFrame = new MagickImage(image);
        frame.Composite(spriteFrame, CompositeOperator.Over);

        // Set the playback speed
        frame.AnimationDelay = (int)(sprite.GMS2PlaybackSpeed);

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