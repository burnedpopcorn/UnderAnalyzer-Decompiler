using Underanalyzer;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.AST;
using Underanalyzer.Decompiler.GameSpecific;
using Underanalyzer.Decompiler.ControlFlow;
using System.Threading.Tasks;
using System.Collections.Generic;

EnsureDataLoaded();
string codePath = Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar + "scr_enums.gml";

GlobalDecompileContext globalDecompileContext = new(Data);
DecompileSettings decompilerSettings = new DecompileSettings();
decompilerSettings.MacroDeclarationsAtTop = true;
decompilerSettings.CreateEnumDeclarations = true;
string enumName = Data.ToolInfo.DecompilerSettings.UnknownEnumName;
decompilerSettings.UnknownEnumName = enumName;
decompilerSettings.UnknownEnumValuePattern = Data.ToolInfo.DecompilerSettings.UnknownEnumValuePattern;

HashSet<long> values = new HashSet<long>();

List<UndertaleCode> toDump = new();
foreach (UndertaleCode code in Data.Code) {
	if (code.ParentEntry is null) {
		toDump.Add(code);
	}
}

SetProgressBar(null, "Decompiling code entries...", 0, toDump.Count);
StartProgressBarUpdater();

await DumpCode();

await StopProgressBarUpdater();
HideProgressBar();

// https://github.com/UnderminersTeam/Underanalyzer/blob/main/Underanalyzer/Decompiler/AST/Nodes/EnumDeclNode.cs
List<long> sorted = new List<long>(values);
sorted.Sort((a, b) => Math.Sign(a - b));

string code = "enum " + enumName + " {\n";
long expectedValue = 0;
foreach (long val in sorted) {
	string name = string.Format(decompilerSettings.UnknownEnumValuePattern, val.ToString().Replace("-", "m"));
	if (val == expectedValue) {
		code += "	" + name + ",\n";
		if (expectedValue != long.MaxValue) {
			expectedValue++;
		}
	} else {
		code += "	" + name + " = " + val.ToString() + ",\n";
		if (expectedValue != long.MaxValue) {
			expectedValue = val + 1;
		} else {
			expectedValue = val;
		}
	}
}
code += "}";
File.WriteAllText(codePath, code);
ScriptMessage("Exported to: " + codePath);


async Task DumpCode()
{
	if (Data.GlobalFunctions is null) {
		SetProgressBar(null, "Building the cache of all global functions...", 0, 0);
		await Task.Run(() => GlobalDecompileContext.BuildGlobalFunctionCache(Data));
		SetProgressBar(null, "Code Entries", 0, toDump.Count);
	}
	await Task.Run(() => Parallel.ForEach(toDump, DumpCode));
}


void DumpCode(UndertaleCode code)
{
    if (code is not null)
    {
        try
        {
			if (code != null) {
				var context = new DecompileContext(globalDecompileContext, code, decompilerSettings);
				BlockNode rootBlock = (BlockNode)context.DecompileToAST();
				foreach (IStatementNode stmt in rootBlock.Children) {
					if (stmt is EnumDeclNode decl && decl.Enum.Name == enumName) {
						foreach (GMEnumValue val in decl.Enum.Values) {
							values.Add(val.Value);
						}
					}
				}
			}
        }
        catch (Exception e)
        {
			ScriptMessage("Error while decompiling " + code.Name.Content + ": " + e.ToString());
        }
    }

    IncrementProgressParallel();
}