using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//typedef c++ <3
using magic8 = System.UInt64;
using asciiz = System.String;

using int8 = System.Byte;
using int16 = System.Int16;
using int32 = System.Int32;
using int64 = System.Int64;

using uint8 = System.Byte;
using uint16 = System.UInt16;
using uint32 = System.UInt32;
using uint64 = System.UInt64;




namespace AITexExtract
{
    class PAK
    {

        struct binhead
        {
            public uint32 count1;
            public uint32 texNameCount;
            public uint32 texDatSize;
        };

        struct bintexName
        {
            public string name;
        };

        struct bintexHeader //48bytes
        {
            public byte[] magic; //4
            public DXGI_FORMAT dxtType; //type 0xD => diffuse(bc7) 0x8 => normal (bc5)
            public uint32 unk2;
            public uint32 texchunkSize;
            public uint16 varSlotSub;
            public uint16 varSlot;
            public uint16 varSlot2;
            public uint16 width;
            public uint16 height;
            public uint16 unk6_a;
            public uint32 unk7;
            public uint32 unk8;
            public uint32 unk9;
            public uint32 unk10;
            public uint32 unk11;
        };




        struct PAK_Header
        {
            public uint32 blank;
            public uint32 unk1;
            public uint32 unk2;
            public uint32 entryCount1;
            public uint32 entryCount2;
            public uint32 unk3;
            public uint32 unk4;
            public uint32 unk5;
        };



        struct PK_Entry
        {
            public string fpath;
            public uint16 width;
            public uint16 height;
            public bintexHeader texHeader;

            public uint32 blank;
            public uint32 blank2;
            public uint32 sizeA;
            public uint32 sizeB;
            public uint32 unk1;
            public uint32 unk2;
            public byte[] flags; //4
            public uint32 unk3;
            public uint16 unkey;
            public uint16 entryIndex;
            public uint32 unkn4;
            public uint32 blankPointer;
            public uint32 unkn5;
        };



        public enum PlatformType: byte
        {
            PC = 0x2,
            XBOX = 0x3,
            PS3 = 0x4
        }


        public enum DXGI_FORMAT : int
        {
            DXGI_FORMAT_B8G8R8A8_UNORM = 0x2,
            DXGI_FORMAT_B8G8R8_UNORM = 0x5,
            DXGI_FORMAT_BC1_UNORM = 0x6,
            DXGI_FORMAT_BC3_UNORM = 0x9,
            DXGI_FORMAT_BC5_UNORM = 0x8,
            DXGI_FORMAT_BC7_UNORM = 0xD
        }

        private bool ignoreCompressed;
        private string In_FilePath;
        private string Out_FolderPath;

        //Reader
        private BinaryReader br;
        private FileStream fs;

        //Reader2
        private BinaryReader br2;
        private FileStream fs2;

        //Writer
        private BinaryWriter bw;
        private FileStream fw;


        //CORE DATA ITEMS
        PAK_Header header;
        private List<PK_Entry> entries = new List<PK_Entry>();

        //BIN
        binhead bheader;
        private List<bintexName> texNames = new List<bintexName>();
        private List<bintexHeader> texHeads = new List<bintexHeader>();



        public PAK(string InFilePath, string OutFolderPath,bool ignoreCompressed)
        {
            this.In_FilePath = InFilePath;
            this.Out_FolderPath = OutFolderPath;
            this.ignoreCompressed = ignoreCompressed;
        }


        public void ParseExport()
        {
            //TODO: IMPLEMENT


        }


        public void DebugExport()
        {

            //fire up the stream and reader
            Program.VText("Initalizing PAK Export..");
            fs = new FileStream(In_FilePath, FileMode.Open, FileAccess.Read);
            br = new BinaryReader(fs);
            IO.ByteOrder byteSex = IO.ByteOrder.BigEndian;
            IO.ByteOrder byteSex2 = IO.ByteOrder.LittleEndian;

            header.blank = IO.ReadUInt32(br,byteSex);
            if (header.blank != 0)
            {
                Program.PError("ERROR: looking for blank 4bytes");
                return;
            }


            header.unk1= IO.ReadUInt32(br, byteSex);
            header.unk2 = IO.ReadUInt32(br, byteSex);
            header.entryCount1 = IO.ReadUInt32(br, byteSex);
            header.entryCount2 = IO.ReadUInt32(br, byteSex);
            header.unk3= IO.ReadUInt32(br, byteSex);
            header.unk4 = IO.ReadUInt32(br, byteSex);
            header.unk5 = IO.ReadUInt32(br, byteSex);



            //Read each entry and process
            Program.VText("Reading tex Package entries..");
            for (uint i = 0; i < this.header.entryCount1; i++)
            {
                PK_Entry curEntry;
                curEntry.fpath = string.Empty;
                curEntry.width = 0;
                curEntry.height = 0;
                curEntry.texHeader = new bintexHeader();
                curEntry.blank = IO.ReadUInt32(br, byteSex);
                curEntry.blank2 = IO.ReadUInt32(br, byteSex);
                curEntry.sizeA = IO.ReadUInt32(br, byteSex);
                curEntry.sizeB = IO.ReadUInt32(br, byteSex);
                curEntry.unk1 = IO.ReadUInt32(br, byteSex);
                curEntry.unk2 = IO.ReadUInt32(br, byteSex);
                curEntry.flags = IO.ReadBytes(br, 4, byteSex);
                curEntry.unk3 = IO.ReadUInt32(br, byteSex);
                curEntry.unkey = IO.ReadUInt16(br, byteSex);
                curEntry.entryIndex = IO.ReadUInt16(br, byteSex);
                curEntry.unkn4 = IO.ReadUInt32(br, byteSex);
                curEntry.blankPointer = IO.ReadUInt32(br, byteSex);
                curEntry.unkn5 = IO.ReadUInt32(br, byteSex);
                //append
                entries.Add(curEntry);
            }


            //connect to the bin file and retrive names + strips

            //make name
            string rstrb =  Path.GetFileNameWithoutExtension(this.In_FilePath);
            string rstrb2 = Path.GetFileNameWithoutExtension(rstrb);
            string headBinFileName = rstrb2 + "_HEADERS" + Path.GetExtension(rstrb) + ".BIN";
            string fheadBinFile = Path.Combine(Path.GetDirectoryName(this.In_FilePath),headBinFileName);

            if(!File.Exists(fheadBinFile))
            {
                Program.VText("ERROR cant find" + headBinFileName);
                throw new Exception("ERROR cant find" + headBinFileName);
            }

            Program.VText("Applying glue and magic");

            using (fs2 = new FileStream(fheadBinFile, FileMode.Open, FileAccess.Read))
            {
                br2 = new BinaryReader(fs2);

                //read head
                this.bheader.count1 = IO.ReadUInt32(br2, byteSex2);
                this.bheader.texNameCount = IO.ReadUInt32(br2, byteSex2);
                this.bheader.texDatSize = IO.ReadUInt32(br2, byteSex2);

                //read filenames and folders
                for (int i = 0; i < bheader.texNameCount; i++)
                {
                    bintexName curTex;
                    curTex.name = IO.ReadStringASCIIZ(br2);
                    texNames.Add(curTex);
                }

                //Align
                //  ---> fs2.Position += Cra0Utilz.PaddingAlign(fs2.Position, 16);
                fs2.Seek(bheader.texDatSize + 12, SeekOrigin.Begin); //NOP?>


                //read headers
                for (int j = 0; j < bheader.texNameCount; j++)
                {
                    bintexHeader curTex;
                    curTex.magic = IO.ReadBytes(br2, 4, byteSex2);
                    curTex.dxtType = (DXGI_FORMAT)IO.ReadInt32(br2, byteSex2);
                    curTex.unk2 = IO.ReadUInt32(br2, byteSex2);
                    curTex.texchunkSize = IO.ReadUInt32(br2, byteSex2);
                    curTex.varSlotSub = IO.ReadUInt16(br2, byteSex2);
                    curTex.varSlot = IO.ReadUInt16(br2, byteSex2);
                    curTex.varSlot2 = IO.ReadUInt16(br2, byteSex2);
                    curTex.width = IO.ReadUInt16(br2, byteSex2);
                    curTex.height = IO.ReadUInt16(br2, byteSex2);
                    curTex.unk6_a = IO.ReadUInt16(br2, byteSex2);
                    curTex.unk7 = IO.ReadUInt32(br2, byteSex2);
                    curTex.unk8 = IO.ReadUInt32(br2, byteSex2);
                    curTex.unk9 = IO.ReadUInt32(br2, byteSex2);
                    curTex.unk10 = IO.ReadUInt32(br2, byteSex2);
                    curTex.unk11 = IO.ReadUInt32(br2, byteSex2);
                    texHeads.Add(curTex);
                }


            }


            //set filenames and size
            for (int cc = 0; cc < entries.Count; cc++)
            {
                PK_Entry centry = entries[cc];
                centry.fpath = texNames[centry.entryIndex].name;
                centry.width = texHeads[centry.entryIndex].width;
                centry.height = texHeads[centry.entryIndex].height;
                centry.texHeader = texHeads[centry.entryIndex];
                //int csize = DDSMOD.computeRAWImageSize(centry.width, centry.height, 32);
                //Program.VText(csize.ToString());
                entries[cc] = centry;
            }




            //now export those entries
            Program.VText("Exporting Package entries..");
            foreach (PK_Entry pkFile in this.entries)
            {
                fs.Seek(fs.Position, SeekOrigin.Begin); //NOP?>


                if (pkFile.sizeA != pkFile.sizeB)
                {
                    Program.VText("ERROR SIZEA/B Mismatch not extracting");
                    throw new Exception("Error size a/b mismatch");
                }


                string outFilePath = Path.Combine(Out_FolderPath,pkFile.fpath);
                string mdirpath = Path.GetDirectoryName(outFilePath);
                Cra0Utilz.CreatePath(mdirpath);

                byte[] dataChunk = br.ReadBytes((int)pkFile.sizeA);


                //  using (FileStream fw = new FileStream(outFilePath, FileMode.Create, FileAccess.Write))
                //  {
                //       bw = new BinaryWriter(fw);
                //       bw.Write(br.ReadBytes((int)pkFile.sizeA));
                // }





                //write dds
                DDSMOD myDDS;

                if (pkFile.texHeader.dxtType == DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM )
                {
                    myDDS = new DDSMOD(dataChunk, pkFile.width, pkFile.height,32,0,DDSMOD.DDS_Format.ATI2N);
                    myDDS.Save(outFilePath + ".dds");
                }
                else if (pkFile.texHeader.dxtType == DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM)
                {
                    myDDS = new DDSMOD(dataChunk, pkFile.width, pkFile.height, 32, 0, DDSMOD.DDS_Format.Dxt1);
                    myDDS.Save(outFilePath + ".dds");

                }
                else if (pkFile.texHeader.dxtType == DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM)
                {
                    myDDS = new DDSMOD(dataChunk, pkFile.width, pkFile.height, 32, 0, DDSMOD.DDS_Format.Dxt5);
                    myDDS.Save(outFilePath + ".dds");

                }
                else if (pkFile.texHeader.dxtType == DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM)
                {
                    myDDS = new DDSMOD(dataChunk, pkFile.width, pkFile.height, 32, 0, DDSMOD.DDS_Format.UNCOMPRESSED_GENERAL);
                    myDDS.Save(outFilePath + ".dds");

                }
                else if (pkFile.texHeader.dxtType == DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM)
                {
                    myDDS = new DDSMOD(dataChunk, pkFile.width, pkFile.height);
                    //save
                    myDDS.SaveCrude(outFilePath + ".dds");

                }
                else
                {
                    //    throw new Exception("unknown dxgi format");
                    myDDS = new DDSMOD(dataChunk, pkFile.width, pkFile.height);
                    //save
                    myDDS.SaveCrude(outFilePath + ".dds.unk");
                    Program.VText("WARNING THIS TEXTURE IS UNKNOWN");
                }






                Program.VText("-->Saved: " + Path.GetFileName(outFilePath));
                Program.VText("");

                //Supporting txt file
#if true
                StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(outFilePath),Path.GetFileNameWithoutExtension(outFilePath) + ".info"), false, UTF8Encoding.ASCII);
                sw.WriteLine("TextureInfo");
                sw.WriteLine("{");
                sw.WriteLine("  TexPath: " + pkFile.fpath);
                sw.WriteLine("  Width: " + pkFile.width);
                sw.WriteLine("  Height: " + pkFile.height);
                sw.WriteLine("  ChunkSizeA: " + pkFile.sizeA);
                sw.WriteLine("  ChunkSizeB: " + pkFile.sizeB);
                sw.WriteLine("  ");
                sw.WriteLine("      TexHeader");
                sw.WriteLine("      {");
                sw.WriteLine("          magic4:" + Cra0Utilz.GetString_ASCII(pkFile.texHeader.magic));
                sw.WriteLine("          DXGI_FORMAT:" + (pkFile.texHeader.dxtType.ToString()));
                sw.WriteLine("          unk2:" + (pkFile.texHeader.unk2));
                sw.WriteLine("          texchunkSize:" + (pkFile.texHeader.texchunkSize));
                sw.WriteLine("          varSlotSub:" + (pkFile.texHeader.varSlotSub));
                sw.WriteLine("          varSlot:" + (pkFile.texHeader.varSlot));
                sw.WriteLine("          varSlot2:" + (pkFile.texHeader.varSlot2));
                sw.WriteLine("          width:" + (pkFile.texHeader.width));
                sw.WriteLine("          height:" + (pkFile.texHeader.height));
                sw.WriteLine("          unk6_a:" + (pkFile.texHeader.unk6_a));
                sw.WriteLine("          unk7:" + (pkFile.texHeader.unk7));
                sw.WriteLine("          unk8:" + (pkFile.texHeader.unk8));
                sw.WriteLine("          unk9:" + (pkFile.texHeader.unk9));
                sw.WriteLine("          unk10:" + (pkFile.texHeader.unk10));
                sw.WriteLine("          unk11:" + (pkFile.texHeader.unk11));
                sw.WriteLine("      }");
                sw.WriteLine("        ");
                sw.WriteLine("}");

                sw.Close();
#endif
            }


            //close reader and underlying stream
            br.Close();
            Program.VText("");
            Program.VText("Done!");

        }





    }
}
