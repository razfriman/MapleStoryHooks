using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyHook;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace MapleStoryHooks
{
    public class Main : IEntryPoint
    {
        internal static MapleStoryHookInterface Interface;
        internal static List<LocalHook> hooks;

        #region Original Functions
        internal static DOutPacketInit OutPacketInitOriginal;
        internal static DEncodeByte EncodeByteOriginal;
        internal static DEncodeShort EncodeShortOriginal;
        internal static DEncodeInt EncodeIntOriginal;
        internal static DEncodeBuffer EncodeBufferOriginal;
        internal static DEncodeString EncodeStringOriginal;
        internal static DSendPacket SendPacketOriginal;

        internal static DDecodeByte DecodeByteOriginal;
        internal static DDecodeShort DecodeShortOriginal;
        internal static DDecodeInt DecodeIntOriginal;
        internal static DDecodeBuffer DecodeBufferOriginal;
        internal static DDecodeString DecodeStringOriginal;
        //internal static DRecvPacket DecrypDataOriginal;
        
        #endregion

        #region Address Patterns
        internal static readonly string OutPacketInitPattern = "B8 ?? ?? ?? 00 E8 ?? ?? ?? 00 51 51 56 8B F1 83 66 04 00";
        internal static readonly string EncodeBytePattern = "56 8B F1 6A 01 E8 ?? ?? ?? ?? 8B 4E 08 8B 46 04";
        internal static readonly string EncodeShortPattern = "56 8B F1 6A 02 E8 ?? ?? ?? ?? 8B 4E 08 8B 46 04";
        internal static readonly string EncodeIntPattern = "56 8B F1 6A 04 E8 ?? ?? ?? ?? 8B 4E 08 8B 46 04";
        internal static readonly string EncodeBufferPattern = "56 57 8B 7C 24 10 8B F1 57 ?? ?? ?? ?? ?? 8B 46 04 03 46 08 57 FF 74 24 10 50 ?? ?? ?? ?? ?? 01 7E 08 83 C4 0C 5F 5E C2 08 00";
        internal static readonly string EncodeStringPattern = "B8 ?? ?? ?? 00 E8 ?? ?? ?? 00 51 56 8B F1 8B 45 08 83 65 FC 00 85 C0 74 05 8B 40 FC EB 02 33 C0 83 C0 02 50 8B CE E8 ?? ?? ?? ?? 8B 46 04 03 46 08 50 51 8D 45 08 8B CC 89 65 F0 50 E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 01 46 08 83 4D FC FF 59 59 8D 4D 08 E8 ?? ?? ?? ?? 8B 4D F4 64 89 0D 00 00 00 00 5E C9 C2 04 00";
        internal static readonly string SendPacketPattern = "B8 ?? ?? ?? 00 E8 ?? ?? ?? 00 51 56 57 8B F9 8D 77 ?? 8B CE 89 ?? ?? E8 ?? ?? ?? ?? 8B 47";

        internal static readonly string DecodeBytePattern = "55 8B EC 51 8B 51 14 8B 41 08 56 0F B7 71 0C 2B F2 03 C2 83 FE 01 5E 73 15 ?? ?? ?? ?? ?? 8D 45 FC 50 C7 45 FC 26 00 00 00 ?? ?? ?? ?? ?? 8A 00 42 89 51 14 C9 C3";
        internal static readonly string DecodeShortPattern = "55 8B EC 51 8B 51 14 8B 41 08 56 0F B7 71 0C 2B F2 03 C2 83 FE 02 5E 73 15 ?? ?? ?? ?? ?? 8D 45 FC 50 C7 45 FC 26 00 00 00 ?? ?? ?? ?? ?? 66 8B 00 83 C2 02 89 51 14 C9 C3";
        internal static readonly string DecodeIntPattern = "55 8B EC 51 8B 51 14 8B 41 08 56 0F B7 71 0C 2B F2 03 C2 83 FE 04 5E 73 15 ?? ?? ?? ?? ?? 8D 45 FC 50 C7 45 FC 26 00 00 00 ?? ?? ?? ?? ?? 8B 00 83 C2 04 89 51 14 C9 C3";
        internal static readonly string DecodeBufferPattern = "55 8B EC 56 8B F1 0F B7 56 0C 8B 4E 14 8B 46 08 57 8B 7D 0C 2B D1 03 C1 3B D7 73 15 ?? ?? ?? ?? ?? 8D 45 0C 50 C7 45 0C 26 00 00 00 ?? ?? ?? ?? ?? 57 50 FF 75 08 ?? ?? ?? ?? ?? 83 C4 0C 01 7E 14 5F 5E 5D C2 08 00";
        internal static readonly string DecodeStringPattern = "B8 ?? ?? ?? 00 E8 ?? ?? ?? 00 51 51 83 65 EC 00 83 65 F0 00 56 57 8B F1 8B 46 14 0F B7 4E 0C 6A 01 2B C8 5F 51 8B 4E 08 03 C8 51 8D 45 F0 50 89 7D FC ?? ?? ?? ?? ?? 01 46 14 8B 75 08 83 26 00 83 C4 0C 8D 45 F0 50 8B CE ?? ?? ?? ?? ?? 89 7D EC 80 65 FC 00 8D 4D F0 ?? ?? ?? ?? ?? 8B 4D F4 5F 8B C6 5E 64 89 0D 00 00 00 00 C9 C2 04 00";
        //internal static readonly string DecryptDataPattern = "5F 5E 5B C9 C2 04 00 B8 ?? ?? ?? 00 E8 ?? ?? ?? ?? 83 EC ?? 53 56 57 33";
        #endregion

        #region Addresses
        internal static IntPtr OutPacketInitAddress;
        internal static IntPtr EncodeByteAddress;
        internal static IntPtr EncodeShortAddress;
        internal static IntPtr EncodeIntAddress;
        internal static IntPtr EncodeBufferAddress;
        internal static IntPtr EncodeStringAddress;
        internal static IntPtr SendPacketAddress;

        internal static IntPtr DecodeByteAddress;
        internal static IntPtr DecodeShortAddress;
        internal static IntPtr DecodeIntAddress;
        internal static IntPtr DecodeBufferAddress;
        internal static IntPtr DecodeStringAddress;
        //internal static IntPtr DecryptDataAddress;
        #endregion

        internal Form2 form = new Form2();

        public Main(RemoteHooking.IContext InContext, String InChannelName)
        {
            Interface = RemoteHooking.IpcConnectClient<MapleStoryHookInterface>(InChannelName);
        }

        public void Run(RemoteHooking.IContext InContext, String InChannelName)
        {

            try
            {
                // Call Host
                Interface.IsInstalled(RemoteHooking.GetCurrentProcessId());

                LocalHook.EnableRIPRelocation(); // no idea what this does

                DebugAddresses();

                LoadAddresses();

                LoadOriginalFunctions();

                hooks = new List<LocalHook>();

                hooks.Add(LocalHook.Create(OutPacketInitAddress, new DOutPacketInit(form.OutPacketInitHooked), this));
                hooks.Add(LocalHook.Create(EncodeByteAddress, new DEncodeByte(form.EncodeByteHooked), this));
                hooks.Add(LocalHook.Create(EncodeShortAddress, new DEncodeShort(form.EncodeShortHooked), this));
                hooks.Add(LocalHook.Create(EncodeIntAddress, new DEncodeInt(form.EncodeIntHooked), this));
                hooks.Add(LocalHook.Create(EncodeBufferAddress, new DEncodeBuffer(form.EncodeBufferHooked), this));
                hooks.Add(LocalHook.Create(EncodeStringAddress, new DEncodeString(form.EncodeStringHooked), this));

                if (SendPacketAddress.ToInt32() > 0)
                {
                    //hooks.Add(LocalHook.Create(SendPacketAddress, new DSendPacket(form.SendPacketHooked), this));
                }

                hooks.Add(LocalHook.Create(DecodeByteAddress, new DDecodeByte(form.DecodeByteHooked), this));
                hooks.Add(LocalHook.Create(DecodeShortAddress, new DDecodeShort(form.DecodeShortHooked), this));
                hooks.Add(LocalHook.Create(DecodeIntAddress, new DDecodeInt(form.DecodeIntHooked), this));
                hooks.Add(LocalHook.Create(DecodeBufferAddress, new DDecodeBuffer(form.DecodeBufferHooked), this));
                hooks.Add(LocalHook.Create(DecodeStringAddress, new DDecodeString(form.DecodeStringHooked), this));
                //hooks.Add(LocalHook.Create(DecryptDataAddress, new DDecryptData(form.DecryptDataHooked), this));

                hooks.ForEach(hook => hook.ThreadACL.SetExclusiveACL(new Int32[] { 0 }));

                Interface.WriteConsole("Initialized Hooks: " + hooks.Count);
                
                form.ShowDialog();

            }
            catch (Exception e)
            {
                Interface.WriteConsole("ERROR: " + e);
            }

        }

        private void LoadAddresses()
        {
            Scanner scanner = new Scanner();

            OutPacketInitAddress = scanner.FindPattern(OutPacketInitPattern, 0);
            EncodeByteAddress = scanner.FindPattern(EncodeBytePattern, 0);
            EncodeShortAddress = scanner.FindPattern(EncodeShortPattern, 0);
            EncodeIntAddress = scanner.FindPattern(EncodeIntPattern, 0);
            EncodeBufferAddress = scanner.FindPattern(EncodeBufferPattern, 0);
            EncodeStringAddress = scanner.FindPattern(EncodeStringPattern, 0);
            SendPacketAddress = scanner.FindPattern(SendPacketPattern, 0);
            DecodeByteAddress = scanner.FindPattern(DecodeBytePattern, 0);
            DecodeShortAddress = scanner.FindPattern(DecodeShortPattern, 0);
            DecodeIntAddress = scanner.FindPattern(DecodeIntPattern, 0);
            DecodeBufferAddress = scanner.FindPattern(DecodeBufferPattern, 0);
            DecodeStringAddress = scanner.FindPattern(DecodeStringPattern, 0);
   
        }

        private void LoadOriginalFunctions()
        {
            OutPacketInitOriginal = (DOutPacketInit)Marshal.GetDelegateForFunctionPointer(OutPacketInitAddress, typeof(DOutPacketInit));
            EncodeByteOriginal = (DEncodeByte)Marshal.GetDelegateForFunctionPointer(EncodeByteAddress, typeof(DEncodeByte));
            EncodeShortOriginal = (DEncodeShort)Marshal.GetDelegateForFunctionPointer(EncodeShortAddress, typeof(DEncodeShort));
            EncodeIntOriginal = (DEncodeInt)Marshal.GetDelegateForFunctionPointer(EncodeIntAddress, typeof(DEncodeInt));
            EncodeBufferOriginal = (DEncodeBuffer)Marshal.GetDelegateForFunctionPointer(EncodeBufferAddress, typeof(DEncodeBuffer));
            EncodeStringOriginal = (DEncodeString)Marshal.GetDelegateForFunctionPointer(EncodeStringAddress, typeof(DEncodeString));

            if (SendPacketAddress.ToInt32() > 0)
            {
                //SendPacketOriginal = (DSendPacket)Marshal.GetDelegateForFunctionPointer(SendPacketAddress, typeof(DSendPacket));
            }
            

            DecodeByteOriginal = (DDecodeByte)Marshal.GetDelegateForFunctionPointer(DecodeByteAddress, typeof(DDecodeByte));
            DecodeShortOriginal = (DDecodeShort)Marshal.GetDelegateForFunctionPointer(DecodeShortAddress, typeof(DDecodeShort));
            DecodeIntOriginal = (DDecodeInt)Marshal.GetDelegateForFunctionPointer(DecodeIntAddress, typeof(DDecodeInt));
            DecodeBufferOriginal = (DDecodeBuffer)Marshal.GetDelegateForFunctionPointer(DecodeBufferAddress, typeof(DDecodeBuffer));
            DecodeStringOriginal = (DDecodeString)Marshal.GetDelegateForFunctionPointer(DecodeStringAddress, typeof(DDecodeString));   
        }

        private void DebugAddresses()
        {
            Scanner scanner = new Scanner();

            Interface.WriteConsole("OUT PACKET " + scanner.FindPatternAsHex(OutPacketInitPattern, 0));
            Interface.WriteConsole("S BYTE " + scanner.FindPatternAsHex(EncodeBytePattern, 0));
            Interface.WriteConsole("S SHORT " + scanner.FindPatternAsHex(EncodeShortPattern, 0));
            Interface.WriteConsole("S INT " + scanner.FindPatternAsHex(EncodeIntPattern, 0));
            Interface.WriteConsole("S BUFFER " + scanner.FindPatternAsHex(EncodeBufferPattern, 0));
            Interface.WriteConsole("S STRING " + scanner.FindPatternAsHex(EncodeStringPattern, 0));
            Interface.WriteConsole("SEND PACKET " + scanner.FindPatternAsHex(SendPacketPattern, 0));

            Interface.WriteConsole("R BYTE " + scanner.FindPatternAsHex(DecodeBytePattern, 0));
            Interface.WriteConsole("R SHORT " + scanner.FindPatternAsHex(DecodeShortPattern, 0));
            Interface.WriteConsole("R INT " + scanner.FindPatternAsHex(DecodeIntPattern, 0));
            Interface.WriteConsole("R BUFFER " + scanner.FindPatternAsHex(DecodeBufferPattern, 0));
            Interface.WriteConsole("R STRING " + scanner.FindPatternAsHex(DecodeStringPattern, 0));
            //Interface.WriteConsole("RECV PACKET " + scanner.FindPatternAsHex(DecryptDataPattern, 0));
        }

        #region Delegates
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate int DOutPacketInit(IntPtr @this, int nType, int bLoopback);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate void DEncodeByte(IntPtr @this, byte n);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate void DEncodeShort(IntPtr @this, Int16 n);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate void DEncodeInt(IntPtr @this, Int32 n);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate void DEncodeBuffer(IntPtr @this, IntPtr bufferPointer, UInt32 uSize);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate void DEncodeString(IntPtr @this, IntPtr stringPointer);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate int DSendPacket(IntPtr @this, IntPtr packetPointer);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate byte DDecodeByte(IntPtr @this);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate UInt16 DDecodeShort(IntPtr @this);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate UInt32 DDecodeInt(IntPtr @this);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate void DDecodeBuffer(IntPtr @this, IntPtr bufferPointer, UInt32 uSize);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate IntPtr DDecodeString(IntPtr @this, IntPtr resultPointer);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public delegate int DDecryptData(IntPtr @this, int dwKey);
        #endregion

    }

    [StructLayout(LayoutKind.Sequential)]
    public class CInPacket
    {
        public int Loopback; //0x00
        public int State; //0x04
        public IntPtr BufferPointer; //0x08
        public UInt16 Length; //0x0C
        public UInt16 RawSeq; //0x0E
        public UInt16 DataLen; //0x10
        public UInt16 UNK_SHORT; //0x12
        public UInt32 Offset; //0x14


        public byte[] ToArray()
        {
            byte[] buffer = new byte[Length];
            Marshal.Copy(BufferPointer, buffer, 0, Length);
            return buffer;
        }

        public BinaryReader ToReader()
        {
            MemoryStream memStream = new MemoryStream(ToArray());
            memStream.Position = Offset;
            BinaryReader reader = new BinaryReader(memStream);
            return reader;
        }
 
    }

    [StructLayout(LayoutKind.Sequential)]
    public class COutPacket
    {
        public bool Loopback;
        public IntPtr BufferPointer;
        public Int32 Offset;
        public bool Encrypted;

        public byte[] ToArray()
        {
            byte[] buffer = new byte[Offset];
            Marshal.Copy(BufferPointer, buffer, 0, Offset);
            return buffer;
        }
    }

}