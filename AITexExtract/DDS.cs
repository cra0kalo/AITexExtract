using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AITexExtract
{

    /// <summary>
    /// DDS Class Partial Class for reading/writting image files from any source. Right now it's for Ratchet and Clank
    /// Will support just the basics nothing fancy here
    ///
    /// DOTO:
    /// Add support for DX11 textures BC5 etc
    /// </summary>
    public class DDSMOD
    {


        private byte[] DDS_CHUNK;


        private bool HasMipMaps;
        private bool UsesPitchorLinearSize;
        private int Pixelres;
        private DDS_Format File_DDSFORMAT;

        private enum ByteOrder : int
        {
            LittleEndian,
            BigEndian
        }




        /////////////////// DDS ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private const byte HeaderSizeInBytes = 128; // all non-image data together is 128 Bytes


        //Global Declare             DDS HEADER STRUCTURE
        private uint DDS_MAGIC = 0x44445320; // "DDS "
        private uint dwSize = 0x7C000000; // 124 (little endian)
        private uint dwFlags; // (comes in 2 parts) first short is 4013 2nd short is 8
        private uint dwHeight; // 720
        private uint dwWidth; // 1280
        private uint dwPitchOrLinearSize; // For compressed formats, this is the total number of bytes for the main image.
        private uint dwDepth = 0x0; // For volume textures, this is the depth of the volume.
        private uint dwMipMapCount; // total number of levels in the mipmap chain of the main image.

        //shit we hardly need
        private UInt32[] dwReserved1 = new UInt32[11]; // 11 UInt32s 11- 1 = 10 because the 0th element is also counted

        // Pixelformat sub-struct, 32 bytes
        private UInt32 pfSize = 32; // Size of Pixelformat structure. This member must be set to 32.
        private UInt32 pfFlags; // Flags to indicate valid fields.
        private UInt32 pfFourCC; // This is the four-character code for compressed formats.

        private UInt32 pfRGBBitCount; // For RGB formats, this is the total number of bits in the format. dwFlags should include DDpf_RGB in this case. This value is usually 16, 24, or 32. For A8R8G8B8, this value would be 32.
        private UInt32 pfRBitMask; // For RGB formats, these three fields contain the masks for the red, green, and blue channels. For A8R8G8B8, these values would be 0x00ff0000, 0x0000ff00, and 0x000000ff respectively.
        private UInt32 pfGBitMask; // ..
        private UInt32 pfBBitMask; // ..
        private UInt32 pfABitMask; // For RGB formats, this contains the mask for the alpha channel, if any. dwFlags should include DDpf_ALPHAPIXELS in this case. For A8R8G8B8, this value would be 0xff000000.

        // Capabilities sub-struct, 16 bytes
        private UInt32 dwCaps1; // always includes DDSCAPS_TEXTURE. with more than one main surface DDSCAPS_COMPLEX should also be set.
        private UInt32 dwCaps2; // For cubic environment maps, DDSCAPS2_CUBEMAP should be included as well as one or more faces of the map
        private UInt32 dwCaps3;
        private UInt32 dwCaps4;
        private UInt32 dwReserved2; // reserverd

        //   --------------------------------------- -------------That now makes up 128 bytes which is our header---------- --------------------------------------------------------------

        //             // Surface Description
        private enum DDSFLAG_Surface : uint
        {

            DDSD_CAPS = 0x1, //Required in every .dds file.
            DDSD_HEIGHT = 0x2, //Required in every .dds file.
            DDSD_WIDTH = 0x4, //Required in every .dds file.
            DDSD_PITCH = 0x8, //Required when pitch is provided for an uncompressed texture.
            DDSD_PIXELFORMAT = 0x1000, //Required in every .dds file.
            DDSD_MIPMAPCOUNT = 0x20000, //Required in a mipmapped texture.
            DDSD_LINEARSIZE = 0x80000, //Required when pitch is provided for a compressed texture.
            DDSD_DEPTH = 0x800000 //Required in a depth texture.
        }

        //             // Pixelformat
        private enum DDSFLAG_PixelFormat : uint
        {

            NONE = 0x0, // not part of DX, added for convenience
            ALPHAPIXELS = 0x1,
            FOURCC = 0x4,
            RGB = 0x40,
            RGBA = 0x41

        }

        //             // DDS Format

        //// texture types v2.0
        public enum DDS_Format : uint
        {
            UNCOMPRESSED_GENERAL,
            Unknown,
            Dxt1,
            Dxt3,
            Dxt5,
            R8G8B8,
            B8G8R8,
            Bgra5551,
            Bgra4444,
            Bgr565,
            Alpha8,
            X8R8G8B8,
            A8R8G8B8,
            A8B8G8R8,
            X8B8G8R8,
            RGB555,
            R32F,
            R16F,
            A32B32G32R32F,
            A16B16G16R16F,
            Q8W8V8U8,
            CxV8U8,
            G16R16F,
            G32R32F,
            G16R16,
            A2B10G10R10,
            A16B16G16R16,
            ATI2N,
            BC7_UNORM
        }

        ////type
        private enum DDS_Type : int
        {
            Compressed,
            Uncompressed
        }


        ////FourCC
        private enum FourCC : uint
        {
            D3DFMT_DXT1 = 0x31545844,
            D3DFMT_DXT2 = 0x32545844,
            D3DFMT_DXT3 = 0x33545844,
            D3DFMT_DXT4 = 0x34545844,
            D3DFMT_DXT5 = 0x35545844,
            D3DFMT_ATI2N = 0x32495441,
            DX10 = 0x30315844,
            DXGI_FORMAT_BC4_UNORM = 0x55344342,
            DXGI_FORMAT_BC4_SNORM = 0x53344342,
            DXGI_FORMAT_BC5_UNORM = 0x32495441,
            DXGI_FORMAT_BC5_SNORM = 0x53354342,

            //DXGI_FORMAT_R8G8_B8G8_UNORM
            D3DFMT_R8G8_B8G8 = 0x47424752,

            //DXGI_FORMAT_G8R8_G8B8_UNORM
            D3DFMT_G8R8_G8B8 = 0x42475247,

            //DXGI_FORMAT_R16G16B16A16_UNORM
            D3DFMT_A16B16G16R16 = 36,

            //DXGI_FORMAT_R16G16B16A16_SNORM
            D3DFMT_Q16W16V16U16 = 110,

            //DXGI_FORMAT_R16_FLOAT
            D3DFMT_R16F = 111,

            //DXGI_FORMAT_R16G16_FLOAT
            D3DFMT_G16R16F = 112,

            //DXGI_FORMAT_R16G16B16A16_FLOAT
            D3DFMT_A16B16G16R16F = 113,

            //DXGI_FORMAT_R32_FLOAT
            D3DFMT_R32F = 114,

            //DXGI_FORMAT_R32G32_FLOAT
            D3DFMT_G32R32F = 115,

            //DXGI_FORMAT_R32G32B32A32_FLOAT
            D3DFMT_A32B32G32R32F = 116,

            D3DFMT_UYVY = 0x59565955,
            D3DFMT_YUY2 = 0x32595559,
            D3DFMT_CxV8U8 = 117,

            //This is set only by the nvidia exporter, it is not set by the dx texture tool
            //,it is ignored by the dx texture tool but it returns the ability to be opened in photoshop so I decided to keep it.
            D3DFMT_Q8W8V8U8 = 63



        }

        private enum eDDSCAPS : uint
        {
            NONE = 0x0, // not part of DX, added for convenience
            COMPLEX = 0x8, // should be set for any DDS file with more than one main surface
            TEXTURE = 0x1000, // should always be set
            MIPMAP = 0x400000 // only for files with MipMaps
        }



        /// <summary>
        /// Constructor for the Class
        /// </summary>
        public DDSMOD(byte[] DATACHUNK, int image_width, int image_height, int resolution, int mipmaps_count, DDS_Format format)
        {

            //What will we pass to this class in order to spit out a shitty dds image
            //- DataChunk as byte()
            //- Width
            //- Height
            //- resolution (16, 24, 32)
            //- MipMapsCount as integer
            //- format (RGB) (DX1) (ARGB)

            //take what we got from the user and put it to use

            DDS_CHUNK = DATACHUNK;

            dwHeight = Convert.ToUInt32(image_height);
            dwWidth = Convert.ToUInt32(image_width);
            Pixelres = resolution;
            MipmapsHandle(mipmaps_count);
            File_DDSFORMAT = format;


            PrepareDDSHeader(); ////prepare header before we write


            //Thats it now wait for the write method to be executed


        }



        public DDSMOD(byte[] DATACHUNK, int image_width, int image_height)
        {

            //CRUDE AS FUCK

            //take what we got from the user and put it to use
            DDS_CHUNK = DATACHUNK;
            dwHeight = Convert.ToUInt32(image_height);
            dwWidth = Convert.ToUInt32(image_width);


            //Thats it now wait for the write method to be executed




        }


        //Method snapshots

        private void PrepareDDSHeader()
        {
            ////takes info passed from and prepares the header using some logic operations

            //PitchOrLinearSize
            Prep_PitchOrLinearSize(true); ////always use the pitch


            //FlagsA
            Prep_dwFlagsA(File_DDSFORMAT, UsesPitchorLinearSize, false);

            //FlagsB
            Prep_dwFlagsB(File_DDSFORMAT, Pixelres);

            //FourCC if required
            Prep_FourCC(File_DDSFORMAT);

            //RGBBitCount for uncompreesed formats
            Prep_RGBBitCount(File_DDSFORMAT, Pixelres);

            //Set mask for uncompressed textures
            Prep_pfRGBA_MASK(File_DDSFORMAT);

            //DwCaps1 (lazy work here)
            Prep_dwCaps1();

            //Finalize
            Prep_Finalize();


        }


        private void Prep_PitchOrLinearSize(bool usingthis)
        {

            if (usingthis == true)
            {
                UsesPitchorLinearSize = true;
                dwPitchOrLinearSize = Convert.ToUInt32(computePitch(Convert.ToInt32(dwWidth), Convert.ToInt32(dwHeight), Pixelres, (int)File_DDSFORMAT));
            }
            else
            {
                UsesPitchorLinearSize = false;
                dwPitchOrLinearSize = 0x0;

            }

        }


        public static int computeRAWImageSize(int width, int height, int resolution)
        {
            int pitch = ((width + 3) / 4) * ((height + 3) / 4) * ((resolution + 3) / 4);
            pitch *= 2;
            return pitch;
        }


        public static int computePitch(int width, int height, int resolution, int compressionFormat)
        {
            int pitch = ((width + 3) / 4) * ((height + 3) / 4) * ((resolution + 3) / 4);
            switch (compressionFormat)
            {
            case (int)DDS_Format.Dxt1:
                pitch *= 1; // 1 for rgb
                break;
            case (int)DDS_Format.Dxt3:
                //pitch *= 16 'True 16 block size (DOESNT TAKE INTO ACCOUNT WE ARE X BY DEPTH)
                pitch *= 2; //1 for alpha 1 for rgb
                break;
            case (int)DDS_Format.Dxt5:
                pitch *= 2; //True 16 block size ( 1 ALPHA 1 RGB)
                break;
            case (int)DDS_Format.ATI2N:
                pitch *= 2; //True 16 block size ( 1 ALPHA 1 RGB)
                break;
            case (int)DDS_Format.BC7_UNORM:
                pitch *= 2; //True 16 block size ( 1 ALPHA 1 RGB)
                break;
            default:
                pitch *= 8; //shortcut for uncompressed
                break;
            }
            return pitch;
        }

        public static int computeRAWImageSizeMIPMAP(int width, int height,int resolution)
        {
            int mipsize = 0;

            width = width / 2;
            height = height / 2;


            int curWidth = 0;
            int curHeight = 0;


            while (width != 0 || height != 0)
            {
                curWidth = width;
                curHeight = height;
                if (curWidth == 0)
                {
                    curWidth = 1;
                }
                else if (curHeight == 0)
                {
                    curHeight = 1;
                }

                //mipsize += ((width + 3) \ 4) * ((height + 3) \ 4) * ((resolution + 3) \ 4) * 2
                mipsize += (int)((((width + 3) / 4) * ((height + 3) / 4) * ((resolution + 3) / 4) * 8) / 4); //// 4 for DXT5 and DXT3



                width /= 2;
                height /= 2;
            }


            return mipsize;
        }


        private int computeMipMapSize(int width, int height, int resolution, int compressionFormat)
        {


            int mipsize = 0;

            width = width / 2;
            height = height / 2;


            int curWidth = 0;
            int curHeight = 0;

            switch (compressionFormat)
            {
            case (int)DDS_Format.Dxt1:

                ////override resolution to 24 for dxt1
                resolution = 24;


                while (width != 0 || height != 0)
                {
                    curWidth = width;
                    curHeight = height;
                    if (curWidth == 0)
                    {
                        curWidth = 1;
                    }
                    else if (curHeight == 0)
                    {
                        curHeight = 1;
                    }

                    ////mipsize += (((width + 3) \ 4) * ((height + 3) \ 4) * ((resolution + 3) \ 4) * 1)  '// 1 for DX1 (THIS IS BROKEN IF WE HAVE DX1 WITH ALPHA WE ARE SCREWED)
                    mipsize += (int)((((width + 3) / 4) * ((height + 3) / 4) * ((resolution + 3) / 4) * 8) / 6);
                    //dxt1 with alpha => resolution 32, ratio 6 (gets defaulted to 24 because its dxt1 anyway)
                    //dxt1 without alpha => resolution 24, ratio 6
                    //dxt3 or dxt5 => resolution 32, ratio 4






                    width /= 2;
                    height /= 2;
                }


                break;
            case (int)DDS_Format.Dxt3:

                //nothing here yet
                break;
            case (int)DDS_Format.Dxt5:

                while (width != 0 || height != 0)
                {
                    curWidth = width;
                    curHeight = height;
                    if (curWidth == 0)
                    {
                        curWidth = 1;
                    }
                    else if (curHeight == 0)
                    {
                        curHeight = 1;
                    }

                    //mipsize += ((width + 3) \ 4) * ((height + 3) \ 4) * ((resolution + 3) \ 4) * 2
                    mipsize += (int)((((width + 3) / 4) * ((height + 3) / 4) * ((resolution + 3) / 4) * 8) / 4); //// 4 for DXT5 and DXT3



                    width /= 2;
                    height /= 2;
                }







                break;
            default:
                mipsize = -1;
                break;
            }




            return mipsize;
        }


        private void SetTextureFormat(DDS_Format texformat)
        {

            switch (texformat)
            {

            case DDS_Format.A8R8G8B8:

                //bitmask as follows
                pfRBitMask = 0xFF0000;
                pfGBitMask = 0xFF00;
                pfBBitMask = 0xFF;
                pfABitMask = unchecked(0xFF000000U); //// wOw just wow VB.NET ur fucken retarded have to append UI to the end of 0xff000000 so it works WHAT THE ACTUAL FUCK

                break;
            case DDS_Format.Dxt1:
                //bitmask as follows
                pfRBitMask = 0x0;
                pfGBitMask = 0x0;
                pfBBitMask = 0x0;
                pfABitMask = 0x0;
                break;
            case DDS_Format.Dxt3:
                //bitmask as follows
                pfRBitMask = 0x0;
                pfGBitMask = 0x0;
                pfBBitMask = 0x0;
                pfABitMask = 0x0;
                break;
            case DDS_Format.Dxt5:
                //bitmask as follows
                pfRBitMask = 0x0;
                pfGBitMask = 0x0;
                pfBBitMask = 0x0;
                pfABitMask = 0x0;
                break;
            case DDS_Format.ATI2N:
                //bitmask as follows
                pfRBitMask = 0x0;
                pfGBitMask = 0x0;
                pfBBitMask = 0x0;
                pfABitMask = 0x0;

                break;
            }


        }


        private void MipmapsHandle(int input)
        {


            if (input <= 0)
            {
                //Nomipmaps
                dwMipMapCount = 0x0;
                HasMipMaps = false;

            }
            else
            {
                //Mipmaps used
                dwMipMapCount = Convert.ToUInt32(input);
                HasMipMaps = true;

            }




        }


        //--------FLAG A----------
        private void Prep_dwFlagsA(DDS_Format format, bool pitch_or_linear_provided, bool uses_depth)
        {

            //temp variables
            bool DDSD_CAPS = true; ////required to be true
            bool DDSD_HEIGHT = true; ////required to be true
            bool DDSD_WIDTH = true; ////required to be true
            bool DDSD_PITCH = false;
            bool DDSD_PIXELFORMAT = true; ////required to be true
            bool DDSD_MIPMAPCOUNT = false;
            bool DDSD_LINEARSIZE = false;
            bool DDSD_DEPTH = false;


            //test_format
            if (format == DDS_Format.Dxt1 || format == DDS_Format.Dxt3 || format == DDS_Format.Dxt5 || format == DDS_Format.ATI2N)
            {

                //compressed

                if (pitch_or_linear_provided == true)
                {

                    //its a compressed texture with the linearsize provided
                    DDSD_LINEARSIZE = true;

                }

            }
            else
            {
                //uncompressed
                if (pitch_or_linear_provided == true)
                {

                    //its a uncompreesed texture with the pitch provided
                    // DDSD_PITCH = True '/ disabling this because for some fucken reason if you export a texture from photoshop that has the pitch provided in the file this flag is not set for some reason
                    DDSD_LINEARSIZE = true; //// leaving this here because it works

                }

            }


            //mipmaps
            if (HasMipMaps == true)
            {

                //texture uses mipmaps
                DDSD_MIPMAPCOUNT = true;

            }

            //depth (unused)



            //feed to our calculator get our integer and feed back to the global variable
            dwFlags = Calc_dwFlagsA(DDSD_CAPS, DDSD_HEIGHT, DDSD_WIDTH, DDSD_PITCH, DDSD_PIXELFORMAT, DDSD_MIPMAPCOUNT, DDSD_LINEARSIZE, DDSD_DEPTH);



        }

        private uint Calc_dwFlagsA(bool DDS_CAPS, bool DDS_HEIGHT, bool DDS_WIDTH, bool DDS_PITCH, bool DDS_PIXELFORMAT, bool DDS_MIPMAPCOUNT, bool LINEARSIZE, bool DDS_DEPTH)
        {

            //Calculates the 4BYTE WORD FLAG from what we are given

            uint total_owning = 0;

            //Test each flaf

            if (DDS_CAPS == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_CAPS;
            }

            if (DDS_HEIGHT == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_HEIGHT;
            }

            if (DDS_WIDTH == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_WIDTH;
            }

            if (DDS_PITCH == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_PITCH;
            }

            if (DDS_PIXELFORMAT == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_PIXELFORMAT;
            }

            if (DDS_MIPMAPCOUNT == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_MIPMAPCOUNT;
            }

            if (LINEARSIZE == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_LINEARSIZE;
            }

            if (DDS_DEPTH == true)
            {
                total_owning += (uint)DDSFLAG_Surface.DDSD_DEPTH;
            }

            return total_owning;

        }

        //------------------------


        //--------FLAG B----------
        private void Prep_dwFlagsB(DDS_Format format, int res)
        {

            bool DDPF_FOURCC = false;
            bool DDPF_RGB = false;
            bool DDPF_ALPHAPIXELS = false;



            if (format == DDS_Format.Dxt1 || format == DDS_Format.Dxt3 || format == DDS_Format.Dxt5 || format == DDS_Format.ATI2N)
            {
                //compressed

                //set FourCC flag
                DDPF_FOURCC = true;

                //no need to set alpha channel its always going to only have thoe fourcc flag


            }
            else
            {
                //uncompressed

                //SET uncompressed flag
                DDPF_RGB = true;



                if (res == 24)
                {
                    //no alpha channel
                    DDPF_ALPHAPIXELS = false;

                }
                else if (res == 32)
                {
                    //has alpha channel
                    DDPF_ALPHAPIXELS = true;

                }

            }

            //- FourCC
            //- RGB
            //- Alpha or not


            pfFlags = Calc_dwFlagsB(DDPF_FOURCC, DDPF_RGB, DDPF_ALPHAPIXELS);

        }

        private uint Calc_dwFlagsB(bool FOURCC, bool RGB, bool ALPHAPIXELS)
        {

            //Calculates the 4BYTE WORD FLAG from what we are given (2nd flag)

            uint total_owning = 0;

            //Test each flag
            if (FOURCC == true)
            {
                total_owning += (uint)DDSFLAG_PixelFormat.FOURCC;
            }

            if (RGB == true)
            {
                total_owning += (uint)DDSFLAG_PixelFormat.RGB;
            }

            if (ALPHAPIXELS == true)
            {
                total_owning += (uint)DDSFLAG_PixelFormat.ALPHAPIXELS;
            }


            return total_owning;

        }

        //------------------------



        //-------FourCC-----------
        private void Prep_FourCC(DDS_Format ddsformat)
        {

            switch (ddsformat)
            {

            case DDS_Format.Dxt1:
                pfFourCC = (UInt32)FourCC.D3DFMT_DXT1;
                break;
            case DDS_Format.Dxt3:
                pfFourCC = (UInt32)FourCC.D3DFMT_DXT3;
                break;
            case DDS_Format.Dxt5:
                pfFourCC = (UInt32)FourCC.D3DFMT_DXT5;
                break;
            case DDS_Format.ATI2N:
                pfFourCC = (UInt32)FourCC.D3DFMT_ATI2N;
                break;
            default:
                pfFourCC = 0x0;

                break;
            }


        }

        //------------------------



        //-------pfRGBBitCount-----------
        private void Prep_RGBBitCount(DDS_Format ddsformat, int res)
        {

            if (ddsformat == DDS_Format.Dxt1 || ddsformat == DDS_Format.Dxt3 || ddsformat == DDS_Format.Dxt5 || ddsformat == DDS_Format.ATI2N)
            {
                //compressed

                //ignore write 00
                pfRGBBitCount = 0x0;


            }
            else
            {
                //uncompressed

                //Must set for the resolution (24 for no alpha, 32 for alpha)
                pfRGBBitCount = Convert.ToUInt32(res);

            }


        }

        //-------------------------------

        //-------pf(RGBA)BitMask-----------
        private void Prep_pfRGBA_MASK(DDS_Format ddsformat)
        {

            SetTextureFormat(ddsformat);


        }

        //-------------------------------


        //-------dwCaps1-----------
        private void Prep_dwCaps1()
        {

            //DO TO: Extend to support other stuff

            dwCaps1 = (UInt32)eDDSCAPS.TEXTURE;


        }

        //-------------------------


        //-------Finalize Stuff-----------
        private void Prep_Finalize()
        {

            dwCaps2 = (UInt32)eDDSCAPS.NONE;
            dwCaps3 = (UInt32)eDDSCAPS.NONE;
            dwCaps4 = (UInt32)eDDSCAPS.NONE;

            //Reversed
            dwReserved2 = 0x0;

        }

        //--------------------------------



        //---------------------------------------------------------------------BIG ENDIAN WRITTER--------------------------------------------------------------------------------------------------------

        private void NBinWrite(BinaryWriter writer, ByteOrder byteOrder, uint value2write)
        {

            byte[] buffer_cach = null;
            byte[] buffer_out = null;
            //takes our input turn 2 bytes then reverse byte array and spit out

            if (byteOrder == ByteOrder.BigEndian)
            {

                Cra0Utilz.IntegerToByte((int)value2write, ref buffer_cach);

                buffer_out = Cra0Utilz.ConvertEndian(buffer_cach);

                writer.Write(buffer_out);

            }
            else if (byteOrder == ByteOrder.LittleEndian )
            {
                writer.Write(value2write);
            }


        }


        //---------------------------------------------------------------------BIG ENDIAN WRITTER--------------------------------------------------------------------------------------------------------

        public void Save(string path_File2write)
        {
            //Save out the DDS file

            FileStream outputStream = new FileStream(path_File2write, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(outputStream);


            //Write Header first
            WriteHeader(outputStream, bw);

            //Write chunk
            bw.Write(DDS_CHUNK);

            //dust hands


            //Once we have written to the file close the stream and writter
            outputStream.Close();
            bw.Close();



        }


        public void SaveCrude(string path_File2write)
        {
            //Save out the DDS file

            FileStream outputStream = new FileStream(path_File2write, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(outputStream);


            //FUCK IT MAKE UP A WACKY HEADER THAT PROB WONT EVEN WORK
            bw.Write(542327876);
            bw.Write(124); // 124 (little endian)
            bw.Write(659463);
            bw.Write(dwHeight);
            bw.Write(dwWidth);
            bw.Write(computePitch((int)this.dwWidth,(int)this.dwHeight,32,3));
            bw.Write(1);
            bw.Write(8); //mipcount


            foreach (UInt32 value in dwReserved1)
            {
                bw.Write(value);
            }

            bw.Write(32);
            bw.Write(4);
            bw.Write(808540228);


            bw.Write(pfRGBBitCount);


            //volfin
            pfRBitMask = 0xFF0000;
            pfGBitMask = 0xFF00;
            pfBBitMask = 0xFF;
            pfABitMask = 0xFF000000;

            bw.Write(pfRBitMask);
            bw.Write(pfGBitMask);
            bw.Write(pfBBitMask);
            bw.Write(pfABitMask);


            bw.Write(4198408);
            bw.Write(dwCaps2);
            bw.Write(dwCaps3);
            bw.Write(dwCaps4);


            bw.Write(dwReserved2);


            bw.Write(98);
            bw.Write(3);
            bw.Write(0);
            bw.Write(1);
            bw.Write(0);


            //Write chunk
            bw.Write(DDS_CHUNK);

            //dust hands


            //Once we have written to the file close the stream and writter
            outputStream.Close();
            bw.Close();



        }




        private void WriteHeader(FileStream stream, BinaryWriter bSpit)
        {
            //Header has been prepared HOPEFULLY by the prepare method now we just write the values to the stream

            NBinWrite(bSpit, ByteOrder.BigEndian, DDS_MAGIC); // "DDS "
            NBinWrite(bSpit, ByteOrder.BigEndian, dwSize); // 124 (little endian)
            bSpit.Write(dwFlags);
            bSpit.Write(dwHeight);
            bSpit.Write(dwWidth);
            bSpit.Write(dwPitchOrLinearSize);
            bSpit.Write(dwDepth);
            bSpit.Write(dwMipMapCount);


            foreach (UInt32 value in dwReserved1)
            {
                bSpit.Write(value);
            }

            bSpit.Write(pfSize);
            bSpit.Write(pfFlags);
            bSpit.Write(pfFourCC);


            bSpit.Write(pfRGBBitCount);
            bSpit.Write(pfRBitMask);
            bSpit.Write(pfGBitMask);
            bSpit.Write(pfBBitMask);
            bSpit.Write(pfABitMask);


            bSpit.Write(dwCaps1);
            bSpit.Write(dwCaps2);
            bSpit.Write(dwCaps3);
            bSpit.Write(dwCaps4);


            bSpit.Write(dwReserved2);


            //done and dusted

        }




        public void Read(object path2read)
        {
            //Reads the headerfile of a DDS and print to console



        }


        public struct Color8888
        {
            private byte r; // change the order of names to change the
            private byte g; // order of the output ARGB or BGRA, etc...
            private byte b; // Last one is MSB, 1st is LSB.
            private byte a;
        }





        //Matej Tomčík's code

        public static void decodeBlock(byte[] encoded, byte[][] decoded)
        {
            //JAVA TO VB CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
            //ORIGINAL LINE: Dim colors[][] As Integer = new Integer[4][4]
            int[][] colors = RectangularArrays.ReturnRectangularIntegerArray(4, 4);
            int[] indices = new int[16];
            int[] alpha = new int[8];
            int[] alphaIndices = new int[16];
            decodeColors(encoded, colors, indices);
            alpha[0] = (Convert.ToInt32(encoded[0])) & 0xFF;
            int a1 = alpha[0];
            alpha[1] = (Convert.ToInt32(encoded[1])) & 0xFF;
            int a2 = alpha[1];
            if (a1 <= a2)
            {
                for (int i = 1; i <= 4; i++)
                {
                    alpha[1 + i] = ((5 - i) * a1 + i * a2) / 5;
                }
                alpha[6] = 0;
                alpha[7] = 0xFF;
            }
            else
            {
                for (int i = 1; i <= 6; i++)
                {
                    alpha[1 + i] = ((7 - i) * a1 + i * a2) / 7;
                }
            }
            for (int i = 0; i <= 1; i++)
            {
                int value = 0;
                for (int j = 0; j <= 2; j++)
                {
                    value = value | ((Convert.ToInt32(encoded[2 + i * 3 + j])) & 0xFF) << (8 * j);
                }
                for (int j = 0; j <= 7; j++)
                {
                    alphaIndices[i * 8 + j] = value & 0x7;
                    value = value >> 3;
                }
            }
            for (int i = 0; i <= 3; i++)
            {
                for (int j = 0; j <= 3; j++)
                {
                    int index = indices[i * 4 + j];
                    decoded[i][j * 4] = Convert.ToByte(colors[index][0]);
                    decoded[i][j * 4 + 1] = Convert.ToByte(colors[index][1]);
                    decoded[i][j * 4 + 2] = Convert.ToByte(colors[index][2]);
                    decoded[i][j * 4 + 3] = Convert.ToByte(alpha[alphaIndices[i * 4 + j]]);
                }
            }
        }

        //----------------------------------------------------------------------------------------
        //	Copyright © 2007 - 2013 Tangible Software Solutions Inc.
        //	This class can be used by anyone provided that the copyright notice remains intact.
        //
        //	This class provides the logic to simulate Java rectangular arrays, which are jagged
        //	arrays with inner arrays of the same length. A size of -1 indicates unknown length.
        //----------------------------------------------------------------------------------------
        internal partial class RectangularArrays
        {
            internal static int[][] ReturnRectangularIntegerArray(int Size1, int Size2)
            {
                int[][] Array = null;
                if (Size1 > -1)
                {
                    Array = new int[Size1][];
                    if (Size2 > -1)
                    {
                        for (int Array1 = 0; Array1 < Size1; Array1++)
                        {
                            Array[Array1] = new int[Size2];
                        }
                    }
                }
                else
                {
                    Array = null;
                }

                return Array;
            }
        }











        public static void decodeColors(byte[] encoded, int[][] colors, int[] indices)
        {
            unpackRgb565(encoded, 8, colors[0]);
            unpackRgb565(encoded, 10, colors[1]);

            for (int i = 0; i <= 2; i++)
            {
                int ca = colors[0][i];
                int cb = colors[1][i];

                colors[2][i] = (2 * ca + cb) / 3;
                colors[3][i] = (ca + 2 * cb) / 3;
            }

            for (int i = 0; i <= 3; i++)
            {
                sbyte packed = Convert.ToSByte(encoded[12 + i]);
                indices[i * 4] = packed & 0x3;
                indices[i * 4 + 1] = (packed >> 2) & 0x3;
                indices[i * 4 + 2] = (packed >> 4) & 0x3;
                indices[i * 4 + 3] = (packed >> 6) & 0x3;
            }
        }



        public static int unpackRgb565(byte[] packed, int offset, int[] unpacked)
        {
            int value = ((Convert.ToInt32(packed[offset])) & 0xFF) | (((Convert.ToInt32(packed[offset + 1])) & 0xFF) << 8);
            int r = (value >> 11) & 0x1F;
            int g = (value >> 5) & 0x3F;
            int b = value & 0x1F;
            unpacked[0] = (r << 3) | (r >> 2);
            unpacked[1] = (g << 2) | (g >> 4);
            unpacked[2] = (b << 3) | (b >> 2);
            unpacked[3] = 0xFF;
            return value;
        }





    }




    internal static class DXT5
    {




        private static void ComputeReflectivity(byte[] imageDataRGBA8888, uint uiWidth, uint uiHeight, ref float sX, ref float sY, ref float sZ)
        {

            sX = 0.0F;
            sY = sX;
            sZ = sY;

            float[] sTable = new float[256];

            // Precalculate all possible reflectivity values
            for (int i = 0; i <= 255; i++)
            {
                sTable[i] = Convert.ToSingle(Math.Pow(Convert.ToSingle(i) / 255.0F, 2.2F));
            }

            // Compute reflectivity on RGB channels.
            float sTempX = 0F;
            float sTempY = 0F;
            float sTempZ = 0F;
            float sInverse = 0F;

            for (uint j = 0; j <= uiHeight - 1U; j++)
            {
                sTempX = 0.0F;
                sTempY = sTempX;
                sTempZ = sTempY;

                for (uint i = 0; i <= uiWidth - 1U; i++)
                {
                    int uiIndex = Convert.ToInt32((i + j * uiWidth) * 4U);
                    sTempX += sTable[imageDataRGBA8888[uiIndex]];
                    sTempY += sTable[imageDataRGBA8888[uiIndex + 1]];
                    sTempZ += sTable[imageDataRGBA8888[uiIndex + 2]];
                }

                sInverse = 1.0F / uiWidth;

                sX += sTempX * sInverse;
                sY += sTempY * sInverse;
                sZ += sTempZ * sInverse;
            }

            sInverse = 1.0F / uiHeight;

            sX *= sInverse;
            sY *= sInverse;
            sZ *= sInverse;
        }



        public static byte[] DecompressDXT5Image(int width, int height, byte[] blocks, bool flipRGB = false)
        {
            const int bytesPerBlock = 16;

            // Intialise the image output (RGBA, 32-bit, 4 bytes per pixel)
            byte[] rgba = new byte[width * height * 4];
            int sourceIndex = 0;

            // Loop over blocks.
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    // Decompress the block.
                    byte[] targetRgba = new byte[4 * 16];
                    DecompressDXT5Block(targetRgba, blocks, sourceIndex);

                    // Write the decompressed pixels to the correct image locations
                    int sourcePixelOffset = 0;

                    for (int py = 0; py <= 3; py++)
                    {
                        for (int px = 0; px <= 3; px++)
                        {
                            // Get the target location
                            int sx = x + px;
                            int sy = y + py;

                            if (sx < width && sy < height)
                            {
                                var targetPixelOffset = 4 * (width * sy + sx);

                                // Copy the rgba value
                                if (flipRGB)
                                {
                                    // Flip RGB -> BGR
                                    for (int i = 2; i >= 0; i--)
                                    {
                                        rgba[targetPixelOffset] = targetRgba[sourcePixelOffset + i];
                                        targetPixelOffset += 1;
                                    }
                                    // Copy alpha
                                    rgba[targetPixelOffset] = targetRgba[sourcePixelOffset + 3];
                                    targetPixelOffset += 1;
                                    sourcePixelOffset += 4;
                                }
                                else
                                {
                                    for (int i = 0; i <= 3; i++)
                                    {
                                        rgba[targetPixelOffset] = targetRgba[sourcePixelOffset];
                                        targetPixelOffset += 1;
                                        sourcePixelOffset += 1;
                                    }
                                }
                            }
                            else
                            {
                                // Skip this pixel as its outside the image
                                sourcePixelOffset += 4;
                            }
                        }
                    }

                    // Advance
                    sourceIndex += bytesPerBlock;
                }
            }

            return rgba;
        }

        private static void DecompressDXT5Block(byte[] rgba, byte[] block, int blockIndex)
        {
            DecompressAlphaDXT5(rgba, block, blockIndex);
            DecompressColourDXT5(rgba, block, blockIndex + 8);
        }

        private static void DecompressAlphaDXT5(byte[] rgba, byte[] block, int blockIndex)
        {
            byte[] alphaBlock = new byte[8];
            Array.Copy(block, blockIndex, alphaBlock, 0, alphaBlock.Length);

            // Get the two alpha values.
            byte alpha0 = alphaBlock[0];
            byte alpha1 = alphaBlock[1];

            // Compare the values to build the codebook
            byte[] codes = new byte[8];
            EvaluatePalette(alpha0, alpha1, codes);

            // Decode the indices
            byte[] indices = new byte[16];
            UnpackIndices(alphaBlock, indices);

            // Write out the indexed codebook values
            for (int i = 0; i <= 15; i++)
            {
                rgba[i * 4 + 3] = codes[indices[i]];
            }
        }

        private static int Unpack565(byte[] packed, int packedOffset, byte[] colour, int colourOffset)
        {
            // Build the packed value
            int value = Convert.ToInt32(packed[packedOffset]) | (Convert.ToInt32(packed[packedOffset + 1]) << 8);

            // Get the components in the stored range
            byte red = Convert.ToByte((value >> 11) & 0x1F);
            byte green = Convert.ToByte((value >> 5) & 0x3F);
            byte blue = Convert.ToByte((value & 0x1F));

            // Scale up to 8 bits
            colour[colourOffset] = Convert.ToByte((red << 3) | (red >> 2));
            colour[colourOffset + 1] = Convert.ToByte((green << 2) | (green >> 4));
            colour[colourOffset + 2] = Convert.ToByte((blue << 3) | (blue >> 2));
            colour[colourOffset + 3] = 255;

            return value;
        }

        private static void DecompressColourDXT5(byte[] rgba, byte[] block, int blockIndex)
        {
            // Unpack the endpoints
            byte[] codes = new byte[16];
            Unpack565(block, blockIndex, codes, 0);
            Unpack565(block, blockIndex + 2, codes, 4);

            // Generate the midpoints
            for (int i = 0; i <= 2; i++)
            {
                int c = codes[i];
                int d = codes[i + 4];

                codes[i + 8] = Convert.ToByte((c * 2 + d) / 3);
                codes[i + 12] = Convert.ToByte((c + d * 2) / 3);
            }

            // Fill in alpha for intermediate values
            codes[8 + 3] = 255;
            codes[12 + 3] = 255;

            // Unpack the indices
            byte[] indices = new byte[16];
            for (int i = 0; i <= 3; i++)
            {
                byte packed = block[blockIndex + i + 4];
                int index = i * 4;
                indices[index] = Convert.ToByte(packed & 0x3);
                indices[index + 1] = Convert.ToByte((packed >> 2) & 0x3);
                indices[index + 2] = Convert.ToByte((packed >> 4) & 0x3);
                indices[index + 3] = Convert.ToByte((packed >> 6) & 0x3);
            }

            // Store out the colours.
            for (int i = 0; i <= 15; i++)
            {
                int offset = 4 * indices[i];
                for (int j = 0; j <= 3; j++)
                {
                    rgba[i * 4 + j] = codes[offset + j];
                }
            }
        }

        private static void EvaluatePalette8(int alpha0, int alpha1, byte[] alpha)
        {
            // 8-alpha block:  derive the other six alphas.
            // Bit code 000 = alpha0, 001 = alpha1, others are interpolated.
            alpha[0] = Convert.ToByte(alpha0);
            alpha[1] = Convert.ToByte(alpha1);
            alpha[2] = Convert.ToByte((6 * alpha0 + 1 * alpha1) / 7); // bit code 010
            alpha[3] = Convert.ToByte((5 * alpha0 + 2 * alpha1) / 7); // bit code 011
            alpha[4] = Convert.ToByte((4 * alpha0 + 3 * alpha1) / 7); // bit code 100
            alpha[5] = Convert.ToByte((3 * alpha0 + 4 * alpha1) / 7); // bit code 101
            alpha[6] = Convert.ToByte((2 * alpha0 + 5 * alpha1) / 7); // bit code 110
            alpha[7] = Convert.ToByte((1 * alpha0 + 6 * alpha1) / 7); // bit code 111
        }

        private static void EvaluatePalette6(int alpha0, int alpha1, byte[] alpha)
        {
            // 6-alpha block.
            // Bit code 000 = alpha0, 001 = alpha1, others are interpolated.
            alpha[0] = Convert.ToByte(alpha0);
            alpha[1] = Convert.ToByte(alpha1);
            alpha[2] = Convert.ToByte((4 * alpha0 + 1 * alpha1) / 5); // Bit code 010
            alpha[3] = Convert.ToByte((3 * alpha0 + 2 * alpha1) / 5); // Bit code 011
            alpha[4] = Convert.ToByte((2 * alpha0 + 3 * alpha1) / 5); // Bit code 100
            alpha[5] = Convert.ToByte((1 * alpha0 + 4 * alpha1) / 5); // Bit code 101
            alpha[6] = 0x0; // Bit code 110
            alpha[7] = 0xFF; // Bit code 111
        }

        private static void EvaluatePalette(int alpha0, int alpha1, byte[] alpha)
        {
            if (alpha0 > alpha1)
            {
                EvaluatePalette8(alpha0, alpha1, alpha);
            }
            else
            {
                EvaluatePalette6(alpha0, alpha1, alpha);
            }
        }

        private static void UnpackIndices(byte[] block, byte[] indices)
        {
            int sourceIndex = 2;
            int destIndex = 0;

            for (int i = 0; i <= 1; i++)
            {
                // Grab 3 bytes
                int value = 0;
                for (int j = 0; j <= 2; j++)
                {
                    value = value | (Convert.ToInt32(block[sourceIndex]) << 8 * j);
                    sourceIndex += 1;
                }

                // Unpack 8 3-bit values from it
                for (int j = 0; j <= 7; j++)
                {
                    int index = (value >> 3 * j) & 0x7;
                    indices[destIndex] = Convert.ToByte(index);
                    destIndex += 1;
                }
            }
        }

        public static byte[] DecompressATI2Image(int width, int height, byte[] blocks, bool flipRGB = false)
        {
            const int bytesPerBlock = 16;

            // Intialise the image output (RGBA, 32-bit, 4 bytes per pixel)
            byte[] rgba = new byte[width * height * 4];
            int sourceIndex = 0;

            // Loop over blocks.
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    // Decompress the block.
                    byte[] targetRgba = new byte[4 * 16];
                    DecompressATI2Block(targetRgba, blocks, sourceIndex);

                    // Write the decompressed pixels to the correct image locations
                    int sourcePixelOffset = 0;

                    for (int py = 0; py <= 3; py++)
                    {
                        for (int px = 0; px <= 3; px++)
                        {
                            // Get the target location
                            int sx = x + px;
                            int sy = y + py;

                            if (sx < width && sy < height)
                            {
                                var targetPixelOffset = 4 * (width * sy + sx);

                                // Copy the rgba value
                                if (flipRGB)
                                {
                                    // Flip RGB -> BGR
                                    for (int i = 2; i >= 0; i--)
                                    {
                                        rgba[targetPixelOffset] = targetRgba[sourcePixelOffset + i];
                                        targetPixelOffset += 1;
                                    }
                                    // Copy alpha
                                    rgba[targetPixelOffset] = targetRgba[sourcePixelOffset + 3];
                                    targetPixelOffset += 1;
                                    sourcePixelOffset += 4;
                                }
                                else
                                {
                                    for (int i = 0; i <= 3; i++)
                                    {
                                        rgba[targetPixelOffset] = targetRgba[sourcePixelOffset];
                                        targetPixelOffset += 1;
                                        sourcePixelOffset += 1;
                                    }
                                }
                            }
                            else
                            {
                                // Skip this pixel as its outside the image
                                sourcePixelOffset += 4;
                            }
                        }
                    }

                    // Advance
                    sourceIndex += bytesPerBlock;
                }
            }

            return rgba;
        }

        private static void DecompressATI2Block(byte[] rgba, byte[] block, int sourceIndex)
        {
            DecompressATI2Alpha(rgba, block, sourceIndex);
            DecompressATI2Color(rgba);
        }

        private static void DecompressATI2Alpha(byte[] rgba, byte[] block, int sourceIndex)
        {
            // Initialise blocks.
            byte[] blockX = new byte[8];
            byte[] blockY = new byte[8];
            Array.Copy(block, sourceIndex, blockX, 0, blockX.Length);
            Array.Copy(block, sourceIndex + 8, blockY, 0, blockY.Length);

            // Initialise X and Y pallete.
            byte[] alphaArray = new byte[8];

            // Evaluate X palette.
            byte alpha0 = blockX[0];
            byte alpha1 = blockX[1];
            EvaluatePalette(alpha0, alpha1, alphaArray);

            // Unpack indices.
            byte[] indicies = new byte[16];
            UnpackIndices(blockX, indicies);

            // Write indexed R value.
            for (int i = 0; i <= 15; i++)
            {
                rgba[i * 4] = alphaArray[indicies[i]];
            }

            // Evaluate Y palette.
            alpha0 = blockY[0];
            alpha1 = blockY[1];
            EvaluatePalette(alpha0, alpha1, alphaArray);

            // Unpack indices.
            UnpackIndices(blockY, indicies);

            // Write indexed G value (and B and A value).
            for (int i = 0; i <= 15; i++)
            {
                rgba[i * 4 + 1] = alphaArray[indicies[i]];
                rgba[i * 4 + 2] = 0;
                rgba[i * 4 + 3] = 255;
            }
        }

        private static void DecompressATI2Color(byte[] rgba)
        {
            for (int i = 0; i <= 15; i++)
            {
                BuildNormal(rgba[i * 4], rgba[i * 4 + 1], rgba, i);
            }
        }

        private static void BuildNormal(byte x, byte y, byte[] rgba, int index)
        {
            int t = 127 * 128 - (x - 127) * (x - 128) - (y - 127) * (y - 128);
            if (t > 0)
            {
                rgba[index * 4 + 2] = Convert.ToByte(Convert.ToInt32(Math.Sqrt(t)) + 128);
            }
            else
            {
                rgba[index * 4 + 2] = 0x7F;
            }
        }
    }


    //-----------------------------------------------------------------------------------------


}
