using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public static class Illness
{  
  [DllImport ("libillness.so", EntryPoint="verify_method")]
  internal static extern void NativeVerify(IntPtr m);
  
  [DllImport ("libillness.so", EntryPoint="disassemble_method_native")]
  internal static extern void DisassembleMethodNative(IntPtr m);
  
  [DllImport ("libillness.so", EntryPoint="disassemble_method_il_old")]
  internal static extern void DisassembleMethodIL_Old(IntPtr m);
  
  [DllImport ("libillness.so", EntryPoint="disassemble_method_il_new")]
  internal static extern void DisassembleMethodIL_New(IntPtr m);
  
  [DllImport ("libillness.so", EntryPoint="disassemble_method_il")]
  internal static extern void DisassembleMethodIL(IntPtr m);
    
  internal static Type GetInternalType(string name)
  {
    foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
    {
      var t = a.GetType(name);
      if (t != null)
        return t;
    }

    return null;
  }
  
  static FieldInfo monoMethodMhandleField = 
    Illness.GetInternalType("System.Reflection.MonoMethod").
      GetField("mhandle", BindingFlags.NonPublic |
        BindingFlags.Instance);
                                    
  static FieldInfo dynamicMethodMhandleField =
    typeof(DynamicMethod).
      GetField("mhandle", BindingFlags.NonPublic |
        BindingFlags.Instance);
        
  static IntPtr GetNativePtr(MethodInfo m)
  {
    if(m == null)
      throw new ArgumentNullException("m");
    if(m is DynamicMethod)
    {
      var rmh = (RuntimeMethodHandle)dynamicMethodMhandleField.GetValue(m);
      if(rmh.Value == IntPtr.Zero)
        throw new Exception(string.Format("DynamicMethod {0} has not been compiled. Call CreateDelegate first.", m));
      return rmh.Value;
    }
    else
    {
      return (IntPtr)monoMethodMhandleField.GetValue(m);
    }
  }
  
  public static void Disassemble(MethodInfo m)
  {
    // TODO crashes if m does not verify, verify first
    IntPtr mhandle = GetNativePtr(m);
    DisassembleMethodNative(mhandle);
  }
  
  public static void DisassembleIL_New(MethodInfo m)
  {
    IntPtr mhandle = GetNativePtr(m);
    DisassembleMethodIL_New(mhandle);
  }
  
  public static void DisassembleIL_Old(MethodInfo m)
  {
    IntPtr mhandle = GetNativePtr(m);
    DisassembleMethodIL_Old(mhandle);
  }
  
  public static void DisassembleIL(MethodInfo m)
  {
    IntPtr mhandle = GetNativePtr(m);
    DisassembleMethodIL(mhandle);
  }
  
  public static void Verify(MethodInfo m)
  {
    IntPtr mhandle = GetNativePtr(m);
    Illness.NativeVerify(mhandle);
  }
}