using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace MapleStoryHooks
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            timer1.Start();
        }

        public static readonly int MAX_PACKETS = 1000;

        public MaplePacket CurrentPacket { get; set; }
        public DateTime CurrentTime { get; set; }

        

        public delegate void DPacketFinished(MaplePacket packet);

        public void OnPacketFinished(MaplePacket packet)
        {
            string data = "";


            if (packet.Segments.Count == 1)
            {
                data = "<no data>";
            }
            else
            {
                for (int i = 1; i < packet.Segments.Count; i++)
                {
                    data += packet.Segments[i].ToHexString() + " ";
                }
                data.TrimEnd(' ');
            }
            

            PacketSegment opcodeSegment = packet.Segments[0];
            string opcode = opcodeSegment.ToHexString().PadLeft(4, '0');

            if (opcodeSegment.Type == PacketSegmentType.SHORT || opcodeSegment.Type == PacketSegmentType.BYTE)
            {
                if (opcodeSegment.ToShort() > -1 && opcodeSegment.ToShort() < 0x200)
                {
                    try
                    {
                        listView1.Items.Add(new ListViewItem(new string[] { packet.Direction, packet.ToArray().Length.ToString(), BitConverter.ToString(packet.ToArray()) }));
                        listView2.Items.Add(new ListViewItem(new string[] { packet.Direction, opcode, data }));
                    }
                    catch (Exception e)
                    {
                        Main.Interface.WriteConsole("Packet_Finished Error: " + e.StackTrace + "\r\n" + e.Message);
                    }
                }
            }

            if (listView2.Items.Count > MAX_PACKETS)
            {
                listView1.Items.RemoveAt(0);
                listView2.Items.RemoveAt(0);
            }
        }

        public void AddSegment(int id, PacketSegment segment)
        {
            try
            {
                CurrentTime = DateTime.Now;

                if (CurrentPacket == null)
                {
                    CurrentPacket = new MaplePacket(id, segment.Direction);
                    CurrentPacket.Segments.Add(segment);
                }
                else
                {
                    if (CurrentPacket.Id == id)
                    {
                        CurrentPacket.Segments.Add(segment);
                    }
                    else
                    {
                        MaplePacket oldPacket = CurrentPacket;

                        this.Invoke(new DPacketFinished(OnPacketFinished), oldPacket);
                        CurrentPacket = new MaplePacket(id, segment.Direction);
                        CurrentPacket.Segments.Add(segment);
                    }
                }

                CurrentTime = DateTime.Now;
            }
            catch (Exception e)
            {
                Main.Interface.WriteConsole("Add_Segment Error: " + e.StackTrace + "\r\n" + e.Message);
            }
        }

        #region Hooked Methods
        public int OutPacketInitHooked(IntPtr @this, int nType, int bLoopback)
        {
            return Main.OutPacketInitOriginal(@this, nType, bLoopback);
        }

        public void EncodeByteHooked(IntPtr @this, byte n)
        {
            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.BYTE, n, "SEND");

            AddSegment(@this.ToInt32(), segment);

            Main.EncodeByteOriginal(@this, n);
        }

        public void EncodeShortHooked(IntPtr @this, Int16 n)
        {
            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.SHORT, n, "SEND");

            AddSegment(@this.ToInt32(), segment);

            Main.EncodeShortOriginal(@this, n);
        }

        public void EncodeIntHooked(IntPtr @this, Int32 n)
        {
            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.INT, n, "SEND");

            AddSegment(@this.ToInt32(), segment);

            Main.EncodeIntOriginal(@this, n);
        }

        public void EncodeBufferHooked(IntPtr @this, IntPtr bufferPointer, UInt32 uSize)
        {
            byte[] data = new byte[uSize];
            Marshal.Copy(bufferPointer, data, 0, (int)uSize);

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.BUFFER, data, "SEND");

            AddSegment(@this.ToInt32(), segment);

            Main.EncodeBufferOriginal(@this, bufferPointer, uSize);
        }

        public void EncodeStringHooked(IntPtr @this, IntPtr stringPointer)
        {

            string s = Marshal.PtrToStringAnsi(stringPointer);

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.STRING, s, "SEND");

            AddSegment(@this.ToInt32(), segment);

            Main.EncodeStringOriginal(@this, stringPointer);
        }


        public int SendPacketHooked(IntPtr @this, IntPtr packetPointer)
        {
            COutPacket packet = (COutPacket)Marshal.PtrToStructure(@this, typeof(COutPacket));
            byte[] data = packet.ToArray();

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.BUFFER, data, "SEND WHOLE");

            AddSegment(@this.ToInt32(), segment);

            return Main.SendPacketOriginal(@this, packetPointer);
        }


        public byte DecodeByteHooked(IntPtr @this)
        {
            CInPacket packet = (CInPacket)Marshal.PtrToStructure(@this, typeof(CInPacket));
            byte result = packet.ToReader().ReadByte();

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.BYTE, result, "RECV");
            AddSegment(@this.ToInt32(), segment);

            return Main.DecodeByteOriginal(@this);
        }

        public UInt16 DecodeShortHooked(IntPtr @this)
        {
            CInPacket packet = (CInPacket)Marshal.PtrToStructure(@this, typeof(CInPacket));
            short result = packet.ToReader().ReadInt16();

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.SHORT, result, "RECV");
            AddSegment(@this.ToInt32(), segment);

            return Main.DecodeShortOriginal(@this);
        }

        public UInt32 DecodeIntHooked(IntPtr @this)
        {
            CInPacket packet = (CInPacket)Marshal.PtrToStructure(@this, typeof(CInPacket));
            int result = packet.ToReader().ReadInt32();

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.INT, result, "RECV");
            AddSegment(@this.ToInt32(), segment);

            return Main.DecodeIntOriginal(@this);
        }

        public void DecodeBufferHooked(IntPtr @this, IntPtr bufferPointer, UInt32 uSize)
        {
            CInPacket packet = (CInPacket)Marshal.PtrToStructure(@this, typeof(CInPacket));
            byte[] result = packet.ToReader().ReadBytes((int)uSize);

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.BUFFER, result, "RECV");
            AddSegment(@this.ToInt32(), segment);

            Main.DecodeBufferOriginal(@this, bufferPointer, uSize);
        }

        public IntPtr DecodeStringHooked(IntPtr @this, IntPtr resultPointer)
        {
            CInPacket packet = (CInPacket)Marshal.PtrToStructure(@this, typeof(CInPacket));
            BinaryReader reader = packet.ToReader();
            int length = reader.ReadInt16();
            string result = Encoding.ASCII.GetString(reader.ReadBytes(length));

            reader.Close(); // Should close all other readers????

            PacketSegment segment = new PacketSegment(@this.ToInt32(), PacketSegmentType.STRING, result, "RECV");
            AddSegment(@this.ToInt32(), segment);

            return Main.DecodeStringOriginal(@this, resultPointer);
        }

        //public int DecryptDataHooked(IntPtr @this, int dwKey)
        //{
        //int result = Main.DecrypDataOriginal(@this, dwKey);
        //return result;
        //}

        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (CurrentPacket != null && CurrentTime != null && CurrentTime.Ticks > 0 && DateTime.Now.Ticks - CurrentTime.Ticks > 500)
            {
                MaplePacket oldPacket = CurrentPacket;
                this.Invoke(new DPacketFinished(OnPacketFinished), oldPacket);
                CurrentPacket = null;
                CurrentTime = DateTime.Now;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            listView2.Items.Clear();
        }
    } 
}
