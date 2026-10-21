// Patches Nemurimouto's Assembly-CSharp.dll: unlocks gallery, talents, shop and the activation code.
// Usage: NemurimoutoMod <Assembly-CSharp.dll> <output.dll>
//    or: NemurimoutoMod   (run from the game folder: backs up to .bak once, then patches in place)
using Mono.Cecil;
using Mono.Cecil.Cil;

string input, output;
if (args.Length == 2) (input, output) = (args[0], args[1]);
else if (args.Length == 0)
{
    output = Path.Combine(AppContext.BaseDirectory, "Nemurimouto_Data", "Managed", "Assembly-CSharp.dll");
    if (!File.Exists(output)) { Console.WriteLine($"Khong tim thay {output}\nHay dat file nay vao thu muc co Nemurimouto.exe."); Pause(); return 1; }
    input = output + ".bak";
    if (!File.Exists(input)) File.Copy(output, input);  // keep the original; re-runs always patch from it
}
else { Console.WriteLine("Usage: NemurimoutoMod [<Assembly-CSharp.dll> <output.dll>]"); return 1; }

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(input))!);
using var asm = AssemblyDefinition.ReadAssembly(input, new ReaderParameters { AssemblyResolver = resolver });
var module = asm.MainModule;

MethodDefinition Find(string type, string method) =>
    module.GetType(type)?.Methods.SingleOrDefault(m => m.Name == method)
    ?? throw new Exception($"Not found: {type}.{method} (game version changed?)");

void ReturnConst(string type, string method, int value)
{
    var body = Find(type, method).Body;
    body.Instructions.Clear();
    body.ExceptionHandlers.Clear();
    body.Variables.Clear();
    var il = body.GetILProcessor();
    il.Emit(OpCodes.Ldc_I4, value);
    il.Emit(OpCodes.Ret);
    Console.WriteLine($"  {type}.{method} -> {value}");
}

// Gallery: every scene counts as seen.
ReturnConst("GalleryPlaceholderButton", "IsUnlockedFor", 1);
// Talents: earned points, not get_Points — TalentState.Normalize trims learned ranks down to Earned.
ReturnConst("Nemurimouto.Talents.TalentState", "get_Earned", 999);
// Shop: items that need a talent are always listed.
ReturnConst("Nemurimouto.Items.ShopStockRules", "Unlocked", 1);
// Money: always 99,999,999; payouts check against int.MaxValue so this stays far below the cap.
ReturnConst("GameManager", "get_Money", 99_999_999);

// Love/Lust: Evaluate clamps the stored total into the current level's band; feed it the maximum
// (5680) so each stat sits at its next threshold and the next relationship event is always ready.
var evaluate = Find("Nemurimouto.Stats.RelationshipStatLevels", "Evaluate");
var clamp = evaluate.Body.Instructions.Single(i => i.OpCode == OpCodes.Call && ((MethodReference)i.Operand).Name == "ClampTotal");
if (clamp.Previous.OpCode != OpCodes.Ldarg_0) throw new Exception("RelationshipStatLevels.Evaluate has an unexpected shape");
evaluate.Body.GetILProcessor().Replace(clamp.Previous, Instruction.Create(OpCodes.Ldc_R4, 5680f));
Console.WriteLine("  RelationshipStatLevels.Evaluate -> total 5680");

// Activation code: Activate(code) compares code.ToUpper() with "FROG123"; feed it the code itself.
var activate = Find("SettingsManager", "Activate");
var first = activate.Body.Instructions[0];
if (first.OpCode != OpCodes.Ldarg_1) throw new Exception("SettingsManager.Activate has an unexpected shape");
activate.Body.GetILProcessor().Replace(first, Instruction.Create(OpCodes.Ldstr, "FROG123"));
Console.WriteLine("  SettingsManager.Activate -> always FROG123");

asm.Write(output);
Console.WriteLine($"Wrote {output}");
if (args.Length == 0) { Console.WriteLine("Xong! Mo game de choi."); Pause(); }
return 0;

static void Pause() { Console.WriteLine("Nhan Enter de dong..."); Console.ReadLine(); }
