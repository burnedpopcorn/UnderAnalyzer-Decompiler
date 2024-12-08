// Exports a list of asset names in a data file in order.
// Originally Made by Grossley and colinator27.
// Improved by burnedpopcorn180

// Should work for both UTMT and UnderAnalyzer (GUI)

using System.Text;
using System;
using System.IO;

EnsureDataLoaded();

// Get the path, and check for overwriting
string outputPath = Path.Combine(Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar, "Asset_List.txt");
if (File.Exists(outputPath))
{
    bool overwriteCheck = ScriptQuestion(@"An 'Asset_List.txt' file already exists. 
Would you like to overwrite it?");
    if (overwriteCheck)
        File.Delete(outputPath);
    else
    {
        ScriptError("An 'Asset_List.txt' file already exists. Please remove it and try again.", "Error: Export already exists.");
        return;
    }
}

using (StreamWriter writer = new StreamWriter(outputPath))
{
	writer.WriteLine("BetterExportAssetOrder");
	writer.WriteLine("Improved by burnedpopcorn180");
	writer.WriteLine("Originally made by Grossley and colinator27");
	writer.WriteLine("");
	writer.WriteLine("Assets Found:");
	writer.WriteLine("");
	writer.WriteLine("Sounds: " + Data.Sounds.Count);
	writer.WriteLine("Sprites: " + Data.Sprites.Count);
	writer.WriteLine("Backgrounds: " + Data.Backgrounds.Count);
	writer.WriteLine("Paths: " + Data.Paths.Count);
	writer.WriteLine("Scripts: " + Data.Scripts.Count);
	writer.WriteLine("Fonts: " + Data.Fonts.Count);
	writer.WriteLine("Objects: " + Data.GameObjects.Count);
	writer.WriteLine("Timelines: " + Data.Timelines.Count);
	writer.WriteLine("Rooms: " + Data.Rooms.Count);
	writer.WriteLine("Shaders: " + Data.Shaders.Count);
	writer.WriteLine("Extensions: " + Data.Extensions.Count);
	writer.WriteLine("");

    // Write Sounds.
    writer.WriteLine("--------------------- SOUNDS ---------------------");
    if (Data.Sounds.Count > 0) 
    {
	var resourcecount = 0;
        foreach (UndertaleSound sound in Data.Sounds) {
            writer.WriteLine(resourcecount + " - " + sound.Name.Content);
			++resourcecount;
		}
    }
	else if (Data.Sounds.Count == 0) {
		writer.WriteLine("No Sounds could be Found");
	}
    // Write Sprites.
    writer.WriteLine("--------------------- SPRITES ---------------------");
    if (Data.Sprites.Count > 0) 
    {
	var resourcecount = 0;
        foreach (var sprite in Data.Sprites) {
            writer.WriteLine(resourcecount + " - " + sprite.Name.Content);
			++resourcecount;
		}
    }
    else if (Data.Sprites.Count == 0) {
		writer.WriteLine("No Sprites could be Found");
	}
    // Write Backgrounds.
    writer.WriteLine("------------------- BACKGROUNDS -------------------");
    if (Data.Backgrounds.Count > 0)
    {
	var resourcecount = 0;
        foreach (var background in Data.Backgrounds) {
            writer.WriteLine(resourcecount + " - " + background.Name.Content);
			++resourcecount;
		}
    }
    else if (Data.Backgrounds.Count == 0) {
		writer.WriteLine("No Backgrounds could be Found");
	}
    // Write Paths.
    writer.WriteLine("---------------------- PATHS ----------------------");
    if (Data.Paths.Count > 0) 
    {
	var resourcecount = 0;
        foreach (UndertalePath path in Data.Paths) {
            writer.WriteLine(resourcecount + " - " + path.Name.Content);
			++resourcecount;
		}
    }
    else if (Data.Paths.Count == 0) {
		writer.WriteLine("No Paths could be Found");
	}
    // Write Scripts.
    writer.WriteLine("--------------------- SCRIPTS ---------------------");
    if (Data.Scripts.Count > 0) 
    {
	var resourcecount = 0;
        foreach (UndertaleScript script in Data.Scripts) {
            writer.WriteLine(resourcecount + " - " + script.Name.Content);
			++resourcecount;
		}
    }
    else if (Data.Scripts.Count == 0) {
		writer.WriteLine("No Scripts could be Found");
	}
    // Write Fonts.
    writer.WriteLine("---------------------- FONTS ----------------------");
    if (Data.Fonts.Count > 0) 
    {
	var resourcecount = 0;
        foreach (UndertaleFont font in Data.Fonts) {
            writer.WriteLine(resourcecount + " - " + font.Name.Content);
			++resourcecount;
		}
    }
	else if (Data.Fonts.Count == 0) {
		writer.WriteLine("No Fonts could be Found");
	}
    // Write Objects.
    writer.WriteLine("--------------------- OBJECTS ---------------------");
    if (Data.GameObjects.Count > 0) 
    {
	var resourcecount = 0;
        foreach (UndertaleGameObject gameObject in Data.GameObjects) {
            writer.WriteLine(resourcecount + " - " + gameObject.Name.Content);
			++resourcecount;
		}
    }
    else if (Data.GameObjects.Count == 0) {
		writer.WriteLine("No Objects could be Found");
	}
    // Write Timelines.
    writer.WriteLine("-------------------- TIMELINES --------------------");
    if (Data.Timelines.Count > 0)
    {
	var resourcecount = 0;
        foreach (UndertaleTimeline timeline in Data.Timelines) {
            writer.WriteLine(resourcecount + " - " + timeline.Name.Content);
			++resourcecount;
		}
    }
	else if (Data.Timelines.Count == 0) {
		writer.WriteLine("No Timelines could be Found");
	}
    // Write Rooms.
    writer.WriteLine("---------------------- ROOMS ----------------------");
    if (Data.Rooms.Count > 0)
    {
	var resourcecount = 0;
        foreach (UndertaleRoom room in Data.Rooms) {
            writer.WriteLine(resourcecount + " - " + room.Name.Content);
			++resourcecount;
		}
    }
	else if (Data.Rooms.Count == 0) {
		writer.WriteLine("No Rooms could be Found");
	}
    // Write Shaders.
    writer.WriteLine("--------------------- SHADERS ---------------------");
    if (Data.Shaders.Count > 0)
    {
	var resourcecount = 0;
        foreach (UndertaleShader shader in Data.Shaders) {
            writer.WriteLine(resourcecount + " - " + shader.Name.Content);
			++resourcecount;
		}
    }
	else if (Data.Shaders.Count == 0) {
		writer.WriteLine("No Shaders could be Found");
	}
    // Write Extensions.
    writer.WriteLine("-------------------- EXTENSIONS --------------------");
    if (Data.Extensions.Count > 0) 
    {
	var resourcecount = 0;
        foreach (UndertaleExtension extension in Data.Extensions) {
            writer.WriteLine(resourcecount + " - " + extension.Name.Content);
			++resourcecount;
		}
    }
	else if (Data.Extensions.Count == 0) {
		writer.WriteLine("No Extensions could be Found");
	}
    // TODO: Perhaps detect GMS2.3, export those asset names as well.
}
