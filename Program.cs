using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using System.Threading;
using System.Reflection.Emit;
using System.Numerics;

public static class Program
{  
 public static Type BuildMyType()
 {
  AppDomain myDomain = AppDomain.CurrentDomain;
  AssemblyName myAsmName = new AssemblyName();
  myAsmName.Name = "MyDynamicAssembly.dll";

  AssemblyBuilder myAsmBuilder = myDomain.DefineDynamicAssembly(
    myAsmName,
    AssemblyBuilderAccess.RunAndSave);
  ModuleBuilder myModBuilder = myAsmBuilder.DefineDynamicModule(
    "MyJumpTableDemo.dll");

  TypeBuilder myTypeBuilder = myModBuilder.DefineType("JumpTableDemo",
    TypeAttributes.Public);
  MethodBuilder myMthdBuilder = myTypeBuilder.DefineMethod("SwitchMe", 
   MethodAttributes.Public |
   MethodAttributes.Static,
   typeof(string), 
   new Type[] {typeof(int)});

  ILGenerator myIL = myMthdBuilder.GetILGenerator();

  Label defaultCase = myIL.DefineLabel(); 
  Label endOfMethod = myIL.DefineLabel(); 

  // We are initializing our jump table. Note that the labels
  // will be placed later using the MarkLabel method. 

  Label[] jumpTable = new Label[] { myIL.DefineLabel(),
    myIL.DefineLabel(),
    myIL.DefineLabel(),
    myIL.DefineLabel(),
    myIL.DefineLabel() };

  // arg0, the number we passed, is pushed onto the stack.
  // In this case, due to the design of the code sample,
  // the value pushed onto the stack happens to match the
  // index of the label (in IL terms, the index of the offset
  // in the jump table). If this is not the case, such as
  // when switching based on non-integer values, rules for the correspondence
  // between the possible case values and each index of the offsets
  // must be established outside of the ILGenerator.Emit calls,
  // much as a compiler would.

    myIL.Emit(OpCodes.Ldarg_0);
    myIL.Emit(OpCodes.Switch, jumpTable);

  // Branch on default case
    myIL.Emit(OpCodes.Br_S, defaultCase);

  // Case arg0 = 0
    myIL.MarkLabel(jumpTable[0]); 
    myIL.Emit(OpCodes.Ldstr, "are no bananas");
    myIL.Emit(OpCodes.Br_S, endOfMethod);

  // Case arg0 = 1
    myIL.MarkLabel(jumpTable[1]); 
    myIL.Emit(OpCodes.Ldstr, "is one banana");
    myIL.Emit(OpCodes.Br_S, endOfMethod);

  // Case arg0 = 2
    myIL.MarkLabel(jumpTable[2]); 
    myIL.Emit(OpCodes.Ldstr, "are two bananas");
    myIL.Emit(OpCodes.Br_S, endOfMethod);

  // Case arg0 = 3
    myIL.MarkLabel(jumpTable[3]); 
    myIL.Emit(OpCodes.Ldstr, "are three bananas");
    myIL.Emit(OpCodes.Br_S, endOfMethod);

  // Case arg0 = 4
    myIL.MarkLabel(jumpTable[4]); 
    myIL.Emit(OpCodes.Ldstr, "are four bananas");
    myIL.Emit(OpCodes.Br_S, endOfMethod);

  // Default case
    myIL.MarkLabel(defaultCase);
    myIL.Emit(OpCodes.Ldstr, "are many bananas");

    myIL.MarkLabel(endOfMethod);
    myIL.Emit(OpCodes.Ret);

    var ret = myTypeBuilder.CreateType();
    myAsmBuilder.Save("MyDynamicAssembly.dll");
    return ret;

  }
  
  public static double Prum(double b) {
    return b + b.ToString().Length;
  }
  
  public static double Factor = 8.6;
  
  
  // public static int AddIntegers(int a, int b) {
  //   return a + b;
  // }
  
  public static Vector3 JustZero()
  {
    return Vector3.Zero;
  }
  
  public static Vector3 AddVectors(Vector3 a, Vector3 b) {
    return a + b;
  }
  
  public static Vector3 AddVectorsAgain(Vector3 a, Vector3 b) {
    return AddVectors(a, b) + AddVectors(a, b) + JustZero();
  }
  
  public static string Process(string s)
  {
    return "~" + s + "!";
  }
  
  public static string Greet() {
    return Process("Hello, World!").Length.ToString();
  }
  
  public static void Loop(int n) {
    for(int i=0; i<n; i++) {
      Console.WriteLine("Hello {0}", i);
    }
  }
  
  public static void TryCatch(int n)
  {
    try
    {
      for(int i=0; i<n; i++)
      {
        Console.WriteLine("Hello {0}", i);
      }
    }
    catch(Exception e)
    {
      Console.WriteLine("Oops");
    }
  }
  
  public static void Main(string[] args)
  {
    
    var t = BuildMyType();
    var m = t.GetMethod("SwitchMe");
    // Console.WriteLine(m.GetType());
    // Illness.DisassembleIL(m);
    // Prum(8.4);
    // AddIntegers(9, 8);
    // AddIntegers(9, 8.4);
    // Illness.DisassembleIL_New(m);
    // Illness.DisassembleIL_New(typeof(Program).GetMethod("Greet"));
    // Illness.Disassemble(typeof(Program).GetMethod("Greet"));
    // Greet();
    // Illness.Disassemble(typeof(Program).GetMethod("Greet"));
    
    // Illness.DisassembleIL(m);
    // Illness.Verify(typeof(double).GetMethod("ToString", new Type[] { }));
    // Illness.Verify(m);
    // Illness.Verify(typeof(Program).GetMethod("AddVectors2"));
    
    DynamicMethod squareIt = new DynamicMethod(
        "SquareIt", 
        typeof(long), 
        new Type[] { typeof(int) },
        typeof(Program).Module);
    
    ILGenerator il = squareIt.GetILGenerator();
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Conv_I8);
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Conv_I8);
    il.Emit(OpCodes.Mul);
    il.Emit(OpCodes.Ret);
    
    squareIt.CreateDelegate(typeof(Func<int, long>));
    
    // Console.WriteLine(m.Invoke(null, new object [] { 1 }));
    
    Illness.DisassembleIL_New(m);
    // Illness.DisassembleIL_New(squareIt);
    // Console.WriteLine("OK");
    
    // var mm = t.GetMethod("SwitchMe");
    // var s = mm.Invoke(null, new object[]{ 1 });
    // Console.WriteLine(s);
    
    // Illness.Disassemble(typeof(Program).GetMethod("AddIntegers"));
    // Illness.Disassemble(m);
    

    // Illness.Verify(mm);
    // Illness.Disassemble(mm);
    // Illness.DisassembleMethod2("Program.exe", "", "Program", "Prum", 1);
  }
}