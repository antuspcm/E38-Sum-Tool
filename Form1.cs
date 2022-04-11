using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace E38SumTool
{
    
    public partial class Form1 : Form
    {
        struct segment
        {
            public uint start;
            public uint end;
            public ushort lsum;
            public ushort lcvn;
            public ushort csum;
            public ushort ccvn;
        }
        
        byte[] bin;
        segment[] seg;
                
        public Form1()
        {
            InitializeComponent();
            // allocate data for segments, sums, cvns
            seg = new segment[7];
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void loadbin_Click(object sender, EventArgs e)
        {
            loadbindialog.ShowDialog();
            processbin(loadbindialog.FileName);
        }

        private void processbin(string binfile)
        {
            log.AppendText("Attempting to load " + binfile + System.Environment.NewLine);

            try
            {
                bin = File.ReadAllBytes(binfile);
            }
            catch (Exception ex)
            {
                log.AppendText("Failed!" + System.Environment.NewLine);
            }
            finally
            {
                log.AppendText("Bin Loaded" + System.Environment.NewLine);
            }

            if (bin.Length != 0x200000)
            {
                log.AppendText(binfile + "Is not 2Mb. Skipping" + System.Environment.NewLine);
                return;
            }

            
            
            // load index
            uint index = 0x10000;
            seg[1].start = getuint(bin, index + 0x24);
            seg[1].end = getuint(bin, index + 0x28);
            seg[2].start = getuint(bin, index + 0x48);
            seg[2].end = getuint(bin, index + 0x4c);
            seg[3].start = getuint(bin, index + 0x6b);
            seg[3].end = getuint(bin, index + 0x6f);
            seg[4].start = getuint(bin, index + 0x8e);
            seg[4].end = getuint(bin, index + 0x92);
            seg[5].start = getuint(bin, index + 0xb1);
            seg[5].end = getuint(bin, index + 0xb5);
            seg[6].start = getuint(bin, index + 0xd4);
            seg[6].end = getuint(bin, index + 0xd8);

            for (int a = 1; a <= 6; a++)
            {
                if (seg[a].start > 0x200000)
                {
                    log.AppendText("Segment " + a + " start is out of range" + System.Environment.NewLine);
                    return;
                }

                if (seg[a].end > 0x200000)
                {
                    log.AppendText("Segment " + a + " end is out of range" + System.Environment.NewLine);
                    return;
                }
                
                if (seg[a].start % 2 != 0)
                {
                    log.AppendText("Segment " + a + " does not start on a word boundry" + System.Environment.NewLine);
                    return;
                }
                if (seg[a].end % 2 == 0)
                {
                    log.AppendText("Segment " + a + " does not end on a word boundry" + System.Environment.NewLine);
                    return;
                }
                if (seg[a].end - seg[a].start < 0x24)
                {
                    log.AppendText("Segment " + a + " is impossibly short" + System.Environment.NewLine);
                    return;
                }
            }
           
            // display segment data in form
            s1s.Text = seg[1].start.ToString("X8");
            s1e.Text = seg[1].end.ToString("X8");
            s2s.Text = seg[2].start.ToString("X8");
            s2e.Text = seg[2].end.ToString("X8");
            s3s.Text = seg[3].start.ToString("X8");
            s3e.Text = seg[3].end.ToString("X8");
            s4s.Text = seg[4].start.ToString("X8");
            s4e.Text = seg[4].end.ToString("X8");
            s5s.Text = seg[5].start.ToString("X8");
            s5e.Text = seg[5].end.ToString("X8");
            s6s.Text = seg[6].start.ToString("X8");
            s6e.Text = seg[6].end.ToString("X8");

            // load data, log
            for (int i = 1; i <= 6; i++)
            {
                log.AppendText("Segment "+i+": "+seg[i].start.ToString("X8") + "-" + seg[i].end.ToString("X8") + System.Environment.NewLine);
                seg[i].lsum = getushort(bin, seg[i].start);
                logushort("loaded sum: ", seg[i].lsum);
                seg[i].lcvn=getushort(bin, seg[i].start+0x1E);
                logushort("loaded cvn: ", seg[i].lcvn);
                seg[i].csum=segmentsum(bin, seg[i].start, seg[i].end);
                seg[i].ccvn=segmentcvn(bin, seg[i].start, seg[i].end);
            }

            updatedisplay();
            testall();
        }

        private void updatedisplay()
        {
            cc1.Text = seg[1].ccvn.ToString("X4");
            cc2.Text = seg[2].ccvn.ToString("X4");
            cc3.Text = seg[3].ccvn.ToString("X4");
            cc4.Text = seg[4].ccvn.ToString("X4");
            cc5.Text = seg[5].ccvn.ToString("X4");
            cc6.Text = seg[6].ccvn.ToString("X4");

            lc1.Text = seg[1].lcvn.ToString("X4");
            lc2.Text = seg[2].lcvn.ToString("X4");
            lc3.Text = seg[3].lcvn.ToString("X4");
            lc4.Text = seg[4].lcvn.ToString("X4");
            lc5.Text = seg[5].lcvn.ToString("X4");
            lc6.Text = seg[6].lcvn.ToString("X4");

            ls1.Text = seg[1].lsum.ToString("X4");
            ls2.Text = seg[2].lsum.ToString("X4");
            ls3.Text = seg[3].lsum.ToString("X4");
            ls4.Text = seg[4].lsum.ToString("X4");
            ls5.Text = seg[5].lsum.ToString("X4");
            ls6.Text = seg[6].lsum.ToString("X4");

            cs1.Text = seg[1].csum.ToString("X4");
            cs2.Text = seg[2].csum.ToString("X4");
            cs3.Text = seg[3].csum.ToString("X4");
            cs4.Text = seg[4].csum.ToString("X4");
            cs5.Text = seg[5].csum.ToString("X4");
            cs6.Text = seg[6].csum.ToString("X4");
        }

        public void testall()
        {
            testcvns();
            testsums();
        }

        private void logushort(string msg, uint val) {
            log.AppendText(msg + " " + val.ToString("X4") + System.Environment.NewLine);
        }

        private void loguint(string msg, uint val)
        {
            log.AppendText(msg + " " + val.ToString("X8") + System.Environment.NewLine);
        }

        public ushort segmentcvn(byte[] bin, uint s, uint e)
        {
            uint sum = gmcrc16(bin, 0, s + 2, s + 0x1d);
            sum = gmcrc16(bin, sum, s + 0x20, e);
            sum = swapab(sum);
            return (ushort) sum;
        }

        public ushort segmentsum(byte[] bin, uint s, uint e)
        {
            int sum = 0;
			for (uint i=s+2; i<=e; i+=2) {
				sum += ((bin[i]<<8) + bin[i+1]);
			}
            sum = (((sum & 0xFFFF) ^ 0xFFFF) + 1) & 0xFFFF;
            return (ushort) sum;
        }

        private ushort getushort(byte[] array, uint loc) {
            int rc;
            rc = array[loc] * 0x100;
            rc += array[loc+1];
            return (ushort) rc;
        }

        private uint getuint(byte[] array, uint loc) {
            uint rc;
            rc  = (uint)array[loc]     << 24;
            rc += (uint)array[loc + 1] << 16;
            rc += (uint)array[loc + 2] << 8;
            rc += (uint)array[loc + 3];
            return rc;
        }

        private uint gmcrc16(byte[] bin, uint init, uint s, uint e)
        {
            uint num;
            byte num2;
            byte num3;
            byte num4;
            uint num5;
            uint num6;

            uint[] crc16t = new uint[] { 
                0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241, 0xC601, 0x06C0,
                0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440, 0xCC01, 0x0CC0, 0x0D80, 0xCD41,
                0x0F00, 0xCFC1, 0xCE81, 0x0E40, 0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0,
                0x0880, 0xC841, 0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
                0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41, 0x1400, 0xD4C1,
                0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641, 0xD201, 0x12C0, 0x1380, 0xD341,
                0x1100, 0xD1C1, 0xD081, 0x1040, 0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1,
                0xF281, 0x3240, 0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
                0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41, 0xFA01, 0x3AC0,
                0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840, 0x2800, 0xE8C1, 0xE981, 0x2940,
                0xEB01, 0x2BC0, 0x2A80, 0xEA41, 0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1,
                0xEC81, 0x2C40, 0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
                0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041, 0xA001, 0x60C0,
                0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240, 0x6600, 0xA6C1, 0xA781, 0x6740,
                0xA501, 0x65C0, 0x6480, 0xA441, 0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0,
                0x6E80, 0xAE41, 0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
                0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41, 0xBE01, 0x7EC0,
                0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40, 0xB401, 0x74C0, 0x7580, 0xB541,
                0x7700, 0xB7C1, 0xB681, 0x7640, 0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0,
                0x7080, 0xB041, 0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
                0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440, 0x9C01, 0x5CC0,
                0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40, 0x5A00, 0x9AC1, 0x9B81, 0x5B40,
                0x9901, 0x59C0, 0x5880, 0x9841, 0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1,
                0x8A81, 0x4A40, 0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
                0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641, 0x8201, 0x42C0,
                0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040 };
            
            try
            {
                num = s; // location counter
                while (num <= e) // until final address (inclusive)
                {
                    num2 = bin[num]; // num2=current byte
                    num3 = (byte) (init & 0xff); //num3=sum low byte
                    num4 = (byte) ((init & 0xff00) / 0x100); //num4=sum high byte
                    num5 = crc16t[num3 ^ num2]; // sum low byte xord with data used as index to another xor table
                    init = (num5 ^ num4) & 0xffff; // xor table byte xord with sum high byte
                    num++; // next byte
                }
                num6 = init; // return the sum
                return num6;
            }
            catch (Exception ex)
            {
            }
            return 0;
            }

			private void testsums()
            {
                if (string.Compare(ls1.Text, cs1.Text) == 0)
                {
                    cs1.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    cs1.ForeColor = System.Drawing.Color.Red;
                }
                if (string.Compare(ls2.Text, cs2.Text) == 0)
                {
                    cs2.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    cs2.ForeColor = System.Drawing.Color.Red;
                }
                if (string.Compare(ls3.Text, cs3.Text) == 0)
                {
                    cs3.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    cs3.ForeColor = System.Drawing.Color.Red;
                }
                if (string.Compare(ls4.Text, cs4.Text) == 0)
                {
                    cs4.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    cs4.ForeColor = System.Drawing.Color.Red;
                }
                if (string.Compare(ls5.Text, cs5.Text) == 0)
                {
                    cs5.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    cs5.ForeColor = System.Drawing.Color.Red;
                }
                if (string.Compare(ls6.Text, cs6.Text) == 0)
                {
                    cs6.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    cs6.ForeColor = System.Drawing.Color.Red;
                }

            }

            private void testcvns()
            {
            if (string.Compare(lc1.Text, cc1.Text) == 0)
            {
                cc1.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                cc1.ForeColor = System.Drawing.Color.Red;
            }
            if (string.Compare(lc2.Text, cc2.Text) == 0)
            {
                cc2.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                cc2.ForeColor = System.Drawing.Color.Red;
            }
            if (string.Compare(lc3.Text, cc3.Text) == 0)
            {
                cc3.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                cc3.ForeColor = System.Drawing.Color.Red;
            }
            if (string.Compare(lc4.Text, cc4.Text) == 0)
            {
                cc4.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                cc4.ForeColor = System.Drawing.Color.Red;
            }
            if (string.Compare(lc5.Text, cc5.Text) == 0)
            {
                cc5.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                cc5.ForeColor = System.Drawing.Color.Red;
            }
            if (string.Compare(lc6.Text, cc6.Text) == 0)
            {
                cc6.ForeColor = System.Drawing.Color.Green;
            }
            else
            {
                cc6.ForeColor = System.Drawing.Color.Red;
            }
        }

        public uint swapab(uint p0)
        {
            uint num;
            uint num2;
            uint num3;
            try
            {
                num = (p0 & 0xff00) / 0x100;
                num2 = p0 & 0xff;
                num3 = (num2 * 0x100) + num;
                return num3;
            }
            catch (Exception ex)
            {
            }
            return 0;
        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);

        }

        private void savetobin_Click(object sender, EventArgs e)
        {

        }
    }
}
