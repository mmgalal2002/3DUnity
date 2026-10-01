using System;
using System.Runtime.InteropServices;

namespace RoomStudio {
public static class FloorPlanFilePicker {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
 // Keep this ABI structure blittable. In Mono, Marshal.SizeOf on a class with
 // its own SizeOf field initializer can recursively construct that class.
 [StructLayout(LayoutKind.Sequential)] struct OpenFileName {
  public int structSize;
  public IntPtr owner,instance;
  public IntPtr filter,customFilter;
  public int maxCustomFilter,filterIndex;
  public IntPtr file;
  public int maxFile;
  public IntPtr fileTitle;public int maxFileTitle;
  public IntPtr initialDirectory,title;
  public int flags;
  public short fileOffset,fileExtension;public IntPtr defaultExtension;
  public IntPtr customData,hook,templateName;
  public IntPtr reserved;public int reservedInt,flagsEx;
 }
 [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool GetOpenFileNameW(ref OpenFileName data);
 [DllImport("comdlg32.dll")] static extern uint CommDlgExtendedError();
 [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
#endif
 public static string Open() {
#if UNITY_EDITOR
  return UnityEditor.EditorUtility.OpenFilePanel("Choose a floor-plan guide","","");
#elif UNITY_STANDALONE_WIN
  var data=new OpenFileName{structSize=Marshal.SizeOf(typeof(OpenFileName)),owner=GetActiveWindow(),filterIndex=1,maxFile=32768,
   flags=0x00080000|0x00001000|0x00000800|0x00000008|0x02000000};
  try {
   data.filter=Marshal.StringToHGlobalUni("Floor plans (PNG, JPEG, JSON)\0*.png;*.jpg;*.jpeg;*.json\0\0");
   data.title=Marshal.StringToHGlobalUni("Choose a floor-plan guide");
   data.file=Marshal.AllocHGlobal(data.maxFile*2);Marshal.WriteInt16(data.file,0);
   if(GetOpenFileNameW(ref data))return Marshal.PtrToStringUni(data.file);
   uint error=CommDlgExtendedError();if(error!=0)throw new Exception("Windows file picker failed ("+error+"). Use Load from path.");return null;
  } finally {
   if(data.filter!=IntPtr.Zero)Marshal.FreeHGlobal(data.filter);
   if(data.title!=IntPtr.Zero)Marshal.FreeHGlobal(data.title);
   if(data.file!=IntPtr.Zero)Marshal.FreeHGlobal(data.file);
  }
#else
  throw new PlatformNotSupportedException("Use Load from path on this platform.");
#endif
 }
}
}
